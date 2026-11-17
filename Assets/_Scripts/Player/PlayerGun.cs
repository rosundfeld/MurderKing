using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

//GDD section 8 - ShotDog: short range cone, high knockback, low capacity.
//Every hungry customer hit is fully fed (but pays little, see RushEconomy).
//Ammo comes from the same stock used for orders: reloading eats the food the player is holding.
public class PlayerShotDog : NetworkBehaviour
{
    [Serializable]
    public struct AmmoSource
    {
        public KitchenObjectSO kitchenObjectSO;
        public int shells;
    }

    private enum ReloadFailReason : byte
    {
        HoldingNothing,
        MagazineFull,
        NotAmmo
    }

    //---------------EVENTS-----------------
    public event EventHandler OnShellsChanged;

    //Owner only: a human readable explanation of every ammo change, for the ammo UI
    public event EventHandler<OnAmmoEventArgs> OnAmmoEvent;
    public class OnAmmoEventArgs : EventArgs
    {
        public string message;
        public bool isWarning;
    }

    //---------------FIELDS-----------------
    [Header("Ammo")]
    [SerializeField] private List<AmmoSource> ammoSourceList;
    [SerializeField] private int magazineSize = 6;
    [SerializeField] private int startingShells = 6;

    [Header("Blast")]
    [SerializeField] private float cooldown = 0.7f;
    [SerializeField] private float range = 7f;
    [SerializeField] private float halfAngle = 30f;
    [SerializeField] private float knockbackForce = 16f;
    [SerializeField] private int pelletCount = 7;

    //---------------NETWORK VARIABLES-----------------
    private NetworkVariable<int> shells = new NetworkVariable<int>(0);

    //---------------PRIVATE VARIABLES-----------------
    private float nextFireTime;
    private float serverNextFireTime;

    //---------------UNITY METHODS-----------------
    public override void OnNetworkSpawn()
    {
        shells.OnValueChanged += Shells_OnValueChanged;
        if (IsServer)
        {
            shells.Value = startingShells;
        }
        if (IsOwner)
        {
            GameInput.Instance.OnReloadAction += GameInput_OnReloadAction;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && GameInput.Instance != null)
        {
            GameInput.Instance.OnReloadAction -= GameInput_OnReloadAction;
        }
    }

    private void Shells_OnValueChanged(int previousValue, int newValue)
    {
        OnShellsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void GameInput_OnReloadAction(object sender, EventArgs e)
    {
        if (!GameManager.Instance.IsGamePlaying())
            return;
        ReloadServerRpc();
    }

    //---------------FIRE-----------------
    //Called by the owning Player when Fire is pressed away from a cutting counter
    public void TryFire()
    {
        if (!IsOwner || !GameManager.Instance.IsGamePlaying() || Time.time < nextFireTime)
            return;

        nextFireTime = Time.time + cooldown;

        if (shells.Value <= 0)
        {
            RaiseAmmoEvent("Sem munição! Segure " + GetAmmoSourcesText() + " e aperte [R]", true);
            SoundManager.Instance.PlayEmptyGunSound(transform.position);
            return;
        }

        FireServerRpc(transform.forward);
    }

    [Rpc(SendTo.Server)]
    private void FireServerRpc(Vector3 direction)
    {
        //Small tolerance for latency jitter between owner and server
        if (shells.Value <= 0 || Time.time < serverNextFireTime)
            return;

        serverNextFireTime = Time.time + cooldown * 0.8f;
        shells.Value--;

        direction.y = 0f;
        direction.Normalize();

        int fedCount = 0;
        if (CustomerManager.Instance != null)
        {
            fedCount = CustomerManager.Instance.ApplyConeBlast(transform.position, direction, range, halfAngle, knockbackForce);
        }

        FireClientRpc(transform.position, direction, fedCount, shells.Value);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void FireClientRpc(Vector3 origin, Vector3 direction, int fedCount, int shellsLeft)
    {
        Vector3 muzzle = origin + Vector3.up * 1f + direction * 0.6f;
        for (int i = 0; i < pelletCount; i++)
        {
            float spread = UnityEngine.Random.Range(-halfAngle, halfAngle);
            Vector3 pelletDirection = Quaternion.Euler(0f, spread, 0f) * direction;
            HotDogPellet.Spawn(muzzle, pelletDirection, range * UnityEngine.Random.Range(0.7f, 1f));
        }
        SoundManager.Instance.PlayShotDogSound(origin);

        if (IsOwner)
        {
            string result = fedCount > 0 ? $"alimentou {fedCount} cliente(s)" : "errou, ninguém no cone";
            RaiseAmmoEvent($"-1 tiro ({result}) · restam {shellsLeft}", fedCount == 0);
        }
    }

    //---------------RELOAD-----------------
    [Rpc(SendTo.Server)]
    private void ReloadServerRpc()
    {
        Player player = GetComponent<Player>();
        if (!player.HasKitchenObject())
        {
            ReloadFailedOwnerRpc(ReloadFailReason.HoldingNothing, -1);
            return;
        }
        if (shells.Value >= magazineSize)
        {
            ReloadFailedOwnerRpc(ReloadFailReason.MagazineFull, -1);
            return;
        }

        KitchenObject kitchenObject = player.GetKitchenObject();
        foreach (AmmoSource ammoSource in ammoSourceList)
        {
            if (ammoSource.kitchenObjectSO == kitchenObject.GetKitchenObjectSO())
            {
                int shellsBefore = shells.Value;
                shells.Value = Mathf.Min(magazineSize, shells.Value + ammoSource.shells);
                KitchenObject.DestroyKitchenObject(kitchenObject);
                ReloadClientRpc(shells.Value - shellsBefore, GameManagerMultiplayer.Instance.GetKitchenObjectSOIndex(ammoSource.kitchenObjectSO), shells.Value);
                return;
            }
        }

        ReloadFailedOwnerRpc(ReloadFailReason.NotAmmo, GameManagerMultiplayer.Instance.GetKitchenObjectSOIndex(kitchenObject.GetKitchenObjectSO()));
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ReloadClientRpc(int shellsAdded, int kitchenObjectSOIndex, int shellsNow)
    {
        SoundManager.Instance.PlayReloadSound(transform.position);

        if (IsOwner)
        {
            string itemName = GameManagerMultiplayer.Instance.GetKitchenObjectSOFromIndex(kitchenObjectSOIndex).objectName;
            RaiseAmmoEvent($"+{shellsAdded} recarga ({itemName} virou munição) · agora {shellsNow}", false);
        }
    }

    [Rpc(SendTo.Owner)]
    private void ReloadFailedOwnerRpc(ReloadFailReason reason, int kitchenObjectSOIndex)
    {
        switch (reason)
        {
            case ReloadFailReason.HoldingNothing:
                RaiseAmmoEvent("Recarga: mão vazia. Segure " + GetAmmoSourcesText(), true);
                break;
            case ReloadFailReason.MagazineFull:
                RaiseAmmoEvent("Recarga: pente já está cheio", true);
                break;
            case ReloadFailReason.NotAmmo:
                string itemName = GameManagerMultiplayer.Instance.GetKitchenObjectSOFromIndex(kitchenObjectSOIndex).objectName;
                RaiseAmmoEvent($"Recarga: {itemName} não é munição. Use " + GetAmmoSourcesText(), true);
                break;
        }
    }

    private void RaiseAmmoEvent(string message, bool isWarning)
    {
        OnAmmoEvent?.Invoke(this, new OnAmmoEventArgs { message = message, isWarning = isWarning });
    }

    //---------------PUBLIC API-----------------
    public int GetShells()
    {
        return shells.Value;
    }

    public int GetMagazineSize()
    {
        return magazineSize;
    }

    public string GetAmmoSourcesText()
    {
        List<string> ammoNames = new List<string>();
        foreach (AmmoSource ammoSource in ammoSourceList)
        {
            ammoNames.Add($"{ammoSource.kitchenObjectSO.objectName} (+{ammoSource.shells})");
        }
        return string.Join(" ou ", ammoNames);
    }
}
