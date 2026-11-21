
public interface IInteractable
{
    public string Name { get; }
    public string InteractionVerb { get; }
    public bool InteractionIsLong { get; }
    public string InteractionText => InteractionVerb + " " + Name;

    public bool CanBeInteracted { get; }
}

public interface IUsable : IInteractable
{
    bool IInteractable.InteractionIsLong => IsLong;
    public bool IsLong { get; }

    bool IInteractable.CanBeInteracted => true;

    public void OnUsedBy(Interactor interactor);
}

public interface IHolder : IInteractable
{
    string IInteractable.InteractionVerb => "Place";
    bool IInteractable.InteractionIsLong => IsLong;
    public bool IsLong { get; }

    bool IInteractable.CanBeInteracted => true;
    public ICarriable Carriable { get; set; }
    public bool IsOccupied => Carriable != null;

    public bool TryHold(ICarriable carriable)
    {
        if (IsOccupied || CanHold(carriable)) { return false; }

        Carriable = carriable;
        OnHold();
        return true;
    }
    public bool CanHold(ICarriable carriable) => true;
    public void OnHold() { }

    public bool TryTake(out ICarriable carriable)
    {
        carriable = null;
        if (!IsOccupied || CanTake()) { return false; }

        OnTake();
        carriable = Carriable;
        Carriable = null;
        return true;
    }
    public bool CanTake() => true;
    public void OnTake() { }

    public bool TryDrop()
    {
        if (!IsOccupied || !Carriable.IsDroppable || CanDrop()) { return false; }
        
        OnDrop();
        Carriable = null;
        return true;
    }
    public bool CanDrop() => true;
    public void OnDrop() { }
}

public interface ICarriable : IInteractable
{
    string IInteractable.InteractionVerb => "Pick up";
    bool IInteractable.InteractionIsLong => IsLong;
    public bool IsLong { get; }

    public bool IsDroppable { get; }

    public void OnPickedUpBy(IHolder interactor);
    public void OnDropped();
}

public interface IControllable : IInteractable
{
    string IInteractable.InteractionVerb => Controller == null ? "Enter" : "Exit";
    bool IInteractable.InteractionIsLong => IsLong;
    public bool IsLong { get; }

    public Interactor Controller { get; protected set; }

    public void Enter(Interactor interactor)
    {
        Controller = interactor;
        OnEnteredBy(interactor);
    }

    public void Exit()
    {
        OnExited();
        Controller = null;
    }

    public void OnEnteredBy(Interactor interactor);
    public void WhileEntered();
    public void OnExited();
}