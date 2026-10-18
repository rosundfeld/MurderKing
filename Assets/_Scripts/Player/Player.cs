using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using Unity.Cinemachine;

public class Player : NetworkBehaviour, IKitchenObjectParent
{

    public static event EventHandler OnAnyPlayerSpawned;
    public static event EventHandler OnAnyPickedSomething;
    //---------------SINGLETON NETCODE-----------------
    public static Player LocalInstance { get; private set; }


    //---------------EVENTS-----------------
    public event EventHandler OnPickedSomething;
    public event EventHandler<OnSelectedCounterChangedEventArgs> OnSelectedCounterChanged;
    public class OnSelectedCounterChangedEventArgs : EventArgs
    {
        public BaseCounter selectedCounter;
    }


    //---------------FIELDS-----------------
    [SerializeField] private float moveSpeed = 7f;

    [SerializeField] private LayerMask countersLayerMask;
    [SerializeField] private LayerMask collisionsLayerMask;
    [SerializeField] private Transform kitchenObjectHoldPoint;
    [SerializeField] private List<Vector3> spawnPositionList;
    [SerializeField] private PlayerVisual playerVisual;

    //---------------PRIVATE VARIABLES-----------------
    private KitchenObject kitchenObject;
    private bool isWalking;
    private Vector3 lastInteractDir;
    private BaseCounter selectedCounter;

    //---------------UNITY METHODS-----------------

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LocalInstance = this;

            //Each host/client has its own camera, so it only follows the player it owns
            CinemachineCamera cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
            if (cinemachineCamera != null)
            {
                cinemachineCamera.Target.TrackingTarget = transform;
            }
        }

        int playerIndex = GameManagerMultiplayer.Instance.GetPlayerDataIndexFromClientID(OwnerClientId);
        transform.position = GameManager.Instance.GetSpawnPosition(playerIndex, spawnPositionList[playerIndex]);

        OnAnyPlayerSpawned?.Invoke(this, EventArgs.Empty);

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        }

    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {

        if (clientId == OwnerClientId && HasKitchenObject())
        {
            KitchenObject.DestroyKitchenObject(GetKitchenObject());
        }

    }

    private void Start()
    {
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
        GameInput.Instance.OnFireAction += GameInput_OnInteractAlternateAction;

        PlayerData playerData = GameManagerMultiplayer.Instance.GetPlayerDataFromPlayerIndex((int)OwnerClientId);
        playerVisual.SetPlayerColor(GameManagerMultiplayer.Instance.GetPlayerColor(playerData.colorId));
    }

    private void GameInput_OnInteractAction(object sender, System.EventArgs e)
    {
        if (!GameManager.Instance.IsGamePlaying())
            return;
        if (selectedCounter != null)
        {
            Debug.Log("Interacting with counter: " + selectedCounter.name);
            selectedCounter.Interact(this);
        }
    }

    private void GameInput_OnInteractAlternateAction(object sender, System.EventArgs e)
    {
        if (!GameManager.Instance.IsGamePlaying())
            return;
        if (selectedCounter != null)
        {
            selectedCounter.InteractAlternate(this);
        }
    }

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        HandleMovement();
        HandleInteractions();
    }

    private void HandleInteractions()
    {
        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();

        Vector3 moveDirection = new Vector3(inputVector.x, 0, inputVector.y);

        if (moveDirection != Vector3.zero)
        {
            lastInteractDir = moveDirection;
        }

        float interactionDistance = 2f;

        if (Physics.Raycast(transform.position, lastInteractDir, out RaycastHit raycastHit, interactionDistance, countersLayerMask))
        {
 
            if (raycastHit.transform.TryGetComponent(out BaseCounter baseCounter))
            {
                //Use ao invés de Tags (TryGetComponent) para não precisar ficar criando tags e ficar mais organizado, além de ser mais performático além de já tratar do Null 
                if (baseCounter != selectedCounter)
                {
                    SetSelectedCounter(baseCounter);
                }
            }
            else
            {
                SetSelectedCounter(null);
            }
        }
        else
        {
            SetSelectedCounter(null);
        }
    }


    public bool IsWalking()
    {
        return isWalking;
    }

    private void HandleMovement()
    {
        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();

        Vector3 moveDirection = new Vector3(inputVector.x, 0, inputVector.y);

        float moveDistance = moveSpeed * Time.deltaTime;
        float playerRadius = 0.7f;

        bool canMove = !Physics.BoxCast(transform.position, Vector3.one * playerRadius, moveDirection, Quaternion.identity, moveDistance, collisionsLayerMask); //RAYCAST PARA ANDAR, MTO MELHOR QUE COLLIDER

        if (!canMove)
        {
            //Cannot move towards moveDirection, try to move only on the X axis
            Vector3 moveDirectionX = new Vector3(moveDirection.x, 0, 0).normalized;
            canMove = (moveDirectionX.x < -.5f || moveDirectionX.x > .5f) && !Physics.BoxCast(transform.position, Vector3.one * playerRadius, moveDirectionX, Quaternion.identity, moveDistance, collisionsLayerMask);
            if (canMove)
            {
                moveDirection = moveDirectionX;
            }
            else
            {
                //Cannot move on X axis, try to move only on the Z axis
                Vector3 moveDirectionZ = new Vector3(0, 0, moveDirection.z).normalized;
                canMove = (moveDirectionZ.z < -.5f || moveDirectionZ.z > .5f) && !Physics.BoxCast(transform.position, Vector3.one * playerRadius, moveDirectionZ, Quaternion.identity, moveDistance, collisionsLayerMask);
                if (canMove)
                {
                    moveDirection = moveDirectionZ;
                }
            }
        }
        if (canMove)
        {
            transform.position += moveDistance * moveDirection;
        }
        else
        {
            //Cannot move in any direction
        }
        isWalking = moveDirection != Vector3.zero;

        float rotationSpeed = 10f;
        transform.forward = Vector3.Slerp(transform.forward, moveDirection, Time.deltaTime * rotationSpeed);
    }

    private void SetSelectedCounter(BaseCounter selectedCounter)
    {
        this.selectedCounter = selectedCounter;

        OnSelectedCounterChanged?.Invoke(this, new OnSelectedCounterChangedEventArgs
        {
            selectedCounter = selectedCounter
        });
    }


    //---------------IKitchenObjectParent Implementation-----------------
    public Transform GetKitchenObjectFollowTransform()
    {
        return kitchenObjectHoldPoint;
    }

    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        this.kitchenObject = kitchenObject;

        if (kitchenObject != null)
        {
            OnPickedSomething?.Invoke(this, EventArgs.Empty);
            OnAnyPickedSomething?.Invoke(this, EventArgs.Empty);
        }
    }

    public KitchenObject GetKitchenObject()
    {
        return kitchenObject;
    }

    public void ClearKitchenObject()
    {
        kitchenObject = null;
    }

    public bool HasKitchenObject()
    {
        return kitchenObject != null;
    }

    public static void ResetStaticData()
    {
        OnAnyPlayerSpawned = null;
    }

    public NetworkObject GetNetworkObject()
    {
        return NetworkObject;
    }
}
