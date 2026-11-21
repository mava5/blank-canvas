
#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.UI;


[DefaultExecutionOrder(0)]
public class MenuBook : MonoBehaviour
{
    public static Vector2? CanvasMousePosition { get; private set; } = null;

    public SkinnedMeshRenderer Renderer => renderer;
    public GameObject BookVisualObject => bookVisualObject;
    public Camera BookCamera => bookCamera;
    public Camera PageCanvasRayCamera => pageCanvasRayCamera;
    public RenderTexture CanvasRayRT => canvasRayRT;
    
    public Vector2 MouseNormalizedPosition { get; private set; } = Vector2.zero;
    public bool IsOpen { get; private set; } = false;

    [SerializeField] private Camera bookCamera = null;

    [Header("Page Canvas:")]
    [SerializeField] private Camera pageCanvasCamera = null;
    [SerializeField] private Camera pageCanvasRayCamera = null;
    [SerializeField] private Canvas pageCanvas = null;
    [SerializeField] private CanvasGroup pageCanvasGroup = null;
    [SerializeField] private RectTransform pageCenterTransform = null;
    [SerializeField] private AspectRatioFitter leftPageFitter = null;
    [SerializeField] private AspectRatioFitter rightPageFitter = null;
    [SerializeField, Range(100, 2000)] private int canvasHeight = 1000;
    [SerializeField, Range(0.01f, 10f)] private float pageAspectRatio = 1f;

    [Header("Skinned Mesh:")]
    [SerializeField] private GameObject bookVisualObject = null;
    [SerializeField] private Transform bookTransform = null;
    [SerializeField] private Transform leftBone = null;
    [SerializeField] private Transform rightBone = null;
    [SerializeField] new private SkinnedMeshRenderer renderer;

    private MaterialPropertyBlock mpb = null;
    private RenderTexture pageCanvasRT = null;
    private RenderTexture canvasRayRT = null;

    private bool isInteractable = true;
    private float openingAlpha = 0f;

    private Vector3 endPosition = Vector3.zero;
    private Vector3 endRotation = Vector3.zero;


    #region Lifecycle ============================================================================= Lifecycle

    protected virtual void Awake()
    {
        endPosition = bookTransform.position;
        endRotation = bookTransform.eulerAngles;

        mpb = new();
        renderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_PageAspectRatio", pageAspectRatio);
        renderer.SetPropertyBlock(mpb);

        canvasRayRT = new(1, 1, 16, RenderTextureFormat.RG32)
        { filterMode = FilterMode.Point, useMipMap = false, antiAliasing = 1 };
        canvasRayRT.Create();
        pageCanvasRayCamera.targetTexture = canvasRayRT;
        pageCanvasRayCamera.enabled = true;

        pageCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        pageCanvas.gameObject.SetActive(true);

        bookCamera.gameObject.SetActive(true);
        pageCanvasCamera.gameObject.SetActive(true);
        pageCanvasRayCamera.gameObject.SetActive(true);

        UpdatePageCanvasRenderTexture();
    }

    protected virtual void Start()
    {
        CanvasRayRendererFeature.Instance.SetMenuBook(this);
    }

    protected virtual void Update()
    {
        if (!IsOpen || !isInteractable)
        {
            CanvasMousePosition = null;
            return;
        }

        Vector3 mousePosition = Input.mousePosition;
        MouseNormalizedPosition = new(mousePosition.x / Screen.width, mousePosition.y / Screen.height);

        CanvasMousePosition = GetCanvasPosition();
    }

    protected virtual void OnDestroy()
    {
        CanvasRayRendererFeature.Instance.SetMenuBook(null);
        CleanUpRenderTexture(ref pageCanvasRT);
        CleanUpRenderTexture(ref canvasRayRT);
    }

    #endregion Lifecycle


    #region Public Methods ======================================================================== Public Methods

    public void SetInteractable(bool value)
    {
        isInteractable = value;
        pageCanvasGroup.interactable = IsOpen && isInteractable;
    }

    public void SetOpeningAlpha(float value)
    {
        openingAlpha = Mathf.Clamp01(value);

        IsOpen = openingAlpha > 0f;
        renderer.enabled = IsOpen;
        pageCanvasGroup.interactable = IsOpen && isInteractable;

        bookTransform.position = Vector3.Lerp(endPosition + new Vector3(0f, -0.1f, 0f), endPosition, openingAlpha);
        bookTransform.eulerAngles = Vector3.Lerp(endRotation + new Vector3(20f, 0f, 0f), endRotation, openingAlpha);
        leftBone.localEulerAngles = new(0f, 0f, Mathf.Lerp(0f, 80f, openingAlpha));
        rightBone.localEulerAngles = new(0f, 0f, Mathf.Lerp(0f, -80f, openingAlpha));

        if (mpb == null)
        {
            mpb = new();
            renderer.GetPropertyBlock(mpb);
        }

        mpb.SetFloat("_OpeningAlpha", openingAlpha);
        renderer.SetPropertyBlock(mpb);
    }

    #endregion Public Methods


    #region Private Methods ======================================================================= Private Methods

    private void CleanUpRenderTexture(ref RenderTexture texture)
    { if (texture != null) { texture.Release(); texture = null; } }

    private void UpdatePageCanvasRenderTexture()
    {
        CleanUpRenderTexture(ref pageCanvasRT);

        int width = Mathf.RoundToInt(canvasHeight * pageAspectRatio) * 2;
        int height = canvasHeight;

        pageCanvasRT = new(width, height, 16, RenderTextureFormat.ARGB32)
        { filterMode = FilterMode.Bilinear, useMipMap = false, antiAliasing = 2 };
        pageCanvasRT.Create();

        pageCanvasCamera.targetTexture = pageCanvasRT;
        mpb.SetTexture("_CanvasTexture", pageCanvasRT);
        renderer.SetPropertyBlock(mpb);
    }

    private Vector2? GetCanvasPosition()
    {
        if (CanvasRayRendererFeature.Instance == null ||
            !CanvasRayRendererFeature.Instance.CanvasRayUV.HasValue) { return null; }

        Vector2 pagesOffset = new(0.0244f, -0.0822f);
        float pagesHeight = 0.585f;
        float centerWidth = 0.031f;

        Vector2 uv = CanvasRayRendererFeature.Instance.CanvasRayUV.Value;
        uv -= pagesOffset;

        Vector2 uvScale_pivotPoint = new(0.25f, 0.25f);
        Vector2 uvScale_scale = new(pageAspectRatio * 2f * pagesHeight, pagesHeight);
        Vector2 uvScale_uv = uv;
        uvScale_uv -= uvScale_pivotPoint;
        uvScale_uv *= new Vector2(1f / uvScale_scale.x, 1f / uvScale_scale.y);
        uvScale_uv += uvScale_pivotPoint;
        uv = uvScale_uv;

        uv *= 2f;
        bool unshiftedOnLeftPage = uv.x <= 0.5f;
        uv.x += (unshiftedOnLeftPage ? 1 : -1) * centerWidth;
        bool shiftedOnLeftPage = uv.x <= 0.5f;

        bool valid = ((unshiftedOnLeftPage && shiftedOnLeftPage) ||
            (!unshiftedOnLeftPage && !shiftedOnLeftPage)) &&
            uv.x > 0f && uv.x <= 1f && uv.y > 0f && uv.y <= 1f;

        return valid ? uv * new Vector2(pageCanvasRT.width, pageCanvasRT.height) : null;
    }

    #endregion Private Methods


#if UNITY_EDITOR

    protected virtual void OnValidate()
    {
        if (Application.isPlaying) { return; }

        EditorApplication.delayCall += OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        EditorApplication.delayCall -= OnEditorUpdate;

        if (leftPageFitter != null)
        { leftPageFitter.aspectRatio = pageAspectRatio; }

        if (rightPageFitter != null)
        { rightPageFitter.aspectRatio = pageAspectRatio; }
    }

#endif
}
