
#if UNITY_EDITOR
using UnityEditor;
#endif

using Cysharp.Threading.Tasks;

using UnityEngine;

using Utility;

using System;


#if UNITY_EDITOR
[InitializeOnLoad]
#endif
[CreateAssetMenu(fileName = "GlobalShaderSettings", menuName = "SO Singletons/GlobalShaderSettings")]
public class GlobalShaderSettings : ScriptableObjectSingleton<GlobalShaderSettings>
{
    public static GlobalShaderSettings Instance { get; private set; } = null;
    

    [Header("Animation:")]
    [SerializeField, Range(0f, 10f)] private float animation_speed = 0f;

    [Header("Lighting:")]
    [SerializeField, Range(0, 100)] private int lighting_steps = 8;
    [SerializeField, Range(0f, 1f)] private float lighting_brightness = 0f;
    [SerializeField, Range(0f, 2f)] private float lighting_noiseSize = 0f;
    [SerializeField] private Vector3 lighting_noiseOffset = Vector3.zero;
    [SerializeField, Range(0f, 1f)] private float lighting_noiseStrength = 0f;

    [Header("Hatching:")]
    [SerializeField] private Texture hatching_texture = null;
    [SerializeField, Range(0f, 10f)] private float hatching_size = 0f;
    [SerializeField, Range(0f, 1f)] private float hatching_blending = 0f;
    [SerializeField, Range(-2f, 2f)] private float hatching_smudging = 0f;
    [SerializeField, Range(0f, 1f)] private float hatching_strength = 0f;

    [Header("Editor:")]
    [SerializeField, Range(0f, 10f)] private float editor_checkerSize = 0f;

    [Header("Materials:")]
    [SerializeField] private Material fullSceneMaterial = null;

    protected override bool RequiresPersistentContainer => false;
    protected override TypeAndName[] ExpectedArgs => null;


    #region Lifecycle ============================================================================= Lifecycle

    protected override UniTask<GlobalShaderSettings> InitializeAsync(params object[] args)
    {
        Instance = this;
        ApplySettings();
        return UniTask.FromResult(this);
    }

    protected override void OnInitializationFailed() { }

    #endregion Lifecycle


    #region Public Methods ======================================================================== Public Methods

    public void ApplySettings()
    {
        Shader.SetGlobalFloat("_G_Animation_Speed", animation_speed);

        Shader.SetGlobalFloat("_G_Lighting_Steps", lighting_steps);
        Shader.SetGlobalFloat("_G_Lighting_Brightness", lighting_brightness);
        Shader.SetGlobalFloat("_G_Lighting_NoiseSize", lighting_noiseSize);
        Shader.SetGlobalVector("_G_Lighting_NoiseOffset", lighting_noiseOffset);
        Shader.SetGlobalFloat("_G_Lighting_NoiseStrength", lighting_noiseStrength);

        Shader.SetGlobalTexture("_G_Hatching_Texture", hatching_texture);
        Shader.SetGlobalFloat("_G_Hatching_Size", hatching_size);
        Shader.SetGlobalFloat("_G_Hatching_Blending", hatching_blending);
        Shader.SetGlobalFloat("_G_Hatching_Smudging", hatching_smudging);
        Shader.SetGlobalFloat("_G_Hatching_Strength", hatching_strength);

        Shader.SetGlobalFloat("_G_Editor_CheckerSize", editor_checkerSize);
    }

    #endregion Public Methods


#if UNITY_EDITOR

    static GlobalShaderSettings() => EditorApplication.update += OnEditorUpdate;

    private static void OnEditorUpdate()
    {
        EditorApplication.update -= OnEditorUpdate;

        Instance = Resources.Load<GlobalShaderSettings>("GlobalShaderSettings");

        if (Instance != null)
        { Instance.ApplySettings(); }
    }

#endif
}


#if UNITY_EDITOR

public class GlobalShaderSettingsWindow : EditorWindow
{
    private GlobalShaderSettings settings = null;
    private SerializedObject serializedObject = null;
    private Vector2 scrollPosition = Vector2.zero;

    [MenuItem("Tools/Settings/Global Shader Settings")]
    public static void ShowWindow()
    {
        var window = GetWindow<GlobalShaderSettingsWindow>("Global Shader Settings");
        window.minSize = new Vector2(200, 200);
        window.Show();
        window.Focus();
    }

    private void OnEnable()
    { Undo.undoRedoPerformed += OnUndoRedo; }

    private void OnDisable()
    { Undo.undoRedoPerformed -= OnUndoRedo; }

    private void OnGUI()
    {
        if (settings == null)
        {
            EditorGUILayout.LabelField("Missing Global Shader Settings!");

            if (GUILayout.Button("Refresh"))
            { settings = Resources.Load<GlobalShaderSettings>("GlobalShaderSettings"); }

            if (settings == null) { return; }
        }

        if (serializedObject == null || serializedObject.targetObject != settings)
        { serializedObject = new(settings); }


        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUI.BeginChangeCheck();
        serializedObject.Update();

        SerializedProperty iteratedProperty = serializedObject.GetIterator();
        bool enterChildren = true;
        while (iteratedProperty.NextVisible(enterChildren))
        {
            enterChildren = false;
            
            if (iteratedProperty.name != "m_Script")
            {
                GUIContent label = new(TrimmedLabelAttribute.GetProcessedName(iteratedProperty.displayName));

                // Force the GUI to be rendered as if in wide mode
                // Otherwise the components of a vector value are placed on a second row
                bool previousWideMode = EditorGUIUtility.wideMode;
                EditorGUIUtility.wideMode = true;
                EditorGUILayout.PropertyField(iteratedProperty, label, false);
                EditorGUIUtility.wideMode = previousWideMode;
            }
        }

        serializedObject.ApplyModifiedProperties();
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(settings);
            settings.ApplySettings();
        }

        EditorGUILayout.EndScrollView();
    }

    private void OnUndoRedo()
    {
        if (settings != null)
        { settings.ApplySettings(); }
    }
}

#endif