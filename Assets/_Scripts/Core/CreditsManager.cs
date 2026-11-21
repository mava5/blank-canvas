
using DG.Tweening;
using DG.Tweening.Core.Easing;
using ObjectDrawing;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class CreditsManager : MonoBehaviour
{
    private class BrushStroke
    {
        private static BrushStroke previousStroke;

        private readonly bool isErasing;
        private readonly int colorIndex;
        private readonly int strokeNum;
        private readonly int drawsGoal;
        private int drawsDone;

        private readonly CreditsManager manager;
        private readonly List<ColoredBrush> brushes;
        private readonly RectTransform paintArea;

        private readonly Vector2 normalDir;
        private readonly float noiseOffset;

        private float targetAlpha;


        public BrushStroke(int colorIndex, int drawsGoal, RectTransform paintArea,
            CreditsManager manager, List<ColoredBrush> brushes)
        {
            this.colorIndex = colorIndex;
            strokeNum = Random.Range(-1000, 1000);
            this.drawsGoal = Mathf.Max(drawsGoal - 1, 0);
            drawsDone = 0;

            this.manager = manager;
            this.brushes = brushes;
            this.paintArea = paintArea;

            normalDir = Vector2.up;
            noiseOffset = Random.Range(-1000f, 1000f);
            targetAlpha = 0f;
        }

        public void SetTargetAlpha(float value) => targetAlpha = Mathf.Clamp01(value);

        public bool TryDrawNext()
        {
            if (drawsDone > drawsGoal) { return false; }

            if (this != previousStroke)
            {
                brushes[colorIndex].IsErasing = isErasing;
                brushes[colorIndex].StrokeNum = strokeNum;
            }
            previousStroke = this;

            Vector3[] corners = new Vector3[4];

            paintArea.GetWorldCorners(corners);
            float xMin = float.MaxValue, xMax = float.MinValue;
            float yMin = float.MaxValue, yMax = float.MinValue;

            foreach (Vector3 corner in corners)
            {
                if (corner.x < xMin) { xMin = corner.x; }
                if (corner.x > xMax) { xMax = corner.x; }
                if (corner.y < yMin) { yMin = corner.y; }
                if (corner.y > yMax) { yMax = corner.y; }
            }

            Vector3 left = new(xMin - 1f, (yMin + yMax) / 2f, -8f);
            Vector3 right = new(xMax + 1f, (yMin + yMax) / 2f, -8f);

            if (drawsGoal == 0)
            {
                DrawingSystem.DrawOrtho(brushes[colorIndex], new(left.x, left.y, -8f), Quaternion.Euler(0f, 0f, 0f), (manager.lastRows ? 2f : 1f) * manager.brushRadius);
                return false;
            }

            float alpha = drawsDone / (float)drawsGoal;
            float alphaNext = Mathf.Clamp01((drawsDone + 1) / (float)drawsGoal);
            float easeAlpha = EaseManager.Evaluate(Ease.InOutSine, null, alpha, 1f, 0f, 0f);
            float easeAlphaNext = EaseManager.Evaluate(Ease.InOutSine, null, alphaNext, 1f, 0f, 0f);
            float alpha_A = 1f - Mathf.Clamp01(Mathf.Abs((alpha * 2f) - 1f));

            (Vector2 pos0, float rot0) = GetPosRot(left, right, easeAlpha);
            (Vector2 pos1, float rot1) = GetPosRot(left, right, (easeAlpha + easeAlphaNext) / 2f);

            brushes[colorIndex].Intensity = Mathf.Pow(alpha_A, 0.5f);
            DrawingSystem.DrawOrtho(brushes[colorIndex], new(pos0.x, pos0.y, -8f), Quaternion.Euler(0f, 0f, rot0), alpha_A * (manager.lastRows ? 2f : 1f) * manager.brushRadius);

            brushes[colorIndex].Intensity = Mathf.Pow(alpha_A * 0.5f, 0.5f);
            DrawingSystem.DrawOrtho(brushes[colorIndex], new(pos1.x, pos1.y, -8f), Quaternion.Euler(0f, 0f, rot1), alpha_A * (manager.lastRows ? 2f : 1f) * manager.brushRadius);

            drawsDone++;

            return true;
        }

        public bool TryDrawUpToTargetAlpha(int maxDraws) => TryDrawUpToAlpha(targetAlpha, maxDraws);

        public bool TryDrawUpToAlpha(float inputAlpha, int maxDraws)
        {
            if (drawsDone > drawsGoal) { return false; }

            float alpha = Mathf.Clamp01(inputAlpha);
            int drawsTarget = Mathf.FloorToInt(drawsGoal * alpha) + 1;
            int drawsToDo = Mathf.Clamp(drawsTarget - drawsDone, 0, maxDraws);

            for (int i = 0; i < drawsToDo; i++)
            { if (!TryDrawNext()) { return false; } }

            return true;
        }


        private (Vector2 position, float rotation) GetPosRot(Vector3 left, Vector3 right, float alpha)
        {
            Vector2 pos0 = GetPos(left, right, alpha);
            Vector2 pos1 = GetPos(left, right, alpha + 0.01f);
            Vector2 offset = pos1 - pos0;
            float rotation = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;

            return (pos0, -(rotation - 90f));
        }

        private Vector2 GetPos(Vector3 left, Vector3 right, float alpha)
        {
            float perlin = (Mathf.PerlinNoise1D(noiseOffset + (alpha / manager.noiseSize)) - 0.5f) * manager.noiseAmplitude;
            float alpha_v = Mathf.Clamp01(Mathf.Abs((alpha * 2f) - 1f));
            perlin *= Mathf.Lerp(manager.noiseAmplitudeEndMultiplier, 1f, alpha_v);

            return Vector2.Lerp(left, right, alpha) + normalDir * perlin;
        }
    }

    [SerializeField] private Image fadeImage = null;
	[SerializeField] private AudioSource musicSource = null;
	[SerializeField] private RectTransform creditsTransform = null;
    [SerializeField] private Transform drawableQuadTransform = null;
    [SerializeField] private AnimationCurve creditsSpeedCurve = null;
	[SerializeField] private List<Color> colors = new();

    [Header("Brush:")]
    [SerializeField, Range(0f, 5f)] private float brushRadius = 0.1f;
    [SerializeField, Range(0f, 5f)] private float noiseAmplitude = 0.1f;
    [SerializeField, Range(0f, 1f)] private float noiseAmplitudeEndMultiplier = 1f;
    [SerializeField, Range(0.01f, 5f)] private float noiseSize = 1f;

    [Header("Strokes:")]
    [SerializeField, Range(1, 10)] private int maxSimulatenousStrokes = 5;
    [SerializeField, Range(0f, 3f)] private float strokeDuration = 0.1f;
    [SerializeField, Range(1, 100)] private int maxDrawsPerStroke = 10;
    [SerializeField, Range(1, 10)] private int maxDrawsPerFrame = 2;

    [Header("Audio:")]
    [SerializeField] private AudioSource audioSource = null;
    [SerializeField] private List<AudioClip> brushAudioClips = new();

    private RTHandle drawingColorsRTH = null;

	private List<RectTransform> paintAreas = new();

    private float creditsAlpha = 0f;
    private bool lastRows = false;

    private LinkedList<(Tween tween, BrushStroke brushStroke)> brushStrokes = new();

    private List<Color> currentColors = new();
    private List<TextureBrush> brushes = new();

    private float fadingAlpha = 0f;
    private Tween fadingTween = null;
    private bool fadingOut = false;


    protected virtual void Awake()
    {
        UpdateDrawingColorsTexture();

        foreach (GameObject gameObject in GameObject.FindGameObjectsWithTag("Finish").ToList())
		{ paintAreas.Add((RectTransform)gameObject.transform); }

        for (int i = 0; i < colors.Count; i++)
        { brushes.Add(new(null) { IsJittered = true, IsErasing = false, ColorIndex = i }); }

        musicSource.volume = 0f;
        audioSource.volume = 0f;

        fadeImage.color = Color.black;
        fadeImage.gameObject.SetActive(true);

        fadingTween = DOVirtual.Float(0f, 1f, 2f, alpha =>
        {
            fadingAlpha = alpha;
            fadeImage.color = Color.Lerp(Color.black, Color.clear, alpha);
            musicSource.volume = alpha;
            audioSource.volume = alpha;
        })
            .SetEase(Ease.InOutSine)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                fadingAlpha = 1f;
                fadeImage.color = Color.clear;
                musicSource.volume = 1f;
                audioSource.volume = 1f;
            });
	}

	protected virtual void Update()
	{
#if UNITY_EDITOR
        if (currentColors.Count != colors.Count)
        {
            UpdateDrawingColorsTexture();
        }
        else
        {
            for (int i = 0; i < colors.Count; i++)
            {
                if (currentColors[i] != colors[i])
                {
                    UpdateDrawingColorsTexture();
                    break;
                }
            }
        }
#endif

        bool wantsToSkip = Input.GetKeyDown(KeyCode.Space);
        bool isOver = creditsAlpha >= 1f;

        if (!fadingOut && (wantsToSkip || isOver))
        {
            fadingOut = true;

            if (fadingTween?.IsActive() ?? false)
            { fadingTween.Kill(); }

            fadingTween = DOVirtual.Float(fadingAlpha, 0f, wantsToSkip ? 0.5f : 3f, alpha =>
            {
                fadeImage.color = Color.Lerp(Color.black, Color.clear, alpha);
                musicSource.volume = alpha;
                audioSource.volume = alpha;
            })
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    fadingAlpha = 0f;
                    fadeImage.color = Color.black;
                    musicSource.volume = 0f;
                    audioSource.volume = 0f;
                    SceneManager.LoadScene("_HubScene");
                });
        }

        var node = brushStrokes.First;
        while (node != null)
        {
            var nextNode = node.Next;
            var (tween, brushStroke) = node.Value;

            if (!brushStroke.TryDrawUpToTargetAlpha(maxDrawsPerFrame))
            {
                if (tween != null && tween.IsActive()) { tween.Kill(); }
                brushStrokes.Remove(node);
            }

            node = nextNode;
        }

        Vector3[] corners = new Vector3[4];

        for (int i = paintAreas.Count - 1; i >= 0; i--)
        {
			RectTransform paintArea = paintAreas[i];
            paintArea.GetWorldCorners(corners);
            
			foreach (Vector3 corner in corners)
            {
                if (corner.y < -1f) { continue; }

				paintAreas.RemoveAt(i);
                if (paintAreas.Count < 2) { lastRows = true; }

                audioSource.PlayOneShot(brushAudioClips[Random.Range(0, brushAudioClips.Count)], Random.Range(0.95f, 1.05f));

                if (brushStrokes.Count > maxSimulatenousStrokes)
                {
                    (Tween oldTween, _) = brushStrokes.Last();
                    if (oldTween != null && oldTween.IsActive()) { oldTween.Kill(); }
                    brushStrokes.RemoveLast();
                }

                int colorIndex = Random.Range(0, colors.Count);
                BrushStroke brushStroke = new(colorIndex, maxDrawsPerStroke, paintArea, this, brushes.OfType<ColoredBrush>().ToList());

                Tween tween = DOVirtual.Float(0f, 1f, strokeDuration, alpha => { brushStroke.SetTargetAlpha(alpha); })
                    .OnComplete(() => brushStroke.SetTargetAlpha(1f));

                brushStrokes.AddFirst((tween, brushStroke));

                break;
            }
        }

        creditsAlpha = Mathf.Clamp01(creditsAlpha + (Time.deltaTime / 25f));
        float easeAlpha = creditsSpeedCurve.Evaluate(creditsAlpha);

        creditsTransform.anchorMin = new(0f, Mathf.Lerp(-1.8f, 0f, easeAlpha));
        creditsTransform.anchorMax = new(1f, Mathf.Lerp(0.2f, 2f, easeAlpha));
        drawableQuadTransform.position = creditsTransform.position - Vector3.forward * 5f;
    }

    private void UpdateDrawingColorsTexture()
    {
        drawingColorsRTH?.Release();

        drawingColorsRTH = RTHandles.Alloc(DrawingSystem.Instance.MaxColors * 2, 2,
            name: "DrawingColorsTexture", colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
            autoGenerateMips: false, useMipMap: false);

        Texture2D tempTexture = new(DrawingSystem.Instance.MaxColors * 2, 2);

        for (int i = 0; i < DrawingSystem.Instance.MaxColors; i++)
        {
            Color color = i < colors.Count ? colors[i] : Color.clear;

            for (int xOff = 0; xOff < 2; xOff++)
            {
                for (int yOff = 0; yOff < 2; yOff++)
                { tempTexture.SetPixel(i * 2 + xOff, yOff, color); }
            }
        }

        currentColors.Clear();
        for (int i = 0; i < colors.Count; i++)
        { currentColors.Add(colors[i]); }

        tempTexture.Apply();
        Graphics.Blit(tempTexture, drawingColorsRTH);
        Destroy(tempTexture);

        Shader.SetGlobalTexture("_G_DrawingColors", drawingColorsRTH);
    }
}
