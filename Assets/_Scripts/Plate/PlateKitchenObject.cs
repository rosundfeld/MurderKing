using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class PlateKitchenObject : KitchenObject
{

    public event EventHandler<OnIngredientAddedEventArgs> OnIngredientAdded;
    public class OnIngredientAddedEventArgs : EventArgs
    {
        public KitchenObjectSO kitchenObjectSO;
    }
    private List<KitchenObjectSO> kitchenObjectSOList;
    [SerializeField] private List<KitchenObjectSO> validKitchenObjectSOList;

    protected override void Awake()
    {
        base.Awake();
        kitchenObjectSOList = new List<KitchenObjectSO>();
    }

    public bool TryAddIngredient(KitchenObjectSO kitchenObjectSO)
    {
        //Add ingredient to plate
        if (!validKitchenObjectSOList.Contains(kitchenObjectSO))
        {
            Debug.LogError("KitchenObjectSO is not valid for plate: " + kitchenObjectSO);
            return false;
        }

        if (kitchenObjectSOList.Contains(kitchenObjectSO))
        {
            Debug.LogError("KitchenObjectSO is not valid for plate: " + kitchenObjectSO);
            return false;
        }

        AddIngredientServerRpc(
            GameManagerMultiplayer.Instance.GetKitchenObjectSOIndex(kitchenObjectSO)
        );

        return true;
    }

    [Rpc(SendTo.Server)]
    private void AddIngredientServerRpc(int kitchenObjectSOIndex)
    {
        AddIngredientServerRpcClientRpc(kitchenObjectSOIndex);
    }

    [ClientRpc]
    private void AddIngredientServerRpcClientRpc(int kitchenObjectSOIndex)
    {
        KitchenObjectSO kitchenObjectSO = GameManagerMultiplayer.Instance.GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);
        kitchenObjectSOList.Add(kitchenObjectSO);

        OnIngredientAdded?.Invoke(this, new OnIngredientAddedEventArgs
        {
            kitchenObjectSO = kitchenObjectSO
        });
    }

    public List<KitchenObjectSO> GetKitchenObjectSOList()
    {
        return kitchenObjectSOList;
    }
}
