using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Shotgun")]
public class ShotgunSO : WeaponSO
{
    [Header("Shotgun")]
    public float coneHalfAngle = 30f;

    public override Weapon CreateWeapon(IWeaponTarget target)
    {
        return new Shotgun(this, target);
    }
}
