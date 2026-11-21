
using System;

using UnityEngine;


/// <summary>
/// <br/>   Applied to component classes.
/// <br/>   Hides other components of given type.
/// </summary>'
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class HideComponentAttribute : PropertyAttribute
{
    public readonly Type[] TypesToHide = new Type[0];
    public HideComponentAttribute(params Type[] typesToHide) => TypesToHide = typesToHide;
}
