using UnityEngine;

//A table where customers sit and wait for their order (one seat per CustomerPoint).
//Singleplayer: the DeliveryCounter feeds them directly; interacting here with a plate hands it to the nearest customer.
//Table service (multiplayer / dev toggle): a waiter brings the dishes from the DeliveryCounter and serves them here.
//Needs a NetworkObject (BaseCounter is a NetworkBehaviour) and a collider on the Counters layer to be selectable.
public class CustomerTable : BaseCounter
{
    [Tooltip("Seats. Customers sitting here always face the center of these points")]
    [SerializeField] private Transform[] customerPoints;

    public override void Interact(Player player)
    {
        CustomerManager customerManager = CustomerManager.Instance;
        if (customerManager == null)
        {
            return;
        }

        if (customerManager.IsTableServiceMode())
        {
            customerManager.ServeTable(this, player);
        }
        else if (player.HasKitchenObject() && player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject))
        {
            //Same as handing the plate in open space: the table just happens to be selected
            customerManager.DeliverPlate(plateKitchenObject, player, true);
        }
    }

    public Transform[] GetCustomerPoints()
    {
        return customerPoints;
    }

    public Vector3 GetCenter()
    {
        Vector3 sum = Vector3.zero;
        foreach (Transform customerPoint in customerPoints)
        {
            sum += customerPoint.position;
        }
        return customerPoints.Length > 0 ? sum / customerPoints.Length : transform.position;
    }
}
