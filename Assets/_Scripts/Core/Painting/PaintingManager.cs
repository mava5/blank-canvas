
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UtilityEditor;
#endif

using Cysharp.Threading.Tasks;

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

using Utility;
using UtilityExtensions;

using static TryResult;
using Cysharp.Threading.Tasks.Triggers;


public class PaintingManager : MonoBehaviour
{
    internal Player Player { get; private set; } = null;
    internal bool IsPaintingFinished { get; private set; } = false;


    [SerializeField] private List<PaintingArea> areas = new();

    private Scene paintingScene = default;


    #region Lifecycle ============================================================================= Lifecycle



    #endregion Lifecycle


    #region Loading / Unloading =================================================================== Loading / Unloading

    internal static async UniTask<TryResult<PaintingManager>> TryLoadForAsync(PaintableCanvas canvas)
    {
        if (canvas == null)
        { Util.LogError<PaintingManager>("Canvas is null!"); return Fail(); }

        if (canvas.Painting == null)
        { Util.LogError<PaintingManager>("Canvas has no painting ScriptableObject!"); return Fail(); }

        string sceneName = canvas.Painting.SceneName;

        AsyncOperation sceneLoading = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        if (sceneLoading == null)
        { Util.LogError<PaintingManager>($"Failed to start scene {canvas.Painting.SceneName} loading!"); return Fail(); }

        await sceneLoading;
        Scene paintingScene = SceneManager.GetSceneByName(sceneName);

        if (!paintingScene.IsValid())
        { Util.LogError<PaintingManager>($"Could not load scene {canvas.Painting.SceneName}!"); return Fail(); }

        if (!paintingScene.TryGetRootComponent(out PaintingManager paintingManager))
        { Util.LogError<PaintingManager>("Painting scene has no PaintingManager!"); return Fail(); }

        // TODO: Merge PlayerMove to Player logic
        if (!paintingScene.TryGetRootComponent(out Player player))
        { Util.LogError<PaintingManager>("Painting scene has no player!"); return Fail(); }

        paintingManager.Player = player;
        paintingManager.paintingScene = paintingScene;

        return Ok(paintingManager);
    }

    public async UniTask UnloadAsync()
    {
        await SceneManager.UnloadSceneAsync(paintingScene);
    }

    #endregion Loading / Unloading
}


#if UNITY_EDITOR

[CustomEditor(typeof(PaintingManager))]
internal class PaintingManagerEditor : AdvancedEditor
{
    private PaintingManager manager = null;

    private SerializedProperty areasProperty = null;

    private List<(PaintingArea area, int index, string issue)> areaIssues = new();
    
    private List<PaintingArea> areas = new();
    private List<SerializedObject> areaObjects = new();
    private List<SerializedProperty> areaCheckpointProperties = new();

    private List<PaintingCheckpoint> checkpoints = new();
    private List<SerializedObject> checkpointObjects = new();
    private List<SerializedProperty> checkpointNextProperties = new();



    #region Lifecycle ============================================================================= Lifecycle

    protected override void OnEnableInternal()
    {
        manager = (PaintingManager)target;
        areasProperty = serializedObject.FindProperty("areas");
        UpdateData();

        Undo.undoRedoPerformed += OnUndoRedo;
    }

    protected override void OnDisableInternal()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    private void OnUndoRedo() => UpdateData();

    #endregion Lifecycle


    #region Inspector ============================================================================= Inspector

    protected override void OnInspectorGUIBegin()
    {
        EditorGUI.BeginChangeCheck();
    }

    protected override void OnInspectorGUIEnd()
    {
        if (EditorGUI.EndChangeCheck())
        { UpdateData(); }

        if (areaIssues.Count > 0)
        {
            EditorGUILayout.Space();

            foreach ((PaintingArea area, int index, string issue) in areaIssues)
            {
                string areaName = area != null ? area.name : "NULL";
                EditorGUILayout.LabelField($"[{index}: {areaName}]: {issue}");
            }
        }
    }

    #endregion Inspector


    #region Private Methods ======================================================================= Private Methods

    private void UpdateData()
    {
        areas.Clear();

        for (int i = 0; i < areasProperty.arraySize; i++)
        { areas.Add((PaintingArea)areasProperty.GetArrayElementAtIndex(i).objectReferenceValue); }

        UpdateSerializedObjectList(ref areas, ref areaObjects, ref areaCheckpointProperties, "checkpoint");

        checkpoints.Clear();
        areaIssues.Clear();

        for (int i = 0; i < areas.Count; i++)
        {
            PaintingArea area = areas[i];
            
            if (area == null)
            { areaIssues.Add((null, i, "Missing area reference!")); continue; }
            
            SerializedObject areaObject = areaObjects[i];
            PaintingCheckpoint checkpoint = areaCheckpointProperties[i].objectReferenceValue as PaintingCheckpoint;

            if (checkpoint == null)
            { areaIssues.Add((area, i, "No checkpoint!")); continue; }

            checkpoints.Add(checkpoint);
        }

        UpdateSerializedObjectList(ref checkpoints, ref checkpointObjects, ref checkpointNextProperties, "next");


        for (int i = 0; i < checkpoints.Count; i++)
        {
            PaintingCheckpoint checkpoint = checkpoints[i];
            SerializedObject checkpointObject = checkpointObjects[i];
            SerializedProperty nextCheckpointProperty = checkpointNextProperties[i];
            PaintingCheckpoint nextCheckpoint = (i == checkpoints.Count - 1) ? null : checkpoints[i + 1];

            nextCheckpointProperty.objectReferenceValue = nextCheckpoint;
            checkpointObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(checkpoint);
        }
    }

    private void UpdateSerializedObjectList<T>(ref List<T> objects, ref List<SerializedObject> serializedObjects,
        ref List<SerializedProperty> serializedProperties, string serializedPropertyName) where T : Object
    {
        List<SerializedObject> newSerializedObjects = new();
        List<SerializedProperty> newSerializedProperties = new();

        HashSet<SerializedObject> newSerializedObjectsMap = new();
        HashSet<SerializedProperty> newSerializedPropertiesMap = new();

        foreach (T obj in objects)
        {
            SerializedObject serializedObject = (obj != null) ? serializedObjects.FirstOrDefault(so => so != null && so.targetObject == obj) ?? new(obj) : null;
            SerializedProperty serializedProperty = (obj != null) ? serializedProperties.FirstOrDefault(sp => sp != null && sp.serializedObject == serializedObject) ??
                serializedObject.FindProperty(serializedPropertyName) : null;

            newSerializedObjects.Add(serializedObject);
            newSerializedObjectsMap.Add(serializedObject);
            newSerializedProperties.Add(serializedProperty);
            newSerializedPropertiesMap.Add(serializedProperty);
        }

        foreach (SerializedObject remainingSerializedObject in serializedObjects)
        { if (!newSerializedObjectsMap.Contains(remainingSerializedObject)) { remainingSerializedObject?.Dispose(); } }

        foreach (SerializedProperty remainingSerializedProperty in serializedProperties)
        { if (!newSerializedPropertiesMap.Contains(remainingSerializedProperty)) { remainingSerializedProperty?.Dispose(); } }

        serializedObjects = newSerializedObjects;
        serializedProperties = newSerializedProperties;
    }

    #endregion Private Methods
}

#endif
