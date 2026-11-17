using UnityEngine;

//Immutable blueprint of a weapon. Runtime state lives in the Weapon it creates, never here.
public abstract class WeaponSO : ScriptableObject
{
    public string weaponName;
    public Sprite weaponIcon;

    [Header("Ammo")]
    [Min(1)] public int clipSize = 6;
    [Min(0)] public int startingAmmo = 6;
    [Tooltip("Seconds without shooting after a reload")]
    [Min(0f)] public float reloadTime;

    [Header("Shot")]
    [Tooltip("Shots per second")]
    [Min(0.01f)] public float fireRate = 1.4f;
    public float range = 7f;
    public float knockbackForce = 16f;
    [Tooltip("Hunger removed per hit (the bar goes up to CustomerManager.RAGE_MAX = 120). Anger is not affected; " +
             "the customer only leaves when hunger reaches 0. Default 120 = one hit always feeds.")]
    [Min(0f)] public float hungerReduction = 120f;

    [Header("Visuals")]
    [Min(1)] public int pelletCount = 1;
    public float pelletSpreadAngle;

    //Factory method: each concrete SO knows which runtime Weapon it describes
    public abstract Weapon CreateWeapon(IWeaponTarget target);
}
