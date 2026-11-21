
using System;

using UnityEngine;


namespace UtilityEnum
{
    public enum Space { Local, World };
    public enum Axis2D { x, y }
    public enum Axis3D { x, y, z }

    public enum VerticalAnchor
    { Top, Center, Bottom }

    public enum HorizontalAnchor
    { Left, Center, Right }

    public enum ColorChannel { r, g, b, a }

    public enum POT
    {
        _1, _2, _4, _8, _16, _32,
        _64, _128, _256, _512, _1024,
        _2048, _4096, _8192, _16384,
        _32768, _65536, _131072
    }

    /// <summary> The type of an inspectable Unity object. </summary>
    [Flags]
    public enum ObjType
    {
        None = 0,

        /// <summary> A normal scene-bound <see cref="GameObject"/>. </summary>
        Instance = 1 << 0,

        /// <summary> A prefab asset. </summary>
        Prefab = 1 << 1,

        /// <summary> An instantiated <see cref="GameObject"/> of a prefab asset. </summary>
        PrefabCopy = 1 << 2,

        /// <summary> A <see cref="UnityEngine.ScriptableObject"/>. </summary>
        ScriptableObject = 1 << 3,
    }

    public enum LoadingState
    {
        None,
        Unloaded,
        Unloading,
        Loaded,
        Loading,
    }
}
