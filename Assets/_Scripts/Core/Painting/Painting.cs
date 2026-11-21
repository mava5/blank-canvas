
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UtilityEditor;
#endif

using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using UtilityExtensions;


[CreateAssetMenu(fileName = "Painting", menuName = "ObjectDrawing/Painting")]
public class Painting : ScriptableObject
{
    public string SceneName => sceneName;
    public List<Color> Colors => colors;

    public string PaintingName => name.Substring("_".Length, name.Length - ("_" + "Painting").Length);

    [SerializeField] private string sceneName = "";
    [SerializeField] private List<Color> colors = new();
}


#if UNITY_EDITOR

[CustomEditor(typeof(Painting))]
internal class PaintingEditor : AdvancedEditor
{
    private SerializedProperty sceneNameProperty = null;
    private SerializedProperty colorsProperty = null;

    protected override void OnEnableInternal()
    {
        sceneNameProperty = serializedObject.FindProperty("sceneName");
        colorsProperty = serializedObject.FindProperty("colors");
    }

    protected override bool OnPropertyFieldBegin(ModifiedProperty modifiedProperty)
    {
        if (modifiedProperty.Property.propertyPath == sceneNameProperty.propertyPath)
        {
            string paintingName = target.name;
            string desiredPaintingName = $"_{GetSimplePaintingFolderName(target)}Painting";

            if (paintingName != desiredPaintingName)
            {
                EditorGUILayout.LabelField($"Rename painting to: \"{desiredPaintingName}\"!", centeredLabel);
                StopDrawing();
                return false;
            }

            List<(string name, bool inBuild)> sceneNames = GetScenesNamesInSameFolderAs(target);
            string desiredSceneName = $"_{GetSimplePaintingFolderName(target)}Scene";

            if (sceneNames.Count == 0)
            {
                EditorGUILayout.LabelField($"No painting scene in folder!", centeredLabel);
                StopDrawing();
                return false;
            }
            else if (sceneNames.All(s => s.name != desiredSceneName))
            {
                EditorGUILayout.LabelField($"Rename painting scene to: \"{desiredSceneName}\"!", centeredLabel);
                StopDrawing();
                return false;
            }
            
            (string name, bool inBuild) paintingScene = sceneNames.First(s => s.name == desiredSceneName);

            if (!paintingScene.inBuild)
            {
                EditorGUILayout.LabelField($"Add painting scene \"{desiredSceneName}\" to build!", centeredLabel);
                StopDrawing();
                return false;
            }

            if (sceneNameProperty.stringValue != desiredSceneName)
            { sceneNameProperty.stringValue = desiredSceneName; }

            return false;
        }

        return true;
    }

    protected override void OnInspectorGUIEnd()
    {
        if (colorsProperty.arraySize > 7)
        { colorsProperty.ResizeArray(7, Color.white); }
    }


    public List<(string name, bool inBuild)> GetScenesNamesInSameFolderAs(Object asset)
    {
        List<(string, bool inBuild)> sceneNames = new();
        string folderPath = GetAssetFolderPath(asset);

        string[] guids = AssetDatabase.FindAssets("t:Scene", new string[] { folderPath });
        foreach (string guid in guids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

            bool inBuild = false;
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.path == scenePath && scene.enabled)
                { inBuild = true; break; }
            }

            sceneNames.Add((sceneName, inBuild));
        }

        return sceneNames;
    }

    private string GetAssetFolderPath(Object asset) => Path.GetDirectoryName(AssetDatabase.GetAssetPath(asset));

    private string GetAssetFolderName(Object asset) => Path.GetFileName(GetAssetFolderPath(asset));

    private string GetSimplePaintingFolderName(Object paintingAsset)
    {
        string simpleName = GetAssetFolderName(paintingAsset);

        if (!simpleName.Contains("_")) { return simpleName; }

        return simpleName[(simpleName.LastIndexOf('_') + 1)..];
    }
}

#endif
