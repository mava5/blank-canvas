
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
#endif

using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// <br/>   Doesn't draw anything but acts as a raycast target.
/// <br/>   Code from: <see href="https://discussions.unity.com/t/ui-panel-without-image-component-as-raycast-target-it-is-possible/152401/4"/>
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class InvisibleRaycastTarget : Graphic
{
    public override void SetMaterialDirty() { }
    public override void SetVerticesDirty() { }
    protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();
}


#if UNITY_EDITOR

[CanEditMultipleObjects, CustomEditor(typeof(InvisibleRaycastTarget), false)]
public class InvisibleRaycastTargetEditor : GraphicEditor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
        { EditorGUILayout.PropertyField(m_Script); }
        serializedObject.FindProperty("m_RaycastTarget").boolValue = true;

        serializedObject.ApplyModifiedProperties();
    }
}

#endif
