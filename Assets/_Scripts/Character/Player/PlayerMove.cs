
using System;
using UnityEngine;
using UnityEngine.InputSystem;

using Utility;


[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMove : MonoBehaviour
{
    public Camera Camera => camera;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 10f;
    //[SerializeField] private float sprintSpeed = 20f;
    [SerializeField] private float crouchSpeed = 3f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravityStrength = 1f;
    [SerializeField] private LayerMask groundLayerMask = default;
    [SerializeField] private FootstepAudio footstepAudio = null;

    [Header("Ladder")]
    [SerializeField] private float climbSpeed = 4f;

    [Header("Mouse Look")]
    [SerializeField] private float minViewDistance = 25f;
    [SerializeField] private new Camera camera = null;

    [Header("Materials:")]
    [SerializeField] private PhysicsMaterial airBorneMaterial = null;
    [SerializeField] private PhysicsMaterial groundedMaterial = null;

    [Header("Crouch")]
    [SerializeField] private float crouchScaleY = 0.5f;

    private Vector2 moveInput = Vector2.zero;
    private Vector2 lookInput = Vector2.zero;

    private float xRotation = 0f;
    private float currentSpeed = 0f;

    private GameObject groundObject = null;

    private bool isGrounded = false;
    private bool isCrouching = false;
    //private bool isSprinting = false;

    private bool isHoldingCrouch = false;
    private bool isHoldingJump = false;

    private bool isNearLadder = false;
    private bool isClimbing = false;

    private new Rigidbody rigidbody = null;
    private CapsuleCollider capsule = null;
    private SphereCollider sphere = null;
    private AudioListener audioListener = null;

    private float originalHeight = 0f;
    private float originalSphereOffset = 0f;
    private bool active = true;

    private Action onDestroy = null;

    private Vector3 lastGroundPosition = Vector3.zero;
    private float furthestDistanceInAir = 0f;

    private float mouseSensitivity = 0f;


    public void SetPitch(float pitch)
    {
        xRotation = pitch;
    }

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        sphere = GetComponent<SphereCollider>();
        audioListener = camera.GetComponent<AudioListener>();

        rigidbody.useGravity = false;

        GameSettings.Controls.Sensitivity.SubscribeUntilDestroyed(sensitivity =>
        mouseSensitivity = Mathf.Lerp(0.01f, 1f, sensitivity), ref onDestroy, initialize: true);
    }

    void Start()
    {
        currentSpeed = walkSpeed;
        originalHeight = capsule.height;
        originalSphereOffset = sphere.center.y;

        if (!Services.Game.IsPaused)
        {
            rigidbody.freezeRotation = true;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (camera == null)
        { Util.LogError<PlayerMove>("Camera not set!"); enabled = false; return; }
    }

    private void Update()
    {
        if (!active) { return; }

        Look();
    }

    private void FixedUpdate()
    {
        if (!active)
        {
            footstepAudio.SetVelocity(0f);
            return;
        }

        HandleSpeed();
        GroundCheck();

        if (isClimbing)
        {
            Climb();
        }
        else
        {
            Walk();
        }

        //capsule.sharedMaterial = isGrounded ? groundedMaterial : airBorneMaterial;
    }

    private void OnDestroy()
    {
        onDestroy?.Invoke();
    }

    public void SetActive(bool value, bool affectListener = true)
    {
        active = value;

        if (affectListener)
        { audioListener.enabled = value; }

        lookInput = Vector2.zero;
        moveInput = Vector2.zero;
        if (!active)
        {
            rigidbody.linearVelocity = Vector3.zero;
            lookInput = Vector2.zero;
            moveInput = Vector2.zero;
        }
    }

    public void LookTowardsPoint(Vector3 point)
    {
        Vector3 direction = point - camera.transform.position;
        Vector3 projectedDirection = new(direction.x, 0f, direction.z);

        if (projectedDirection.sqrMagnitude > 0f)
        {
            Quaternion bodyRotation = Quaternion.LookRotation(projectedDirection);
            transform.rotation = bodyRotation;
        }

        float distance = new Vector2(direction.x, direction.z).magnitude;
        float pitch = Mathf.Atan2(direction.y, distance) * Mathf.Rad2Deg;

        xRotation = -pitch;
        xRotation = Mathf.Clamp(xRotation, -minViewDistance, minViewDistance);

        camera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void Walk()
    {
        Vector3 move = new(moveInput.x, 0f, moveInput.y);
        Vector3 moveDirection = transform.TransformDirection(move).normalized;

        Vector3 targetVelocity = moveDirection * currentSpeed;

        Vector3 velocity = rigidbody.linearVelocity;
        velocity.x = targetVelocity.x;
        velocity.z = targetVelocity.z;

        velocity += gravityStrength * Time.fixedDeltaTime * Physics.gravity;

        rigidbody.linearVelocity = velocity;

        footstepAudio.SetVelocity(targetVelocity.magnitude);
    }

    private void Climb()
    {
        Vector3 move = new(moveInput.x, 0f, moveInput.y);
        Vector3 moveDirection = transform.TransformDirection(move).normalized;

        Vector3 climb = new(0f, (isHoldingJump ? 1 : 0) - (isHoldingCrouch ? 1 : 0));
        Vector3 climbDirection = transform.TransformDirection(climb).normalized;

        rigidbody.linearVelocity = moveDirection * currentSpeed + climbDirection * climbSpeed;

        footstepAudio.SetVelocity(0f);
    }

    private void HandleSpeed()
    {
        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        //else if (isSprinting)
        //{
        //    currentSpeed = sprintSpeed;
        //}
        else
        {
            currentSpeed = walkSpeed;
        }
    }

    private void Look()
    {
        if (Cursor.lockState != CursorLockMode.Locked) { return; }

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -minViewDistance, minViewDistance);

        camera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void Jump()
    {
        if (isGrounded && !isCrouching)
        {
            rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void GroundCheck()
    {
        bool wasGrounded = isGrounded;

        Ray ray = new(transform.position - transform.up * ((capsule.height / 2f) - capsule.radius), Vector3.down);
        isGrounded = Physics.SphereCast(ray, capsule.radius * 0.9f, out RaycastHit hitInfo, (capsule.radius * (1f - 0.9f)) + 0.05f, groundLayerMask.value);

        if (isGrounded)
        {
            groundObject = hitInfo.collider.gameObject;
            footstepAudio.OnLand(groundObject, playLandingSound: !wasGrounded && furthestDistanceInAir > 0.2f);
            lastGroundPosition = rigidbody.position;
            furthestDistanceInAir = 0f;
        }
        else
        {
            groundObject = null;
            footstepAudio.OnJump();
            furthestDistanceInAir = Mathf.Max(furthestDistanceInAir, (lastGroundPosition - rigidbody.position).magnitude);
        }
    }

    private void Crouch(bool state)
    {
        if (isClimbing) { return; }

        if (isCrouching == state) return;

        isCrouching = state;

        float newHeight = isCrouching ? originalHeight * crouchScaleY : originalHeight;
        float newSphereOffset = isCrouching ? originalSphereOffset * (crouchScaleY * 0.9f) : originalSphereOffset;
        float heightDifference = capsule.height - newHeight;

        rigidbody.linearVelocity = new Vector3(
            rigidbody.linearVelocity.x,
            Mathf.Min(rigidbody.linearVelocity.y, 0f),
            rigidbody.linearVelocity.z
        );

        capsule.height = newHeight;
        sphere.center = new Vector3(0f, newSphereOffset, 0f);
        
        transform.position -= new Vector3(0, heightDifference / 2f, 0);
    }

    void StartClimbing()
    {
        Crouch(false);
        isClimbing = true;
        rigidbody.linearVelocity = Vector3.zero;
    }

    void StopClimbing()
    {
        isClimbing = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ladder"))
        {
            isNearLadder = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ladder"))
        {
            isNearLadder = false;
            StopClimbing();
        }
    }

    // ================= INPUT SYSTEM =================

    private void OnMove(InputValue value)
    {
        if (!active) { return; }

        moveInput = value.Get<Vector2>().normalized;

        if (isNearLadder && !isClimbing)
        {
            StartClimbing();
        }
    }

    private void OnLook(InputValue value)
    {
        if (!active) { return; }

        lookInput = value.Get<Vector2>();
    }

    private void OnJump(InputValue value)
    {
        isHoldingJump = value.isPressed;

        if (!active) { return; }

        if (value.isPressed)
        {
            Jump();
        }
    }

    //private void OnSprint(InputValue value)
    //{
    //    if (!active) { return; }
    //
    //    isSprinting = value.isPressed;
    //}

    private void OnCrouch(InputValue value)
    {
        isHoldingCrouch = value.isPressed;

        if (!active) { return; }

        Crouch(value.isPressed);
    }
}