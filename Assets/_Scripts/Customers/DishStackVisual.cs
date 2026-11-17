using System;
using System.Collections.Generic;
using UnityEngine;

//Base for the table service dish stacks: rebuilds a pile of ServedDishVisual whenever the dishes change.
//Subclasses only decide WHICH dishes belong to this stack (DeliveryCounterDishesVisual, PlayerDishStackVisual).
public abstract class DishStackVisual : MonoBehaviour
{
    [SerializeField] private Transform stackPoint;
    [SerializeField] private ServedDishVisual dishVisualPrefab;
    [SerializeField] private float offsetY = 0.1f;

    private readonly List<CustomerManager.DishInfo> dishInfoList = new List<CustomerManager.DishInfo>();
    private readonly List<ServedDishVisual> dishVisualList = new List<ServedDishVisual>();

    protected abstract void GetDishes(CustomerManager customerManager, List<CustomerManager.DishInfo> resultList);

    private void Start()
    {
        if (CustomerManager.Instance == null)
        {
            return;
        }
        CustomerManager.Instance.OnDishesChanged += CustomerManager_OnDishesChanged;
        UpdateVisual();
    }

    private void OnDestroy()
    {
        //The player prefab is despawned during the match, the manager outlives it
        if (CustomerManager.Instance != null)
        {
            CustomerManager.Instance.OnDishesChanged -= CustomerManager_OnDishesChanged;
        }
    }

    private void CustomerManager_OnDishesChanged(object sender, EventArgs e)
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        foreach (ServedDishVisual dishVisual in dishVisualList)
        {
            Destroy(dishVisual.gameObject);
        }
        dishVisualList.Clear();

        GetDishes(CustomerManager.Instance, dishInfoList);

        foreach (CustomerManager.DishInfo dishInfo in dishInfoList)
        {
            ServedDishVisual dishVisual = Instantiate(dishVisualPrefab, stackPoint);
            dishVisual.transform.localPosition = new Vector3(0f, offsetY * dishVisualList.Count, 0f);
            dishVisual.SetDish(dishInfo);
            dishVisualList.Add(dishVisual);
        }
    }
}
