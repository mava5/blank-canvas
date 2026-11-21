
using UnityEngine;


/// <summary> Handles 2D input reading for a character. </summary>
public abstract class CharacterInput3D : MonoBehaviour
{
    /// <remarks> x = Horizontal delta, y = Vertical delta </remarks>
    public Vector2 Look { get; protected set; } = Vector2.zero;

    /// <remarks> x = Left / right, y = Forward / backward </remarks>
    public Vector2 Movement { get; protected set; } = Vector2.zero;

    public bool WantsToJump { get; protected set; } = false;
    public bool WantsToSprint { get; protected set; } = false;
    public bool WantsToInteract { get; protected set; } = false;
}
