
#if UNITY_EDITOR
using UnityEditor;
#endif

using ObjectDrawing;

using System.Collections.Generic;

using UnityEngine;
using UtilityEditor;


public class PaintableObject : DrawableObject
{
#if UNITY_EDITOR
    [SerializeField] protected bool showHiddenComponents = false;
    [SerializeField] protected List<bool> foldoutStates = new();
#endif
}

#if UNITY_EDITOR

[CustomEditor(typeof(PaintableObject)), CanEditMultipleObjects]
internal class PaintableObjectEditor : DrawableObjectEditor { }

#endif


//#if UNITY_EDITOR

//[CustomEditor(typeof(DrawableObject)), CanEditMultipleObjects]
//internal class DrawableObjectEditor : AdvancedEditor
//{


//    protected override void OnEnableInternal()
//    {

//        drawingSystemProperty = serializedObject.FindProperty("drawingSystem");
//    }

//    protected override void OnPropertyFieldEnd(ModifiedProperty modifiedProperty)
//    {
//        if (modifiedProperty.Property.propertyPath == drawingSystemProperty.propertyPath)
//        {
//            drawingSystem = drawingSystemProperty.objectReferenceValue as DrawingSystem;

//            if (drawingSystem == null)
//            {
//                EditorGUILayout.LabelField("No DrawingSystem Set!", centeredLabel);
//                StopDrawing();
//            }
//        }
//    }

//    protected override void OnInspectorGUIEnd()
//    {
//        if (meshRenderer == null || drawableObject.TryGetComponent(out meshRenderer)) { return; }

//        mpb ??= new();
//        meshRenderer.GetPropertyBlock(mpb);
//        mpb.SetFloat("_TextureSize", drawableObject.GetTargetTextureSize());
//        meshRenderer.SetPropertyBlock(mpb);
//    }







//    private DrawableObject drawableObject = null;
//    private MeshFilter meshFilter = null;
//    private MeshRenderer meshRenderer = null;

//    private MaterialPropertyBlock mpb = null;

//    private SerializedProperty usesUniqueTextureProperty = null;
//    private SerializedProperty meshProperty = null;
//    private SerializedProperty materialProperty = null;
//    private SerializedProperty uniqueTexturePathProperty = null;
//    private SerializedProperty genericTexturePathProperty = null;

//    private SerializedProperty materialSizeProperty = null;
//    private SerializedProperty materialOffsetProperty = null;

//    private SerializedProperty tintProperty = null;
//    private SerializedProperty lightInfluenceProperty = null;
//    private SerializedProperty hatchingMultiplierProperty = null;
//    private SerializedProperty hatchingOffset1Property = null;
//    private SerializedProperty hatchingOffset2Property = null;

//    private Vector3 previousSize = Vector3.zero;


//    #region Lifecycle ============================================================================= Lifecycle

//    protected override void OnEnableInternal()
//    {
//        drawableObject = (DrawableObject)target;
//        meshFilter = drawableObject.GetComponent<MeshFilter>();
//        meshRenderer = drawableObject.GetComponent<MeshRenderer>();

//        usesUniqueTextureProperty = serializedObject.FindProperty("usesHybridTexture");
//        meshProperty = serializedObject.FindProperty("mesh");
//        materialProperty = serializedObject.FindProperty("material");
//        uniqueTexturePathProperty = serializedObject.FindProperty("uniqueTexturePath");
//        genericTexturePathProperty = serializedObject.FindProperty("genericTexturePath");

//        materialSizeProperty = serializedObject.FindProperty("materialSize");
//        materialOffsetProperty = serializedObject.FindProperty("materialOffset");

//        tintProperty = serializedObject.FindProperty("tint");
//        lightInfluenceProperty = serializedObject.FindProperty("lightInfluence");
//        hatchingMultiplierProperty = serializedObject.FindProperty("hatchingMultiplier");
//        hatchingOffset1Property = serializedObject.FindProperty("hatchingOffset1");
//        hatchingOffset2Property = serializedObject.FindProperty("hatchingOffset2");

//        RefreshVisuals();

//        Undo.undoRedoPerformed += RefreshVisuals;
//    }

//    protected override void OnDisableInternal()
//    {
//        Undo.undoRedoPerformed -= RefreshVisuals;
//    }

//    protected override void OnInspectorGUIEnd()
//    {
//        if (meshFilter == null && !drawableObject.TryGetComponent(out meshFilter))
//        { EditorGUILayout.LabelField("Missing MeshFilter!", centeredLabel); return; }

//        if (meshRenderer == null && !drawableObject.TryGetComponent(out meshRenderer))
//        { EditorGUILayout.LabelField("Missing MeshRenderer!", centeredLabel); return; }

//        if (DrawingSystem.Instance == null)
//        { EditorGUILayout.LabelField("No DrawingSystem created!", centeredLabel); return; }

//        bool usesHybridTexture = DrawTextureTypeProperty();
//        EditorGUILayout.Space();
//        EditorGUILayout.LabelField("Material:", headerLabel);
//        if (usesHybridTexture)
//        {
//            DrawField(meshProperty, () =>
//            {
//                Mesh mesh = (Mesh)meshProperty.objectReferenceValue;

//                if (mesh != null)
//                { uniqueTexturePathProperty.stringValue = AssetDatabase.GetAssetPath(mesh); }

//                SetMesh(mesh);
//                UpdateTextureOffsetVariables();
//            });
//        }
//        else
//        {
//            DrawField(meshProperty, () => SetMesh((Mesh)meshProperty.objectReferenceValue));
//            DrawGenericTexturePathField();
//            DrawField(materialSizeProperty, () => SetMPB("_MaterialSize", materialSizeProperty.floatValue));
//            DrawField(materialOffsetProperty, () => SetMPB("_MaterialOffset", materialOffsetProperty.vector2Value));
//        }
//        EditorGUILayout.Space();
//        EditorGUILayout.LabelField("Visuals:", headerLabel);
//        DrawField(tintProperty, () => SetMPB("_Tint", tintProperty.colorValue));
//        DrawField(lightInfluenceProperty, () => SetMPB("_LightInfluence", lightInfluenceProperty.floatValue));
//        DrawField(hatchingMultiplierProperty, () => SetMPB("_HatchingMultiplier", hatchingMultiplierProperty.floatValue));
//        DrawField(hatchingOffset1Property, () => SetMPB("_HatchingOffset1", hatchingOffset1Property.floatValue));
//        DrawField(hatchingOffset2Property, () => SetMPB("_HatchingOffset2", hatchingOffset2Property.floatValue));
//    }

//    #endregion Lifecycle


//    #region Drawing =============================================================================== Drawing

//    private bool DrawTextureTypeProperty()
//    {
//        bool startState = usesUniqueTextureProperty.boolValue;

//        EditorGUILayout.BeginHorizontal();
//        EditorGUILayout.PrefixLabel("Texture type");
//        GUI.backgroundColor = startState ? Color.gray : Color.white;
//        if (GUILayout.Button("Generic") && usesUniqueTextureProperty.boolValue == true)
//        {
//            SetMaterial(HubAssetManager.EditorInstance.GenericHubMaterial);
//            materialProperty.objectReferenceValue = HubAssetManager.EditorInstance.GenericHubMaterial;
//            usesUniqueTextureProperty.boolValue = false;
//            UpdateTextureOffsetVariables();
//        }
//        GUI.backgroundColor = startState ? Color.white : Color.gray;
//        if (GUILayout.Button("Unique") && usesUniqueTextureProperty.boolValue == false)
//        {
//            SetMaterial(HubAssetManager.EditorInstance.UniqueHubMaterial);
//            materialProperty.objectReferenceValue = HubAssetManager.EditorInstance.UniqueHubMaterial;
//            usesUniqueTextureProperty.boolValue = true;
//            UpdateTextureOffsetVariables();
//        }
//        GUI.backgroundColor = Color.white;
//        EditorGUILayout.EndHorizontal();

//        return usesUniqueTextureProperty.boolValue;
//    }

//    private void DrawField(SerializedProperty property, Action onChanged = null)
//    {
//        EditorGUI.BeginChangeCheck();
//        EditorGUILayout.PropertyField(property, true);

//        if (!EditorGUI.EndChangeCheck()) { onChanged?.Invoke(); }
//    }

//    private void DrawGenericTexturePathField()
//    {
//        EditorGUILayout.BeginHorizontal();
//        EditorGUILayout.PrefixLabel("Generic Texture");
//        if (GUILayout.Button(HubAssetManager.GetReadableNameFromPath(genericTexturePathProperty.stringValue)))
//        {
//            GenericMenu menu = new();

//            foreach (string texturePath in HubAssetManager.EditorInstance.GetGenericTexturePaths().Prepend("None"))
//            {
//                string textureName = HubAssetManager.GetReadableNameFromPath(texturePath);

//                menu.AddItem(new GUIContent(textureName), false, () =>
//                {
//                    serializedObject.Update();

//                    genericTexturePathProperty.stringValue = texturePath;
//                    UpdateTextureOffsetVariables();

//                    serializedObject.ApplyModifiedProperties();
//                });
//            }

//            menu.ShowAsContext();
//        }

//        EditorGUILayout.EndHorizontal();
//    }

//    #endregion Drawing


//    #region Private Methods ======================================================================= Private Methods

//    private void UpdateTextureOffsetVariables()
//    {
//        if (HubAssetManager.EditorInstance == null) { return; }

//        string currentTexturePath = usesUniqueTextureProperty.boolValue ?
//            uniqueTexturePathProperty.stringValue :
//            genericTexturePathProperty.stringValue;

//        HubAssetManager.EditorInstance.SetTextureVariablesFor(meshRenderer, currentTexturePath, usesUniqueTextureProperty.boolValue);
//    }

//    private void SetMaterial(Material material)
//    {
//        Undo.RecordObject(meshRenderer, "Changed material");
//        meshRenderer.material = material;
//        EditorUtility.SetDirty(meshRenderer);
//    }

//    private void SetMesh(Mesh mesh)
//    {
//        Undo.RecordObject(meshFilter, "Changed mesh");
//        meshFilter.sharedMesh = mesh;
//        EditorUtility.SetDirty(meshFilter);
//    }

//    private void SetMPB<T>(string variableName, T value)
//    {
//        mpb ??= new();
//        meshRenderer.GetPropertyBlock(mpb);
//        if (value is int intValue) { mpb.SetInteger(variableName, intValue); }
//        else if (value is bool boolValue) { mpb.SetInteger(variableName, boolValue ? 1 : 0); }
//        else if (value is float floatValue) { mpb.SetFloat(variableName, floatValue); }
//        else if (value is Color colorValue) { mpb.SetColor(variableName, colorValue); }
//        else if (value is Vector2 vector2Value) { mpb.SetVector(variableName, vector2Value); }
//        else if (value is Vector3 vector3Value) { mpb.SetVector(variableName, vector3Value); }
//        else if (value is Vector4 vector4Value) { mpb.SetVector(variableName, vector4Value); }
//        else if (value is Texture textureValue) { mpb.SetTexture(variableName, textureValue); }
//        meshRenderer.SetPropertyBlock(mpb);
//    }

//    private void RefreshVisuals()
//    {
//        serializedObject.Update();

//        hubObject.RefreshVisuals();

//        mpb ??= new();
//        meshRenderer.GetPropertyBlock(mpb);
//        mpb.SetFloat("_MaterialSize", materialSizeProperty.floatValue);
//        mpb.SetVector("_MaterialOffset", materialOffsetProperty.vector2Value);
//        mpb.SetColor("_Tint", tintProperty.colorValue);
//        mpb.SetFloat("_LightInfluence", lightInfluenceProperty.floatValue);
//        mpb.SetFloat("_HatchingMultiplier", hatchingMultiplierProperty.floatValue);
//        mpb.SetFloat("_HatchingOffset1", hatchingOffset1Property.floatValue);
//        mpb.SetFloat("_HatchingOffset2", hatchingOffset2Property.floatValue);
//        meshRenderer.SetPropertyBlock(mpb);

//        SetMaterial(usesUniqueTextureProperty.boolValue ?
//            HubAssetManager.EditorInstance.UniqueHubMaterial :
//            HubAssetManager.EditorInstance.GenericHubMaterial);

//        SetMesh((Mesh)meshProperty.objectReferenceValue);

//        UpdateTextureOffsetVariables();
//    }

//    #endregion Private Methods
//}

//#endif
