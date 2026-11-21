
#if UNITY_EDITOR
using UnityEditor;
using UtilityEditor;
#endif

using System;

using UnityEngine;

using UtilityEnum;


/// <summary> Holds one <see cref="PickupableObject"/>. </summary>
public class ObjectHolder : MonoBehaviour//, IHolder
{
    public PickupableObject HeldObject { get; private set; } = null;

    [SerializeField] private Collider interactionHitbox = null;

    [Header("Holding:")]
    [SerializeField] private VerticalAnchor anchor = VerticalAnchor.Bottom;
    [SerializeField] private Transform holdPoint = null;


    public void OnUsedBy(Interactor interactor)
    {

    }

    public float GetPriority()
    {
        return 1f;
    }

    public string GetInteractionText() => "Place";
    
    public void OnInteractedBy(Interactor interactor)
    {

    }

    //public void EjectObject(Vector3 velocity)
    //{
    //    if (HeldObject == null) { return; }

    //    HeldObject.
    //}

    //public TryBool TryTake(out PickupableObject takenObject)
    //{


    //    return true;
    //}

    //public TryBool TryPlace(PickupableObject objectToPlace)
    //{
    //    if (HeldObject != null)
    //    { return new("Already contains object"); }



    //    return true;
    //}

    private void OnDrawGizmosSelected()
    {
        
    }
}


#if UNITY_EDITOR

[CustomEditor(typeof(ObjectHolder))]
internal class ObjectHolderEditor : AdvancedEditor { }

#endif
