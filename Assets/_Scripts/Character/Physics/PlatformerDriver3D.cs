
using System;

using UnityEngine;


/// <summary> Handles platformer like physics. </summary>
[Serializable]
public class PlatformerDriver3D : PhysicsDriver3D
{
    [Header("Movement:")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float deceleration = 60f;


    [Header("Jumping:")]
    [SerializeField] private float jumpVelocity = 20f;

    [Tooltip("How long until character can jump again.")]
    [SerializeField] private float jumpCooldown = 0.1f;

    [Tooltip("How long jump button can be held down.")]
    [SerializeField] private float maxJumpTime = 0.5f;

    [Tooltip("How long until character can no longer jump after leaving ground.")]
    [SerializeField] private float coyoteTime = 0.2f;

    [Tooltip("Towards the end of a jump, a value is lerped from 1 to this.\n" +
        "The resulting value is used to multiply current jump velocity.")]
    [SerializeField] private float endJumpVelocityMultiplier = 0.5f;

    [Tooltip("If jump ends abruptly, it gets multiplied by this.\n" +
        "Multiplier lerps towards 1 over the course of the jump.")]
    [SerializeField] private float abruptJumpEndVelocityMultiplier = 0.5f;


    [Header("Materials:")]
    [SerializeField] private PhysicsMaterial jumpingPhysicsMaterial = null;
    [SerializeField] private PhysicsMaterial groundedPhysicsMaterial = null;


    [SerializeField] private bool TEMP_canAlwaysJump = false;


    private bool jumping = false;
    private bool forceJump = false;

    private float jumpTimeLeft = 0;
    private float lastGroundedTime = 0;

    // The character can get stuck in a V shaped crevace of walls
    // To combat this previous wall signs over a specific timeframe are combined together
    // If it reaches a certain threshold, character is said to be stuck
    private int wallNormalDirectionFlips = 0;
    private int lastSlidingDirectionSign = 0;
    private float stuckCheckTimeLeft = 0f;

    private float stuckDistanceThreshold = 1f;
    private Vector3 stuckStartPosition = Vector3.zero;

    private float maxStuckCheckTime = 1f;


    #region Public Methods ====================================================================================================== Public Methods

    public void ForceJump() => forceJump = true;

    #endregion Public Methods


    #region Physics Logic ======================================================================================================= Physics Logic

    protected override void OnActivated()
    {
        wallNormalDirectionFlips = 0;
        stuckCheckTimeLeft = maxStuckCheckTime;
        stuckStartPosition = Rigidbody.transform.position;
    }

    protected override void UpdateRigidbodyInternal(float deltaTime)
    {
        Debug.Log(Input.Movement);
        // Jumping ---------------------------------------------------------------------------------------------------- Jumping

        // Started jump
        bool coyoteTimeActive = Time.time - lastGroundedTime < coyoteTime;
        if (((forceJump || (TEMP_canAlwaysJump && Input.WantsToJump)) && !jumping) ||
            (Input.WantsToJump && !jumping && (info.IsGrounded || coyoteTimeActive)))
        {
            jumping = true;
            jumpTimeLeft = maxJumpTime;
            lastGroundedTime = 0f;
        }

        // Jumping
        else if (jumping && (forceJump || (TEMP_canAlwaysJump && Input.WantsToJump) || Input.WantsToJump) && jumpTimeLeft > 0f)
        {
            jumpTimeLeft -= deltaTime;
            targetVelocity.y = jumpVelocity * Mathf.Lerp(endJumpVelocityMultiplier, 1, jumpTimeLeft / maxJumpTime);
        }

        // Jump ended
        else if (jumping && ((!forceJump && !Input.WantsToJump) || jumpTimeLeft < 0f))
        {
            // Jump ended abruptly
            if (jumpTimeLeft > 0f)
            {
                float jumpTimeAlpha = jumpTimeLeft / maxJumpTime;
                targetVelocity.y *= Mathf.Lerp(1, abruptJumpEndVelocityMultiplier, jumpTimeAlpha);
            }

            forceJump = false;
            jumping = false;
            jumpTimeLeft = 0f;
        }


        // Moving ----------------------------------------------------------------------------------------------------- Moving

        // A small delay is added before character can be considered to be on a platform again after jumping
        // This is done to prevent double jumping due to coyote time
        //bool onPlatform = info.OnPlatform && jumpTimeLeft < maxJumpTime - jumpCooldown;
        //if (onPlatform) { lastGroundedTime = Time.time; }

        //float horizontalInput = Input.Horizontal;

        //if (info.TouchingWall)
        //{
        //    // Hitting a ceiling head-on stops the character's jump
        //    if (info.TouchingCeiling)
        //    { jumping = false; jumpTimeLeft = 0; }

        //    // Some flat enough floors can be slid on and they limit movement up them
        //    if (info.TouchingFloor)
        //    {
        //        // Get sliding direction from floor tangent vector
        //        Vector2 floorTangent = new(info.WallNormal.y, -info.WallNormal.x);
        //        Vector2 slidingDirection = floorTangent * Mathf.Sign(info.WallNormal.x);
        //        int slidingDirectionSign = (int)Mathf.Sign(slidingDirection.x);

        //        // Stuck logic
        //        if ((stuckCheckTimeLeft -= deltaTime) < 0f)
        //        {
        //            wallNormalDirectionFlips = 0;
        //            stuckCheckTimeLeft = maxStuckCheckTime;
        //            stuckStartPosition = Rigidbody.transform.position;
        //        }
        //        if (lastSlidingDirectionSign != slidingDirectionSign)
        //        {
        //            lastSlidingDirectionSign = slidingDirectionSign;
        //            wallNormalDirectionFlips++;
        //        }
        //        physicsHandler.SetStuck(wallNormalDirectionFlips > 15 &&
        //            (Rigidbody.transform.position - stuckStartPosition).magnitude < stuckDistanceThreshold);

        //        // Character is trying to go up a tilted floor
        //        if (info.WallNormal.x > 0 && horizontalInput < 0 ||
        //            info.WallNormal.x < 0 && horizontalInput > 0)
        //        { horizontalInput *= info.SlidingControllability; }

        //        // Move character in direction of tilted floor when they have no sliding control
        //        horizontalInput = Mathf.Lerp(slidingDirection.x, horizontalInput, info.SlidingControllability);
        //    }
        //    else
        //    {
        //        wallNormalDirectionFlips = 0;
        //        stuckCheckTimeLeft = maxStuckCheckTime;
        //    }

        //    // TODO: Add wall sliding?
        //}

        Vector2 movementVelocity = Input.Movement * moveSpeed;
        targetVelocity += new Vector3(movementVelocity.x, movementVelocity.y);
    }

    protected override bool TryGetPhysicsMaterialInternal(out PhysicsMaterial material)
    { material = info.IsGrounded ? groundedPhysicsMaterial : jumpingPhysicsMaterial; return true; }

    #endregion Physics Logic
}
