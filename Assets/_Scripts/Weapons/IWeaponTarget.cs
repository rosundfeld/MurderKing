using UnityEngine;

//What a weapon can hit. Implemented by CustomerManager, so weapons never depend on it directly.
public interface IWeaponTarget
{
    //Pushes everything inside the cone and removes hungerReduction of hunger; returns how many were fully fed
    int ApplyConeBlast(Vector3 origin, Vector3 direction, float range, float halfAngle, float knockbackForce, float hungerReduction);
}
