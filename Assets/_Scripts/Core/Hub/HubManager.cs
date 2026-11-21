
using Cysharp.Threading.Tasks;

using DG.Tweening;
using ObjectDrawing;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using UtilityExtensions;


public class HubManager : MonoBehaviour
{
    private abstract class ActionBase
    {
        public abstract string FinalText { get; }
        public abstract KeyCode ActivationKey { get; }
        public abstract Func<UniTask> ActivationAction { get; }
    }

    private class InteractionAction : ActionBase
    {
        public override string FinalText => $"<scale=1.2>[</scale><b>{Key}</b><scale=1.2>]</scale> " +
            $"<b>{ActionString}{(TargetName != null ? (" " + TargetName) : "")}</b>";
        public override KeyCode ActivationKey => Key;
        public override Func<UniTask> ActivationAction => Action;

        public KeyCode Key = KeyCode.None;
        public string ActionString = "";
        public string TargetName = "";
        public Func<UniTask> Action = null;
    }

    private class DescriptionAction : ActionBase
    {
        public override string FinalText => Description;
        public override KeyCode ActivationKey => KeyCode.None;
        public override Func<UniTask> ActivationAction => null;

        public string Description = "";
    }


    [SerializeField] private Player player = null;
    [SerializeField] private LayerMask hubInteractablesLayer = default;
    [SerializeField] private LayerMask paintingInteractablesLayer = default;
    [SerializeField] private List<TEMP_PaintingData> paintingDatas = new();

    [Header("UI:")]
    [SerializeField] private TextMeshProUGUI bigObjectiveTMPTarget = null;
    [SerializeField] private TextMeshProUGUI smallObjectiveTMPTarget = null;
    [SerializeField] private TextMeshProUGUI objectiveTMP = null;
    [SerializeField] private TextMeshProUGUI sightTMP = null;
    [SerializeField] private TextMeshProUGUI controlsTMP = null;
    [SerializeField] private Image fadeImage = null;

    [Header("Audio:")]
    [SerializeField] private AudioSource musicSource = null;
    [SerializeField] private AudioClip mainThemeClip = null;

    [Header("Door:")]
    [SerializeField] private AudioSource doorAudioSource = null;
    [SerializeField] private Transform doorJointTransform = null;
    [SerializeField] private GameObject doorExitObject = null;

    public static int PaintMode = 2;
    public static int DrawableObjectVisible = 0;
    public static int HatchingTextureMovement = 0;

    private bool painting = false;

    private string objectiveString = "";
    private TEMP_PaintingData currentPaintingData = null;

    private Sequence objectiveTween = null;

    private (float min, float max) bigObjectiveAnchor = (0f, 0f);
    private (float min, float max) smallObjectiveAnchor = (0f, 0f);

    private bool isBusy = false;

    private bool isDoorOpen = false;

    public static bool[] PaintingFinishedStates = new bool[0];


    protected virtual void Awake()
    {
        bigObjectiveTMPTarget.text = "";
        smallObjectiveTMPTarget.text = "";
        objectiveTMP.text = "";
        sightTMP.text = "";
        controlsTMP.text = "";

        bigObjectiveTMPTarget.gameObject.SetActive(false);
        smallObjectiveTMPTarget.gameObject.SetActive(false);

        fadeImage.transform.parent.gameObject.SetActive(true);
        fadeImage.gameObject.SetActive(true);
        fadeImage.color = Color.black;

        RectTransform bigRect = (RectTransform)bigObjectiveTMPTarget.transform;
        RectTransform smallRect = (RectTransform)smallObjectiveTMPTarget.transform;
        bigObjectiveAnchor = new(bigRect.anchorMin.y, bigRect.anchorMax.y);
        smallObjectiveAnchor = new(smallRect.anchorMin.y, smallRect.anchorMax.y);

        if (PaintingFinishedStates.Length != paintingDatas.Count)
        { PaintingFinishedStates = new bool[paintingDatas.Count]; }

        for (int i = 0; i < paintingDatas.Count; i++)
        { paintingDatas[i].Initialize(i); }

#if UNITY_EDITOR
        //foreach (TEMP_PaintingData paintingData in paintingDatas)
        //{ paintingData.SetIsFinished(false); }
#endif

        if (paintingDatas.All(d => d.IsFinished))
        {
            isDoorOpen = true;
            doorJointTransform.localEulerAngles = new(0f, -80f, 0f);
        }
    }

    protected virtual void Start()
    {
        player.SetPosition(new Vector3(3.75f, 0f, 3.25f));
        player.TEMP_PlayerMove.LookTowardsPoint(paintingDatas[0].Canvas.transform.position);
        player.TEMP_PlayerMove.SetActive(false, affectListener: false);

        StartAsync().Forget();
    }

    private async UniTask StartAsync()
    {
        musicSource.volume = 0f;
        await SetCanvasActive(null);
        objectiveTMP.text = "";
        await FadeScreenIn();
        SetObjective("Get New Canvas");
    }

    protected virtual void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F5))
        {
            foreach (TEMP_PaintingData paintingData in paintingDatas)
            { paintingData.SetIsFinished(false); }
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            foreach (TEMP_PaintingData paintingData in paintingDatas)
            { paintingData.SetIsFinished(true); }
        }
#endif

        ResolveControls().Forget();
    }

    private async UniTask ResolveControls()
    {
        ActionBase sightAction = null;
        ActionBase globalAction = null;

        if (currentPaintingData != null)
        {
            PaintableCanvas canvas = currentPaintingData.Canvas;

            if (painting)
            {
                player.Camera.transform.position = canvas.transform.position + canvas.transform.forward * 0.9f;
                player.Camera.transform.rotation = Quaternion.LookRotation(-canvas.transform.forward);

                globalAction = new InteractionAction()
                {
                    Key = KeyCode.Q,
                    ActionString = "Stop painting",
                    TargetName = currentPaintingData.Canvas.Painting.PaintingName,
                    Action = () =>
                    {
                        painting = false;
                        canvas.SetActive(false);
                        player.TEMP_PlayerMove.SetActive(true, affectListener: true);

                        player.TEMP_PlayerMove.SetPitch(0f);
                        player.Camera.transform.localPosition = new(0f, 0.5f, 0f);
                        player.SetPosition(new(canvas.transform.position.x + 1f, 0f, 0f));
                        player.TEMP_PlayerMove.LookTowardsPoint(canvas.transform.position);

                        return UniTask.CompletedTask;
                    }
                };

                sightAction = GetSightActionForPlayer(currentPaintingData.Canvas.PaintingManager.Player);
            }
            else
            {
                sightAction = GetSightActionForPlayer(player);
            }
        }
        else
        {
            sightAction = GetSightActionForPlayer(player);
        }

        sightTMP.text = sightAction?.FinalText ?? "";
        controlsTMP.text = globalAction?.FinalText ?? "";

        if (sightAction != null)
        {
            if (!isBusy && Input.GetKeyDown(sightAction.ActivationKey) && sightAction.ActivationAction != null)
            {
                await sightAction.ActivationAction.Invoke();
                return;
            }
        }
        if (globalAction != null)
        {
            if (!isBusy && Input.GetKeyDown(globalAction.ActivationKey) && globalAction.ActivationAction != null)
            {
                await globalAction.ActivationAction.Invoke();
                return;
            }
        }
    }

    public async UniTask FadeScreenOut()
    {
        if (isBusy) { return; }

        isBusy = true;
        player.TEMP_PlayerMove.SetActive(false, affectListener: false);
        fadeImage.color = new(0f, 0f, 0f, 0f);
        fadeImage.gameObject.SetActive(true);

        await DOVirtual.Float(0f, 1f, 1f, alpha =>
        {
            fadeImage.color = new(0f, 0f, 0f, alpha);
            musicSource.volume = 1f - alpha;
        })
            .SetEase(Ease.InOutSine)
            .SetUpdate(true)
            .AsyncWaitForCompletion();

        isBusy = false;
    }

    public async UniTask FadeScreenIn()
    {
        if (isBusy) { return; }

        isBusy = true;

        await DOVirtual.Float(1f, 0f, 1f, alpha =>
        {
            fadeImage.color = new(0f, 0f, 0f, alpha);
            musicSource.volume = 1f - alpha;
        })
            .SetEase(Ease.InOutSine)
            .SetUpdate(true)
            .AsyncWaitForCompletion();

        fadeImage.color = new(0f, 0f, 0f, 0f);
        fadeImage.gameObject.SetActive(false);
        player.TEMP_PlayerMove.SetActive(true, affectListener: false);
        isBusy = false;
    }

    public async UniTask ResetProgress()
    {
        foreach (TEMP_PaintingData paintingData in paintingDatas)
        { paintingData.SetIsFinished(false); }

        player.TEMP_PlayerMove.SetActive(false, affectListener: false);
        
        if (currentPaintingData != null && currentPaintingData.Canvas != null &&
            currentPaintingData.Canvas.PaintingManager != null &&
            currentPaintingData.Canvas.PaintingManager.Player != null)
        { currentPaintingData.Canvas.PaintingManager.Player.TEMP_PlayerMove.SetActive(false, affectListener: false); }
        
        await FadeScreenOut();

        SceneManager.LoadScene("_HubScene");
    }

    private async UniTask SetCanvasActive(int? index)
    {
        if (isBusy) { return; }

        isBusy = true;
        currentPaintingData = null;

        if (index.HasValue && index >= 0 && index < paintingDatas.Count)
        {
            musicSource.clip = paintingDatas[index.Value].ThemeClip;
            musicSource.Play();
        }
        else
        {
            musicSource.clip = mainThemeClip;
            musicSource.Play();
        }

        for (int i = 0; i < paintingDatas.Count; i++)
        {
            TEMP_PaintingData paintingData = paintingDatas[i];
            bool selected = index.HasValue && index.Value == i;

            paintingData.PaintingSupplies.SetActive(!selected);
            paintingData.PaintContainer.SetActive(selected);
            paintingData.CanvasObject.SetActive(selected);

            if (selected)
            {
                currentPaintingData = paintingData;

                if (!paintingData.Canvas.IsLoaded)
                { await paintingData.Canvas.TryLoadAsync(); }
            }
            else
            {
                if (paintingData.Canvas.IsLoaded)
                { await paintingData.Canvas.TryUnloadAsync(); }
            }
        }

        isBusy = false;
    }

    private void SetObjective(string value)
    {
        if (objectiveTween != null && objectiveTween.IsActive())
        { objectiveTween.Kill(); }

        objectiveString = value ?? "";
        objectiveTMP.alpha = 0f;
        objectiveTMP.text = objectiveString;

        if (string.IsNullOrEmpty(objectiveString)) { return; }
        
        RectTransform tmpRect = (RectTransform)objectiveTMP.transform;

        void SetAnchor(float alpha)
        {
            tmpRect.anchorMin = new(0f, Mathf.Lerp(bigObjectiveAnchor.min, smallObjectiveAnchor.min, alpha));
            tmpRect.anchorMax = new(1f, Mathf.Lerp(bigObjectiveAnchor.max, smallObjectiveAnchor.max, alpha));
        }

        SetAnchor(0f);

        objectiveTween = DOTween.Sequence()
            .Append(DOVirtual.Float(0f, 1f, 1f, alpha => { objectiveTMP.alpha = alpha; }).SetEase(Ease.InSine))
            .AppendInterval(2f)
            .Append(DOVirtual.Float(0f, 1f, 2f, alpha => { SetAnchor(alpha); }).SetEase(Ease.InOutSine))
            .SetUpdate(true);
    }

    private ActionBase GetSightActionForPlayer(Player player)
    {
        if (player.Camera.gameObject.TryGetComponent(out PickUp pickUp))
        {
            pickUp.TEMP_IgnoreButton = true;

            DrawableObject drawableObject =
                pickUp.TEMP_HeldObject != null ? pickUp.TEMP_HeldObject.GetComponent<DrawableObject>() :
                pickUp.TEMP_HoveredObject != null ? pickUp.TEMP_HoveredObject.GetComponent<DrawableObject>() : null;

            string targetName = (drawableObject != null && drawableObject.ColorCoverages.Any(c => c > 0.2f)) ?
                (pickUp.TEMP_HeldObject != null ? pickUp.TEMP_HeldObject.name : pickUp.TEMP_HoveredObject != null ? pickUp.TEMP_HoveredObject.name : "???") : "???";

            if (pickUp.TEMP_HeldObject != null)
            {
                return new InteractionAction()
                {
                    Key = KeyCode.E,
                    ActionString = "Drop",
                    TargetName = targetName,
                    Action = () => { pickUp.PressPickUp(); return UniTask.CompletedTask; }
                };
            }
            else if (pickUp.TEMP_HoveredObject != null)
            {
                return new InteractionAction()
                {
                    Key = KeyCode.E,
                    ActionString = "Grab",
                    TargetName = targetName,
                    Action = () => { pickUp.PressPickUp(); return UniTask.CompletedTask; }
                };
            }
        }

        bool inHub = player.gameObject.scene.name.Contains("Hub");
        LayerMask layerMask = inHub ? hubInteractablesLayer : paintingInteractablesLayer;
        Transform camT = player.Camera.transform;

        if (!Physics.Raycast(camT.position, camT.forward, out RaycastHit hit, 2f, layerMask) ||
            !hit.transform.CompareTag("Interactable")) { return null; }

        GameObject hitObject = hit.transform.gameObject;

        if (inHub)
        {
            for (int i = 0; i < paintingDatas.Count; i++)
            {
                var data = paintingDatas[i];

                if (hitObject == data.Souvenir)
                {
                    return new DescriptionAction()
                    { Description = data.IsFinished ? data.SouvenirDescription : "???" };
                }
                if (hitObject == data.FinishedPainting)
                {
                    return new DescriptionAction()
                    { Description = data.PaintingDescription };
                }
                if (hitObject == data.Canvas.gameObject && Vector3.Dot(camT.forward, -data.Canvas.transform.forward) > 0.5f)
                {
                    return new InteractionAction()
                    {
                        Key = KeyCode.E,
                        ActionString = "Start painting",
                        TargetName = data.Canvas.Painting.PaintingName,
                        Action = () =>
                        {
                            painting = true;
                            player.TEMP_PlayerMove.SetActive(false, affectListener: true);
                            data.Canvas.SetActive(true);

                            return UniTask.CompletedTask;
                        }
                    };
                }
                if (hitObject == data.PaintingSupplies)
                {
                    return new InteractionAction()
                    {
                        Key = KeyCode.E,
                        ActionString = "Select",
                        TargetName = data.Canvas.Painting.PaintingName,
                        Action = async () =>
                        {
                            await FadeScreenOut();
                            await SetCanvasActive(i);
                            player.SetPosition(new Vector3(currentPaintingData.Canvas.transform.position.x + 1.5f, 0f, 0f));
                            await UniTask.NextFrame();
                            player.TEMP_PlayerMove.LookTowardsPoint(currentPaintingData.Canvas.transform.position + (Vector3.up * 0.2f));
                            objectiveTMP.text = "";
                            await FadeScreenIn();
                            SetObjective(data.ObjectiveString);
                        }
                    };
                }
                if (hitObject == doorExitObject)
                {
                    if (isDoorOpen)
                    {
                        return new InteractionAction()
                        {
                            Key = KeyCode.E,
                            ActionString = "Exit the room",
                            TargetName = null,
                            Action = async () =>
                            {
                                await FadeScreenOut();
                                SceneManager.LoadScene("CreditsScene");
                            }
                        };
                    }
                    else
                    {
                        return new DescriptionAction()
                        { Description = "Locked" };
                    }
                }
            }
        }
        else
        {
            Transform finishedPaintingT = currentPaintingData.FinishedPainting.transform;

            return new InteractionAction()
            {
                Key = KeyCode.E,
                ActionString = hitObject.name,
                TargetName = null,
                Action = async () =>
                {
                    await FadeScreenOut();
                    currentPaintingData.Canvas.SetActive(false);
                    this.player.TEMP_PlayerMove.SetActive(true, affectListener: true);
                    this.player.TEMP_PlayerMove.SetActive(false, affectListener: false);
                    currentPaintingData.SetIsFinished(true);
                    await SetCanvasActive(null);
                    painting = false;
                    Vector3 position = finishedPaintingT.position + (finishedPaintingT.forward * 2f);
                    this.player.Camera.transform.localPosition = new(0f, 0.5f, 0f);
                    this.player.SetPosition(new Vector3(position.x, 0f, position.z));
                    this.player.TEMP_PlayerMove.LookTowardsPoint(finishedPaintingT.position);
                    objectiveTMP.text = "";
                    await FadeScreenIn();

                    if (!isDoorOpen && paintingDatas.All(d => d.IsFinished))
                    {
                        SetObjective("Exit the room");

                        isDoorOpen = true;

                        doorAudioSource.Play();
                        DOVirtual.Float(0f, 1f, 2f, alpha =>
                        {
                            doorJointTransform.localEulerAngles = new(0f, Mathf.Lerp(0f, -80f, alpha), 0f);
                        })
                        .SetEase(Ease.OutSine)
                        .SetUpdate(true);
                    }
                    else
                    {
                        SetObjective("Get New Canvas");
                    }
                }
            };
        }

        return null;
    }
}


[Serializable]
public class TEMP_PaintingData
{
    public string Name { get; private set; } = "";
    public bool IsFinished { get; private set; } = false;

    public PaintableCanvas Canvas = null;
    public GameObject CanvasObject = null;
    public GameObject PaintContainer = null;
    public GameObject FinishedPainting = null;
    public GameObject PaintingSupplies = null;
    public GameObject Souvenir = null;
    public AudioClip ThemeClip = null;
    public string ObjectiveString = "";
    public string PaintingDescription = "";
    public string SouvenirDescription = "";

    private int paintingIndex = -1;

    //private string CompletionKey => Name + "Completed";

    public void Initialize(int index)
    {
        paintingIndex = index;

        //if (!PlayerPrefs.HasKey(CompletionKey))
        //{ PlayerPrefs.SetInt(CompletionKey, 0); }

        Name = Canvas.Painting.PaintingName;
        IsFinished = HubManager.PaintingFinishedStates[paintingIndex];
        FinishedPainting.SetActive(IsFinished);
        Souvenir.SetActive(IsFinished);
    }

    public void SetIsFinished(bool value)
    {
        IsFinished = value;
        FinishedPainting.SetActive(value);
        Souvenir.SetActive(value);
        HubManager.PaintingFinishedStates[paintingIndex] = value;
    }
}