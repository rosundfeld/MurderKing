using System;
using Unity.Netcode;
using UnityEngine;

//Snapshot of a customer replicated to every client through CustomerManager's NetworkList.
//The server keeps the full simulation state; clients only render this.
public struct CustomerData : INetworkSerializable, IEquatable<CustomerData>
{
    public ushort id;
    public Vector3 position;
    public byte mood;          //CustomerMood
    public byte phase;         //CustomerPhase
    public byte recipeIndex;
    public byte rage;          //hunger + anger, 0..CustomerManager.RAGE_MAX
    public bool stunned;
    public ushort orderTicket; //0 = hasn't reached the counter yet; otherwise arrival order
    public byte slot;          //seat index, NO_SLOT when standing (overflow) or leaving

    public const byte NO_SLOT = byte.MaxValue;

    public CustomerMood Mood => (CustomerMood)mood;
    public CustomerPhase Phase => (CustomerPhase)phase;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref id);
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref mood);
        serializer.SerializeValue(ref phase);
        serializer.SerializeValue(ref recipeIndex);
        serializer.SerializeValue(ref rage);
        serializer.SerializeValue(ref stunned);
        serializer.SerializeValue(ref orderTicket);
        serializer.SerializeValue(ref slot);
    }

    public bool Equals(CustomerData other)
    {
        return id == other.id && position == other.position && mood == other.mood && phase == other.phase
            && recipeIndex == other.recipeIndex && rage == other.rage && stunned == other.stunned && orderTicket == other.orderTicket
            && slot == other.slot;
    }
}

//Multiplayer table service: a cooked dish is NOT tied to any customer.
//It waits on the DeliveryCounter, a waiter stacks it, and any table with someone who ordered that recipe takes it.
public struct DishData : INetworkSerializable, IEquatable<DishData>
{
    public ushort id;
    public byte recipeIndex;
    public byte state;    //DishState
    public ulong carrier; //clientId holding it when state == OnTray

    public DishState State => (DishState)state;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref id);
        serializer.SerializeValue(ref recipeIndex);
        serializer.SerializeValue(ref state);
        serializer.SerializeValue(ref carrier);
    }

    public bool Equals(DishData other)
    {
        return id == other.id && recipeIndex == other.recipeIndex && state == other.state && carrier == other.carrier;
    }
}

public enum DishState : byte
{
    AtCounter,  //delivered at the DeliveryCounter, waiting for a waiter
    OnTray      //on a player's stack (DishData.carrier)
}

//GDD section 4: the states a hungry customer goes through
public enum CustomerMood : byte
{
    Normal,
    Faminto,
    Desesperado,
    Furioso,
    Colapso
}

public enum CustomerPhase : byte
{
    WalkingIn,
    Waiting,
    Attacking,
    Leaving
}
