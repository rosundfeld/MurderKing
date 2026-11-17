using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeliveryManagerUI : MonoBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private Transform recipeTemplate;

    private void Awake()
    {
        recipeTemplate.gameObject.SetActive(false);
    }

    private void Start()
    {
        //In rush mode the orders are the customers waiting at the counter, in arrival order
        if (CustomerManager.Instance != null)
        {
            CustomerManager.Instance.OnOrderListChanged += CustomerManager_OnOrderListChanged;
        }
        else
        {
            DeliveryManager.Instance.OnRecipeSpawned += DeliveryManager_OnRecipeSpawned;
            DeliveryManager.Instance.OnRecipeCompleted += DeliveryManager_OnRecipeCompleted;
        }

        UpdateVisual();
    }

    private void CustomerManager_OnOrderListChanged(object sender, System.EventArgs e)
    {
        UpdateVisual();
    }

    private void DeliveryManager_OnRecipeSpawned(object sender, System.EventArgs e)
    {
        UpdateVisual();
    }

    private void DeliveryManager_OnRecipeCompleted(object sender, System.EventArgs e)
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        foreach (Transform child in container)
        {
            if (child == recipeTemplate) continue;
            Destroy(child.gameObject);
        }

        List<RecipeSO> recipeSOList = CustomerManager.Instance != null
            ? CustomerManager.Instance.GetOrderRecipeSOList()
            : DeliveryManager.Instance.GetWaitingRecipeSOList();

        foreach (RecipeSO recipeSO in recipeSOList)
        {
            Transform recipeTransform = Instantiate(recipeTemplate, container);
            recipeTransform.gameObject.SetActive(true);
            recipeTransform.GetComponent<DeliveryManagerSingleUI>().SetRecipeSO(recipeSO);
        }
    }
}
