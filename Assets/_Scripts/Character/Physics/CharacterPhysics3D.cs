
using System.Collections.Generic;

using UnityEngine;

using Utility;
using UtilityEditor;
using UtilityEnum;


public enum ColliderShape
{
    Box,
    Sphere,
    Capsule,
}

public struct PhysicsStateInfo3D
{
    public Vector3 Velocity;
    public Vector3 ProjectedVelocity;
    public float BaseGravityVelocity;
    public bool IsGrounded;
}

/// <summary>
/// <br/>   Handles physics for a character based on ther state and input.
/// <br/>   A list of <see cref="PhysicsDriver3D"/> will determine how physics are handled.
/// <br/>   A <see cref="CharacterInput3D"/> determines the inputs.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CharacterPhysics3D : MonoBehaviour
{
    internal Collider Collider => collider;


    [Foldout("Generic")]
    [Tooltip("Used when active physics driver didn't have a material to set.")]
    [SerializeField] private PhysicsMaterial finalFallbackMaterial = null;
    [SerializeField] private LayerMask groundCheckLayer = 0;
    [SerializeField] private LayerMask hazardCheckLayer = 0;
    [SerializeField] private LayerMask collectableCheckLayer = 0;
    [SerializeField] private new Collider collider = null;
    [SerializeField] private float baseGravityVelocity = 0f;

    [Foldout("Ground Check")]
    [SerializeField, TrimmedLabel] private Vector3 groundCheck_relativeOffset = Vector3.down;
    [SerializeField, TrimmedLabel] private ColliderShape groundCheck_shape = ColliderShape.Box;
    [SerializeField, TrimmedLabel, ShowIf("groundCheck_shape", ColliderShape.Box)] private Vector3 groundCheckBox_relativeSize = Vector3.one;
    [SerializeField, TrimmedLabel, ShowIfNot("groundCheck_shape", ColliderShape.Box)] private float groundCheckSphere_radius = 1f;
    [SerializeField, TrimmedLabel, ShowIfNot("groundCheck_shape", ColliderShape.Box)] private Axis3D groundCheckSphere_radiusAxis = Axis3D.x;
    [SerializeField, TrimmedLabel, ShowIf("groundCheck_shape", ColliderShape.Capsule)] private Vector3 groundCheckCapsule_start = Vector3.zero;
    [SerializeField, TrimmedLabel, ShowIf("groundCheck_shape", ColliderShape.Capsule)] private Vector3 groundCheckCapsule_end = Vector3.zero;

    [EndFoldout]
    [SerializeReference, RenameableList] private List<PhysicsDriver3D> drivers = new();

    public PhysicsStateInfo3D Info => info;
    public bool IsStuck { get; private set; } = false;

    public Rigidbody Rigidbody { get; private set; } = null;
    public CharacterInput3D Input { get; private set; } = null;
    public PhysicsDriver3D ActiveDriver { get; private set; } = null;

    public LayerMask GroundCheckLayer => groundCheckLayer;
    public LayerMask HazardCheckLayer => hazardCheckLayer;
    public LayerMask CollectableCheckLayer => collectableCheckLayer;


    private PhysicsStateInfo3D info = new();
    private PhysicsMaterial currentMaterial = null;


    #region Events ========================================================================================== Events

    public IEvent<bool> OnGroundedChanged => onGroundedChanged;
    private readonly EventCaller<bool> onGroundedChanged = new();

    public IEvent WhileStuck => whileStuck;
    private readonly EventCaller whileStuck = new();

    public IEvent<Collider> OnHazardTrigger => onHazardTrigger;
    private readonly EventCaller<Collider> onHazardTrigger = new();
    public IEvent<Collision> OnHazardCollision => onHazardCollision;
    private readonly EventCaller<Collision> onHazardCollision = new();

    public IEvent<Collider> OnCollectableTrigger => onCollectableTrigger;
    private readonly EventCaller<Collider> onCollectableTrigger = new();
    public IEvent<Collision> OnCollectableCollide => onCollectableCollision;
    private readonly EventCaller<Collision> onCollectableCollision = new();

    #endregion Events


    #region Lifecycle ======================================================================================= Lifecycle

    public void Awake()
    {
        Input = gameObject.GetComponent<CharacterInput3D>();
        Rigidbody = gameObject.GetComponent<Rigidbody>();
        
        // TODO: DEBUG - Removed as PlayerMove doesn't account for gravity
        //Rigidbody.useGravity = false;

        ApplyMaterial(finalFallbackMaterial);

        if (Input == null)
        {
            Util.LogError<CharacterPhysics3D>("A character input component couldn't be found!");
            enabled = false;
            return;
        }

        if (drivers.Count == 0)
        {
            Util.LogError<CharacterPhysics3D>("No physics drivers added!");
            enabled = false;
            return;
        }

        foreach (var driver in drivers)
        { driver.SetPhysicsHandler(this); }
        ActiveDriver = drivers[0];

        AwakeInternal();
    }

    protected virtual void AwakeInternal() { }

    private void FixedUpdate()
    {
        info.Velocity = Rigidbody.linearVelocity;
        info.ProjectedVelocity = Vector3.ProjectOnPlane(info.Velocity, ActiveDriver?.UpDirection ?? Vector3.up);
        info.BaseGravityVelocity = baseGravityVelocity;
        info.IsGrounded = PerformGroundCheck();

        ActiveDriver?.UpdateRigidbody();

        // Pick material in order: active driver material -> active driver fallback material -> final fallback material
        // Note: If all are null, the sharedMaterial is set to null -> global PhysicsMaterial2D is used
        if (!ActiveDriver.TryGetPhysicsMaterial(out var newMaterial)) { newMaterial = finalFallbackMaterial; }

        if (currentMaterial != newMaterial)
        {
            currentMaterial = newMaterial;
            ApplyMaterial(newMaterial);
        }

        if (IsStuck) { whileStuck?.Invoke(); }
    }


    private void OnCollisionEnter(Collision collision)
    {
        if ((hazardCheckLayer.value & (1 << collision.gameObject.layer)) != 0) { onHazardCollision.Invoke(collision); }
        else if ((collectableCheckLayer.value & (1 << collision.gameObject.layer)) != 0) { onCollectableCollision.Invoke(collision); }
    }

    private void OnTriggerEnter(Collider collider)
    {
        if ((hazardCheckLayer.value & (1 << collider.gameObject.layer)) != 0) { onHazardTrigger.Invoke(collider); }
        else if ((collectableCheckLayer.value & (1 << collider.gameObject.layer)) != 0) { onCollectableTrigger.Invoke(collider); }
    }

    #endregion Lifecycle


    #region Public Methods ================================================================================== Public Methods

    public void SetDriver(string driverName)
    {
        foreach (var driver in drivers)
        {
            if (driver.Name == driverName)
            { ActiveDriver = driver; }
        }
    }

    public void SetStuck(bool isStuck) => IsStuck = isStuck;

    #endregion Public Methods


    #region Protected Methods =============================================================================== Protected Methods

    protected bool PerformGroundCheck()
    {
        Vector3 origin = Rigidbody.position + new Vector3(
            transform.localScale.x * groundCheck_relativeOffset.x,
            transform.localScale.y * groundCheck_relativeOffset.y,
            transform.localScale.z * groundCheck_relativeOffset.z);

        switch (groundCheck_shape)
        {
            case ColliderShape.Box:
                Vector3 halfExtents = new Vector3(
                    Mathf.Abs(transform.localScale.x * groundCheckBox_relativeSize.x),
                    Mathf.Abs(transform.localScale.y * groundCheckBox_relativeSize.y),
                    Mathf.Abs(transform.localScale.z * groundCheckBox_relativeSize.z)) / 2f;
                return Physics.CheckBox(origin, halfExtents, Rigidbody.transform.rotation, groundCheckLayer);

            case ColliderShape.Sphere:
            case ColliderShape.Capsule:
                float radius = groundCheckSphere_radius * groundCheckSphere_radiusAxis switch
                {
                    Axis3D.x => transform.localScale.x,
                    Axis3D.y => transform.localScale.y,
                    Axis3D.z => transform.localScale.z,
                    _ => 1f
                };
                if (groundCheck_shape == ColliderShape.Sphere)
                {
                    return Physics.CheckSphere(origin, radius, groundCheckLayer);
                }
                else
                {
                    Vector3 start = transform.TransformPoint(groundCheckCapsule_start + groundCheck_relativeOffset);
                    Vector3 end = transform.TransformPoint(groundCheckCapsule_end + groundCheck_relativeOffset);
                    return Physics.CheckCapsule(start, end, radius, groundCheckLayer);
                }

            default: return false;
        }
    }

    #endregion Protected Methods


    #region Private Methods ================================================================================= Private Methods

    private void ApplyMaterial(PhysicsMaterial material) => collider.sharedMaterial = material;

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + new Vector3(
            transform.localScale.x * groundCheck_relativeOffset.x,
            transform.localScale.y * groundCheck_relativeOffset.y,
            transform.localScale.z * groundCheck_relativeOffset.z);

        Gizmos.color = new(1f, 0f, 0f, 0.4f);
        if (groundCheck_shape == ColliderShape.Box)
        {
            Vector3 extents = new Vector3(
                Mathf.Abs(transform.localScale.x * groundCheckBox_relativeSize.x),
                Mathf.Abs(transform.localScale.y * groundCheckBox_relativeSize.y),
                Mathf.Abs(transform.localScale.z * groundCheckBox_relativeSize.z));
            Gizmos.DrawWireCube(origin, extents);
        }
        else if (groundCheck_shape == ColliderShape.Sphere ||
            groundCheck_shape == ColliderShape.Capsule)
        {
            float radius = groundCheckSphere_radius * groundCheckSphere_radiusAxis switch
            {
                Axis3D.x => transform.localScale.x,
                Axis3D.y => transform.localScale.y,
                Axis3D.z => transform.localScale.z,
                _ => 1f
            };
            if (groundCheck_shape == ColliderShape.Sphere)
            {
                Gizmos.DrawWireSphere(origin, radius);
            }
            else
            {
                Vector3 start = transform.TransformPoint(groundCheckCapsule_start + groundCheck_relativeOffset);
                Vector3 end = transform.TransformPoint(groundCheckCapsule_end + groundCheck_relativeOffset);
                Gizmos.DrawWireSphere(start, radius);
                Gizmos.DrawWireSphere(end, radius);
                Gizmos.DrawLine(start, end);
            }
        }
    }

    #endregion Private Methods
}


#if UNITY_EDITOR

[UnityEditor.CustomEditor(typeof(CharacterPhysics3D))]
public class CharacterPhysics3DEditor : AdvancedEditor { }

#endif
