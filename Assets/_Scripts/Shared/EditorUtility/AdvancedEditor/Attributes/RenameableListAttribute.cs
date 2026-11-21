
using System;


/// <summary>
/// <br/>   Applied to <see cref="List{T}"/> or arrays.
/// <br/>   Changes the elements' titles to text fields pointing to:
/// <br/>   [<see cref="SerializeField"/>, <see cref="HideInInspector"/>] private string name = "";
/// <br/>   
/// <br/>   <see cref="HideInInspector"/> is optional but avoids duplicate text fields for name.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class RenameableListAttribute : Attribute { }
