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
    }

    public bool Equals(CustomerData other)
    {
        return id == other.id && position == other.position && mood == other.mood && phase == other.phase
            && recipeIndex == other.recipeIndex && rage == other.rage && stunned == other.stunned;
    }
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
