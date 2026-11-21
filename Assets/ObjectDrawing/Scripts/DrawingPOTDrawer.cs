
#if UNITY_EDITOR
using ObjectDrawing;
using UnityEditor;
#endif

using UnityEngine;

using Utility;
using UtilityEditor;


/// <summary> Same as <see cref="POTAttribute"/>, but uses min and max texture sizes of <see cref="DrawingSystem"/>. </summary>
public class DrawingPOTAttribute : POTAttribute { }


#if UNITY_EDITOR

/// <summary>
/// <br/>   Gets min and max values from:
/// <br/>   [<see cref="SerializeField"/>] private <see cref="DrawingSystem"/> drawingSystem;
/// </summary>
[CustomPropertyDrawer(typeof(DrawingPOTAttribute))]
public class DrawingPOTDrawer : POTDrawer
{
    protected override bool TryGetMinMax(SerializedProperty property, PropertyAttribute attribute, out Vector2Int range)
    {
        range = default;

        if (DrawingSystem.Instance == null) { return false; }

        if (attribute is DrawingPOTAttribute)
        {
            range = new(DrawingSystem.Instance.MinTextureSize, DrawingSystem.Instance.MaxTextureSize);
            return true;
        }

        return false;
    }
}

#endif
