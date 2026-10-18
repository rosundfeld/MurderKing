using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class ContainerCounter : BaseCounter
{

    public event EventHandler OnPlayerGrabbedObject;
    public static event EventHandler OnAnyObjectGrabbed;
    [SerializeField] private KitchenObjectSO kitchenObjectSO;


    // Start is called before the first frame update
    public override void Interact(Player player)
    {
        if (!player.HasKitchenObject())
        {
            //Player is not carrying something
            KitchenObject.SpawnKitchenObject(kitchenObjectSO, player);


            InteractLogicServerRpc();
        }

    }

    [Rpc(SendTo.Server)]
    private void InteractLogicServerRpc()
    {
        InteractLogicClientRpc();
    }

    [ClientRpc]
    private void InteractLogicClientRpc()
    {
        OnPlayerGrabbedObject?.Invoke(this, EventArgs.Empty);
        OnAnyObjectGrabbed?.Invoke(this, EventArgs.Empty);
    }
}
