using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

//Orchestrator: owns the weapons, listens to GameInput and delegates to the active Weapon.
//It never knows which concrete weapon is active. UI and visuals only hear about it through the events.
//Ammo comes from the same stock used for orders: reloading eats the food the player is holding.
public class PlayerGun : NetworkBehaviour
{
    [Serializable]
    public struct AmmoSource
    {
        public KitchenObjectSO kitchenObjectSO;
        public int shells;
    }

    //---------------STATIC-----------------
    public static PlayerGun LocalInstance { get; private set; }
    public static event Action<PlayerGun> OnLocalInstanceSpawned;

    public static void ResetStaticData()
    {
        OnLocalInstanceSpawned = null;
    }

    //---------------EVENTS-----------------
    public event Action<WeaponShootData> OnWeaponShoot;
    public event Action<Weapon> OnActiveWeaponChanged;
    public event Action<AmmoData> OnAmmoChanged;
    public event Action OnWeaponReloaded;
    //Owner only
    public event Action OnWeaponEmpty;
    public event Action<ReloadFailReason> OnReloadFailed;

    //---------------FIELDS-----------------
    [SerializeField] private Player player;
    [SerializeField] private List<WeaponSO> weaponSOList;
    [SerializeField] private List<AmmoSource> ammoSourceList;

    //---------------PRIVATE VARIABLES-----------------
    //shortcut: weapon state is replicated by RPC only, a client joining mid-game would see stale ammo/weapon of the
    //other players; move both to NetworkVariables if late join becomes a thing
    private readonly List<Weapon> weaponList = new List<Weapon>();
    private int activeWeaponIndex;
    private Weapon activeWeapon;

    //---------------UNITY METHODS-----------------
    public override void OnNetworkSpawn()
    {
        if (weaponSOList.Count == 0)
        {
            Debug.LogError("PlayerGun has no weapons. Run 'Murder King/Configurar POC do Rush' or fill Weapon SO List on the Player prefab.", this);
            return;
        }

        foreach (WeaponSO weaponSO in weaponSOList)
        {
            weaponList.Add(weaponSO.CreateWeapon(CustomerManager.Instance));
        }
        activeWeapon = weaponList[0];

        if (IsOwner)
        {
            LocalInstance = this;
            GameInput.Instance.OnFireAction += GameInput_OnFireAction;
            GameInput.Instance.OnReloadAction += GameInput_OnReloadAction;
            GameInput.Instance.OnWeaponSwapAction += GameInput_OnWeaponSwapAction;
            OnLocalInstanceSpawned?.Invoke(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (LocalInstance == this)
        {
            LocalInstance = null;
        }
        if (IsOwner && GameInput.Instance != null)
        {
            GameInput.Instance.OnFireAction -= GameInput_OnFireAction;
            GameInput.Instance.OnReloadAction -= GameInput_OnReloadAction;
            GameInput.Instance.OnWeaponSwapAction -= GameInput_OnWeaponSwapAction;
        }
    }

    //---------------INPUT (owner)-----------------
    private void GameInput_OnFireAction(object sender, EventArgs e)
    {
        if (!GameManager.Instance.IsGamePlaying() || player.IsFireUsedByCounter())
            return;

        if (activeWeapon.IsEmpty)
        {
            OnWeaponEmpty?.Invoke();
            return;
        }

        Vector3 aimDirection = GetAimDirection(out float aimDistance);
        //Face the shot so it reads clearly; movement turns the player back as soon as they walk
        transform.forward = aimDirection;
        ShootServerRpc(aimDirection, aimDistance);
    }

    //Towards the cursor, projected on the ground plane at the player's feet. Gamepad (or no camera) keeps the
    //facing direction and aims as far as the weapon reaches.
    private Vector3 GetAimDirection(out float aimDistance)
    {
        aimDistance = Mathf.Infinity;
        Camera aimCamera = Camera.main;
        if (aimCamera != null && GameInput.Instance.TryGetAimScreenPosition(out Vector2 screenPosition))
        {
            Ray ray = aimCamera.ScreenPointToRay(screenPosition);
            if (new Plane(Vector3.up, transform.position).Raycast(ray, out float distance))
            {
                Vector3 aimDirection = ray.GetPoint(distance) - transform.position;
                aimDirection.y = 0f;
                aimDistance = aimDirection.magnitude;
                //Cursor right on top of the player gives no usable direction
                if (aimDirection.sqrMagnitude > 0.01f)
                {
                    return aimDirection.normalized;
                }
            }
        }
        return transform.forward;
    }

    private void GameInput_OnReloadAction(object sender, EventArgs e)
    {
        if (!GameManager.Instance.IsGamePlaying())
            return;
        ReloadServerRpc();
    }

    private void GameInput_OnWeaponSwapAction(object sender, EventArgs e)
    {
        if (!GameManager.Instance.IsGamePlaying())
            return;
        SwapWeaponServerRpc();
    }

    //---------------SHOOT-----------------
    [Rpc(SendTo.Server)]
    private void ShootServerRpc(Vector3 direction, float aimDistance)
    {
        direction.y = 0f;
        direction.Normalize();

        if (!activeWeapon.ShootWeapon(transform.position, direction, aimDistance, Time.time, out int hitCount))
            return;

        ShootClientRpc(activeWeaponIndex, transform.position, direction, aimDistance, hitCount, activeWeapon.AmmoInClip);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ShootClientRpc(int weaponIndex, Vector3 origin, Vector3 direction, float aimDistance, int hitCount, int ammoInClip)
    {
        Weapon weapon = weaponList[weaponIndex];
        weapon.SyncAmmo(ammoInClip);

        OnWeaponShoot?.Invoke(new WeaponShootData
        {
            weapon = weapon,
            origin = origin,
            direction = direction,
            distance = weapon.GetTravelDistance(aimDistance),
            hitCount = hitCount
        });
        OnAmmoChanged?.Invoke(activeWeapon.GetAmmoData());
    }

    //---------------RELOAD-----------------
    [Rpc(SendTo.Server)]
    private void ReloadServerRpc()
    {
        if (!player.HasKitchenObject())
        {
            ReloadFailedOwnerRpc(ReloadFailReason.HoldingNothing);
            return;
        }
        if (activeWeapon.IsClipFull)
        {
            ReloadFailedOwnerRpc(ReloadFailReason.ClipFull);
            return;
        }

        KitchenObject kitchenObject = player.GetKitchenObject();
        foreach (AmmoSource ammoSource in ammoSourceList)
        {
            if (ammoSource.kitchenObjectSO == kitchenObject.GetKitchenObjectSO())
            {
                activeWeapon.Reload(ammoSource.shells, Time.time);
                KitchenObject.DestroyKitchenObject(kitchenObject);
                ReloadClientRpc(activeWeaponIndex, activeWeapon.AmmoInClip);
                return;
            }
        }

        ReloadFailedOwnerRpc(ReloadFailReason.NotAmmo);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ReloadClientRpc(int weaponIndex, int ammoInClip)
    {
        weaponList[weaponIndex].SyncAmmo(ammoInClip);
        OnWeaponReloaded?.Invoke();
        OnAmmoChanged?.Invoke(activeWeapon.GetAmmoData());
    }

    [Rpc(SendTo.Owner)]
    private void ReloadFailedOwnerRpc(ReloadFailReason reason)
    {
        OnReloadFailed?.Invoke(reason);
    }

    //---------------SWAP-----------------
    [Rpc(SendTo.Server)]
    private void SwapWeaponServerRpc()
    {
        SwapWeaponClientRpc((activeWeaponIndex + 1) % weaponList.Count);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SwapWeaponClientRpc(int weaponIndex)
    {
        activeWeaponIndex = weaponIndex;
        activeWeapon = weaponList[weaponIndex];
        OnActiveWeaponChanged?.Invoke(activeWeapon);
        OnAmmoChanged?.Invoke(activeWeapon.GetAmmoData());
    }

    //---------------PUBLIC API-----------------
    //For listeners to read the initial state once when they subscribe; after that, events only
    public Weapon GetActiveWeapon()
    {
        return activeWeapon;
    }
}
