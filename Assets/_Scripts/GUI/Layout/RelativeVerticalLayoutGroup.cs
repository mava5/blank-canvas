
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
#endif

using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// <br/>   Adds relative padding to top and bottom.
/// <br/>   Also adds relative spacing between elements.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class RelativeVerticalLayoutGroup : VerticalLayoutGroup
{
    public RectTransform Target = null;

    [Range(-1f, 1f)] public float TopWidthRelative = 0f;
    [Range(-1f, 1f)] public float TopHeightRelative = 0f;
    [Range(-1f, 1f)] public float MiddleWidthRelative = 0f;
    [Range(-1f, 1f)] public float MiddleHeightRelative = 0f;
    [Range(-1f, 1f)] public float BottomWidthRelative = 0f;
    [Range(-1f, 1f)] public float BottomHeightRelative = 0f;

    public override void CalculateLayoutInputVertical()
    {
        Vector2 size = Vector2.zero;

        if (Target != null) { size = Target.rect.size; }
        else if (transform.parent is RectTransform parent) { size = parent.rect.size; }

        padding.top = Mathf.RoundToInt(size.x * TopWidthRelative + size.y * TopHeightRelative);
        spacing = size.x * MiddleWidthRelative + size.y * MiddleHeightRelative;
        padding.bottom = Mathf.RoundToInt(size.x * BottomWidthRelative + size.y * BottomHeightRelative);

        base.CalculateLayoutInputVertical();
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(RelativeVerticalLayoutGroup))]
public class RelativeVerticalLayoutGroupEditor : HorizontalOrVerticalLayoutGroupEditor
{
    private SerializedProperty targetProperty = null;
    private SerializedProperty topWidthRelativeProperty = null;
    private SerializedProperty topHeightRelativeProperty = null;
    private SerializedProperty middleWidthRelativeProperty = null;
    private SerializedProperty middleHeightRelativeProperty = null;
    private SerializedProperty bottomWidthRelativeProperty = null;
    private SerializedProperty bottomHeightRelativeProperty = null;

    protected override void OnEnable()
    {
        base.OnEnable();

        targetProperty = serializedObject.FindProperty("Target");
        topWidthRelativeProperty = serializedObject.FindProperty("TopWidthRelative");
        topHeightRelativeProperty = serializedObject.FindProperty("TopHeightRelative");
        middleWidthRelativeProperty = serializedObject.FindProperty("MiddleWidthRelative");
        middleHeightRelativeProperty = serializedObject.FindProperty("MiddleHeightRelative");
        bottomWidthRelativeProperty = serializedObject.FindProperty("BottomWidthRelative");
        bottomHeightRelativeProperty = serializedObject.FindProperty("BottomHeightRelative");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using(new EditorGUI.DisabledScope(true))
        { EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script")); }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(targetProperty);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Top Padding", EditorStyles.boldLabel);
        DrawPropertyField(topWidthRelativeProperty);
        DrawPropertyField(topHeightRelativeProperty);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Middle Spacing", EditorStyles.boldLabel);
        DrawPropertyField(middleWidthRelativeProperty);
        DrawPropertyField(middleHeightRelativeProperty);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Bottom Padding", EditorStyles.boldLabel);
        DrawPropertyField(bottomWidthRelativeProperty);
        DrawPropertyField(bottomHeightRelativeProperty);
        EditorGUILayout.Space();

        serializedObject.ApplyModifiedProperties();
        base.OnInspectorGUI();
    }

    private void DrawPropertyField(SerializedProperty property)
    {
        string name = property.name[1..];
        for (int i = 0; i < property.name.Length - 1; i++)
        { if (char.IsLower(name[0])) { name = name.Remove(0, 1); } else { break; } }

        EditorGUILayout.PropertyField(property, new GUIContent(name));
    }
}
#endif
