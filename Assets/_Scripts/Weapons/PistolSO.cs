using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Pistol")]
public class PistolSO : WeaponSO
{
    [Header("Pistol")]
    [Tooltip("Half angle of the thin line the bullet hits, so shots don't need pixel perfect aim")]
    public float aimAssistAngle = 5f;

    public override Weapon CreateWeapon(IWeaponTarget target)
    {
        return new Pistol(this, target);
    }
}
