
#if UNITY_EDITOR
using UnityEditor;
#endif

using Cysharp.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Experimental.Rendering;

using Utility;
using System.IO;


public enum TextureType
{
    None = -1,
    BaseColor,
    Normal,
    Count,
}

[Serializable]
public class AtlasInfo
{
    public TextureType Type = TextureType.None;
    public Texture2D Atlas = null;
    public List<string> Paths = new();
    public List<Rect> Rects = new();

    public AtlasInfo(TextureType type, Texture2D atlas, List<string> paths, List<Rect> rects)
    { Type = type; Atlas = atlas; Paths = paths; Rects = rects; }
}

[Serializable]
public class ArrayInfo
{
    public TextureType Type = TextureType.None;
    public Texture2DArray Array = null;
    public List<string> Paths = new();

    public ArrayInfo(TextureType type, Texture2DArray array, List<string> paths)
    { Type = type; Array = array; Paths = paths; }
}


#if UNITY_EDITOR
[InitializeOnLoad]
#endif
[CreateAssetMenu(fileName = "HubAssetManager", menuName = "SO Singletons/HubAssetManager")]
public class HubAssetManager : ScriptableObjectSingleton<HubAssetManager>
{
    public static HubAssetManager Instance = null;

    public Material UniqueHubMaterial => uniqueHubMaterial;
    public Material GenericHubMaterial => genericHubMaterial;


    protected override bool RequiresPersistentContainer => false;
    protected override TypeAndName[] ExpectedArgs => null;

    [Header("Materials:")]
    [SerializeField] private Material uniqueHubMaterial = null;
    [SerializeField] private Material genericHubMaterial = null;

    [Header("Infos:")]
    [SerializeField] private List<AtlasInfo> atlasInfos = null;
    [SerializeField] private List<ArrayInfo> arrayInfos = null;

    private Dictionary<string, Dictionary<TextureType, Rect>> atlasTextureRectLookup = new();
    private Dictionary<string, Dictionary<TextureType, int>> arrayTextureIndexLookup = new();

    private readonly static int atlasSize = 2048;
    private readonly static int arraySize = 1024;


    #region Lifecycle ======================================================================================= Lifecycle

    protected override UniTask<HubAssetManager> InitializeAsync(params object[] args)
    {
        Instance = this;

        Initialize();
        return UniTask.FromResult(this);
    }

    protected override void OnInitializationFailed() { }

    private void Initialize()
    {
        foreach (AtlasInfo atlasInfo in atlasInfos)
        {
            TextureType type = atlasInfo.Type;

            Shader.SetGlobalTexture($"_G_HubAtlas_{type}", atlasInfo.Atlas);

            for (int i = 0; i < atlasInfo.Paths.Count; i++)
            {
                string name = atlasInfo.Paths[i];
                Rect rect = atlasInfo.Rects[i];
                atlasTextureRectLookup.TryAdd(name, new());
                atlasTextureRectLookup[name].TryAdd(type, rect);
            }
        }

        foreach (ArrayInfo arrayInfo in arrayInfos)
        {
            TextureType type = arrayInfo.Type;

            Shader.SetGlobalTexture($"_G_HubArray_{type}", arrayInfo.Array);

            for (int i = 0; i < arrayInfo.Paths.Count; i++)
            {
                string name = arrayInfo.Paths[i];
                arrayTextureIndexLookup.TryAdd(name, new());
                arrayTextureIndexLookup[name].TryAdd(type, i);
            }
        }
    }

    #endregion Lifecycle


    #region Public Methods ================================================================================== Public Methods

    public void SetTextureVariablesFor(MeshRenderer meshRenderer, string textureName, bool usesUniqueTexture)
    {
        if (meshRenderer == null) { return; }

        MaterialPropertyBlock mpb = new();
        meshRenderer.GetPropertyBlock(mpb);

        bool hasData =
            (usesUniqueTexture && atlasTextureRectLookup.ContainsKey(textureName)) ||
            (!usesUniqueTexture && arrayTextureIndexLookup.ContainsKey(textureName));

        for (int i = 0; i < (int)TextureType.Count; i++)
        {
            TextureType type = (TextureType)i;
            bool textureTypeIncluded = hasData &&
                ((usesUniqueTexture && atlasTextureRectLookup[textureName].ContainsKey(type)) ||
                (!usesUniqueTexture && arrayTextureIndexLookup[textureName].ContainsKey(type)));

            if (usesUniqueTexture)
            {
                Rect rect = textureTypeIncluded ? atlasTextureRectLookup[textureName][type] : new();
                mpb.SetFloat($"_{type}_Included", textureTypeIncluded ? 1 : 0);
                mpb.SetVector($"_{type}_RectData", new Vector4(rect.width, rect.height, rect.xMin, rect.yMin) / atlasSize);
            }
            else
            {
                int index = textureTypeIncluded ? arrayTextureIndexLookup[textureName][type] : 0;
                mpb.SetFloat($"_{type}_Included", textureTypeIncluded ? 1 : 0);
                mpb.SetFloat($"_{type}_Index", index);
            }
        }

        meshRenderer.SetPropertyBlock(mpb);
    }

    public List<string> GetGenericTexturePaths()
    {
        HashSet<string> paths = new();

        foreach (ArrayInfo arrayInfo in arrayInfos)
        {
            foreach (string name in arrayInfo.Paths)
            { paths.Add(name); }
        }

        return paths.ToList();
    }

    public static string GetReadableNameFromPath(string path) => Path.GetFileNameWithoutExtension(path);

    #endregion Public Methods


#if UNITY_EDITOR

    public static HubAssetManager EditorInstance
    { get { if (editorInstance == null) { InitializeInstance(); } return editorInstance; } }

    private static HubAssetManager editorInstance = null;

    public IEvent OnTexturesBaked => onTexturesBaked;
    private readonly EventCaller onTexturesBaked = new();

    private static Dictionary<TextureType, (TextureFormat texture, GraphicsFormat graphics)> formats = new()
    {
        { TextureType.BaseColor, (TextureFormat.RGBA32, GraphicsFormat.R8G8B8A8_UNorm) },
        { TextureType.Normal, (TextureFormat.RGBA32, GraphicsFormat.R8G8B8A8_UNorm) },
    };


    #region Lifecycle ======================================================================================= Lifecycle

    static HubAssetManager() => EditorApplication.update += OnEditorUpdate;

    private static void OnEditorUpdate()
    {
        EditorApplication.update -= OnEditorUpdate;
        InitializeInstance();
    }

    private static void InitializeInstance()
    {
        editorInstance = Resources.Load<HubAssetManager>("HubAssetManager");

        if (editorInstance != null)
        { editorInstance.Initialize(); }
    }

    #endregion Lifecycle


    #region Tool Method ===================================================================================== Tool Method

    [MenuItem("Tools/Hub/Update Hub Textures")]
    public static void UpdateTextures()
    {
        if (EditorInstance == null)
        { Util.LogError<HubAssetManager>("Missing Hub Asset Manager from Resources folder!"); return; }

        string hubRootPath = "Assets/__Worlds/00_Hub";

        if (!AssetDatabase.IsValidFolder(hubRootPath))
        { Util.LogError<HubAssetManager>($"Missing hub folder: \"{hubRootPath}\"!"); return; }

        editorInstance.atlasInfos = UpdateAtlases(hubRootPath);
        editorInstance.arrayInfos = UpdateArrays(hubRootPath);
        editorInstance.Initialize();
        EditorUtility.SetDirty(editorInstance);

        AssetDatabase.Refresh();

        editorInstance.onTexturesBaked.Invoke();
    }

    #endregion Tool Method


    #region Private Methods ================================================================================= Private Methods

    private static List<AtlasInfo> UpdateAtlases(string rootPath)
    {
        List<AtlasInfo> newAtlasInfos = new();

        foreach ((TextureType type, var pathsToTextures) in GetUniqueTexturesToPack($"{rootPath}/ImportedAssets"))
        {
            List<string> paths = pathsToTextures.Keys.ToList();
            List<Texture2D> textures = pathsToTextures.Values.ToList();

            Texture2D atlas = new(atlasSize, atlasSize, formats[type].texture, mipChain: true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            Texture2D[] readableTextures = textures.Where(t =>
            {
                if (!t.isReadable)
                { Util.LogError<HubAssetManager>($"Texture \"{t.name}\" not readable!"); }
                return t.isReadable;
            }).ToArray();

            if (!Util.TryGenerateAtlas(readableTextures, 32, atlasSize, 8, out List<Rect> rects))
            { Util.LogError<HubAssetManager>("Textures couldn't be fit into atlas!"); return new(); }
            
            for (int i = 0; i < readableTextures.Length; i++)
            { DrawTextureToAtlas(readableTextures[i], atlas, rects[i], type); }
            atlas.Apply();

            newAtlasInfos.Add(new(type, atlas, paths, rects));

            string assetPath = $"{rootPath}/Generated/{type}Atlas.asset";

            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(atlas, assetPath);
            EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssetIfDirty(atlas);
        }

        return newAtlasInfos;
    }

    private static List<ArrayInfo> UpdateArrays(string rootPath)
    {
        List<ArrayInfo> newArrayInfos = new();

        foreach ((TextureType type, var pathsToTextures) in GetGenericTexturesToPack($"{rootPath}/GenericTextures"))
        {
            List<string> paths = pathsToTextures.Keys.ToList();
            List<Texture2D> textures = pathsToTextures.Values.ToList();

            Texture2D[] readableTextures = textures.Where(t =>
            {
                if (!t.isReadable)
                { Util.LogError<HubAssetManager>($"Texture \"{t.name}\" not readable!"); }
                return t.isReadable;
            }).ToArray();

            if (readableTextures.Length == 0) { return new(); }

            Texture2DArray array = new(arraySize, arraySize, readableTextures.Length, formats[type].texture, mipChain: true)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
            };

            for (int i = 0; i < readableTextures.Length; i++)
            { DrawTextureToArray(readableTextures[i], array, i, type); }
            array.Apply();

            newArrayInfos.Add(new(type, array, paths));

            string assetPath = $"{rootPath}/Generated/{type}Array.asset";

            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(array, assetPath);
            EditorUtility.SetDirty(array);
            AssetDatabase.SaveAssetIfDirty(array);
        }

        return newArrayInfos;
    }

    private static Dictionary<TextureType, Dictionary<string, Texture2D>> GetUniqueTexturesToPack(string rootPath)
    {
        List<string> unknownAssetPaths = new();
        List<string> texturePaths = new();
        Dictionary<string, List<(TextureType, Texture2D)>> uniqueTextures = new();

        foreach (GUID guid in AssetDatabase.FindAssetGUIDs("", new[] { rootPath }))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            bool validAssetFound = false;

            foreach (UnityEngine.Object subAsset in subAssets)
            {
                if (subAsset is Mesh mesh)
                {
                    texturePaths.Add(assetPath);
                    validAssetFound = true;
                }
                else if (subAsset is Texture2D texture)
                {
                    if (!TryGetTextureTypeFromPath(assetPath, out TextureType type)) { continue; }

                    string texturePath = GetMeshPathFromTexturePath(assetPath);
                    uniqueTextures.TryAdd(texturePath, new());
                    uniqueTextures[texturePath].Add((type, texture));
                    validAssetFound = true;
                }

                if (validAssetFound) { break; }
            }

            if (!validAssetFound)
            { unknownAssetPaths.Add(assetPath); }
        }

        Dictionary<(string, TextureType), List<Texture2D>> sortedTextures = new();
        foreach (string texturePath in texturePaths)
        {
            Dictionary<TextureType, int> typeCounters = new();
            for (int i = 0; i < (int)TextureType.Count; i++)
            { typeCounters.Add((TextureType)i, 0); }

            if (uniqueTextures.TryGetValue(texturePath, out var textures))
            {
                foreach ((TextureType type, Texture2D texture) in textures)
                {
                    sortedTextures.TryAdd((texturePath, type), new());
                    sortedTextures[(texturePath, type)].Add(texture);
                    typeCounters.TryAdd(type, 0);
                    typeCounters[type]++;
                }
            }

            Util.Log<HubAssetManager>($"Unique:" +
                $" {string.Join(", ", typeCounters.Select(c => $"{c.Key} {c.Value}"))}" +
                $" - {GetReadableNameFromPath(texturePath)}\n");
        }

        foreach (string unknownAssetPath in unknownAssetPaths)
        { Util.LogWarning<HubAssetManager>($"Unknown file: {unknownAssetPath}"); }

        Dictionary<TextureType, Dictionary<string, Texture2D>> results = new();
        foreach (((string texturePath, TextureType type), List<Texture2D> textures) in sortedTextures)
        {
            results.TryAdd(type, new());
            results[type].Add(texturePath, textures[0]);
        }

        return results;
    }

    private static Dictionary<TextureType, Dictionary<string, Texture2D>> GetGenericTexturesToPack(string rootPath)
    {
        List<string> unknownAssetPaths = new();
        Dictionary<string, List<(TextureType, Texture2D)>> genericTextures = new();

        foreach (GUID guid in AssetDatabase.FindAssetGUIDs("", new[] { rootPath }))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid));
            bool validAssetFound = false;

            foreach (UnityEngine.Object subAsset in subAssets)
            {
                if (subAsset is not Texture2D texture ||
                    !TryGetTextureTypeFromPath(assetPath, out TextureType type)) { continue; }

                string texturePath = GetMeshPathFromTexturePath(assetPath);
                genericTextures.TryAdd(texturePath, new());
                genericTextures[texturePath].Add((type, texture));
                validAssetFound = true;
                break;
            }

            if (!validAssetFound)
            { unknownAssetPaths.Add(assetPath); }
        }

        Dictionary<(string, TextureType), List<Texture2D>> sortedTextures = new();
        foreach ((string texturePath, List<(TextureType, Texture2D)> textures) in genericTextures)
        {
            Dictionary<TextureType, int> typeCounters = new();
            for (int i = 0; i < (int)TextureType.Count; i++)
            { typeCounters.Add((TextureType)i, 0); }

            foreach ((TextureType type, Texture2D texture) in textures)
            {
                sortedTextures.TryAdd((texturePath, type), new());
                sortedTextures[(texturePath, type)].Add(texture);
                typeCounters.TryAdd(type, 0);
                typeCounters[type]++;
            }

            Util.Log<HubAssetManager>($"Generic:" +
                $" {string.Join(", ", typeCounters.Select(c => $"{c.Key} {c.Value}"))}" +
                $" - {GetReadableNameFromPath(texturePath)}\n");
        }

        foreach (string unknownAssetPath in unknownAssetPaths)
        { Util.LogWarning<HubAssetManager>($"Unknown file: {unknownAssetPath}"); }

        Dictionary<TextureType, Dictionary<string, Texture2D>> results = new();
        foreach (((string meshName, TextureType type), List<Texture2D> textures) in sortedTextures)
        {
            results.TryAdd(type, new());
            results[type].Add(meshName, textures[0]);
        }

        return results;
    }

    private static string GetMeshPathFromTexturePath(string texturePath) => texturePath[..texturePath.LastIndexOf('_')] + ".fbx";

    private static bool TryGetTextureTypeFromPath(string texturePath, out TextureType type)
    {
        string textureName = GetReadableNameFromPath(texturePath);
        string prefix = textureName[(textureName.LastIndexOf('_') + 1)..].ToLower();

        type = prefix switch
        {
            "basecolor" => TextureType.BaseColor,
            "normal" => TextureType.Normal,
            _ => TextureType.None
        };

        return type != TextureType.None;
    }

    private static void DrawTextureToAtlas(Texture2D source, Texture2D atlas, Rect rect, TextureType type)
    {
        Texture2D resizedSource = GetResizedTexture(source, (int)rect.width, (int)rect.height, type);
        int x = (int)rect.x, y = (int)rect.y, width = (int)rect.width, height = (int)rect.height;
        atlas.SetPixels(x, y, width, height, resizedSource.GetPixels());
        DestroyImmediate(resizedSource);
    }

    private static void DrawTextureToArray(Texture2D source, Texture2DArray array, int index, TextureType type)
    {
        Texture2D resizedSource = GetResizedTexture(source, arraySize, arraySize, type);
        array.SetPixels(resizedSource.GetPixels(), index);
        DestroyImmediate(resizedSource);
    }

    private static Texture2D GetResizedTexture(Texture2D source, int width, int height, TextureType type)
    {
        RenderTextureDescriptor descriptor = new(width, height, formats[type].graphics, 0)
        { sRGB = true };
        
        RenderTexture renderTexture = RenderTexture.GetTemporary(descriptor);

        Graphics.Blit(source, renderTexture);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;

        Texture2D result = new(width, height, formats[type].texture, mipChain: true);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);

        return result;
    }

    #endregion Private Methods

#endif
}
