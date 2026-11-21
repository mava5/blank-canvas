
using UnityEngine;
using UtilityExtensions;


public class RayInteractor : Interactor
{
    [SerializeField] private Transform raySource = null;
    [SerializeField] private float interactionRadius = 5f;


    //protected override IInteractable[] GetFocusedInteractables()
    //{
    //    Ray ray = raySource != null ?
    //        new(raySource.position, raySource.forward) :
    //        new(transform.position, transform.forward);

    //    if (Physics.Raycast(ray, out RaycastHit hit, interactionRadius, layer.value) &&
    //        hit.transform.gameObject.TryGetComponents(out IInteractable[] interactables)) { return interactables; }

    //    return null;
    //}


#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new(1f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
        Gizmos.color = Color.white;
    }

#endif
}
