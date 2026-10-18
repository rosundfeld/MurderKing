using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "BurningRecipe", menuName = "ScriptableObjects/BurningRecipeSO", order = 1)]
public class BurningRecipeSO : ScriptableObject
{
    public KitchenObjectSO input;
    public KitchenObjectSO output;
    public float burningTimerMax;
}
