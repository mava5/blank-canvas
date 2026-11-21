
using System;

using UnityEngine;

using Utility;
using UtilityEditor;


public enum MovementMode
{
    Custom,
    Snappy,
    Accelerated,
}

public enum GravityMode
{
    Custom,
    Multiply,
    Overwrite,
}

/// <summary> Handles updating physics based on input. </summary>
[Serializable]
public abstract class PhysicsDriver3D
{
    public Rigidbody Rigidbody => physicsHandler.Rigidbody;
    public CharacterInput3D Input => physicsHandler.Input;
    
    public bool IsStuck { get; protected set; }

    public string Name => name;
    public Vector3 UpDirection => upDirection;


    [SerializeField, HideInInspector] protected string name = "";

    [Tooltip("Determines the gravity direction as well as horizontal movement plane normal.")]
    [SerializeField] protected Vector3 upDirection = Vector3.up;
    [Tooltip("The material used when this driver's logic doesn't return one.")]
    [SerializeField] protected PhysicsMaterial fallbackMaterial = null;

    [Header("Movement:")]
    [SerializeField] protected MovementMode movementMode = MovementMode.Accelerated;
    [SerializeField] protected float groundAccel = 0f;
    [SerializeField] protected float airAccel = 0f;

    [Header("Gravity:")]
    [SerializeField] protected GravityMode gravityMode = GravityMode.Multiply;
    [SerializeField] protected float gravityMultiplier = 0f;
    [SerializeField] protected float gravityVelocityOverwrite = 0f;

    protected CharacterPhysics3D physicsHandler = null;

    protected PhysicsStateInfo3D info = new();
    protected Vector3 targetVelocity = Vector3.zero;


    #region Public Methods ============================================================================================ Public Methods

    public void SetPhysicsHandler(CharacterPhysics3D physicsHandler) => this.physicsHandler = physicsHandler;

    /// <summary> Updates <see cref="UnityEngine.Rigidbody"/> owned by the assigned <see cref="CharacterPhysics3D"/>. </summary>
    public void UpdateRigidbody()
    {
        if (physicsHandler == null)
        { Util.LogError<PhysicsDriver3D>("A physics handler hasn't been assigned!"); return; }
        
        float deltaTime = Time.fixedDeltaTime;

        info = physicsHandler.Info;
        UpdateRigidbodyInternal(deltaTime);

        Vector3 projectedTargetVelocity = Vector3.ProjectOnPlane(targetVelocity, upDirection);
        Vector3 velocityDifference = projectedTargetVelocity - info.ProjectedVelocity;
        Vector3 acceleration = Vector3.zero;
        Vector3 gravityVelocity = Vector3.zero;

        switch (movementMode)
        {
            case MovementMode.Snappy:
                acceleration = velocityDifference / deltaTime; break;

            case MovementMode.Accelerated:
                acceleration = velocityDifference / deltaTime;
                float control = info.IsGrounded ? groundAccel : airAccel;
                acceleration = Vector3.ClampMagnitude(acceleration, control); break;

            case MovementMode.Custom: break;
        }

        switch (gravityMode)
        {
            case GravityMode.Multiply:
                gravityVelocity = -upDirection * info.BaseGravityVelocity * gravityMultiplier;
                acceleration += gravityVelocity / deltaTime; break;

            case GravityMode.Overwrite:
                gravityVelocity = -UpDirection * gravityVelocityOverwrite;
                acceleration += gravityVelocity / deltaTime; break;

            case GravityMode.Custom: break;
        }

        Rigidbody.AddForce(acceleration, ForceMode.Acceleration);
    }

    /// <summary>
    /// <br/>   Tries to get the current physics material based on physics logic.
    /// <br/>   Defaults to <see cref="fallbackMaterial"/>.
    /// </summary>
    /// <returns> <see cref="bool"/> -> Was physics material successfully gotten </returns>
    public bool TryGetPhysicsMaterial(out PhysicsMaterial material)
    {
        if (TryGetPhysicsMaterialInternal(out material)) { return true; }
        material = fallbackMaterial;
        return false;
    }

    public void SetUnstuck() => IsStuck = false;

    #endregion Public Methods


    #region protected Methods ========================================================================================= protected Methods

    protected virtual void OnActivated() { }
    protected virtual void OnDeactivated() { }

    protected abstract void UpdateRigidbodyInternal(float deltaTime);
    protected abstract bool TryGetPhysicsMaterialInternal(out PhysicsMaterial material);

    #endregion protected Methods
}


#if UNITY_EDITOR

[UnityEditor.CustomEditor(typeof(PhysicsDriver3D))]
public class PhysicsDriver3DEditor : AdvancedEditor { }

#endif
