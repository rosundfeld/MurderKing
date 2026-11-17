using UnityEngine;

//Runtime state of one weapon. The server owns the truth; clients only mirror the ammo through SyncAmmo.
public abstract class Weapon
{
    public WeaponSO Data { get; }
    public int AmmoInClip { get; private set; }
    public bool IsEmpty => AmmoInClip <= 0;
    public bool IsClipFull => AmmoInClip >= Data.clipSize;

    private readonly IWeaponTarget target;
    private float nextShootTime;

    protected Weapon(WeaponSO data, IWeaponTarget target)
    {
        Data = data;
        this.target = target;
        AmmoInClip = Mathf.Min(data.startingAmmo, data.clipSize);
    }

    //Server only. Returns false when the weapon could not shoot (empty, cooling down or reloading).
    //aimDistance is how far from the shooter the cursor was; most weapons ignore it.
    public bool ShootWeapon(Vector3 origin, Vector3 direction, float aimDistance, float time, out int hitCount)
    {
        hitCount = 0;
        if (IsEmpty || time < nextShootTime)
        {
            return false;
        }

        AmmoInClip--;
        nextShootTime = time + 1f / Data.fireRate;
        hitCount = ResolveHits(origin, direction, aimDistance);
        return true;
    }

    //How far the projectile flies, so visuals match what the shot actually hit
    public virtual float GetTravelDistance(float aimDistance)
    {
        return Data.range;
    }

    //The only thing that changes from weapon to weapon: what the shot hits
    protected abstract int ResolveHits(Vector3 origin, Vector3 direction, float aimDistance);

    //Server only. Returns how much ammo actually fit in the clip.
    public int Reload(int amount, float time)
    {
        int ammoBefore = AmmoInClip;
        AmmoInClip = Mathf.Min(Data.clipSize, AmmoInClip + amount);
        nextShootTime = Mathf.Max(nextShootTime, time + Data.reloadTime);
        return AmmoInClip - ammoBefore;
    }

    public void SyncAmmo(int ammoInClip)
    {
        AmmoInClip = ammoInClip;
    }

    public AmmoData GetAmmoData()
    {
        return new AmmoData { ammoInClip = AmmoInClip, clipSize = Data.clipSize };
    }

    protected int Blast(Vector3 origin, Vector3 direction, float range, float halfAngle)
    {
        //No target in scenes without the rush (e.g. testing the kitchen alone)
        return target == null ? 0 : target.ApplyConeBlast(origin, direction, range, halfAngle, Data.knockbackForce, Data.hungerReduction);
    }
}
