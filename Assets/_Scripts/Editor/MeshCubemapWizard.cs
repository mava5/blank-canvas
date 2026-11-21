
#if UNITY_EDITOR

using UnityEngine;

using UnityEditor;

using Utility;


public class MeshCubemapWizard : ScriptableWizard
{
    private static Cubemap s_targetCubemap = null;
    private static Color s_backgroundColor = Color.black;
    private static Shader s_targetShader = null;
    private static Mesh s_targetMesh = null;
    private static Vector3 s_positionOffset = Vector3.zero;
    private static Vector3 s_rotationOffset = Vector3.zero;

    [Header("Cubemap:")]
    [SerializeField] private Cubemap targetCubemap = null;
    [SerializeField] private Color backgroundColor = Color.black;

    [Header("Shader:")]
    [SerializeField] private Shader targetShader = null;

    [Header("Mesh:")]
    [SerializeField] private Mesh targetMesh = null;
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;


    [MenuItem("Tools/Misc/Render Mesh Cubemap")]
    private static void CreateWizard()
    {
        var wizard = DisplayWizard<MeshCubemapWizard>("Render Mesh Cubemap", "Close", "Render");
        wizard.LoadProperties();
        wizard.isValid = true;
    }

    private void OnDestroy() => SaveProperties();

    private void OnWizardOtherButton()
    {
        if (targetMesh == null || targetShader == null || targetCubemap == null)
        { Util.LogError<MeshCubemapWizard>("Fill all fields!"); return; }

        Material tempMaterial = null;
        GameObject meshObject = null, cameraObject = null;

        try
        {
            int tempLayer = 31;
            tempMaterial = new(targetShader);

            meshObject = new("CubemapMesh");
            meshObject.transform.position = positionOffset;
            meshObject.transform.eulerAngles = rotationOffset;
            meshObject.layer = tempLayer;

            MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = targetMesh;

            MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = tempMaterial;

            cameraObject = new("CubemapCamera");
            Camera camera = cameraObject.AddComponent<Camera>();

            camera.cullingMask = 1 << tempLayer;
            camera.clearFlags = CameraClearFlags.Color;
            camera.backgroundColor = backgroundColor;

            camera.RenderToCubemap(targetCubemap);
            EditorUtility.SetDirty(targetCubemap);
        }
        finally
        {
            if (tempMaterial != null) { DestroyImmediate(tempMaterial); }
            if (meshObject != null) { DestroyImmediate(meshObject); }
            if (cameraObject != null) { DestroyImmediate(cameraObject); }
        }
    }

    private void OnWizardCreate() { }

    private void LoadProperties()
    {
        targetCubemap = s_targetCubemap;
        backgroundColor = s_backgroundColor;
        targetShader = s_targetShader;
        targetMesh = s_targetMesh;
        positionOffset = s_positionOffset;
        rotationOffset = s_rotationOffset;
    }

    private void SaveProperties()
    {
        s_targetCubemap = targetCubemap;
        s_backgroundColor = backgroundColor;
        s_targetShader = targetShader;
        s_targetMesh = targetMesh;
        s_positionOffset = positionOffset;
        s_rotationOffset = rotationOffset;
    }
}

#endif
