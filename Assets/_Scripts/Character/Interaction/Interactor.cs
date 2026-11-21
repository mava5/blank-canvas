
using UnityEngine;


public interface IInteractionContextProvider
{

}

public abstract class Interactor : MonoBehaviour
{
    //public bool IsCarryingObject => ownHolder != null && ownHolder.IsOccupied;
    //public bool IsControllingObject => currentlyControlled != null;


    //[SerializeField] protected IHolder ownHolder = null;
    //[SerializeField] protected LayerMask layer = default;
    
    //protected IControllable currentlyControlled = null;
    //protected IInteractable[] focusedInteractables = null;

    //protected string interactText = null;
    //protected string dropText = null;
    //protected string exitText = null;

    //private float interactionStartTime = float.MinValue;
    //private bool interacting = false;


    //protected virtual void Update()
    //{
    //    focusedInteractables = GetFocusedInteractables();
    //    UpdateInteractionTexts();
    //    Debug.Log(focusedInteractables?.Length);

    //}


    //public (string interactText, string dropText, string exitText) GetInteractionTexts() => (interactText, dropText, exitText);

    //public void StartInteracting()
    //{
    //    interacting = true;
    //}

    //public void EndInteracting()
    //{
    //    interacting = false;
    //    float heldTime = 
    //}

    //public void Interact()
    //{
    //    if (focusedInteractables == null || focusedInteractables.Length == 0) { return; }

    //    foreach (IInteractable interactable in focusedInteractables)
    //    {
    //        switch (interactable)
    //        {
    //            case IUsable usable:
    //                usable.OnUsedBy(this);
    //                break;

    //            case IHolder othersHolder:
    //                if (ownHolder != null && ownHolder.TryTake(out ICarriable ownCarriable)) { othersHolder.TryHold(ownCarriable); }
    //                else if (ownHolder != null && othersHolder.TryTake(out ICarriable othersCarriable)) { ownHolder.TryHold(othersCarriable); }
    //                break;

    //            case ICarriable carriable:
    //                if (ownHolder != null && ownHolder.TryHold(carriable)) { carriable.OnPickedUpBy(this); }
    //                break;

    //            case IControllable controllable:
    //                if (currentlyControlled == null) { controllable.OnEnteredBy(this); }
    //                break;
    //        }
    //    }
    //}

    //public void DropCarried()
    //{
    //    if (!IsCarryingObject) { return; }

    //    ownHolder.TryDrop();
    //}

    //public void ExitControlled()
    //{
    //    if (!IsControllingObject) { return; }

    //    currentlyControlled.OnExitedBy(this);
    //    currentlyControlled = null;
    //}


    //protected abstract IInteractable[] GetFocusedInteractables();
    
    //private void UpdateInteractionTexts()
    //{
    //    interactText = focusedInteractable switch
    //    {
    //        IUsable usable => usable.GetInteractionText(),
    //        IHolder holder => holder.IsOccupied ? holder.Carriable.GetInteractionText() : holder.GetInteractionText(),
    //        ICarriable carriable => carriable.GetInteractionText(),
    //        IControllable controllable => controllable.GetInteractionText(),
    //        _ => null
    //    };

    //    dropText = ownHolder?.Carriable?.Get;
    //    exitText = null;
    //}
}
