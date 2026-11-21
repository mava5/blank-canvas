
#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;


#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class EditorShaderVariableSetter
{
#if UNITY_EDITOR

    static EditorShaderVariableSetter()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        SetKeyword(!Application.isPlaying);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    { if (state == PlayModeStateChange.EnteredEditMode) { SetKeyword(!Application.isPlaying); } }

#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void BuildInitialization() => SetKeyword(false);

    private static void SetKeyword(bool state)
    {
        if (state) { Shader.EnableKeyword("_G_IN_EDITOR"); }
        else { Shader.DisableKeyword("_G_IN_EDITOR"); }
    }
}
