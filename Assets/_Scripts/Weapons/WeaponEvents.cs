using UnityEngine;

//Payloads of the PlayerGun events: the only information UI and visuals receive
public struct WeaponShootData
{
    public Weapon weapon;
    public Vector3 origin;
    public Vector3 direction;
    public float distance;
    public int hitCount;
}

public struct AmmoData
{
    public int ammoInClip;
    public int clipSize;
}

public enum ReloadFailReason : byte
{
    HoldingNothing,
    ClipFull,
    NotAmmo
}
