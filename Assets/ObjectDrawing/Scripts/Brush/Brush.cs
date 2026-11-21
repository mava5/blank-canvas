
using System;

using UnityEngine;


namespace ObjectDrawing
{
    ///// <summary> Flags that determine how drawing interacts with previous paint of specific index. </summary>
    //[Flags]
    //public enum DrawFlags
    //{
    //    /// <summary> Paint is completely hidden. </summary>
    //    HideFully,

    //    /// <summary> Paint is shown but drawing won't interact with it. </summary>
    //    ShowButDontDraw,

    //    /// <summary> Paint is shown and can be drawn over. </summary>
    //    ShowAndDraw,
    //}


    public abstract class Brush
    {
        public bool IsJittered = false;

        //internal DrawFlags[] ColorIndexDrawFlags { get; private set; } = null;

        protected internal virtual Material FinalMaterial => BaseMaterial;
        protected internal MaterialPropertyBlock MPB { get; }

        protected internal abstract bool FinalAppliesToPaintOnly { get; }

        protected Material BaseMaterial { get; private set; } = null;


        protected Brush()
        {
            MPB = new();
            BaseMaterial = DrawingSystem.Instance.BaseBrushMaterial;
            //ColorIndexDrawFlags = new DrawFlags[DrawingSystem.Instance.MaxColors];
            //SetAllColorIndexFlags(DrawFlags.ShowAndDraw);
        }


        //public void SetManyColorIndexFlags(params (int, DrawFlags)[] colorIndexDrawFlags)
        //{
        //    if (colorIndexDrawFlags == null || colorIndexDrawFlags.Length == 0) { return; }

        //    foreach ((int index, DrawFlags drawFlags) in colorIndexDrawFlags)
        //    {
        //        if (index < 0 || index >= ColorIndexDrawFlags.Length) { continue; }
        //        ColorIndexDrawFlags[index] = drawFlags;
        //    }
        //}

        //public void SetManyColorIndexFlags(DrawFlags othersFlags, params (int, DrawFlags)[] colorIndexDrawFlags)
        //{
        //    if (colorIndexDrawFlags == null || colorIndexDrawFlags.Length == 0) { return; }

        //    SetAllColorIndexFlags(othersFlags);
        //    SetManyColorIndexFlags(colorIndexDrawFlags);
        //}

        //public void SetAllColorIndexFlags(DrawFlags drawFlags)
        //{ for (int i = 0; i < ColorIndexDrawFlags.Length; i++) { ColorIndexDrawFlags[i] = drawFlags; } }
    }
}
