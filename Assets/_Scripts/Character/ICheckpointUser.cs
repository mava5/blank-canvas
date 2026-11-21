
using UnityEngine;


public interface ICheckpointUser
{
    public Checkpoint Checkpoint { get; }
}

internal interface ICheckpointUserInternal : ICheckpointUser
{
    Checkpoint ICheckpointUser.Checkpoint => CheckpointInternal;
    public Checkpoint CheckpointInternal { get; protected set; }

    public void Enter(Checkpoint checkpoint)
    {
        CheckpointInternal = checkpoint;
        OnEnter();
    }
    public virtual void OnEnter() { }

    public void Skip(Checkpoint checkpoint)
    {
        OnSkip(checkpoint);
    }
    public virtual void OnSkip(Checkpoint checkpoint) { }

    public void SetPosition(Vector3 groundPosition);

    public Bounds GetColliderBounds();
}
