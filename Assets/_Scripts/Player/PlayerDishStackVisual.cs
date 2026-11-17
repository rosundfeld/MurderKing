using System.Collections.Generic;
using UnityEngine;

//On the Player prefab: the dishes THIS player is carrying.
//Every Player instance has its own copy pointing to itself, so each client sees every waiter's stack in the right hands.
//Who picks a dish up is decided by the Interact (server uses the RPC sender), not by this field.
public class PlayerDishStackVisual : DishStackVisual
{
    [Tooltip("The Player this stack belongs to (the root of this same prefab)")]
    [SerializeField] private Player player;

    protected override void GetDishes(CustomerManager customerManager, List<CustomerManager.DishInfo> resultList)
    {
        customerManager.GetTrayDishes(player.OwnerClientId, resultList);
    }
}
