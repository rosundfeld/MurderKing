using UnityEditor;
using UnityEngine;

//Menu "Murder King/Self-check das armas": asserts the ammo/cooldown rules shared by every Weapon
public static class WeaponSelfCheck
{
    private class FakeTarget : IWeaponTarget
    {
        public int ApplyConeBlast(Vector3 origin, Vector3 direction, float range, float halfAngle, float knockbackForce, float hungerReduction)
        {
            return 3;
        }
    }

    [MenuItem("Murder King/Self-check das armas")]
    public static void Run()
    {
        ShotgunSO shotgunSO = ScriptableObject.CreateInstance<ShotgunSO>();
        shotgunSO.clipSize = 2;
        shotgunSO.startingAmmo = 1;
        shotgunSO.fireRate = 1f;
        shotgunSO.reloadTime = 2f;
        Weapon weapon = shotgunSO.CreateWeapon(new FakeTarget());

        Debug.Assert(weapon.ShootWeapon(Vector3.zero, Vector3.forward, 0f, 0f, out int hitCount) && hitCount == 3 && weapon.IsEmpty, "shot spends ammo and reports hits");
        Debug.Assert(!weapon.ShootWeapon(Vector3.zero, Vector3.forward, 0f, 5f, out _), "empty clip does not shoot");
        Debug.Assert(weapon.Reload(5, 5f) == 2 && weapon.IsClipFull, "reload is clamped to the clip size");
        Debug.Assert(!weapon.ShootWeapon(Vector3.zero, Vector3.forward, 0f, 6f, out _), "cannot shoot while reloading");
        Debug.Assert(weapon.ShootWeapon(Vector3.zero, Vector3.forward, 0f, 7f, out _), "shoots after the reload time");
        Debug.Assert(!weapon.ShootWeapon(Vector3.zero, Vector3.forward, 0f, 7.5f, out _), "fire rate cooldown");
        Debug.Assert(weapon.ShootWeapon(Vector3.zero, Vector3.forward, 0f, 8f, out _) && weapon.IsEmpty, "shoots after the cooldown");

        Object.DestroyImmediate(shotgunSO);
        Debug.Log("Self-check das armas terminou: sem erros de assert acima = OK");
    }
}
