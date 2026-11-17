using UnityEngine;

//Explodes where the rocket lands, feeding everyone around the impact point
public class RocketLauncher : Weapon
{
    private readonly RocketLauncherSO rocketLauncherSO;

    public RocketLauncher(RocketLauncherSO rocketLauncherSO, IWeaponTarget target) : base(rocketLauncherSO, target)
    {
        this.rocketLauncherSO = rocketLauncherSO;
    }

    protected override int ResolveHits(Vector3 origin, Vector3 direction, float aimDistance)
    {
        Vector3 impactPoint = origin + direction * GetTravelDistance(aimDistance);
        float halfAngle = rocketLauncherSO.explosionAngle / 2f;

        return Blast(impactPoint, direction, rocketLauncherSO.explosionRadius, halfAngle);
    }

    //Lands on the cursor, but never beyond the weapon's range
    public override float GetTravelDistance(float aimDistance)
    {
        //Max before Min: aimDistance comes from the client, and this order also turns a NaN into 0
        return Mathf.Min(Mathf.Max(aimDistance, rocketLauncherSO.minimumRange), rocketLauncherSO.range);
    }
}
