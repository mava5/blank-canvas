
using System;
using UnityEngine;


/// <summary> Scales <see cref="RectTransform"/> so that it only covers up the phone's safe area. </summary>
[ExecuteAlways, RequireComponent(typeof(RectTransform))]
public class SafeAreaScaler : MonoBehaviour
{
    [Tooltip("Determines if GUI is forced to take into account a bottom safe area.\n" +
        "The area is the same size as the top safe area.")]
    [SerializeField] private bool forceUseBottomSafeArea = false;

#if UNITY_EDITOR
    [SerializeField, Range(0f, 500f)] private float editorSafeAreaHeight = 0f;
#endif

    private RectTransform rectTransform;
    private Rect lastSafeArea = new(0, 0, 0, 0);

    private bool useBottomSafeArea = false;

    private Action onDestroy = null;


    private void Awake()
    {
        //GameSettings.Visuals.GUI.BottomSafeArea.SubscribeUntilDestroyed(v => useBottomSafeArea = v, ref onDestroy, true);
        rectTransform = GetComponent<RectTransform>();
        AttemptUpdateSafeArea();
    }

    private void Update() => AttemptUpdateSafeArea();


    private void AttemptUpdateSafeArea()
    {
        if (Screen.width <= 0 || Screen.height <= 0) { return; }

        Rect safeArea = Screen.safeArea;

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

#if UNITY_EDITOR
        safeArea.height -= editorSafeAreaHeight;
#endif

        if (useBottomSafeArea || forceUseBottomSafeArea)
        {
            float topHeight = Screen.height - safeArea.max.y;
            float heightOffset = Mathf.Max(topHeight - safeArea.y, 0f);
            safeArea.height -= heightOffset;
            safeArea.y += heightOffset;
        }

        if (safeArea != lastSafeArea)
        {
            lastSafeArea = safeArea;
            rectTransform.anchorMin = new(safeArea.min.x / Screen.width, safeArea.min.y / Screen.height);
            rectTransform.anchorMax = new(safeArea.max.x / Screen.width, safeArea.max.y / Screen.height);
        }
    }
}
