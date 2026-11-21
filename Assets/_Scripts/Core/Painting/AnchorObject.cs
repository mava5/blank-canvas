
#if UNITY_EDITOR
using UnityEditor;
#endif

using System;

using UnityEngine;

using Utility;
using UtilityEditor;
using System.Collections.Generic;
using System.Linq;


public class AnchorObject : MonoBehaviour
{
    [SerializeField, Range(0.01f, 50f)] private float radius = 1f;

    private void Awake()
    {
        Shader.SetGlobalVector("_G_AnchorInfo", GetAnchorInfo());
    }

    internal Vector4 GetAnchorInfo() => new(transform.position.x, transform.position.y, transform.position.z, radius);


#if UNITY_EDITOR

    [NonSerialized] private static List<GameObject> objects = new();
    [NonSerialized] private bool connected = false;

    private void OnValidate()
    {
        if (connected) { return; }

        connected = true;
        objects.Add(gameObject);
        
        for (int i = objects.Count - 1; i >= 0; i--)
        { if (objects[i] == null) { objects.RemoveAt(i); } }

        if (objects.Count > 1)
        {
            string names = string.Join(", ", objects.Select(o => o.name));
            Util.LogError<AnchorObject>($"Multiple anchor objects in scene: {names}");
            return;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new(0.8f, 0.7f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.color = Color.white;
    }

#endif
}


#if UNITY_EDITOR

[CustomEditor(typeof(AnchorObject))]
internal class AnchorObjectEditor : AdvancedEditor
{
    private AnchorObject anchorObject = null;

    private Vector3 lastPosition = Vector3.zero;


    protected override void OnEnableInternal()
    {
        anchorObject = (AnchorObject)target;
    }

    protected void OnSceneGUI()
    {
        if ((anchorObject.transform.position - lastPosition).magnitude < 0.001f) { return; }

        lastPosition = anchorObject.transform.position;
        Shader.SetGlobalVector("_G_AnchorInfo", anchorObject.GetAnchorInfo());
    }

    protected override void OnInspectorGUIBegin()
    {
        EditorGUI.BeginChangeCheck();
    }

    protected override void OnInspectorGUIEnd()
    {
        if (!EditorGUI.EndChangeCheck()) { return; }

        Shader.SetGlobalVector("_G_AnchorInfo", anchorObject.GetAnchorInfo());
    }
}

#endif
