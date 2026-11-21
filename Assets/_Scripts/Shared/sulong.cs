
using System;
using UnityEngine;


/// <summary> Serializable <see cref="ulong"/>. </summary>
[Serializable]
public class sulong : ISerializationCallbackReceiver
{
    [SerializeField] private string serializedValue = "";

    private ulong value = 0;


    public sulong(ulong value) => this.value = value;

    // Implicitly casting sulong to regular ulong let's it be used wherever ulong is used
    public static implicit operator ulong(sulong c) => c.value;

    // Implicitly casting sulong from regular ulong allows assigning ulong values to it
    public static implicit operator sulong(ulong value) => new(value);


    public void OnBeforeSerialize() => serializedValue = value.ToString();
    public void OnAfterDeserialize() => ulong.TryParse(serializedValue, out value);

    public override string ToString() => value.ToString();

    public override bool Equals(object obj)
    {
        if (obj is sulong other) { return value == other.value; }
        if (obj is ulong ulongValue) { return value == ulongValue; }
        return false;
    }

    public override int GetHashCode() => value.GetHashCode();
}