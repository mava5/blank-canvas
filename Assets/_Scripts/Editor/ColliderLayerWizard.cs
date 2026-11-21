
#if UNITY_EDITOR

using System.Collections.Generic;

using UnityEditor;
using UnityEditor.SceneManagement;

using UnityEngine;
using UnityEngine.SceneManagement;


public class ColliderLayerWizard : ScriptableWizard
{
    private static bool s_setForChildren = false;
    private static LayerMask s_includeLayers = default;
    private static LayerMask s_excludeLayers = default;

    [Header("Layers:")]
    [SerializeField] private bool setForChildren = true;
    [SerializeField] private LayerMask includeLayers = default;
    [SerializeField] private LayerMask excludeLayers = default;


    [MenuItem("Tools/Misc/Set Selected Collider Layers")]
    private static void CreateWizard()
    {
        var wizard = DisplayWizard<ColliderLayerWizard>("Set Selected Collider Layers", "Close", "Set");

        wizard.isValid = true;
        wizard.LoadProperties();
        Undo.undoRedoPerformed += wizard.UndoRedoPerformed;
    }

    protected virtual void OnDestroy()
    {
        SaveProperties();
        Undo.undoRedoPerformed -= UndoRedoPerformed;
    }

    protected virtual void OnWizardCreate() { }

    protected virtual void OnWizardOtherButton()
    {
        HashSet<Scene> dirtiedScenes = new();

        foreach (GameObject gameObject in Selection.gameObjects)
        {
            bool isPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(gameObject);
            bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(gameObject);

            // Skip instanciated copies of prefabs
            if (isPrefabInstance) { continue; }

            // Skip non-prefab objects in invalid scenes
            if (!isPrefabAsset && !gameObject.scene.IsValid()) { continue; }

            Collider[] colliders = setForChildren ? gameObject.GetComponentsInChildren<Collider>() : gameObject.GetComponents<Collider>();

            foreach (Collider collider in colliders)
            {
                Undo.RecordObject(collider, "Change Layer");
                collider.includeLayers = includeLayers;
                collider.excludeLayers = excludeLayers;
            }

            if (isPrefabAsset)
            {
                EditorUtility.SetDirty(gameObject);
                PrefabUtility.SavePrefabAsset(gameObject);
            }
            else
            {
                dirtiedScenes.Add(gameObject.scene);
            }
        }

        foreach (Scene scene in dirtiedScenes)
        { EditorSceneManager.MarkSceneDirty(scene); }
    }

    private void UndoRedoPerformed() => Repaint();

    private void LoadProperties()
    {
        setForChildren = s_setForChildren;
        includeLayers = s_includeLayers;
        excludeLayers = s_excludeLayers;
    }

    private void SaveProperties()
    {
        s_setForChildren = setForChildren;
        s_includeLayers = includeLayers;
        s_excludeLayers = excludeLayers;
    }
}

#endif
