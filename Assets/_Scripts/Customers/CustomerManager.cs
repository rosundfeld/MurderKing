using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

//Server-authoritative rush simulation (GDD sections 4-7).
//The server owns every customer and mirrors a compact snapshot into a NetworkList at a fixed rate;
//clients render those snapshots with CustomerVisual, so no customer prefab needs to be network-registered.
public class CustomerManager : NetworkBehaviour, IWeaponTarget
{
    public const int RAGE_MAX = 120;
    private const int RAGE_FAMINTO = 30;
    private const int RAGE_DESESPERADO = 55;
    private const int RAGE_FURIOSO = 80;
    private const int RAGE_COLAPSO = 100;

    //---------------SINGLETON NETCODE-----------------
    public static CustomerManager Instance { get; private set; }

    //---------------EVENTS-----------------
    public event EventHandler<OnCustomerReactionEventArgs> OnCustomerReaction;
    public class OnCustomerReactionEventArgs : EventArgs
    {
        public ushort customerId;
        public Reaction reaction;
        public Vector3 position;
    }

    //Fired on every client when a customer reaches the counter or its order leaves the queue
    public event EventHandler OnOrderListChanged;

    //Table service: fired on every client when a dish appears at the counter, moves to a tray or is served
    public event EventHandler OnDishesChanged;

    public event EventHandler OnBurst;
    public event EventHandler OnRushHourStarted;

    public enum Reaction : byte
    {
        Fed,
        FedByGun,
        WrongOrder,
        Hit,
        DeliveryFailed
    }

    //---------------FIELDS-----------------
    [Header("Scene")]
    [SerializeField] private List<RecipeSO> recipeSOList;
    [SerializeField] private Transform servicePoint;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private CartHealth cartHealth;
    [Tooltip("How far from the cart surface attackers stop, so they don't clip into the truck")]
    [SerializeField] private float attackStandOff = 0.6f;
    [Tooltip("Local-only visual, must NOT have a NetworkObject")]
    [SerializeField] private CustomerVisual customerVisualPrefab;

    [Header("Tables")]
    [Tooltip("Seats come from each table's CustomerPoints. Empty = half rings in front of the service point")]
    [SerializeField] private CustomerTable[] tables;
    [Tooltip("How many dishes a waiter can stack")]
    [SerializeField] private int trayCapacity = 3;
    [Tooltip("Where customers without a free seat wait, measured from the service point along its forward")]
    [SerializeField] private float overflowDistance = 2.5f;

    [Header("Dev")]
    [Tooltip("Editor only: use the multiplayer table service (counter -> tray -> table) even in singleplayer")]
    [SerializeField] private bool devForceTableService;

    [Header("Day phases")]
    [Tooltip("Fraction of the match after which the calm period ends and the Rush Hour begins")]
    [SerializeField, Range(0f, 1f)] private float rushStartNormalized = 0.5f;
    [SerializeField] private int maxCustomers = 24;

    [Header("Calm (kitchen + economy)")]
    [SerializeField] private float calmSpawnInterval = 8f;
    [SerializeField] private float calmStartHungerMax = 15f;

    [Header("Rush Hour (combat)")]
    [SerializeField] private float rushSpawnIntervalStart = 2.5f;
    [SerializeField] private float rushSpawnIntervalEnd = 1f;
    [Tooltip("Rush customers arrive with at least half of the hunger bar filled (RAGE_MAX = 120)")]
    [SerializeField] private float rushStartHungerMin = 60f;
    [SerializeField] private float rushStartHungerMax = 75f;
    [SerializeField] private float burstInterval = 20f;
    [SerializeField] private int burstSizeStart = 4;
    [SerializeField] private int burstSizeEnd = 8;

    [Header("Hunger / Anger")]
    [SerializeField] private float hungerPerSecond = 2f;
    [SerializeField] private float wrongOrderAnger = 25f;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2.2f;
    [SerializeField] private float desperateSpeedMultiplier = 1.7f;
    [SerializeField] private float slotSpacing = 1.4f;
    [SerializeField] private float handOffReach = 3f;

    [Header("Cart damage per second")]
    [SerializeField] private float furiousCartDps = 1.5f;
    [SerializeField] private float collapseCartDps = 3f;

    //---------------NETWORK VARIABLES-----------------
    private NetworkList<CustomerData> customerNetworkList;
    //Table service dishes, in the order they were cooked (index 0 = oldest). Changes rarely, so the server edits it directly
    private NetworkList<DishData> dishNetworkList;
    private NetworkVariable<int> customersServed = new NetworkVariable<int>(0);
    private NetworkVariable<int> customersFedByGun = new NetworkVariable<int>(0);
    private NetworkVariable<bool> isRushHour = new NetworkVariable<bool>(false);

    //---------------PRIVATE VARIABLES-----------------
    private class ServerCustomer
    {
        public ushort id;
        public Vector3 position;
        public CustomerPhase phase;
        public float hunger;
        public float anger;
        public int recipeIndex;
        public float stunTimer;
        public Vector3 knockback;
        public int slot = -1;
        public Vector3 exitPosition;
        public ushort orderTicket;

        public float Rage => Mathf.Clamp(hunger + anger, 0f, RAGE_MAX);
    }

    private readonly List<ServerCustomer> serverCustomerList = new List<ServerCustomer>();
    private readonly Dictionary<ushort, CustomerVisual> customerVisualDictionary = new Dictionary<ushort, CustomerVisual>();
    private readonly HashSet<ushort> seenIdSet = new HashSet<ushort>();
    private readonly List<ushort> staleIdList = new List<ushort>();
    private Vector3[] slotPositions;
    private int[] slotTableIndex; //-1 for the half-ring fallback
    private bool[] slotOccupied;
    private readonly List<DishInfo> dishInfoList = new List<DishInfo>();

    public struct DishInfo
    {
        public ushort dishId;
        public RecipeSO recipeSO;
        public DishState state;
        public ulong carrierClientId;
    }
    private ushort nextCustomerId;
    private ushort nextDishId;
    private ushort nextOrderTicket;
    private readonly List<CustomerData> orderDataList = new List<CustomerData>();
    private readonly List<ushort> orderIdList = new List<ushort>();
    private readonly List<ushort> previousOrderIdList = new List<ushort>();
    private readonly List<RecipeSO> orderRecipeSOList = new List<RecipeSO>();
    private float spawnTimer = 2f;
    private float burstTimer;
    private float syncTimer;
    private const float SYNC_INTERVAL = 0.1f;
    private const float KNOCKBACK_STUN = 0.3f;
    //Furious customers attack the closest point of any collider under this tag, so a bigger truck just needs tagged colliders
    private const string FOOD_TRUCK_TAG = "FoodTruck";
    private readonly List<Collider> cartColliderList = new List<Collider>();
    private RushEconomy rushEconomy;

    //---------------UNITY METHODS-----------------
    private void Awake()
    {
        Instance = this;
        customerNetworkList = new NetworkList<CustomerData>();
        dishNetworkList = new NetworkList<DishData>();
        rushEconomy = GetComponent<RushEconomy>();
    }

    public override void OnNetworkSpawn()
    {
        BuildSlots();
        isRushHour.OnValueChanged += IsRushHour_OnValueChanged;
        dishNetworkList.OnListChanged += DishNetworkList_OnListChanged;
        RebuildDishInfoList();

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
            CollectCartColliders();
        }
    }

    //shortcut: collected once at spawn, re-run this if truck parts are added/enabled mid-match
    private void CollectCartColliders()
    {
        cartColliderList.Clear();
        foreach (GameObject cartPart in GameObject.FindGameObjectsWithTag(FOOD_TRUCK_TAG))
        {
            foreach (Collider collider in cartPart.GetComponents<Collider>())
            {
                //ClosestPoint only supports Box/Sphere/Capsule and convex Mesh colliders
                if (collider is MeshCollider meshCollider && !meshCollider.convex)
                {
                    Debug.LogWarning($"{cartPart.name}: non-convex MeshCollider ignored for cart attacks", cartPart);
                    continue;
                }
                cartColliderList.Add(collider);
            }
        }
    }

    //A waiter who drops out puts their dishes back on the counter
    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        for (int i = 0; i < dishNetworkList.Count; i++)
        {
            DishData dishData = dishNetworkList[i];
            if (dishData.State == DishState.OnTray && dishData.carrier == clientId)
            {
                dishData.state = (byte)DishState.AtCounter;
                dishNetworkList[i] = dishData;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_OnClientDisconnectCallback;
        }
    }

    private void IsRushHour_OnValueChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            OnRushHourStarted?.Invoke(this, EventArgs.Empty);
        }
    }

    public override void OnDestroy()
    {
        foreach (CustomerVisual customerVisual in customerVisualDictionary.Values)
        {
            if (customerVisual != null)
            {
                Destroy(customerVisual.gameObject);
            }
        }
        base.OnDestroy();
    }

    private void Update()
    {
        if (IsServer && GameManager.Instance.IsGamePlaying())
        {
            float deltaTime = Time.deltaTime;
            HandleSpawning(deltaTime);
            SimulateCustomers(deltaTime);

            syncTimer -= deltaTime;
            if (syncTimer <= 0f)
            {
                syncTimer = SYNC_INTERVAL;
                SyncNetworkList();
            }
        }

        UpdateVisuals();
    }

    //---------------SPAWNING-----------------
    //First part of the match is calm (cook, earn money); after rushStartNormalized the Rush Hour hits
    private void HandleSpawning(float deltaTime)
    {
        float matchProgress = GameManager.Instance.GetGamePlayingTimerNormalized();

        if (matchProgress < rushStartNormalized)
        {
            spawnTimer -= deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = calmSpawnInterval;
                SpawnCustomer(UnityEngine.Random.Range(0f, calmStartHungerMax));
            }
            return;
        }

        if (!isRushHour.Value)
        {
            isRushHour.Value = true;
            //Opens with a wave right away
            burstTimer = 0f;
        }

        float rushProgress = Mathf.InverseLerp(rushStartNormalized, 1f, matchProgress);

        spawnTimer -= deltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = Mathf.Lerp(rushSpawnIntervalStart, rushSpawnIntervalEnd, rushProgress);
            SpawnCustomer(RushStartHunger());
        }

        //A whole office leaves at once
        burstTimer -= deltaTime;
        if (burstTimer <= 0f)
        {
            burstTimer = burstInterval;
            int burstSize = Mathf.RoundToInt(Mathf.Lerp(burstSizeStart, burstSizeEnd, rushProgress));
            for (int i = 0; i < burstSize; i++)
            {
                SpawnCustomer(RushStartHunger());
            }
            BurstClientRpc();
        }
    }

    private float RushStartHunger()
    {
        return UnityEngine.Random.Range(rushStartHungerMin, rushStartHungerMax);
    }

    private void SpawnCustomer(float startHunger)
    {
        if (serverCustomerList.Count >= maxCustomers || spawnPoints.Length == 0 || recipeSOList.Count == 0)
        {
            return;
        }

        Vector2 offset = UnityEngine.Random.insideUnitCircle * 1.5f;
        Vector3 spawnPosition = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].position + new Vector3(offset.x, 0f, offset.y);
        spawnPosition.y = servicePoint.position.y;

        ServerCustomer customer = new ServerCustomer
        {
            id = nextCustomerId++,
            position = spawnPosition,
            phase = CustomerPhase.WalkingIn,
            hunger = startHunger,
            recipeIndex = UnityEngine.Random.Range(0, recipeSOList.Count),
            exitPosition = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].position,
        };
        customer.exitPosition.y = servicePoint.position.y;
        customer.slot = ClaimSlot();

        serverCustomerList.Add(customer);
        customerNetworkList.Add(ToData(customer));
    }

    //---------------SIMULATION-----------------
    private void SimulateCustomers(float deltaTime)
    {
        Vector3 servicePosition = servicePoint.position;

        for (int i = serverCustomerList.Count - 1; i >= 0; i--)
        {
            ServerCustomer customer = serverCustomerList[i];

            //Stress only builds while standing at the counter waiting for the order
            if (customer.phase == CustomerPhase.Waiting)
            {
                //Capped so a hot dog to the face still makes a visible difference to long-waiting customers
                customer.hunger = Mathf.Min(RAGE_MAX, customer.hunger + hungerPerSecond * deltaTime);
            }

            if (customer.phase != CustomerPhase.Leaving)
            {
                UpdatePhaseFromMood(customer);
            }

            //Standing in the overflow: grab a seat as soon as one frees up
            if (customer.slot < 0 && (customer.phase == CustomerPhase.WalkingIn || customer.phase == CustomerPhase.Waiting))
            {
                customer.slot = ClaimSlot();
            }

            Vector3 target = GetTarget(customer, servicePosition);

            if (customer.stunTimer > 0f)
            {
                customer.stunTimer -= deltaTime;
            }
            else
            {
                float speed = walkSpeed * (GetMood(customer.Rage) >= CustomerMood.Desesperado ? desperateSpeedMultiplier : 1f);
                customer.position = Vector3.MoveTowards(customer.position, target, speed * deltaTime);
            }

            customer.position += customer.knockback * deltaTime;
            customer.knockback *= Mathf.Exp(-6f * deltaTime);
            customer.position.y = servicePosition.y;

            float distanceToTarget = FlatDistance(customer.position, target);
            switch (customer.phase)
            {
                case CustomerPhase.WalkingIn:
                    if (distanceToTarget < 0.2f)
                    {
                        customer.phase = CustomerPhase.Waiting;
                        if (customer.orderTicket == 0 && customer.slot >= 0)
                        {
                            //First time seated: the order is registered in arrival order
                            customer.orderTicket = ++nextOrderTicket;
                        }
                    }
                    break;
                case CustomerPhase.Waiting:
                    if (distanceToTarget > 1f)
                    {
                        //Got knocked out of the line
                        customer.phase = CustomerPhase.WalkingIn;
                    }
                    break;
                case CustomerPhase.Attacking:
                    if (distanceToTarget < 1.5f && customer.stunTimer <= 0f)
                    {
                        float dps = GetMood(customer.Rage) == CustomerMood.Colapso ? collapseCartDps : furiousCartDps;
                        cartHealth.TakeDamage(dps * deltaTime);
                    }
                    break;
                case CustomerPhase.Leaving:
                    if (distanceToTarget < 0.5f)
                    {
                        RemoveCustomerAt(i);
                        continue;
                    }
                    break;
            }
        }

        SeparateCustomers();
    }

    private void UpdatePhaseFromMood(ServerCustomer customer)
    {
        bool isFurious = GetMood(customer.Rage) >= CustomerMood.Furioso;

        if (isFurious && customer.phase != CustomerPhase.Attacking)
        {
            //Gets up and goes after the cart. The seat stays theirs, so they can come back to it
            customer.phase = CustomerPhase.Attacking;
        }
        else if (!isFurious && customer.phase == CustomerPhase.Attacking)
        {
            //Calmed down (e.g. ate some hot dog), goes back to their seat
            if (customer.slot < 0)
            {
                customer.slot = ClaimSlot();
            }
            customer.phase = CustomerPhase.WalkingIn;
        }
    }

    private Vector3 GetTarget(ServerCustomer customer, Vector3 servicePosition)
    {
        switch (customer.phase)
        {
            case CustomerPhase.Leaving:
                return customer.exitPosition;
            case CustomerPhase.Attacking:
                return GetAttackPosition(customer.position, servicePosition);
            default:
                //Every seat taken: crowd in front of the service point
                return customer.slot >= 0 ? slotPositions[customer.slot] : GetOverflowPosition(servicePosition);
        }
    }

    //Each attacker goes for the side of the truck closest to them, so the mob surrounds it
    private Vector3 GetAttackPosition(Vector3 customerPosition, Vector3 servicePosition)
    {
        Vector3 closestPoint = servicePosition;
        float closestDistance = float.MaxValue;
        foreach (Collider cartCollider in cartColliderList)
        {
            if (cartCollider == null || !cartCollider.enabled)
            {
                continue;
            }
            Vector3 point = cartCollider.ClosestPoint(customerPosition);
            float distance = FlatDistance(point, customerPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = point;
            }
        }
        if (closestDistance == float.MaxValue)
        {
            //No tagged colliders: fall back to the service point
            return servicePosition;
        }

        Vector3 away = customerPosition - closestPoint;
        away.y = 0f;
        if (away.sqrMagnitude > 0.0001f)
        {
            closestPoint += away.normalized * attackStandOff;
        }
        closestPoint.y = servicePosition.y;
        return closestPoint;
    }

    private Vector3 GetOverflowPosition(Vector3 servicePosition)
    {
        Vector3 outward = servicePoint.forward;
        outward.y = 0f;
        return servicePosition + outward.normalized * overflowDistance;
    }

    private static bool IsSeated(ServerCustomer customer)
    {
        return customer.slot >= 0 && customer.phase == CustomerPhase.Waiting;
    }

    private void SeparateCustomers()
    {
        const float minDistance = 0.9f;

        for (int i = 0; i < serverCustomerList.Count; i++)
        {
            for (int j = i + 1; j < serverCustomerList.Count; j++)
            {
                ServerCustomer a = serverCustomerList[i];
                ServerCustomer b = serverCustomerList[j];
                if (IsSeated(a) && IsSeated(b))
                {
                    //Seats around a table are closer than minDistance; don't push people off their chairs
                    continue;
                }
                Vector3 delta = b.position - a.position;
                delta.y = 0f;
                float distance = delta.magnitude;
                if (distance < minDistance && distance > 0.0001f)
                {
                    Vector3 push = delta / distance * ((minDistance - distance) * 0.5f);
                    a.position -= push;
                    b.position += push;
                }
            }
        }
    }

    private void RemoveCustomerAt(int index)
    {
        ReleaseSlot(serverCustomerList[index]);
        serverCustomerList.RemoveAt(index);
        customerNetworkList.RemoveAt(index);
    }

    private void SyncNetworkList()
    {
        for (int i = 0; i < serverCustomerList.Count; i++)
        {
            CustomerData customerData = ToData(serverCustomerList[i]);
            if (!customerNetworkList[i].Equals(customerData))
            {
                customerNetworkList[i] = customerData;
            }
        }
    }

    private CustomerData ToData(ServerCustomer customer)
    {
        return new CustomerData
        {
            id = customer.id,
            //Rounded so tiny jitter doesn't mark the element dirty every sync
            position = new Vector3(Mathf.Round(customer.position.x * 50f) / 50f, customer.position.y, Mathf.Round(customer.position.z * 50f) / 50f),
            mood = (byte)GetMood(customer.Rage),
            phase = (byte)customer.phase,
            recipeIndex = (byte)customer.recipeIndex,
            rage = (byte)Mathf.RoundToInt(customer.Rage),
            stunned = customer.stunTimer > 0f,
            orderTicket = customer.orderTicket,
            slot = customer.slot >= 0 ? (byte)customer.slot : CustomerData.NO_SLOT,
        };
    }

    public static CustomerMood GetMood(float rage)
    {
        if (rage >= RAGE_COLAPSO) return CustomerMood.Colapso;
        if (rage >= RAGE_FURIOSO) return CustomerMood.Furioso;
        if (rage >= RAGE_DESESPERADO) return CustomerMood.Desesperado;
        if (rage >= RAGE_FAMINTO) return CustomerMood.Faminto;
        return CustomerMood.Normal;
    }

    //---------------WAITING SLOTS-----------------
    //Half rings in front of the service point, filled from the inside out
    private void BuildSlots()
    {
        if (tables != null && tables.Length > 0)
        {
            BuildTableSlots();
            return;
        }

        slotPositions = new Vector3[maxCustomers];
        slotTableIndex = new int[maxCustomers];
        slotOccupied = new bool[maxCustomers];
        for (int i = 0; i < maxCustomers; i++)
        {
            slotTableIndex[i] = -1;
        }

        Vector3 outward = servicePoint.forward;
        outward.y = 0f;
        outward.Normalize();

        int slotIndex = 0;
        for (int ring = 0; slotIndex < maxCustomers; ring++)
        {
            float radius = 2f + ring * slotSpacing;
            int ringCapacity = Mathf.Max(3, Mathf.FloorToInt(Mathf.PI * radius * (160f / 180f) / slotSpacing));
            for (int k = 0; k < ringCapacity && slotIndex < maxCustomers; k++)
            {
                float angle = Mathf.Lerp(-80f, 80f, ringCapacity == 1 ? 0.5f : k / (float)(ringCapacity - 1));
                slotPositions[slotIndex++] = servicePoint.position + Quaternion.Euler(0f, angle, 0f) * outward * radius;
            }
        }
    }

    //One seat per CustomerPoint, in table order (table 1 seats first)
    private void BuildTableSlots()
    {
        List<Vector3> positionList = new List<Vector3>();
        List<int> tableIndexList = new List<int>();
        for (int t = 0; t < tables.Length; t++)
        {
            foreach (Transform customerPoint in tables[t].GetCustomerPoints())
            {
                Vector3 seatPosition = customerPoint.position;
                seatPosition.y = servicePoint.position.y;
                positionList.Add(seatPosition);
                tableIndexList.Add(t);
            }
        }

        //CustomerData.slot is a byte and NO_SLOT is reserved
        int seatCount = Mathf.Min(positionList.Count, CustomerData.NO_SLOT);
        slotPositions = positionList.GetRange(0, seatCount).ToArray();
        slotTableIndex = tableIndexList.GetRange(0, seatCount).ToArray();
        slotOccupied = new bool[seatCount];
    }

    private int ClaimSlot()
    {
        for (int i = 0; i < slotOccupied.Length; i++)
        {
            if (!slotOccupied[i])
            {
                slotOccupied[i] = true;
                return i;
            }
        }
        return -1;
    }

    private void ReleaseSlot(ServerCustomer customer)
    {
        if (customer.slot >= 0)
        {
            slotOccupied[customer.slot] = false;
            customer.slot = -1;
        }
    }

    //---------------FEEDING (GDD: Alimentar)-----------------
    //handOff = player gives the plate straight to the nearest customer; otherwise it goes through the DeliveryCounter
    public void DeliverPlate(PlateKitchenObject plateKitchenObject, Player player, bool handOff)
    {
        DeliverPlateServerRpc(new NetworkObjectReference(plateKitchenObject.NetworkObject), new NetworkObjectReference(player.NetworkObject), handOff);
    }

    [Rpc(SendTo.Server)]
    private void DeliverPlateServerRpc(NetworkObjectReference plateReference, NetworkObjectReference playerReference, bool handOff, RpcParams rpcParams = default)
    {
        if (!plateReference.TryGet(out NetworkObject plateNetworkObject) || !playerReference.TryGet(out NetworkObject playerNetworkObject))
        {
            //Plate already delivered/destroyed
            return;
        }

        PlateKitchenObject plateKitchenObject = plateNetworkObject.GetComponent<PlateKitchenObject>();
        Player player = playerNetworkObject.GetComponent<Player>();
        if (plateKitchenObject == null || player == null || player.OwnerClientId != rpcParams.Receive.SenderClientId || player.GetKitchenObject() != plateKitchenObject)
        {
            return;
        }

        int recipeIndex = FindRecipeIndex(plateKitchenObject.GetKitchenObjectSOList());

        if (handOff)
        {
            ServerCustomer customer = FindNearestCustomer(player.transform.position, handOffReach);
            if (customer == null)
            {
                //Nobody to hand it to, the player keeps the plate
                return;
            }

            if (customer.recipeIndex == recipeIndex)
            {
                FeedCustomer(customer);
            }
            else
            {
                customer.anger = Mathf.Min(RAGE_MAX, customer.anger + wrongOrderAnger);
                ReactionClientRpc(customer.id, Reaction.WrongOrder, customer.position);
            }
        }
        else if (IsTableServiceMode())
        {
            //Table service: any valid recipe goes on the counter as a free dish; the waiters decide where it goes
            if (recipeIndex >= 0)
            {
                dishNetworkList.Add(new DishData
                {
                    id = nextDishId++,
                    recipeIndex = (byte)recipeIndex,
                    state = (byte)DishState.AtCounter,
                });
            }
            else
            {
                ReactionClientRpc(ushort.MaxValue, Reaction.DeliveryFailed, servicePoint.position);
            }
        }
        else
        {
            //GDD "Servir": the counter serves whoever is closest to snapping first
            ServerCustomer bestCustomer = null;
            foreach (ServerCustomer customer in serverCustomerList)
            {
                if (customer.phase != CustomerPhase.Leaving && customer.recipeIndex == recipeIndex
                    && (bestCustomer == null || customer.Rage > bestCustomer.Rage))
                {
                    bestCustomer = customer;
                }
            }

            if (bestCustomer != null)
            {
                FeedCustomer(bestCustomer);
            }
            else
            {
                ReactionClientRpc(ushort.MaxValue, Reaction.DeliveryFailed, servicePoint.position);
            }
        }

        KitchenObject.DestroyKitchenObject(plateKitchenObject);
    }

    private void FeedCustomer(ServerCustomer customer, bool byGun = false)
    {
        //Paid before resetting: the tip depends on how patient the customer still was
        rushEconomy.RegisterFeed(recipeSOList[customer.recipeIndex], GetMood(customer.Rage), byGun, isRushHour.Value, customer.position);

        ReleaseSlot(customer);
        customer.phase = CustomerPhase.Leaving;
        customer.hunger = 0f;
        customer.anger = 0f;
        customersServed.Value++;
        if (byGun)
        {
            customersFedByGun.Value++;
        }
        ReactionClientRpc(customer.id, byGun ? Reaction.FedByGun : Reaction.Fed, customer.position);
    }

    //---------------TABLE SERVICE (multiplayer)-----------------
    //Single player keeps the direct counter delivery; the dev toggle forces this flow without a second instance
    public bool IsTableServiceMode()
    {
        if (tables == null || tables.Length == 0)
        {
            return false;
        }
#if UNITY_EDITOR
        if (devForceTableService)
        {
            return true;
        }
#endif
        return GameManagerMultiplayer.playMultiplayer;
    }

    //Waiter takes the oldest dish from the counter onto their tray
    public void PickUpDish(Player player)
    {
        PickUpDishServerRpc(new NetworkObjectReference(player.NetworkObject));
    }

    [Rpc(SendTo.Server)]
    private void PickUpDishServerRpc(NetworkObjectReference playerReference, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (!playerReference.TryGet(out NetworkObject playerNetworkObject)) return;
        Player player = playerNetworkObject.GetComponent<Player>();
        if (player == null || player.OwnerClientId != clientId || player.HasKitchenObject()) return;
        if (GetTrayCount(clientId) >= trayCapacity) return;

        //The list is in cooking order, so the first one at the counter is the oldest
        for (int i = 0; i < dishNetworkList.Count; i++)
        {
            DishData dishData = dishNetworkList[i];
            if (dishData.State == DishState.AtCounter)
            {
                dishData.state = (byte)DishState.OnTray;
                dishData.carrier = clientId;
                dishNetworkList[i] = dishData;
                return;
            }
        }
    }

    //Serves every dish on the player's tray that someone seated at this table ordered.
    //Dishes nobody here wants stay on the tray for another table
    public void ServeTable(CustomerTable table, Player player)
    {
        int tableIndex = Array.IndexOf(tables, table);
        if (tableIndex < 0)
        {
            Debug.LogWarning($"{table.name} is not in CustomerManager's tables list");
            return;
        }
        ServeTableServerRpc(tableIndex, new NetworkObjectReference(player.NetworkObject));
    }

    [Rpc(SendTo.Server)]
    private void ServeTableServerRpc(int tableIndex, NetworkObjectReference playerReference, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (!playerReference.TryGet(out NetworkObject playerNetworkObject)) return;
        Player player = playerNetworkObject.GetComponent<Player>();
        if (player == null || player.OwnerClientId != clientId) return;

        //Backwards because served dishes are removed from the list
        for (int i = dishNetworkList.Count - 1; i >= 0; i--)
        {
            DishData dishData = dishNetworkList[i];
            if (dishData.State != DishState.OnTray || dishData.carrier != clientId)
            {
                continue;
            }

            ServerCustomer customer = FindCustomerAtTable(tableIndex, dishData.recipeIndex);
            if (customer != null)
            {
                //FeedCustomer sets them to Leaving, so the next dish can't pick the same customer
                FeedCustomer(customer);
                dishNetworkList.RemoveAt(i);
            }
        }
    }

    //Who at this table receives a dish of this recipe (null = nobody here ordered it)
    private ServerCustomer FindCustomerAtTable(int tableIndex, int recipeIndex)
    {
        //The angriest one gets it first: the waiter puts out the fire closest to reaching the truck
        ServerCustomer angriestCustomer = null;
        foreach (ServerCustomer customer in serverCustomerList)
        {
            //IsSeated, not just slot >= 0: furious customers keep their seat while they're away attacking the truck
            if (!IsSeated(customer) || slotTableIndex[customer.slot] != tableIndex || customer.recipeIndex != recipeIndex)
            {
                continue;
            }

            if (angriestCustomer == null || customer.Rage > angriestCustomer.Rage)
            {
                angriestCustomer = customer;
            }
        }
        return angriestCustomer;
    }

    private int GetTrayCount(ulong clientId)
    {
        int count = 0;
        foreach (DishData dishData in dishNetworkList)
        {
            if (dishData.State == DishState.OnTray && dishData.carrier == clientId)
            {
                count++;
            }
        }
        return count;
    }

    private int FindRecipeIndex(List<KitchenObjectSO> plateKitchenObjectSOList)
    {
        for (int i = 0; i < recipeSOList.Count; i++)
        {
            List<KitchenObjectSO> recipeKitchenObjectSOList = recipeSOList[i].kitchenObjectSOList;
            if (recipeKitchenObjectSOList.Count != plateKitchenObjectSOList.Count)
            {
                continue;
            }

            bool plateContentsMatchesRecipe = true;
            foreach (KitchenObjectSO recipeKitchenObjectSO in recipeKitchenObjectSOList)
            {
                if (!plateKitchenObjectSOList.Contains(recipeKitchenObjectSO))
                {
                    plateContentsMatchesRecipe = false;
                    break;
                }
            }

            if (plateContentsMatchesRecipe)
            {
                return i;
            }
        }
        return -1;
    }

    private ServerCustomer FindNearestCustomer(Vector3 position, float reach)
    {
        ServerCustomer nearestCustomer = null;
        float nearestDistance = reach;
        foreach (ServerCustomer customer in serverCustomerList)
        {
            float distance = FlatDistance(customer.position, position);
            if (customer.phase != CustomerPhase.Leaving && distance < nearestDistance)
            {
                nearestCustomer = customer;
                nearestDistance = distance;
            }
        }
        return nearestCustomer;
    }

    //Client-side check (replicated data) so pressing Interact in empty space doesn't send the plate away
    public bool HasCustomerInReach(Vector3 position)
    {
        foreach (CustomerData customerData in customerNetworkList)
        {
            if (customerData.Phase != CustomerPhase.Leaving && FlatDistance(customerData.position, position) < handOffReach)
            {
                return true;
            }
        }
        return false;
    }

    //---------------FEEDING BY GUN (Alimentar na bala)-----------------
    //Server only. Every customer inside the cone is pushed back and loses hungerReduction of hunger (anger untouched);
    //only the ones whose hunger reaches 0 are fed and leave. Returns how many were fed.
    //Customers already leaving only get knocked around.
    public int ApplyConeBlast(Vector3 origin, Vector3 direction, float range, float halfAngle, float knockbackForce, float hungerReduction)
    {
        direction.y = 0f;
        direction.Normalize();
        int fedCount = 0;

        foreach (ServerCustomer customer in serverCustomerList)
        {
            Vector3 toCustomer = customer.position - origin;
            toCustomer.y = 0f;
            float distance = toCustomer.magnitude;
            if (distance > range || (distance > 0.6f && Vector3.Angle(direction, toCustomer) > halfAngle))
            {
                continue;
            }

            float falloff = 1f - distance / range * 0.6f;
            Vector3 pushDirection = distance > 0.01f ? toCustomer / distance : direction;
            customer.knockback += pushDirection * knockbackForce * falloff;
            customer.stunTimer = KNOCKBACK_STUN;

            if (customer.phase == CustomerPhase.Leaving)
            {
                ReactionClientRpc(customer.id, Reaction.Hit, customer.position);
                continue;
            }

            customer.hunger = Mathf.Max(0f, customer.hunger - hungerReduction);
            if (customer.hunger <= 0f)
            {
                FeedCustomer(customer, true);
                fedCount++;
            }
            else
            {
                //Still hungry: stays (a furious one may calm down and return to the line, see UpdatePhaseFromMood)
                ReactionClientRpc(customer.id, Reaction.Hit, customer.position);
            }
        }

        return fedCount;
    }

    //---------------CLIENT RPCS-----------------
    [Rpc(SendTo.ClientsAndHost)]
    private void ReactionClientRpc(ushort customerId, Reaction reaction, Vector3 position)
    {
        if (customerVisualDictionary.TryGetValue(customerId, out CustomerVisual customerVisual))
        {
            customerVisual.PlayReaction(reaction);
        }

        OnCustomerReaction?.Invoke(this, new OnCustomerReactionEventArgs
        {
            customerId = customerId,
            reaction = reaction,
            position = position
        });
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void BurstClientRpc()
    {
        OnBurst?.Invoke(this, EventArgs.Empty);
    }

    //---------------VISUALS (every client)-----------------
    private void UpdateVisuals()
    {
        if (!IsSpawned)
        {
            return;
        }

        seenIdSet.Clear();
        foreach (CustomerData customerData in customerNetworkList)
        {
            seenIdSet.Add(customerData.id);
            if (!customerVisualDictionary.TryGetValue(customerData.id, out CustomerVisual customerVisual))
            {
                customerVisual = Instantiate(customerVisualPrefab, customerData.position, Quaternion.identity);
                customerVisualDictionary.Add(customerData.id, customerVisual);
            }
            customerVisual.SetData(customerData, GetRecipeSO(customerData.recipeIndex), GetSeatLookTarget(customerData));
        }

        UpdateOrderList();

        staleIdList.Clear();
        foreach (ushort id in customerVisualDictionary.Keys)
        {
            if (!seenIdSet.Contains(id))
            {
                staleIdList.Add(id);
            }
        }
        foreach (ushort id in staleIdList)
        {
            Destroy(customerVisualDictionary[id].gameObject);
            customerVisualDictionary.Remove(id);
        }
    }

    //---------------ORDER QUEUE (every client)-----------------
    //Rebuilt from the replicated snapshot; the event only fires when the queue actually changes,
    //so DeliveryManagerUI doesn't recreate its templates every frame
    private void UpdateOrderList()
    {
        orderDataList.Clear();
        foreach (CustomerData customerData in customerNetworkList)
        {
            if (HasOpenOrder(customerData))
            {
                orderDataList.Add(customerData);
            }
        }
        orderDataList.Sort((a, b) => a.orderTicket.CompareTo(b.orderTicket));

        orderIdList.Clear();
        foreach (CustomerData customerData in orderDataList)
        {
            orderIdList.Add(customerData.id);
        }

        if (!IsSameIdList(orderIdList, previousOrderIdList))
        {
            previousOrderIdList.Clear();
            previousOrderIdList.AddRange(orderIdList);

            orderRecipeSOList.Clear();
            foreach (CustomerData customerData in orderDataList)
            {
                orderRecipeSOList.Add(GetRecipeSO(customerData.recipeIndex));
            }
            OnOrderListChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static bool HasOpenOrder(CustomerData customerData)
    {
        if (customerData.orderTicket == 0 && customerData.Phase != CustomerPhase.Waiting)
        {
            return false;
        }
        return true;
    }

    //Seated customers face the center of their table
    private Vector3? GetSeatLookTarget(CustomerData customerData)
    {
        if (customerData.Phase != CustomerPhase.Waiting || customerData.slot == CustomerData.NO_SLOT
            || slotTableIndex == null || customerData.slot >= slotTableIndex.Length)
        {
            return null;
        }
        int tableIndex = slotTableIndex[customerData.slot];
        return tableIndex >= 0 ? tables[tableIndex].GetCenter() : (Vector3?)null;
    }

    //Dishes only change on cook / pick up / serve / disconnect, so the NetworkList event is enough (no per-frame diff)
    private void DishNetworkList_OnListChanged(NetworkListEvent<DishData> changeEvent)
    {
        RebuildDishInfoList();
        OnDishesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RebuildDishInfoList()
    {
        dishInfoList.Clear();
        foreach (DishData dishData in dishNetworkList)
        {
            dishInfoList.Add(new DishInfo
            {
                dishId = dishData.id,
                recipeSO = GetRecipeSO(dishData.recipeIndex),
                state = dishData.State,
                carrierClientId = dishData.carrier,
            });
        }
    }

    private static bool IsSameIdList(List<ushort> a, List<ushort> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    //---------------PUBLIC API-----------------
    public RecipeSO GetRecipeSO(int recipeIndex)
    {
        return recipeIndex < recipeSOList.Count ? recipeSOList[recipeIndex] : null;
    }

    //Recipes of the customers who already reached the counter, in arrival order
    public List<RecipeSO> GetOrderRecipeSOList()
    {
        return orderRecipeSOList;
    }

    //Dishes waiting on the DeliveryCounter, oldest first
    public void GetCounterDishes(List<DishInfo> resultList)
    {
        resultList.Clear();
        foreach (DishInfo dishInfo in dishInfoList)
        {
            if (dishInfo.state == DishState.AtCounter) resultList.Add(dishInfo);
        }
    }

    //Dishes stacked on this player's tray, oldest first
    public void GetTrayDishes(ulong clientId, List<DishInfo> resultList)
    {
        resultList.Clear();
        foreach (DishInfo dishInfo in dishInfoList)
        {
            if (dishInfo.state == DishState.OnTray && dishInfo.carrierClientId == clientId) resultList.Add(dishInfo);
        }
    }

    public bool IsCarryingDishes(ulong clientId)
    {
        foreach (DishInfo dishInfo in dishInfoList)
        {
            if (dishInfo.state == DishState.OnTray && dishInfo.carrierClientId == clientId) return true;
        }
        return false;
    }

    public int GetCustomersServed()
    {
        return customersServed.Value;
    }

    public int GetCustomersFedByGun()
    {
        return customersFedByGun.Value;
    }

    public bool IsRushHour()
    {
        return isRushHour.Value;
    }

    public float GetSecondsUntilRush()
    {
        float rushPartDuration = GameManager.Instance.GetGamePlayingTimerMax() * (1f - rushStartNormalized);
        return Mathf.Max(0f, GameManager.Instance.GetGamePlayingTimeRemaining() - rushPartDuration);
    }

    public int GetCustomerCount()
    {
        return customerNetworkList.Count;
    }

    public int GetFuriousCustomerCount()
    {
        int furiousCount = 0;
        foreach (CustomerData customerData in customerNetworkList)
        {
            if (customerData.Mood >= CustomerMood.Furioso)
            {
                furiousCount++;
            }
        }
        return furiousCount;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
