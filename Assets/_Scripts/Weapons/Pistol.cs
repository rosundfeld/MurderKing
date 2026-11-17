using UnityEngine;

//Long thin line straight ahead
public class Pistol : Weapon
{
    private readonly PistolSO pistolSO;

    public Pistol(PistolSO pistolSO, IWeaponTarget target) : base(pistolSO, target)
    {
        this.pistolSO = pistolSO;
    }

    protected override int ResolveHits(Vector3 origin, Vector3 direction, float aimDistance)
    {
        //shortcut: the line pierces every customer in it, a single-target hit needs a new method in IWeaponTarget
        return Blast(origin, direction, pistolSO.range, pistolSO.aimAssistAngle);
    }
}
