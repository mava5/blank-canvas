
using UnityEngine;
using UnityEngine.UI;

using HVLayoutGroup = UnityEngine.UI.HorizontalOrVerticalLayoutGroup;


/// <summary> Matches this <see cref="RectTransform"/> size to another's based on relative size. </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(RectTransform))]
public class RelativeSizeMatcher : MonoBehaviour, ILayoutElement, ILayoutSelfController
{
    public RectTransform Target = null;

    public bool IsClamped = true;
    public bool SetMinSize = true;
    public bool SetPreferredSize = true;
    [Range(-2f, 2f)] public float Width = 0f;
    [Range(-2f, 2f)] public float WidthHR = 0f;
    [Range(-2f, 2f)] public float Height = 0f;
    [Range(-2f, 2f)] public float HeightWR = 0f;

    private RectTransform rectTransform = null;
    private HVLayoutGroup hvGroup = null;
    
    private Vector2 targetSize = Vector2.zero;
    private Vector2 finalSize = Vector2.zero;


    #region Lifecycle =================================================================== Lifecycle

    private void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
        OnTransformParentChanged();
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }

    private void OnDisable()
    { LayoutRebuilder.MarkLayoutForRebuild(rectTransform); }

    private void Update()
    {
        Vector2 newTargetSize = Vector2.zero;
        bool hasValidTarget = false;

        if (Target != null)
        { hasValidTarget = true; newTargetSize = Target.rect.size; }
        else if (transform.parent is RectTransform parent)
        { hasValidTarget = true; newTargetSize = parent.rect.size; }

        if (hasValidTarget)
        {
            if (newTargetSize == targetSize) { return; }

            targetSize = newTargetSize;
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }
    }

    private void OnTransformParentChanged()
    { hvGroup = transform.parent != null ? transform.parent.GetComponent<HVLayoutGroup>() : null; }

#if UNITY_EDITOR
    private void OnValidate()
    { LayoutRebuilder.MarkLayoutForRebuild(rectTransform); }
#endif

    #endregion Lifecycle


    #region Layout ====================================================================== Layout

    public float minWidth => SetMinSize ? finalSize.x : 0;
    public float preferredWidth => SetPreferredSize ? finalSize.x : 0;
    public float flexibleWidth => 0;

    public float minHeight => SetMinSize ? finalSize.y : 0;
    public float preferredHeight => SetPreferredSize ? finalSize.y : 0;
    public float flexibleHeight => 0;

    public int layoutPriority => 1;

    private void UpdateTargetSize()
    {
        float widthPx = targetSize.x * Width + targetSize.y * WidthHR;
        float heightPx = targetSize.y * Height + targetSize.x * HeightWR;

        if (IsClamped)
        {
            widthPx = Mathf.Clamp(widthPx, 0, targetSize.x);
            heightPx = Mathf.Clamp(heightPx, 0, targetSize.y);
        }

        finalSize = new(widthPx, heightPx);
    }

    public void CalculateLayoutInputHorizontal() => UpdateTargetSize();
    public void CalculateLayoutInputVertical() => UpdateTargetSize();

    public void SetLayoutHorizontal()
    {
        bool affectedByGroup = hvGroup != null && hvGroup.enabled;
        if (!affectedByGroup || !hvGroup.childControlWidth)
        { rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalSize.x); }
        if (!affectedByGroup) { rectTransform.anchoredPosition = Vector2.zero; }
    }

    public void SetLayoutVertical()
    {
        bool affectedByGroup = hvGroup != null && hvGroup.enabled;
        if (!affectedByGroup || !hvGroup.childControlHeight)
        { rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalSize.y); }
        if (!affectedByGroup) { rectTransform.anchoredPosition = Vector2.zero; }
    }

    #endregion Layout
}
