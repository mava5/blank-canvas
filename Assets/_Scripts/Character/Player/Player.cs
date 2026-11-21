
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


/// <summary>
/// <br/>   Unifies player related components into one class.
/// <br/>   
/// <br/>   Composite class with:
/// <br/>   • Input: <see cref="PlayerInput"/>
/// <br/>   • Status: <see cref="PlayerStatus"/>
/// <br/>   • Visuals: <see cref="PlayerVisuals"/>
/// <br/>   • Visuals: <see cref="PlayerInteractor"/>
/// <br/>   • Physics: <see cref="CharacterPhysics3D"/>
/// <br/>   
/// <br/>   Also cointains:
/// <br/>   • Notebook: <see cref="Notebook"/>
/// </summary>
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerStatus))]
[RequireComponent(typeof(PlayerVisuals))]
[RequireComponent(typeof(PlayerInteractor))]
[RequireComponent(typeof(CharacterPhysics3D))]
// TODO: Merge PlayerMove to CharacterPhysics3D logic
[RequireComponent(typeof(Notebook))]
[RequireComponent(typeof(PlayerMove))]
public class Player : MonoBehaviour, ICheckpointUserInternal
{
    Checkpoint ICheckpointUserInternal.CheckpointInternal { get; set; }

    public PlayerInput Input { get; private set; } = null;
    public PlayerStatus Status { get; private set; } = null;
    public PlayerVisuals Visuals { get; private set; } = null;
    public PlayerInteractor Interactor { get; private set; } = null;
    public CharacterPhysics3D Physics { get; private set; } = null;

    public Notebook Notebook { get; private set; } = null;

    public PlayerMove TEMP_PlayerMove { get; private set; } = null;

    public Camera Camera => camera;

    [SerializeField] new private Camera camera = null;
    [SerializeField] private GameObject visualObject = null;

    private readonly List<AsyncOperationHandle> handlesToRelease = new();


    #region Lifecycle ============================================================================= Lifecycle

    private void Awake()
    {
        Input = GetComponent<PlayerInput>();
        Status = GetComponent<PlayerStatus>();
        Visuals = GetComponent<PlayerVisuals>();
        Physics = GetComponent<CharacterPhysics3D>();
        Interactor = GetComponent<PlayerInteractor>();

        Notebook = GetComponent<Notebook>();
        TEMP_PlayerMove = GetComponent<PlayerMove>();

        Physics.WhileStuck.Always += WhileStuck;
        Physics.OnHazardCollision.Always += OnHazardCollision;
        Physics.OnCollectableTrigger.Always += OnCollectableTrigger;
    }

    private void OnDestroy()
    {
        foreach (var handle in handlesToRelease)
        { if (handle.IsValid()) { Addressables.Release(handle); } }

        Physics.WhileStuck.Always -= WhileStuck;
        Physics.OnHazardCollision.Always -= OnHazardCollision;
        Physics.OnCollectableTrigger.Always -= OnCollectableTrigger;

        handlesToRelease.Clear();
    }

    #endregion Lifecycle


    #region Public Methods ======================================================================== Public Methods

    public void SetPosition(Vector3 groundPosition)
    {
        transform.position = groundPosition + (Vector3.up * (Physics.Collider.bounds.size.y * 0.5f));
    }

    public Bounds GetColliderBounds() => Physics.Collider.bounds;

    #endregion Public Methods


    #region Private Methods ======================================================================= Private Methods

    private void WhileStuck()
    {
     
    }

    private void OnHazardCollision(Collision collision)
    {
       
    }

    
    private void OnCollectableTrigger(Collider collider)
    {
        
    }

    #endregion Private Methods
}
