
using System.Collections.Generic;

using UnityEngine;

using Utility;
using UtilityExtensions;


[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public abstract class Checkpoint : MonoBehaviour
{
    protected BoxCollider Collider { get; private set; } = null;

    [SerializeField] protected Vector3 startPosition = Vector3.zero;
    [SerializeField] protected List<BoxCollider> inBoundsColliders = new();

    [SerializeField] protected Checkpoint next = null;
    protected Checkpoint previous = null;

    protected bool inLoop = false;

    private readonly List<ICheckpointUserInternal> containedUsers = new();


    protected EventCaller onEnter = new();
    public IEvent OnEnter => onEnter;

    protected EventCaller onExit = new();
    public IEvent OnExit => onExit;

    protected EventCaller onSkip = new();
    public IEvent OnSkip => onSkip;


    protected virtual void Awake()
    {
        Collider = GetComponent<BoxCollider>();

        if (next != null)
        { next.previous = this; }

        if (inBoundsColliders.Count == 0)
        { Util.LogWarning<Checkpoint>("inBoundsColliders not set."); }
    }

    protected virtual void Start()
    {
        inLoop = CheckInLoop(forward: true) || CheckInLoop(forward: false);
        enabled = !inLoop;

        if (inLoop)
        {
            Util.LogError<Checkpoint>($"In loop: \"{name}\"!");
            enabled = false;
        }
    }

    protected virtual void Update()
    {
        foreach (ICheckpointUserInternal user in containedUsers)
        {
            bool inBounds = false;

            foreach (BoxCollider inBoundsCollider in inBoundsColliders)
            {
                if (inBoundsCollider.ContainsAnyBoundsCorner(user.GetColliderBounds()))
                { inBounds = true; break; }
            }

            if (inBounds) { continue; }

            user.SetPosition(transform.TransformPoint(startPosition));
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (inLoop) { return; }
        if (!other.gameObject.TryGetComponent(out ICheckpointUserInternal user)) { return; }

        Checkpoint current = user.Checkpoint;
        if (current == this) { return; }
        if (current == null) { EnterBy(user); return; }

        if (!TryGetForwardPathToThis(current, out List<Checkpoint> path)) { return; }

        current.ExitBy(user);

        for (int i = 0; i < path.Count - 1; i++)
        { path[i].SkipBy(user); }

        path[^1].EnterBy(user);
    }

    private void EnterBy(ICheckpointUserInternal user)
    {
        containedUsers.Add(user);
        user.Enter(this);
        OnEntered(user);
        onEnter.Invoke();
    }

    private void ExitBy(ICheckpointUserInternal user)
    {
        containedUsers.Remove(user);
        OnExited(user);
        onExit.Invoke();
    }

    private void SkipBy(ICheckpointUserInternal user)
    {
        user.Skip(this);
        OnSkipped(user);
        onSkip.Invoke();
    }

    protected virtual void OnEntered(ICheckpointUser user) { }

    protected virtual void OnExited(ICheckpointUser user) { }

    protected virtual void OnSkipped(ICheckpointUser user) { }


    protected bool CheckInLoop(bool forward)
    {
        int counter = 0;
        Checkpoint current = this;

        while (current != null)
        {
            if (current.inLoop || counter++ > 1000) { return true; }
            current = forward ? current.next : current.previous;
        }

        return false;
    }

    protected bool TryGetForwardPathToThis(Checkpoint start, out List<Checkpoint> path)
    {
        if (start == null)
        {
            path = new() { this };
            return true;
        }

        path = new();
        int counter = 0;
        Checkpoint current = start;

        while (current != null)
        {
            if (counter++ > 1000) { return false; }

            current = current.next;
            path.Add(current);

            if (current == this) { return true; }
        }

        return false;
    }


#if UNITY_EDITOR

    protected void OnDrawGizmosSelected()
    {
        Gizmos.color = new(0.8f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.TransformPoint(startPosition), 0.25f);
        Gizmos.color = Color.white;
    }

#endif
}
