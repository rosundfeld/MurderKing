using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "KitchenObject", menuName = "ScriptableObjects/KitchenObjectSO", order = 1)]
public class KitchenObjectSO : ScriptableObject
{
    public Transform prefab;
    public Sprite icon;
    public string objectName;

}
