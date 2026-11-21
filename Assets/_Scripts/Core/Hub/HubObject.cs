
#if UNITY_EDITOR
using UnityEditor;
using UtilityEditor;
#endif

using System;
using System.Linq;

using UnityEngine;


[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
[HideComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class HubObject : MonoBehaviour
{
    [SerializeField] protected bool showHiddenComponents = false;

    [SerializeField, HideInInspector] private bool usesUniqueTexture = false;
    [SerializeField, HideInInspector] private Mesh mesh = null;
    [SerializeField, HideInInspector] private Material material = null;
    [SerializeField, HideInInspector] private string uniqueTexturePath = "";
    [SerializeField, HideInInspector] private string genericTexturePath = "";

    // Generic only fields
    [SerializeField, HideInInspector] private float materialSize = 1.0f;
    [SerializeField, HideInInspector] private Vector2 materialOffset = Vector2.zero;

    // Shared fields
    [SerializeField, HideInInspector] private Color tint = Color.white;
    [SerializeField, HideInInspector, Range(0f, 1f)] private float lightInfluence = 1.0f;
    [SerializeField, HideInInspector, Range(0f, 1f)] private float hatchingMultiplier = 1.0f;
    [SerializeField, HideInInspector] private float hatchingOffset1 = 1.0f;
    [SerializeField, HideInInspector] private float hatchingOffset2 = 1.0f;
    [SerializeField, HideInInspector, Range(0f, 1f)] private float fadingAlpha = 0f;

    private MeshFilter meshFilter = null;
    private MeshRenderer meshRenderer = null;
    private MaterialPropertyBlock mpb = null;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        mpb = new();

        meshRenderer.GetPropertyBlock(mpb);

        RefreshVisuals();
        UpdateTextureVariables();
    }

    public void SetTint(Color value) { SetValue(ref tint, value, "_Tint"); }
    public void SetLightInfluence(float value) { SetValue(ref lightInfluence, value, "_LightInfluence"); }
    public void SetHatchingMultiplier(float value) { SetValue(ref hatchingMultiplier, value, "_HatchingMultiplier"); }
    public void SetFadingAlpha(float value) { SetValue(ref fadingAlpha, value, "_FadingAlpha");}

    internal void RefreshVisuals()
    {
        MaterialPropertyBlock mpb = new();
        meshRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_MaterialSize", materialSize);
        mpb.SetVector("_MaterialOffset", materialOffset);
        mpb.SetColor("_Tint", tint);
        mpb.SetFloat("_LightInfluence", lightInfluence);
        mpb.SetFloat("_HatchingMultiplier", hatchingMultiplier);
        mpb.SetFloat("_HatchingOffset1", hatchingOffset1);
        mpb.SetFloat("_HatchingOffset2", hatchingOffset2);
        meshRenderer.SetPropertyBlock(mpb);
    }

    private void SetValue<T>(ref T value, T newValue, string name)
    {
        value = newValue;

        mpb ??= new();
        meshRenderer.GetPropertyBlock(mpb);
        switch (value)
        {
            case bool @bool: mpb.SetFloat(name, @bool ? 1f : 0f); break;
            case int @int: mpb.SetInt(name, @int); break;
            case float @float: mpb.SetFloat(name, @float); break;
            case Color @Color: mpb.SetColor(name, @Color); break;
            case Vector2 @Vector2: mpb.SetVector(name, @Vector2); break;
            case Vector3 @Vector3: mpb.SetVector(name, @Vector3); break;
            case Vector4 @Vector4: mpb.SetVector(name, @Vector4); break;
        }
        meshRenderer.SetPropertyBlock(mpb);
    }

    private void UpdateTextureVariables()
    {
        if (HubAssetManager.Instance == null) { return; }

        string texturePath = usesUniqueTexture ? uniqueTexturePath : genericTexturePath;
        HubAssetManager.Instance.SetTextureVariablesFor(meshRenderer, texturePath, usesUniqueTexture);
    }


#if UNITY_EDITOR

    [NonSerialized] private bool connected = false;

    private void OnDestroy()
    { HubAssetManager.EditorInstance.OnTexturesBaked.Always -= EditorUpdateTextureVariables; }

    private void OnValidate()
    {
        if (connected || Application.isPlaying) { return; }

        connected = true;
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        RefreshVisuals();
        EditorUpdateTextureVariables();
        HubAssetManager.EditorInstance.OnTexturesBaked.Always -= EditorUpdateTextureVariables;
        HubAssetManager.EditorInstance.OnTexturesBaked.Always += EditorUpdateTextureVariables;
    }

    private void EditorUpdateTextureVariables()
    {
        if (HubAssetManager.EditorInstance == null) { return; }
        if (usesUniqueTexture && meshFilter.sharedMesh == null) { return; }

        string texturePath = usesUniqueTexture ? uniqueTexturePath : genericTexturePath;
        HubAssetManager.EditorInstance.SetTextureVariablesFor(meshRenderer, texturePath, usesUniqueTexture);
    }

#endif
}


#if UNITY_EDITOR

[CustomEditor(typeof(HubObject))]
[CanEditMultipleObjects]
internal class HubObjectRendererEditor : AdvancedEditor
{
    private HubObject hubObject = null;
    private MeshFilter meshFilter = null;
    private MeshRenderer meshRenderer = null;

    private MaterialPropertyBlock mpb = null;

    private SerializedProperty usesUniqueTextureProperty = null;
    private SerializedProperty meshProperty = null;
    private SerializedProperty materialProperty = null;
    private SerializedProperty uniqueTexturePathProperty = null;
    private SerializedProperty genericTexturePathProperty = null;

    private SerializedProperty materialSizeProperty = null;
    private SerializedProperty materialOffsetProperty = null;

    private SerializedProperty tintProperty = null;
    private SerializedProperty lightInfluenceProperty = null;
    private SerializedProperty hatchingMultiplierProperty = null;
    private SerializedProperty hatchingOffset1Property = null;
    private SerializedProperty hatchingOffset2Property = null;
    private SerializedProperty fadingAlphaProperty = null;


    #region Lifecycle ============================================================================= Lifecycle

    protected override void OnEnableInternal()
    {
        hubObject = (HubObject)target;
        meshFilter = hubObject.GetComponent<MeshFilter>();
        meshRenderer = hubObject.GetComponent<MeshRenderer>();

        usesUniqueTextureProperty = serializedObject.FindProperty("usesUniqueTexture");
        meshProperty = serializedObject.FindProperty("mesh");
        materialProperty = serializedObject.FindProperty("material");
        uniqueTexturePathProperty = serializedObject.FindProperty("uniqueTexturePath");
        genericTexturePathProperty = serializedObject.FindProperty("genericTexturePath");

        materialSizeProperty = serializedObject.FindProperty("materialSize");
        materialOffsetProperty = serializedObject.FindProperty("materialOffset");

        tintProperty = serializedObject.FindProperty("tint");
        lightInfluenceProperty = serializedObject.FindProperty("lightInfluence");
        hatchingMultiplierProperty = serializedObject.FindProperty("hatchingMultiplier");
        hatchingOffset1Property = serializedObject.FindProperty("hatchingOffset1");
        hatchingOffset2Property = serializedObject.FindProperty("hatchingOffset2");
        fadingAlphaProperty = serializedObject.FindProperty("fadingAlpha");

        RefreshVisuals();

        Undo.undoRedoPerformed += RefreshVisuals;
    }

    protected override void OnDisableInternal()
    {
        Undo.undoRedoPerformed -= RefreshVisuals;
    }

    protected override void OnInspectorGUIEnd()
    {
        if (meshFilter == null && !hubObject.TryGetComponent(out meshFilter))
        { EditorGUILayout.LabelField("Missing MeshFilter!", centeredLabel); return; }

        if (meshRenderer == null && !hubObject.TryGetComponent(out meshRenderer))
        { EditorGUILayout.LabelField("Missing MeshRenderer!", centeredLabel); return; }

        if (HubAssetManager.EditorInstance == null)
        { EditorGUILayout.LabelField("Missing Hub Asset Manager!", centeredLabel); return; }

        bool usesUniqueTexture = DrawTextureTypeProperty();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Material:", headerLabel);
        if (usesUniqueTexture)
        {
            DrawField(meshProperty, () =>
            {
                Mesh mesh = (Mesh)meshProperty.objectReferenceValue;

                if (mesh != null)
                { uniqueTexturePathProperty.stringValue = AssetDatabase.GetAssetPath(mesh); }

                SetMesh(mesh);
                UpdateTextureOffsetVariables();
            });
        }
        else
        {
            DrawField(meshProperty, () => SetMesh((Mesh)meshProperty.objectReferenceValue));
            DrawGenericTexturePathField();
            DrawField(materialSizeProperty, () => SetMPB("_MaterialSize", materialSizeProperty.floatValue));
            DrawField(materialOffsetProperty, () => SetMPB("_MaterialOffset", materialOffsetProperty.vector2Value));
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Visuals:", headerLabel);
        DrawField(tintProperty, () => SetMPB("_Tint", tintProperty.colorValue));
        DrawField(lightInfluenceProperty, () => SetMPB("_LightInfluence", lightInfluenceProperty.floatValue));
        DrawField(hatchingMultiplierProperty, () => SetMPB("_HatchingMultiplier", hatchingMultiplierProperty.floatValue));
        //DrawField(hatchingOffset1Property, () => SetMPB("_HatchingOffset1", hatchingOffset1Property.floatValue));
        //DrawField(hatchingOffset2Property, () => SetMPB("_HatchingOffset2", hatchingOffset2Property.floatValue));
        DrawField(fadingAlphaProperty, () => SetMPB("_FadingAlpha", fadingAlphaProperty.floatValue));
    }

    #endregion Lifecycle


    #region Drawing =============================================================================== Drawing

    private bool DrawTextureTypeProperty()
    {
        bool startState = usesUniqueTextureProperty.boolValue;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Texture type");
        GUI.backgroundColor = startState ? Color.gray : Color.white;
        if (GUILayout.Button("Generic") && usesUniqueTextureProperty.boolValue == true)
        {
            SetMaterial(HubAssetManager.EditorInstance.GenericHubMaterial);
            materialProperty.objectReferenceValue = HubAssetManager.EditorInstance.GenericHubMaterial;
            usesUniqueTextureProperty.boolValue = false;
            UpdateTextureOffsetVariables();
        }
        GUI.backgroundColor = startState ? Color.white : Color.gray;
        if (GUILayout.Button("Unique") && usesUniqueTextureProperty.boolValue == false)
        {
            SetMaterial(HubAssetManager.EditorInstance.UniqueHubMaterial);
            materialProperty.objectReferenceValue = HubAssetManager.EditorInstance.UniqueHubMaterial;
            usesUniqueTextureProperty.boolValue = true;
            UpdateTextureOffsetVariables();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        return usesUniqueTextureProperty.boolValue;
    }

    private void DrawField(SerializedProperty property, Action onChanged = null)
    {
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(property, true);
        
        if (!EditorGUI.EndChangeCheck()) { onChanged?.Invoke(); }
    }

    private void DrawGenericTexturePathField()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Generic Texture");
        if (GUILayout.Button(HubAssetManager.GetReadableNameFromPath(genericTexturePathProperty.stringValue)))
        {
            GenericMenu menu = new();

            foreach (string texturePath in HubAssetManager.EditorInstance.GetGenericTexturePaths().Prepend("None"))
            {
                string textureName = HubAssetManager.GetReadableNameFromPath(texturePath);

                menu.AddItem(new GUIContent(textureName), false, () =>
                {
                    serializedObject.Update();

                    genericTexturePathProperty.stringValue = texturePath;
                    UpdateTextureOffsetVariables();

                    serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }

        EditorGUILayout.EndHorizontal();
    }

    #endregion Drawing


    #region Private Methods ======================================================================= Private Methods
    
    private void UpdateTextureOffsetVariables()
    {
        if (HubAssetManager.EditorInstance == null) { return; }

        string currentTexturePath = usesUniqueTextureProperty.boolValue ?
            uniqueTexturePathProperty.stringValue :
            genericTexturePathProperty.stringValue;

        HubAssetManager.EditorInstance.SetTextureVariablesFor(meshRenderer, currentTexturePath, usesUniqueTextureProperty.boolValue);
    }

    private void SetMaterial(Material material)
    {
        Undo.RecordObject(meshRenderer, "Changed material");
        meshRenderer.material = material;
        EditorUtility.SetDirty(meshRenderer);
    }

    private void SetMesh(Mesh mesh)
    {
        Undo.RecordObject(meshFilter, "Changed mesh");
        meshFilter.sharedMesh = mesh;
        EditorUtility.SetDirty(meshFilter);
    }

    private void SetMPB<T>(string variableName, T value)
    {
        mpb ??= new();
        meshRenderer.GetPropertyBlock(mpb);
        if (value is int intValue) { mpb.SetInteger(variableName, intValue); }
        else if (value is bool boolValue) { mpb.SetInteger(variableName, boolValue ? 1 : 0); }
        else if (value is float floatValue) { mpb.SetFloat(variableName, floatValue); }
        else if (value is Color colorValue) { mpb.SetColor(variableName, colorValue); }
        else if (value is Vector2 vector2Value) { mpb.SetVector(variableName, vector2Value); }
        else if (value is Vector3 vector3Value) { mpb.SetVector(variableName, vector3Value); }
        else if (value is Vector4 vector4Value) { mpb.SetVector(variableName, vector4Value); }
        else if (value is Texture textureValue) { mpb.SetTexture(variableName, textureValue); }
        meshRenderer.SetPropertyBlock(mpb);
    }

    private void RefreshVisuals()
    {
        serializedObject.Update();

        hubObject.RefreshVisuals();

        mpb ??= new();
        meshRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_MaterialSize", materialSizeProperty.floatValue);
        mpb.SetVector("_MaterialOffset", materialOffsetProperty.vector2Value);
        mpb.SetColor("_Tint", tintProperty.colorValue);
        mpb.SetFloat("_LightInfluence", lightInfluenceProperty.floatValue);
        mpb.SetFloat("_HatchingMultiplier", hatchingMultiplierProperty.floatValue);
        mpb.SetFloat("_HatchingOffset1", hatchingOffset1Property.floatValue);
        mpb.SetFloat("_HatchingOffset2", hatchingOffset2Property.floatValue);
        mpb.SetFloat("_FadingAlpha", fadingAlphaProperty.floatValue);
        meshRenderer.SetPropertyBlock(mpb);

        SetMaterial(usesUniqueTextureProperty.boolValue ?
            HubAssetManager.EditorInstance.UniqueHubMaterial :
            HubAssetManager.EditorInstance.GenericHubMaterial);

        SetMesh((Mesh)meshProperty.objectReferenceValue);

        UpdateTextureOffsetVariables();
    }

    #endregion Private Methods
}

#endif
