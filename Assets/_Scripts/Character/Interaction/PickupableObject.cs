
#if UNITY_EDITOR
using UnityEditor;
using UtilityEditor;
using UtilityExtensions;
#endif

using UnityEngine;


/// <summary> Makes object holdable by an <see cref="ObjectHolder"/>. </summary>
public class PickupableObject : MonoBehaviour//, ICarriable
{
    public ObjectHolder Holder { get; private set; }

    public Transform Transform => transform;

    [SerializeField] private Collider interactionHitbox = null;

    [Header("Placement:")]
    [field: SerializeField] public Vector3 CarryBoxSize { get; set; }
    [field: SerializeField] public Vector3 CarryBoxOffset { get; set; }
    [field: SerializeField] public Vector3 CarryBoxRotation { get; set; }

    private void Start()
    {

    }

    public string GetInteractionText() => "Pick up";

    public void OnInteractedBy(Interactor interactor)
    {

    }
}


#if UNITY_EDITOR

[CustomEditor(typeof(PickupableObject))]
internal class PickupableObjectEditor : AdvancedEditor
{
    private Transform transform = null;
    private SerializedProperty carryBoxSizeProperty = null;
    private SerializedProperty carryBoxOffsetProperty = null;
    private SerializedProperty carryBoxRotationProperty = null;

    protected override void OnEnableInternal()
    {
        base.OnEnableInternal();
        
        transform = ((PickupableObject)target).transform;
        carryBoxSizeProperty = serializedObject.FindAutoProperty("CarryBoxSize");
        carryBoxOffsetProperty = serializedObject.FindAutoProperty("CarryBoxOffset");
        carryBoxRotationProperty = serializedObject.FindAutoProperty("CarryBoxRotation");
    }

    public void OnSceneGUI()
    {
        Matrix4x4 originalMatrix = Handles.matrix;

        Vector3 size = carryBoxSizeProperty.vector3Value;
        Vector3 offset = carryBoxOffsetProperty.vector3Value;
        Quaternion rotation = Quaternion.Euler(carryBoxRotationProperty.vector3Value);

        Handles.matrix = transform.localToWorldMatrix * Matrix4x4.TRS(offset, rotation, size);
        Handles.color = Color.red;
        Handles.DrawWireCube(Vector3.zero, Vector3.one);
        Handles.color = Color.white;
        Handles.matrix = originalMatrix;
    }
}

#endif