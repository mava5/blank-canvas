
#if UNITY_EDITOR
using System.Linq;
#endif

using System;
using System.Reflection;

using UnityEngine;


/// <summary>
/// <br/>   Applied to serialized fields.
/// <br/>   Replaces the drawer's label with given one.
/// </summary>'
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class LabelAttribute : PropertyAttribute
{
    public readonly string Label;
    public LabelAttribute(string label) => Label = label;

    public static bool TryProcessLabel(FieldInfo fieldInfo, GUIContent label)
    {
#if UNITY_EDITOR
        LabelAttribute attribute = fieldInfo.GetCustomAttributes<LabelAttribute>().FirstOrDefault();
        if (attribute != null) { attribute.ProcessLabel(label); return true; }
#endif
        return false;
    }

    public void ProcessLabel(GUIContent label)
    { label.text = Label; }
}