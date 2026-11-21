
using UnityEngine;


public abstract class QuickPainter : Painter
{
    [SerializeField] protected bool paintOnAwake = true;
    [SerializeField] protected bool removeAfterPainting = true;


    protected override void Awake()
    {
        base.Awake();

        if (!paintOnAwake) { return; }

        if (CanPaint)
        {
            OnPaint();
            if (removeAfterPainting) { Destroy(this); }
        }
        else
        {
            OnCanPaintChanged.OnceSimple += () =>
            {
                OnPaint();
                if (removeAfterPainting) { Destroy(this); }
            };
        }
    }


    protected abstract void OnPaint();
}
