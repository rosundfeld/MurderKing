using System.Collections.Generic;

//On the DeliveryCounter: dishes delivered by the cook, waiting for a waiter to pick them up
public class DeliveryCounterDishesVisual : DishStackVisual
{
    protected override void GetDishes(CustomerManager customerManager, List<CustomerManager.DishInfo> resultList)
    {
        customerManager.GetCounterDishes(resultList);
    }
}
