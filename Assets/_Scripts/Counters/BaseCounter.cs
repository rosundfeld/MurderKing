using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;

public class BaseCounter : NetworkBehaviour, IKitchenObjectParent
{
    //---------------FIELDS-----------------
    [SerializeField] private Transform counterTopPoint;

    //---------------EVENTS-----------------
    public static event EventHandler OnAnyObjectPlacedHere;

    //---------------PRIVATE VARIABLES-----------------
    private KitchenObject kitchenObject;

    //---------------PUBLIC API-----------------
    public static void ResetStaticData()
    {
        OnAnyObjectPlacedHere = null;
    }

    public virtual void Interact(Player player)
    {
        Debug.Log("BaseCounter Interact");
    }
    public virtual void InteractAlternate(Player player)
    {
        //Debug.Log("BaseCounter Interact Alternate");
    }

    // ----------------IKitchenObjectParent Implementation-----------------
    public Transform GetKitchenObjectFollowTransform()
    {
        return counterTopPoint;
    }

    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        this.kitchenObject = kitchenObject;
        if (kitchenObject != null)
        {
            OnAnyObjectPlacedHere?.Invoke(this, EventArgs.Empty);
        }
    }
    public KitchenObject GetKitchenObject()
    {
        return kitchenObject;
    }

    public void ClearKitchenObject()
    {
        kitchenObject = null;
    }

    public bool HasKitchenObject()
    {
        return kitchenObject != null;
    }

    public NetworkObject GetNetworkObject()
    {
        return NetworkObject;
    }
}
