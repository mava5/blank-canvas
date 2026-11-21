
#if UNITY_EDITOR

using System.IO;

using UnityEditor;

using UnityEngine;


public class DrawableMeshPostProcess : AssetPostprocessor
{
    protected virtual void OnPreprocessModel()
    {
        ModelImporter importer = assetImporter as ModelImporter;
        if (importer == null) { return; }

        if (!IsPathValid(assetPath)) { return; }

        // Scene
        importer.globalScale = 1f;
        importer.useFileUnits = true;
        importer.bakeAxisConversion = false;
        importer.importBlendShapes = false;
        importer.importVisibility = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.preserveHierarchy = false;
        importer.sortHierarchyByName = true;

        // Meshes
        importer.meshCompression = ModelImporterMeshCompression.Off;
        importer.isReadable = false;
        importer.optimizeMeshPolygons = false;
        importer.optimizeMeshVertices = false;
        importer.addCollider = false;

        // Mesh LODs
        importer.generateMeshLods = false;

        // Geometry
        importer.keepQuads = false;
        importer.weldVertices = false;
        importer.indexFormat = ModelImporterIndexFormat.Auto;
        importer.importNormals = ModelImporterNormals.Calculate;
        importer.normalCalculationMode = ModelImporterNormalCalculationMode.AreaAndAngleWeighted;
        importer.normalSmoothingSource = ModelImporterNormalSmoothingSource.FromAngle;
        importer.normalSmoothingAngle = 30f;
        importer.importTangents = ModelImporterTangents.CalculateMikk;
        importer.swapUVChannels = false;
        importer.generateSecondaryUV = false;
        importer.strictVertexDataChecks = false;

        // Other
        importer.animationType = ModelImporterAnimationType.None;
        importer.importConstraints = false;
        importer.importAnimation = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
    }

    protected virtual void OnPostprocessMeshHierarchy(GameObject gameObject)
    {
        if (!IsPathValid(assetPath)) { return; }

        MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
        UnwrapParam unwrapParameters = new()
        {
            hardAngle = 60,
            angleError = 0.01f,
            areaError = 0.01f,
            packMargin = 0.02f,
        };

        foreach (var meshFilter in meshFilters)
        {
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null) { continue; }

            Vector3[] oldVertices = mesh.vertices;
            int[] oldTriangles = mesh.triangles;

            // Offset vertices slightly to avoid automatic vertex welding done in Unwrapping
            Vector3[] tempVertices = mesh.vertices;
            for (int i = 0; i < mesh.vertexCount; i++)
            { tempVertices[i] += Random.onUnitSphere * 0.001f; }
            mesh.SetVertices(tempVertices);

            Vector3[] newVertices = new Vector3[oldTriangles.Length];
            Vector2[] newUV2 = Unwrapping.GeneratePerTriangleUV(mesh, unwrapParameters);
            int[] newTriangles = new int[oldTriangles.Length];

            for (int i = 0; i < oldTriangles.Length; i++)
            {
                int oldIndex = oldTriangles[i];
                newVertices[i] = oldVertices[oldIndex];
                newTriangles[i] = i;
            }

            mesh.vertices = newVertices;
            mesh.triangles = newTriangles;
            mesh.uv2 = newUV2;

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
        }
    }

    private bool IsPathValid(string path)
    {
        string extension = Path.GetExtension(path).ToLower();
        bool isFBX = extension == ".fbx";

        if (!isFBX) { return false; }

        string directoryPath = Path.GetDirectoryName(path);
        string[] directories = directoryPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        bool inCorrectFolder = System.Array.Exists(directories, f => f == "DrawableMeshes");

        return isFBX && inCorrectFolder;
    }
}

#endif
