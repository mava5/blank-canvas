
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif

using System.Reflection;
using System;

using UnityEngine;


/// <summary>
/// <br/>   Applied to serialized fields.
/// <br/>   Trims the label up to the last underscore "_".
/// <br/>   For example "PlayerLight_Color" -> "Color".
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class TrimmedLabelAttribute : PropertyAttribute
{
    public static bool TryProcessLabel(FieldInfo fieldInfo, GUIContent label)
    {
#if UNITY_EDITOR
        TrimmedLabelAttribute attribute = fieldInfo.GetCustomAttributes<TrimmedLabelAttribute>().FirstOrDefault();
        if (attribute != null) { attribute.ProcessLabel(fieldInfo, label); }
#endif
        return false;
    }

    public static string GetProcessedName(string inputName)
    {
        string name = inputName;

        int lastUnderscoreIndex = name.LastIndexOf('_');
        if (lastUnderscoreIndex >= 0 && lastUnderscoreIndex < name.Length - 1)
        {
            name = name[(lastUnderscoreIndex + 1)..];

            if (name.Length > 0)
            { name = char.ToUpper(name[0]) + name[1..]; }
        }

        return name;
    }

    public void ProcessLabel(FieldInfo fieldInfo, GUIContent label)
    {
#if UNITY_EDITOR
        label.text = ObjectNames.NicifyVariableName(GetProcessedName(fieldInfo.Name));
#endif
    }
}
