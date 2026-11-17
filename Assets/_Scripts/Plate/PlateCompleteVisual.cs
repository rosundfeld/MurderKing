using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlateCompleteVisual : MonoBehaviour
{
    [Serializable]
    public struct KitchenObjectSO_GameObject
    {
        public KitchenObjectSO kitchenObjectSO;
        public GameObject gameObject;
    }
    [Tooltip("Empty on visual-only copies (served dish stack), which are filled through ShowIngredients")]
    [SerializeField] private PlateKitchenObject plateKitchenObject;
    [SerializeField] private List<KitchenObjectSO_GameObject> kitchenObjectSOGameObjectList;

    //Awake, not Start: ShowIngredients may be called right after Instantiate, before Start would run
    private void Awake()
    {
        foreach (KitchenObjectSO_GameObject kitchenObjectSO_GameObject in kitchenObjectSOGameObjectList)
        {
            kitchenObjectSO_GameObject.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (plateKitchenObject != null)
        {
            plateKitchenObject.OnIngredientAdded += PlateKitchenObject_OnIngredientAdded;
        }
    }

    private void PlateKitchenObject_OnIngredientAdded(object sender, PlateKitchenObject.OnIngredientAddedEventArgs e)
    {
        ShowIngredient(e.kitchenObjectSO);
    }

    //Builds the finished dish without a networked plate (e.g. from RecipeSO.kitchenObjectSOList)
    public void ShowIngredients(List<KitchenObjectSO> kitchenObjectSOList)
    {
        foreach (KitchenObjectSO kitchenObjectSO in kitchenObjectSOList)
        {
            ShowIngredient(kitchenObjectSO);
        }
    }

    private void ShowIngredient(KitchenObjectSO kitchenObjectSO)
    {
        foreach (KitchenObjectSO_GameObject kitchenObjectSO_GameObject in kitchenObjectSOGameObjectList)
        {
            if (kitchenObjectSO_GameObject.kitchenObjectSO == kitchenObjectSO)
            {
                kitchenObjectSO_GameObject.gameObject.SetActive(true);
            }
        }
    }
}
