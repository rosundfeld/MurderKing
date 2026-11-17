using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Rocket Launcher")]
public class RocketLauncherSO : WeaponSO
{
    [Header("Rocket Launcher")]
    public float explosionRadius = 4f;
    public float explosionAngle = 360f;
    public float minimumRange = 2f;

    public override Weapon CreateWeapon(IWeaponTarget target)
    {
        return new RocketLauncher(this, target);
    }
}
