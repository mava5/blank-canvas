
#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

using Utility;
using UtilityEnum;
using UnityEngine.UIElements;
using System.Reflection;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace UtilityExtensions
{
    public static class Extensions
    {
        #region List<T> =========================================================================== List<T>

        /// <summary> Shuffles the element order of the specified list using Fisher-Yates shuffle. </summary>
        /// <param name="count"> How many elements get shuffled from the start </param>
        public static void Shuffle<T>(this IList<T> list, int count = int.MaxValue)
        {
            count = Mathf.Clamp(count, 0, list.Count - 1);

            for (int i = 0; i < count; i++)
            {
                int r = UnityEngine.Random.Range(i, count);
                T tmp = list[i];
                list[i] = list[r];
                list[r] = tmp;
            }
        }

        /// <summary> Enables simple addition of multiple elements to a list at once. </summary>
        public static void AddMany<T>(this IList<T> list, params T[] items)
        { for (int i = 0; i < items.Length; i++) { list.Add(items[i]); } }

        /// <summary> Finds the first instance of an item of given type. </summary>
        public static bool TryGetFirstOfType<T, R>(this IList<T> list, out R item)
        {
            item = default;

            if (list == null || !typeof(T).IsAssignableFrom(typeof(R))) { return false; }

            foreach (T value in list)
            {
                if (value is R castedValue)
                {
                    item = castedValue;
                    return true;
                }
            }

            return false;
        }

        #endregion List<T>


        #region string ============================================================================ string

        public static string SIFormat(this double value, string unit = "", int decimals = 2, bool baseUnitHasDecimals = true)
        {
            string suffix = "";
            string sign = value < 0 ? "-" : "";
            value = Math.Abs(value);

            if (value < 1_000)
            { if (!baseUnitHasDecimals) { decimals = 0; } }

            else if (value < 1_000_000)
            { value /= 1_000.0; suffix = "K"; }

            else if (value < 1_000_000_000)
            { value /= 1_000_000.0; suffix = "M"; }

            else if (value < 1_000_000_000_000)
            { value /= 1_000_000_000.0; suffix = "G"; }

            else if (value < 1_000_000_000_000_000)
            { value /= 1_000_000_000_000.0; suffix = "T"; }

            else
            { value /= 1_000_000_000_000_000.0; suffix = "P"; }

            return $"{sign}{value.ToString("0." + new string('#', decimals))} {suffix}{unit}";
        }

        public static string SIFormat(this ulong value, string unit = "")
        {
            string suffix = "";

            if (value < 1_000) { }

            else if (value < 1_000_000)
            { value /= 1_000; suffix = "K"; }

            else if (value < 1_000_000_000)
            { value /= 1_000_000; suffix = "M"; }

            else if (value < 1_000_000_000_000)
            { value /= 1_000_000_000; suffix = "G"; }

            else if (value < 1_000_000_000_000_000)
            { value /= 1_000_000_000_000; suffix = "T"; }

            else
            { value /= 1_000_000_000_000_000; suffix = "P"; }

            return $"{value} {suffix}{unit}";
        }

        #endregion string


        #region int =============================================================================== int

        /// <summary> Returns previous power-of-two value of integer. </summary>
        /// <param name="strictlySmaller"> Determines if value changes even if already POT. </param>
        public static int PreviousPOT(this int value, bool strictlySmaller = false)
        {
            if (value <= 1) { return 1; }

            if (strictlySmaller) { value--; }

            value |= (value >> 1);
            value |= (value >> 2);
            value |= (value >> 4);
            value |= (value >> 8);
            value |= (value >> 16);

            return value - (value >> 1);
        }
        
        /// <summary> Returns next power-of-two value of integer. </summary>
        /// <param name="strictlyGreater"> Determines if value changes even if already POT. </param>
        public static int NextPOT(this int value, bool strictlyGreater = false)
        {
            if (value <= 0) { return 1; }
            if (value > (1 << 30)) { return 1 << 30; }

            if (!strictlyGreater) { value--; }

            value |= (value >> 1);
            value |= (value >> 2);
            value |= (value >> 4);
            value |= (value >> 8);
            value |= (value >> 16);

            return value + 1;
        }

        /// <summary> Returns closest power-of-two value of integer. </summary>
        public static int ClosestPOT(this int value)
        {
            if (value == 0) { return 1; }
            if (value > (1 << 30)) { return 1 << 30; }

            int next = value.NextPOT();
            int prev = next >> 1;

            return (value - prev) < (next - value) ? prev : next;
        }


        /// <summary> Returns previous power-of-two value of unsigned integer. </summary>
        /// <param name="strictlySmaller"> Determines if value changes even if already POT. </param>
        public static uint PreviousPOT(this uint value, bool strictlySmaller = false)
        {
            if (value <= 1) { return 1; }

            if (strictlySmaller) { value--; }

            value |= (value >> 1);
            value |= (value >> 2);
            value |= (value >> 4);
            value |= (value >> 8);
            value |= (value >> 16);

            return value - (value >> 1);
        }

        /// <summary> Returns next power-of-two value of unsigned integer. </summary>
        /// <param name="strictlyGreater"> Determines if value changes even if already POT. </param>
        public static uint NextPOT(this uint value, bool strictlyGreater = false)
        {
            if (value == 0) { return 1; }
            if (value > 0x80000000u) { return 0x80000000u; }

            if (!strictlyGreater) { value--; }

            value |= (value >> 1);
            value |= (value >> 2);
            value |= (value >> 4);
            value |= (value >> 8);
            value |= (value >> 16);

            return value + 1;
        }

        /// <summary> Returns closest power-of-two value of unsigned integer. </summary>
        public static uint ClosestPOT(this uint value)
        {
            if (value == 0) { return 1; }
            if (value > 0x80000000u) { return 0x80000000u; }
            
            uint next = value.NextPOT();
            uint prev = next >> 1;

            return (value - prev) < (next - value) ? prev : next;
        }

        #endregion int


        #region Exception ========================================================================= Exception

        /// <summary> Uses <see cref="Debug.LogError(object)"/> to log exception with given context. </summary>
        [HideInCallstack]
        public static void LogWithContext(this Exception ex, string context)
        { Util.LogError(context, ex.Message, ex is ControlledException); }

        /// <summary> Uses <see cref="Debug.LogError(object)"/> to log exception with given context. </summary>
        [HideInCallstack]
        public static void LogWithContext<T>(this Exception ex)
        { Util.LogError<T>(ex.Message, ex is ControlledException); }

        #endregion Exception


        #region Component ========================================================================= Component

        public static T GetOrCache<T>(this Component component, ref T cached) where T : UnityEngine.Object
        {
            if (cached == null) { cached = component.GetComponent<T>(); }
            return cached;
        }

        public static T GetOrAdd<T>(this Component component, ref T cached) where T : Component
        {
            if (cached == null && !component.TryGetComponent(out cached))
            { cached = component.gameObject.AddComponent<T>(); }
            return cached;
        }

        #endregion Component


        #region Transform ========================================================================= Transform
        
        public static T GetComponentInAncestor<T>(this Transform transform) where T : Component
        {
            if (transform == null) { return null; }

            Transform parent = transform.parent;

            while (parent != null)
            {
                if (parent.TryGetComponent(out T component)) { return component; }
                parent = parent.parent;
            }

            return null;
        }

        public static bool TryGetComponentInAncestor<T>(this Transform transform, out T component) where T : Component
        {
            component = transform.GetComponentInAncestor<T>();
            return component != null;
        }

        #endregion Transform


        #region GameObject ======================================================================== GameObject

        public static bool TryGetComponents<T>(this GameObject gameObject, out T[] components)
        {
            components = gameObject.GetComponents<T>();
            return components.Length > 0;
        }

        #endregion GameObject


        #region Scene ============================================================================= Scene

        public static T GetRootComponent<T>(this Scene scene) where T : Component
        {
            if (scene == null || !scene.IsValid() || scene.rootCount == 0) { return null; }

            foreach (GameObject root in scene.GetRootGameObjects())
            { if (root.TryGetComponent(out T component)) { return component; }; }

            return null;
        }

        public static bool TryGetRootComponent<T>(this Scene scene, out T component) where T : Component
        {
            component = scene.GetRootComponent<T>();
            return component != null;
        }

        public static GameObject FindRoot(this Scene scene, string name)
        {
            if (scene == null || !scene.IsValid() || scene.rootCount == 0) { return null; }

            foreach (GameObject root in scene.GetRootGameObjects())
            { if (root.name == name) { return root; }; }

            return null;
        }

        public static bool TryFindRoot(this Scene scene, string name, out GameObject root)
        {
            root = scene.FindRoot(name);
            return root != null;
        }

        #endregion Scene


        #region Color ============================================================================= Color

        public static Color RGBMultiplied(this Color color, float mult)
        { return new(color.r * mult, color.g * mult, color.b * mult, color.a); }

        public static Color RGBMultiplied(this Color color, float rMult, float gMult, float bMult)
        { return new(color.r * rMult, color.g * gMult, color.b * bMult, color.a); }

        public static string ToHexRGB(this Color color) => ColorUtility.ToHtmlStringRGB(color);

        public static string ToHexRGBA(this Color color) => ColorUtility.ToHtmlStringRGBA(color);

        #endregion Color


        #region SerializedProperty ================================================================ SerializedProperty

#if UNITY_EDITOR
        /// <summary> Resizes given serialized array. </summary>
        /// <param name="fillValue"> Value added to new array indices </param>
        public static void ResizeArray<T>(this SerializedProperty property, int size, T fillValue)
        {
            while (property.arraySize > size)
            { property.DeleteArrayElementAtIndex(property.arraySize - 1); }

            while (property.arraySize < size)
            {
                property.InsertArrayElementAtIndex(property.arraySize);
                switch (fillValue)
                {
                    case int v: property.GetArrayElementAtIndex(property.arraySize - 1).intValue = v; break;
                    case bool v: property.GetArrayElementAtIndex(property.arraySize - 1).boolValue = v; break;
                    case Rect v: property.GetArrayElementAtIndex(property.arraySize - 1).rectValue = v; break;
                    case float v: property.GetArrayElementAtIndex(property.arraySize - 1).floatValue = v; break;
                    case Color v: property.GetArrayElementAtIndex(property.arraySize - 1).colorValue = v; break;
                    case string v: property.GetArrayElementAtIndex(property.arraySize - 1).stringValue = v; break;
                    case Bounds v: property.GetArrayElementAtIndex(property.arraySize - 1).boundsValue = v; break;
                    case RectInt v: property.GetArrayElementAtIndex(property.arraySize - 1).rectIntValue = v; break;
                    case Vector2 v: property.GetArrayElementAtIndex(property.arraySize - 1).vector2Value = v; break;
                    case Vector3 v: property.GetArrayElementAtIndex(property.arraySize - 1).vector3Value = v; break;
                    case Vector4 v: property.GetArrayElementAtIndex(property.arraySize - 1).vector4Value = v; break;
                    case Gradient v: property.GetArrayElementAtIndex(property.arraySize - 1).gradientValue = v; break;
                    case BoundsInt v: property.GetArrayElementAtIndex(property.arraySize - 1).boundsIntValue = v; break;
                    case Vector2Int v: property.GetArrayElementAtIndex(property.arraySize - 1).vector2IntValue = v; break;
                    case Vector3Int v: property.GetArrayElementAtIndex(property.arraySize - 1).vector3IntValue = v; break;
                    case Quaternion v: property.GetArrayElementAtIndex(property.arraySize - 1).quaternionValue = v; break;
                    case AnimationCurve v: property.GetArrayElementAtIndex(property.arraySize - 1).animationCurveValue = v; break;
                    case UnityEngine.Object v: property.GetArrayElementAtIndex(property.arraySize - 1).objectReferenceValue = v; break;
                    default: Util.LogWarning("SerializedProperty", $"Unsupported fill type: {typeof(T)}"); break;
                }

            }
        }
#endif

        #endregion


        #region SerializedObject ================================================================== SerializedObject

#if UNITY_EDITOR

        public static SerializedProperty FindAutoProperty(this SerializedObject serializedObject, string name)
        { return serializedObject.FindProperty($"<{name}>k__BackingField"); }

#endif

        #endregion SerializedObject


        #region Collider ========================================================================== Collider

        public static bool ContainsAnyBoundsCorner(this Collider collider, Bounds bounds)
        {
            if (!collider.bounds.Intersects(bounds)) { return false; }

            Vector3 center = bounds.center;
            Vector3 halfSize = bounds.size * 0.5f;

            return
                collider.ContainsPoint(center + new Vector3(halfSize.x, halfSize.y, halfSize.z)) ||
                collider.ContainsPoint(center + new Vector3(-halfSize.x, halfSize.y, halfSize.z)) ||
                collider.ContainsPoint(center + new Vector3(halfSize.x, -halfSize.y, halfSize.z)) ||
                collider.ContainsPoint(center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z)) ||
                collider.ContainsPoint(center + new Vector3(halfSize.x, halfSize.y, -halfSize.z)) ||
                collider.ContainsPoint(center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z)) ||
                collider.ContainsPoint(center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z)) ||
                collider.ContainsPoint(center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z));
        }

        public static bool ContainsBoundsFully(this Collider collider, Bounds bounds)
        {
            if (!collider.bounds.Intersects(bounds)) { return false; }

            Vector3 center = bounds.center;
            Vector3 halfSize = bounds.size * 0.5f;

            return
                collider.ContainsPoint(center + new Vector3(halfSize.x, halfSize.y, halfSize.z)) &&
                collider.ContainsPoint(center + new Vector3(-halfSize.x, halfSize.y, halfSize.z)) &&
                collider.ContainsPoint(center + new Vector3(halfSize.x, -halfSize.y, halfSize.z)) &&
                collider.ContainsPoint(center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z)) &&
                collider.ContainsPoint(center + new Vector3(halfSize.x, halfSize.y, -halfSize.z)) &&
                collider.ContainsPoint(center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z)) &&
                collider.ContainsPoint(center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z)) &&
                collider.ContainsPoint(center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z));
        }

        public static bool ContainsPoint(this Collider collider, Vector3 point)
        { return (point - collider.ClosestPoint(point)).sqrMagnitude < 0.001f; }

        #endregion Collider


        #region UniversalRendererData ============================================================= UniversalRendererData

        private static FieldInfo rendererDataListField = null;

        public static bool TryGetIndexInPipeline(this UniversalRendererData rendererData, out int index)
        {
            index = -1;

            if (rendererDataListField == null)
            {
                rendererDataListField = typeof(UniversalRenderPipelineAsset)
                    .GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance);
                if (rendererDataListField == null) { return false; }
            }

            var pipelineAsset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            if (pipelineAsset == null) { return false; }

            var rendererDataList = (ScriptableRendererData[])rendererDataListField.GetValue(pipelineAsset);
            if (rendererDataList == null) { return false; }

            for (int i = 0; i < rendererDataList.Length; i++)
            {
                if (rendererDataList[i] == rendererData)
                { index = i; return true; }
            }

            return false;
        }

        #endregion UniversalRendererData


        #region Camera ============================================================================ Camera

        public static bool TrySetRenderer(this Camera camera, UniversalRendererData rendererData)
        {
            var cameraData = camera.GetUniversalAdditionalCameraData();
            if (cameraData == null || !rendererData.TryGetIndexInPipeline(out int index)) { return false; }

            cameraData.SetRenderer(index);

            return true;
        }

        #endregion Camera
    }
}
