using Cysharp.Threading.Tasks.Triggers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // New Input System
using Utility;
using UtilityExtensions;

public class PickUp : MonoBehaviour
{
    [Header("Player:")]
    [SerializeField] private Player player = null;
    [SerializeField] private Transform holdArea = null;

    [Header("Psyhics Parameters:")]
    [SerializeField] private float pickupRange = 5.0f;
    [SerializeField] private float followStrength = 10f;
    [SerializeField] private LayerMask interactablesLayer = default;

    private GameObject heldObj;
    private Rigidbody heldObjRB;
    private Collider[] heldObjColliders;
    private Collider[] playerColliders = null;

    private Quaternion startRotationOffset = Quaternion.identity;

    // Input Action (Right Mouse Button)
    private InputAction pickupAction;

    private void Awake()
    {
        playerColliders = player.GetComponentsInChildren<Collider>();

        // Bind right mouse button
        pickupAction = new InputAction(
            name: "Pickup",
            type: InputActionType.Button,
            binding: "<Mouse>/rightButton"
        );
    }

    private void OnEnable()
    {
        pickupAction.Enable();

        pickupAction.started += OnPickupPressed;
        pickupAction.canceled += OnPickupReleased;
    }

    private void OnDisable()
    {
        pickupAction.started -= OnPickupPressed;
        pickupAction.canceled -= OnPickupReleased;

        pickupAction.Disable();
    }

    public GameObject TEMP_HoveredObject = null;
    public GameObject TEMP_HeldObject => heldObj;
    private void Update()
    {
        // DEBUG
        if (heldObj != null)
        {
            TEMP_HoveredObject = null;
        }
        else if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, pickupRange, interactablesLayer) &&
            hit.transform.CompareTag("PickupableObject") && LayerMask.LayerToName(hit.transform.gameObject.layer) == "DrawableObjects")
        {
            TEMP_HoveredObject = hit.transform.gameObject;
        }
        else
        {
            TEMP_HoveredObject = null;
        }

        if (heldObj != null)
        {
            MoveObject();
        }
    }

    public void PressPickUp()
    {
        if (heldObj == null && TEMP_HoveredObject == null) { return; }

        if (heldObj == null && TEMP_HoveredObject != null)
        {
            PickupObject(TEMP_HoveredObject);
            TEMP_HoveredObject = null;
        }
        else if (heldObj != null)
        {
            DropObject();
        }
    }

    public bool TEMP_IgnoreButton = false;
    private void OnPickupPressed(InputAction.CallbackContext context)
    {
        if (TEMP_IgnoreButton) { return; }

        if (heldObj != null) return;

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, pickupRange) &&
            hit.transform.CompareTag("PickupableObject"))
        {
            PickupObject(hit.transform.gameObject);
        }
    }

    private void OnPickupReleased(InputAction.CallbackContext context)
    {
        if (TEMP_IgnoreButton) { return; }

        if (heldObj != null)
        {
            DropObject();
        }
    }

    void MoveObject()
    {
        Vector3 toTarget = holdArea.position - heldObjRB.worldCenterOfMass;

        heldObjRB.linearVelocity = toTarget * followStrength;
        heldObjRB.angularVelocity = Vector3.zero;

        Quaternion targetRotation = holdArea.rotation * startRotationOffset;
        heldObjRB.MoveRotation(targetRotation);
    }

    void PickupObject(GameObject pickObj)
    {
        if (pickObj.GetComponent<Rigidbody>())
        {
            heldObjRB = pickObj.GetComponent<Rigidbody>();
            heldObjRB.useGravity = false;
            heldObjRB.linearDamping = 10f;
            heldObj = pickObj;

            startRotationOffset = Quaternion.Inverse(holdArea.rotation) * pickObj.transform.rotation;

            if (heldObj.TryGetComponents(out heldObjColliders))
            {
                SetCollisionIgnoreState(true);
            }
        }
    }

    void SetCollisionIgnoreState(bool ignore)
    {
        foreach (Collider objectCollider in heldObjColliders)
        {
            foreach (Collider playerCollider in playerColliders)
            {
                Physics.IgnoreCollision(objectCollider, playerCollider, ignore);
            }
        }
    }

    void DropObject()
    {
        heldObjRB.useGravity = true;
        heldObjRB.linearDamping = 1f;

        SetCollisionIgnoreState(false);

        heldObj = null;
        heldObjColliders = null;
    }
}