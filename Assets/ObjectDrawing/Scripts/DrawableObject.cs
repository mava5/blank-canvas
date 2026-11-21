
#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;

using UnityEngine;

using Utility;
using UtilityEditor;
using UtilityEnum;
using UtilityExtensions;
using UnityEngine.Rendering;


namespace ObjectDrawing
{
    [DefaultExecutionOrder(1)]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class DrawableObject : MonoBehaviour
    {
        /// <summary> The index given from <see cref="DrawingProfile"/>. </summary>
        public int? ObjectIndex = null;

        /// <summary> How much of the texture space the UVs occupy. </summary>
        protected internal float UVCoverage { get; private set; } = 1f;

        protected internal int SliceIndex { get; private set; } = -1;
        protected internal int TextureIndex { get; private set; } = -1;
        protected internal int TextureSize { get; private set; } = -1;
        protected internal int XMin { get; private set; } = 0;
        protected internal int YMin { get; private set; } = 0;

        protected internal bool IsPaintable => ObjectIndex.HasValue && SliceIndex != -1 && TextureIndex != -1;

        protected EventCaller<List<int>> onColorCoverageChanged = new();
        public IEvent<List<int>> OnColorCoverageChanged => onColorCoverageChanged;


        [Header("Texture:")]
        [EnableFor(ObjType.Prefab | ObjType.Instance)]
        [SerializeField, DrawingPOT] private int baseTextureSize = 0;
        [Space]
        [ShowFor(ObjType.PrefabCopy | ObjType.Instance)]
        [SerializeField] private bool overwriteTextureSize = false;
        [ShowFor(ObjType.PrefabCopy | ObjType.Instance), ShowIf("overwriteTextureSize")]
        [SerializeField, DrawingPOT] private int newTextureSize = 0;
        [Space]
        [ShowFor(ObjType.PrefabCopy | ObjType.Instance)]
        [SerializeField, Range(1f, 200f)] private float closestViewingDistance = 1f;

        private DrawableObjectTracker tracker = null;

        private MeshRenderer meshRenderer = null;
        private MaterialPropertyBlock mpb = null;

        private uint[] colorAmounts = null;
        private float[] colorCoverages = null;

        public float[] ColorCoverages => colorCoverages;


        #region Lifecycle ========================================================================= Lifecycle

        private void Awake()
        {
            if (!TryGetComponent(out meshRenderer))
            { Util.LogError<DrawableObject>($"A meshRenderer is missing from {gameObject.name}!"); return; }

            colorCoverages = new float[DrawingSystem.Instance.MaxColors];
            colorAmounts = new uint[DrawingSystem.Instance.MaxColors];

            SliceIndex = -1;
            TextureIndex = -1;
            TextureSize = GetTargetTextureSize();
            UpdateRSUV();

            AttemptSetTrackingFromAncestor();
        }

        private void Update()
        {
            if (!IsPaintable) { return; }

            if (!DrawingSystem.Instance.TryGetColorAmountsFor(this, colorAmounts)) { return; }

            List<int> changedColors = new();
            for (int i = 0; i < colorAmounts.Length; i++)
            {
                float coverage = (colorAmounts[i] / (float)(TextureSize * TextureSize)) * UVCoverage;

                if (coverage != colorCoverages[i])
                {
                    //if (coverage > 0.2f && gameObject.name.Contains("Fence")) { Destroy(gameObject); return; }

                    changedColors.Add(i);
                    colorCoverages[i] = coverage;
                }
            }

            if (changedColors.Count > 0)
            { onColorCoverageChanged.Invoke(changedColors); }
        }

        private void OnDestroy()
        { if (tracker != null) { tracker.StopTracking(this); } }

        private void OnTransformParentChanged()
        {
            if (tracker != null)
            { SetTracker(null); }

            SetTracker(transform.GetComponentInAncestor<DrawableObjectTracker>());
        }

        //private void OnBufferChanged()
        //{
        //    if (!IsPaintable || currentProfile == null) { return; }

        //    if (currentProfile.ContentSummary.HasChanged(ObjectIndex.Value, currentInfo))
        //    {
        //        currentInfo = currentProfile.ContentSummary.Get(ObjectIndex.Value);
        //        onContentSummaryChanged.Invoke(currentInfo);
        //    }
        //}

        #endregion Lifecycle


        #region Internal Methods ================================================================== Internal Methods

        internal static void SetRSUV(uint sliceIndex, uint textureIndex, uint textureSize, MeshRenderer meshRenderer)
        {
            uint rsuv =
                (((sliceIndex + 1) & 0xFF) << 24) |
                (((textureIndex + 1) & 0xFFFFF) << 4) |
                ((uint)Mathf.RoundToInt(Mathf.Log(textureSize, 2f)) % 0xF);

            meshRenderer.SetShaderUserValue(rsuv);
        }

        internal void SetObjectIndex(int? objectIndex)
        {
            if (meshRenderer == null) { return; }

            ObjectIndex = objectIndex;
            //mpb.SetInt("_ObjectIndex", ObjectIndex ?? 0);
            //meshRenderer.SetPropertyBlock(mpb);
        }

        internal void SetSliceIndex(int sliceIndex)
        {
            if (meshRenderer == null) { return; }

            SliceIndex = sliceIndex;
            UpdateRSUV();
        }

        internal void SetPositionInformation(int textureIndex, int xMin, int yMin)
        {
            if (meshRenderer == null) { return; }

            TextureIndex = textureIndex;
            XMin = xMin;
            YMin = yMin;
            UpdateRSUV();
        }

        internal void SetTextureSize(int textureSize)
        {
            if (meshRenderer == null) { return; }

            TextureSize = textureSize;
            UpdateRSUV();
        }


        internal int GetTargetTextureSize()
        {
            if (overwriteTextureSize) { return newTextureSize; }

            //DrawingProfile profile = null;

            //if (transform.TryGetComponentInAncestor(out DrawableObjectTracker newTracker))
            //{
            //    profile = newTracker.TargetProfile;
            //}
            //else if (awakeTargetProfile != null && awakeTargetProfile.DefaultTracker != null)
            //{
            //    profile = awakeTargetProfile.DefaultTracker.TargetProfile;
            //}

            //if (profile == null) { return baseTextureSize; }

            Vector3 lossyScale = transform.lossyScale;
            float sizeAverage = (Mathf.Abs(lossyScale.x) + Mathf.Abs(lossyScale.y) + Mathf.Abs(lossyScale.z)) / 3f;
            float distanceMultiplier = Mathf.Clamp01(10f / Mathf.Pow(closestViewingDistance, 0.9f));
            int scaledSize = Mathf.RoundToInt(baseTextureSize * sizeAverage * distanceMultiplier).ClosestPOT();

            return Mathf.Clamp(scaledSize, /*profile*/DrawingSystem.Instance.MinTextureSize, /*profile*/DrawingSystem.Instance.MaxTextureSize);
        }

        internal void FinishRegistration(int objectIndex, RTHandle drawingArrayRTH, int maxTextureSize)
        { SetRegistrationInfo(objectIndex, drawingArrayRTH, maxTextureSize); }

        internal void FinishUnregistration()
        { SetRegistrationInfo(null, null, 0); }


        //internal void RecalculateTextureIndex()
        //{
        //    if (meshRenderer == null || !IsDrawable) { return; }

        //    // TODO: Figure out how to get texture index from atlas node path

        //    TextureIndex = textureIndex;
        //    mpb.SetInt("_TextureIndex", textureIndex);
        //    meshRenderer.SetPropertyBlock(mpb);
        //}

        //internal void SetSliceIndex(int sliceIndex)
        //{
        //    if (meshRenderer == null) { return; }

        //    SliceIndex = sliceIndex;
        //    mpb.SetInt("_SliceIndex", sliceIndex);
        //    meshRenderer.SetPropertyBlock(mpb);
        //}

        //internal bool TryDownscaleTextureSize()
        //{
        //    throw new System.Exception("Not implemented yet!");

        //    if (TextureSize <= DrawingSystem.Instance.MinSliceSize) { return false; }

        //    TextureSize /= 2;
        //    mpb.SetInt("_TextureSize", TextureSize);
        //    return true;
        //}

        #endregion Internal Methods


        #region Private Methods =================================================================== Private Methods

        private void AttemptSetTrackingFromAncestor()
        {
            if (tracker != null) { SetTracker(null); }

            if (transform.TryGetComponentInAncestor(out DrawableObjectTracker newTracker))
            {
                if (newTracker.IsTracking)
                {
                    SetTracker(newTracker);
                }
                else
                {
                    newTracker.OnIsTrackingChanged.OnceSimple += () => { SetTracker(newTracker); };
                }
            }
            //else if (awakeTargetProfile != null && awakeTargetProfile.DefaultTracker != null)
            //{
            //    SetTracker(awakeTargetProfile.DefaultTracker);
            //}
        }

        private void SetTracker(DrawableObjectTracker value)
        {
            if (tracker != null && tracker == value) { return; }

            if (value == null)
            {
                if (tracker != null) { tracker.StopTracking(this); }

                //if (currentProfile != null) { currentProfile.OnBufferChanged.Always -= OnBufferChanged; }

                tracker = null;
                SetSliceIndex(-1);
                SetPositionInformation(-1, 0, 0);
                SetTextureSize(1);
                //currentProfile = null;
            }
            else if (value.TryStartTracking(this, out int sliceIndex, out AtlasNode assignedNode))
            {
                tracker = value;
                //currentProfile = tracker.TargetProfile;

                //currentProfile.OnBufferChanged.Always += OnBufferChanged;

                SetSliceIndex(sliceIndex);
                SetPositionInformation(assignedNode.TextureIndex, assignedNode.Min.x, assignedNode.Min.y);
                SetTextureSize(GetTargetTextureSize());
            }
            else
            {
                Debug.Log("FAIL");
            }

        }

        private void SetRegistrationInfo(int? objectIndex, RTHandle drawingArrayRTH, int maxTextureSize)
        {
            ObjectIndex = objectIndex;

            //mpb.SetTexture("_DrawingArrayTexture", drawingArrayRTH);
            //mpb.SetInt("_MaxTextureSize", maxTextureSize);
            if (meshRenderer != null) { meshRenderer.SetPropertyBlock(mpb); }
        }

        private void UpdateRSUV() => SetRSUV((uint)SliceIndex, (uint)TextureIndex, (uint)TextureSize, meshRenderer);

        //private void SetSliceIndex(int sliceIndex)
        //{
        //    SliceIndex = sliceIndex;
        //    mpb.SetInt("_SliceIndex", SliceIndex);
        //    if (meshRenderer != null) { meshRenderer.SetPropertyBlock(mpb); }
        //}

        //private void SetPositionInformation(int textureIndex, int xMin, int yMin)
        //{
        //    TextureIndex = textureIndex;
        //    XMin = xMin;
        //    YMin = yMin;
        //    mpb.SetInt("_TextureIndex", TextureIndex);
        //    if (meshRenderer != null) { meshRenderer.SetPropertyBlock(mpb); }
        //}

        //private void SetTextureSize(int textureSize)
        //{
        //    TextureSize = textureSize;
        //    mpb.SetInt("_TextureSize", TextureSize);
        //    if (meshRenderer != null) { meshRenderer.SetPropertyBlock(mpb); }
        //}

        #endregion Private Methods



#if UNITY_EDITOR

        [NonSerialized] private bool connected = false;
        [NonSerialized] private MeshRenderer editorMeshRenderer = null;

        private void OnValidate()
        {
            if (!connected)
            {
                connected = true;
                editorMeshRenderer = GetComponent<MeshRenderer>();
            }

            if (Application.isPlaying) { return; }

            SetRSUV(0, 0, (uint)GetTargetTextureSize(), editorMeshRenderer);
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (closestViewingDistance <= 1f) { return; }

            Gizmos.color = new(0.6f, 0.6f, 1f);
            Gizmos.DrawWireSphere(transform.position, closestViewingDistance);
            Gizmos.color = Color.white;
        }

#endif
    }


#if UNITY_EDITOR

    [CustomEditor(typeof(DrawableObject)), CanEditMultipleObjects]
    public class DrawableObjectEditor : AdvancedEditor
    {
        private DrawableObject drawableObject = null;
        private MeshRenderer meshRenderer = null;

        private Vector3 lastSize = Vector3.zero;


        protected override void OnEnableInternal()
        {
            drawableObject = (DrawableObject)target;
            meshRenderer = drawableObject.GetComponent<MeshRenderer>();

            Undo.undoRedoPerformed += UpdateTexture;
        }

        protected override void OnDisableInternal()
        {
            Undo.undoRedoPerformed -= UpdateTexture;
        }

        protected override void OnInspectorGUIBegin()
        {
            EditorGUI.BeginChangeCheck();
        }

        protected override void OnInspectorGUILast()
        {
            if (!EditorGUI.EndChangeCheck() &&
                (drawableObject.transform.lossyScale - lastSize).magnitude < 0.001f) { return; }

            lastSize = drawableObject.transform.lossyScale;
            UpdateTexture();
        }

        private void UpdateTexture()
        {
            DrawableObject.SetRSUV(0, 0, (uint)drawableObject.GetTargetTextureSize(), meshRenderer);
        }
    }

#endif
}
