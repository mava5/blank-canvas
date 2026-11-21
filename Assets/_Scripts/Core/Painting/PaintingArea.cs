
using ObjectDrawing;

using System.Collections.Generic;

using UnityEngine;
using Utility;


public class PaintingArea : DrawableObjectTracker
{
    protected override bool InitialIsTrackingState => false;

    [SerializeField] private PaintingCheckpoint checkpoint = null;
    [SerializeField] private List<PaintingArea> areasToLoad = new();
    [SerializeField] private List<PaintingArea> areasToUnload = new();
    [SerializeField] private List<GameObject> objectsToEnable = new();


    protected override void Awake()
    {
        base.Awake();

        if (checkpoint == null)
        { Util.LogError<PaintingArea>($"Missing checkpoint: \"{name}\"!"); return; }

        checkpoint.OnEnter.Once += OnEnter;
        checkpoint.OnExit.Once += OnExit;

        foreach (GameObject obj in objectsToEnable)
        { obj.SetActive(false); }
    }

    private void OnEnter()
    {
        foreach (PaintingArea area in areasToLoad)
        { area.SetIsTracking(true); }

        foreach (PaintingArea area in areasToUnload)
        { area.CleanUpFully(); }

        foreach (GameObject obj in objectsToEnable)
        { obj.SetActive(true); }
    }

    private void OnExit()
    {

    }
}
