
#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;

using UnityEngine;

using UtilityEnum;
using System.Linq;

using static TryResult;


namespace Utility
{
    // Define IsExternalInit to be able to use init keyword
    // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/init
    namespace System.Runtime.CompilerServices { public interface IsExternalInit { } }

    public class ControlledException : Exception
    {
        public ControlledException() { }
        public ControlledException(string message) : base(message) { }
        public ControlledException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class TypeAndName
    {
        public string Name = null;
        public Type Type = null;

        protected internal TypeAndName(Type type, string name)
        {
            Type = type;
            Name = name;
        }

        public static TypeAndName For<T>(string name) => new(typeof(T), name);
    }

    public static class Util
    {
        #region Math ============================================================================== Math

        public static bool IsPOT(int value)
        { return value > 0 && (value & (value - 1)) == 0; }

        public static int POTValue(POT potType)
        {
            return potType switch
            {
                POT._1 => 1,
                POT._2 => 2,
                POT._4 => 4,
                POT._8 => 8,
                POT._16 => 16,
                POT._32 => 32,
                POT._64 => 64,
                POT._128 => 128,
                POT._256 => 256,
                POT._512 => 512,
                POT._1024 => 1024,
                POT._2048 => 2048,
                POT._4096 => 4096,
                POT._8192 => 8192,
                POT._16384 => 16384,
                POT._32768 => 32768,
                POT._65536 => 65536,
                POT._131072 => 131072,
                _ => 0
            };
        }

        #endregion Math


        #region Exception ========================================================================= Exception

        private static string GetFormatedLog(string context, string message, bool controlled)
        { return $"[{context}{(controlled ? "" : " | Unexpected")}]: {message}"; }

        [HideInCallstack]
        public static void Log<T>(string message, bool controlled = true)
        { Debug.Log(GetFormatedLog(typeof(T).Name, message, controlled)); }
        [HideInCallstack]
        public static void Log(string context, string message, bool controlled = true)
        { Debug.Log(GetFormatedLog(context, message, controlled)); }

        [HideInCallstack]
        public static void LogWarning<T>(string message, bool controlled = true)
        { Debug.LogWarning(GetFormatedLog(typeof(T).Name, message, controlled)); }
        [HideInCallstack]
        public static void LogWarning(string context, string message, bool controlled = true)
        { Debug.LogWarning(GetFormatedLog(context, message, controlled)); }

        [HideInCallstack]
        public static void LogError<T>(string message, bool controlled = true)
        { Debug.LogError(GetFormatedLog(typeof(T).Name, message, controlled)); }
        [HideInCallstack]
        public static void LogError(string context, string message, bool controlled = true)
        { Debug.LogError(GetFormatedLog(context, message, controlled)); }

        #endregion Exception


        #region Textures ========================================================================== Textures

        /// <summary> Generates a list of <see cref="Rect"/> that points to the region of an atlas per given <see cref="Texture2D"/>. </summary>
        public static bool TryGenerateAtlas(ICollection<Texture2D> textures, int minSize, int atlasSize, int padding, out List<Rect> results)
        { return TryGenerateAtlas(textures.Select(t => new Vector2(t.width, t.height)).ToArray(), minSize, atlasSize, padding, out results); }

        /// <summary> Generates a list of <see cref="Rect"/> that points to the region of an atlas per given <see cref="Vector2"/> size. </summary>
        public static bool TryGenerateAtlas(Vector2[] inputSizes, int minSize, int atlasSize, int padding, out List<Rect> results)
        {
            results = new();

            if (inputSizes == null) { return false; }
            if (minSize < 1) { return false; }
            if (minSize > atlasSize) { return false; }
            if (padding < 0) { return false; }
            if (atlasSize <= 0) { return false; }

            int currentIndex = 0;
            Vector2[] sizes = (Vector2[])inputSizes.Clone();
            float[] aspects = inputSizes.Select(s => s.x / s.y).ToArray();
            bool[] isMinSize = new bool[sizes.Length];

            // Tries to downscale size of given index
            (bool shrunk, bool stillTooBig) TryDownscale(int index)
            {
                Vector2 size = sizes[index];
                if (isMinSize[index]) { return (false, size.x > atlasSize || size.y > atlasSize); }

                Vector2 newSize = size / 2f;

                if (newSize.x <= minSize || newSize.y <= minSize)
                {
                    float aspect = aspects[index];

                    newSize = aspect >= 1f ?
                        new(minSize * aspect, minSize) :
                        new(minSize, minSize * (1f / aspect));

                    isMinSize[index] = true;
                }

                sizes[index] = newSize;

                return (true, newSize.x > atlasSize || newSize.y > atlasSize);
            }

            // Downscale textures until all of them fit into the atlas
            if (sizes.Any(s => s.x > atlasSize || s.y > atlasSize))
            {
                while (true)
                {
                    int oversizedCount = 0;

                    for (int i = 0; i < sizes.Length; i++)
                    {
                        (bool shrunk, bool stillTooBig) = TryDownscale(i);

                        // Size cannot physically be included in the atlas
                        if (!shrunk && stillTooBig) { return false; }

                        if (stillTooBig) { oversizedCount++; }
                    }

                    if (oversizedCount == 0) { break; }
                }
            }

            // Try generate atlas until it either succeeds or the textures cannot be scaled down anymore
            int iterationsSinceLastShrink = 0;
            while (true)
            {
                if (Texture2D.GenerateAtlas(sizes, padding, atlasSize, results) &&
                    !results.Any(r => r.width == 0 || r.height == 0)) { return true; }

                if (TryDownscale(currentIndex).shrunk)
                { iterationsSinceLastShrink = 0; }

                currentIndex = (currentIndex + 1) % sizes.Length;

                // All sizes were gone through and none were able to be shrunk
                if (iterationsSinceLastShrink == sizes.Length) { return false; }

                iterationsSinceLastShrink++;
            }
        }

        #endregion Textures
    }
}
