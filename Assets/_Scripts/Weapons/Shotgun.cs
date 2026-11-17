using UnityEngine;

//GDD section 8 - ShotDog: short range cone, high knockback, low capacity
public class Shotgun : Weapon
{
    private readonly ShotgunSO shotgunSO;

    public Shotgun(ShotgunSO shotgunSO, IWeaponTarget target) : base(shotgunSO, target)
    {
        this.shotgunSO = shotgunSO;
    }

    protected override int ResolveHits(Vector3 origin, Vector3 direction, float aimDistance)
    {
        return Blast(origin, direction, shotgunSO.range, shotgunSO.coneHalfAngle);
    }
}
