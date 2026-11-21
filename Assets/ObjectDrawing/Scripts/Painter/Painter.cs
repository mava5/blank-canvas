
using ObjectDrawing;
using UnityEngine;
using UtilityExtensions;


public abstract class Painter : MonoBehaviour
{
    protected bool CanPaint { get; private set; } = false;

    private DrawableObjectTracker tracker = null;

    private readonly EventCaller<bool> onCanPaintChanged = new();
    protected IEvent<bool> OnCanPaintChanged => onCanPaintChanged;


    protected virtual void Awake()
    {
        UpdateTracker();
    }

    protected virtual void OnTransformParentChanged()
    {
        if (tracker != null)
        { tracker.OnIsTrackingChanged.Always -= OnIsTrackingChanged; }

        UpdateTracker();
    }

    protected virtual void OnDestroy()
    {
        if (tracker != null)
        { tracker.OnIsTrackingChanged.Always -= OnIsTrackingChanged; }
    }


    private void UpdateTracker()
    {
        if (transform.TryGetComponentInAncestor(out DrawableObjectTracker newTracker))
        {
            tracker = newTracker;
            tracker.OnIsTrackingChanged.Always += OnIsTrackingChanged;
        }
    }

    private void OnIsTrackingChanged(bool isTracking)
    {
        bool originalState = CanPaint;
        CanPaint = isTracking;

        if (CanPaint != originalState)
        { onCanPaintChanged.Invoke(CanPaint); }
    }
}
