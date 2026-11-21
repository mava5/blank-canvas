
//using ObjectDrawing.Profiles;

using System.Collections.Generic;

using UnityEngine;

using Utility;


namespace ObjectDrawing
{
    [DefaultExecutionOrder(0)]
    public class DrawableObjectTracker : MonoBehaviour
    {
        //public DrawingProfile TargetProfile => targetProfile;


        //[SerializeField] private DrawingProfile targetProfile = null;

        public bool IsTracking { get; private set; } = false;


        protected virtual bool InitialIsTrackingState => true;

        private List<DrawingSlice> occupiedSlices = new();

        private int maxTextureSize = 0;
        private int minTextureSize = 0;

        private bool fullCleanUpUnderway = false;


        private readonly EventCaller<bool> onIsTrackingChanged = new();
        internal IEvent<bool> OnIsTrackingChanged => onIsTrackingChanged;


        #region Lifecycle ========================================================================= Lifecycle

        protected virtual void Awake()
        {
            if (DrawingSystem.Instance == null) { return; }

            maxTextureSize = DrawingSystem.Instance.MaxTextureSize;
            minTextureSize = DrawingSystem.Instance.MinTextureSize;

            SetIsTracking(InitialIsTrackingState);
        }

        protected virtual void OnDestroy() => CleanUpFully();

        #endregion Lifecycle


        #region Internal Methods ================================================================== Internal Methods

        internal bool TryStartTracking(DrawableObject drawableObject, out int sliceIndex, out AtlasNode assignedNode)
        {
            sliceIndex = -1;
            assignedNode = null;

            if (!IsTracking) { return false; }
            if (fullCleanUpUnderway) { return false; }
            if (DrawingSystem.Instance == null || drawableObject.SliceIndex != -1) { return false; }

            // First try to add under the currently occupied slices
            foreach (DrawingSlice drawingSlice in occupiedSlices)
            {
                if (drawingSlice.TryAdd(drawableObject, out assignedNode))
                {
                    DrawingSystem.Instance.AddObjectToBeRegistered(drawableObject);
                    sliceIndex = drawingSlice.Index;
                    return true;
                }
            }

            // Then try to add to a newly reserved slice
            if (!DrawingSystem.Instance.TryReserveSlice(out sliceIndex))
            { Util.LogError<DrawableObjectTracker>("Out of free slices!"); return false; }

            DrawingSlice newSlice = new(sliceIndex, maxTextureSize, minTextureSize);
            Debug.Log("Occupied: " + sliceIndex);
            if (newSlice.TryAdd(drawableObject, out assignedNode) && assignedNode != null)
            {
                DrawingSystem.Instance.AddObjectToBeRegistered(drawableObject);
                occupiedSlices.Add(newSlice);
                return true;
            }
            else
            {
                DrawingSystem.Instance.FreeUpSlice(newSlice);
            }

            return false;
        }

        //internal void SetTargetProfile(DrawingProfile drawingProfile)
        //{
        //    if (occupiedSlices.Count > 0)
        //    { Util.LogError<DrawableObjectTracker>("Can't change profile when in use!"); return; }

        //    DrawingSystem.Instance = drawingProfile;
        //}

        internal void StopTracking(DrawableObject drawableObject)
        {
            if (fullCleanUpUnderway) { return; }
            if (DrawingSystem.Instance == null) { return; }

            for (int i = occupiedSlices.Count - 1; i >= 0; i--)
            {
                DrawingSlice drawingSlice = occupiedSlices[i];

                if (!drawingSlice.TryRemove(drawableObject)) { continue; }

                DrawingSystem.Instance.AddObjectToBeCleanedUp(drawableObject);

                if (drawingSlice.IsEmpty)
                {
                    DrawingSystem.Instance.AddSliceToBeCleanedUp(drawingSlice);
                    occupiedSlices.RemoveAt(i);
                }

                break;
            }
        }

        #endregion Internal Methods


        #region Protected Methods ================================================================= Protected Methods

        protected void SetIsTracking(bool isTracking)
        {
            bool originalState = IsTracking;
            IsTracking = isTracking;

            if (IsTracking != originalState)
            { onIsTrackingChanged.Invoke(isTracking); }
        }

        protected void CleanUpFully()
        {
            if (fullCleanUpUnderway) { return; }

            SetIsTracking(false);
            fullCleanUpUnderway = true;

            if (DrawingSystem.Instance == null || occupiedSlices.Count == 0) { return; }

            foreach (DrawingSlice drawingSlice in occupiedSlices)
            { DrawingSystem.Instance.AddSliceToBeCleanedUp(drawingSlice); }
        }

        #endregion Protected Methods


        //private void ClearNode(AtlasNode node)
        //{
        //    if (node == null) { return; }

        //    if (node.DrawableObject != null)
        //    { node.DrawableObject.SetAtlasNode(null); }

        //    node.DrawableObject = null;
        //    TryMerge(node);
        //}




        ///// <summary> Downscales and merges the root nodes of this and up to 3 more <see cref="DrawingSlice"/> into one. </summary>
        //public bool TryDownscaleAndMergeWith(List<DrawingSlice> drawingSlices)
        //{
        //    List<AtlasNode> roots = new(drawingSlices.Select(s => s.root)) { root };

        //    AtlasNode newRoot = null;


        //    // Validation --------------------------------------------------------------- Validation

        //    if (roots.Count < 1 || roots.Count > 4)
        //    { Util.LogError<DrawingSlice>($"Invalid root count: {roots.Count}!"); return false; }

        //    int startTextureSize = roots[0].TextureSize;
        //    int minTextureSize = roots[0].MinTextureSize;

        //    if (startTextureSize == minTextureSize)
        //    { Util.LogError<DrawingSlice>("Roots cannot be donwscaled!"); return false; }

        //    foreach (AtlasNode node in roots)
        //    {
        //        if (!node.IsRoot)
        //        { Util.LogError<DrawingSlice>("Not all nodes are roots!"); return false; }

        //        if (node.TextureSize != roots[0].TextureSize ||
        //            node.MinTextureSize != roots[0].MinTextureSize)
        //        { Util.LogError<DrawingSlice>("Root texture size mismatch!"); return false; }
        //    }

        //    newRoot = new(startTextureSize, minTextureSize, 0, null, this);

        //    if (!TrySplit(newRoot))
        //    { Util.LogError<DrawingSlice>("New root couldn't be split"); return false; }


        //    // Downscaling -------------------------------------------------------------- Downscaling

        //    Stack<AtlasNode> toDownScale = new(roots);
        //    List<DrawableObject> toUpdateIndexOf = new();

        //    while (toDownScale.Count > 0)
        //    {
        //        AtlasNode node = toDownScale.Pop();

        //        node.TextureSize = node.TextureSize / 2;

        //        // Nothing to downscale further
        //        if (!node.IsSplit) { continue; }

        //        // Remove child nodes if they cannot be downscaled
        //        if (node.TextureSize == minTextureSize)
        //        {
        //            foreach (AtlasNode child in node.Children)
        //            {
        //                if (child.DrawableObject != null)
        //                { child.DrawableObject.SetAtlasNode(null); }
        //            }
        //            TryMerge(node);
        //        }

        //        // Down scale children
        //        else
        //        {
        //            foreach (AtlasNode child in node.Children)
        //            {
        //                if (child.DrawableObject != null)
        //                { toUpdateIndexOf.Add(child.DrawableObject); }

        //                toDownScale.Push(child);
        //            }
        //        }
        //    }


        //    // Merging ------------------------------------------------------------------ Merging

        //    for (int i = 0; i < roots.Count; i++)
        //    {
        //        newRoot.Children[i] = roots[i];
        //        roots[i].Parent = newRoot;
        //        roots[i].PathIndex = i;
        //        roots[i].Slice = this;
        //    }

        //    foreach (DrawableObject drawableObject in toUpdateIndexOf)
        //    { drawableObject.RecalculateTextureIndex(); }

        //    root = newRoot;

        //    foreach (DrawingSlice drawingSlice in drawingSlices)
        //    { drawingSlice.root = new(MaxTextureSize, MinTextureSize, 0, null, drawingSlice); }

        //    return true;
        //}
    }
}
