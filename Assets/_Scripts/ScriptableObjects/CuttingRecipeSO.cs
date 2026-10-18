using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CuttingRecipe", menuName = "ScriptableObjects/CuttingRecipeSO", order = 1)]
public class CuttingRecipeSO : ScriptableObject
{
    public KitchenObjectSO input;
    public KitchenObjectSO output;
    public int cuttingProgressMax;
}
