
#if UNITY_EDITOR
using UnityEditor;
using UtilityEditor;
#endif

using UnityEngine;


public class PaintingCheckpoint : Checkpoint
{
    protected override void OnEntered(ICheckpointUser user)
    {
        Debug.Log("Entered: " + name);
    }

    protected override void OnExited(ICheckpointUser user)
    {
        Debug.Log("Exited: " + name);
    }

    protected override void OnSkipped(ICheckpointUser user)
    {
        Debug.Log("Skipped: " + name);
    }
}


#if UNITY_EDITOR

[CustomEditor(typeof(PaintingCheckpoint))]
internal class PaintingCheckpointEditor : AdvancedEditor
{
    private SerializedProperty nextProperty = null;

    private bool forceDisabled = false;


    protected override void OnEnableInternal()
    {
        nextProperty = serializedObject.FindProperty("next");
    }

    protected override bool OnPropertyFieldBegin(ModifiedProperty modifiedProperty)
    {
        if (forceDisabled) { return true; }

        if (modifiedProperty.Property.propertyPath == nextProperty.propertyPath)
        {
            forceDisabled = true;
            modifiedProperty.SetForceEnableState(false);
        }

        return true;
    }
}

#endif
