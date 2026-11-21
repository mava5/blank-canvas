
using DG.Tweening.Core.Easing;
using System.Net.NetworkInformation;
using UnityEngine;


/// <summary> Handles <see cref="Player"/> visuals such as their animating. </summary>
public class PlayerVisuals : MonoBehaviour
{
    internal bool Initialized { get; private set; } = false;


    [SerializeField] private float relativeShadowRadius = 1f;
    [SerializeField] private float relativeShadowDistance = 2f;

    private Player player;
    private bool facingLeft = true;

    private Vector3 lastScale = Vector3.zero;
    private Vector3 lastPosition = Vector3.zero;

    private int playerShadowPositionID = 0;
    private int playerShadowRadiusID = 0;
    private int playerShadowDistanceID = 0;

    private Vector3 originalSize = Vector3.zero;


    #region Lifecycle ============================================================================= Lifecycle

    private void Awake()
    {
        player = GetComponent<Player>();
        Initialized = true;

        playerShadowPositionID = Shader.PropertyToID("_PlayerShadow_Position");
        playerShadowRadiusID = Shader.PropertyToID("_PlayerShadow_Radius");
        playerShadowDistanceID = Shader.PropertyToID("_PlayerShadow_Distance");

        originalSize = transform.localScale;
    }

    private void Update()
    {
        return;
        var physicsInfo = player.Physics.Info;

        //if (!physicsInfo.OnPlatform && !physicsInfo.TouchingFloor)
        //{ SetFrame(physicsInfo.Velocity.y > 0.01f ? "Jump" : "Fall"); }
        //else { SetFrame("Idle"); }

        if (facingLeft && physicsInfo.Velocity.x > 0.05f)
        {
            facingLeft = false;
            player.transform.localScale = new(-originalSize.x, originalSize.y, originalSize.z);
        }
        else if (!facingLeft && physicsInfo.Velocity.x < -0.05f)
        {
            facingLeft = true;
            player.transform.localScale = originalSize;
        }
    }

    private void LateUpdate()
    {
        if (transform.localScale != lastScale)
        {
            lastScale = transform.localScale;

            Shader.SetGlobalFloat(playerShadowRadiusID, relativeShadowRadius * Mathf.Abs(transform.localScale.x));
            Shader.SetGlobalFloat(playerShadowDistanceID, relativeShadowDistance * Mathf.Abs(transform.localScale.y));
        }

        if (transform.position != lastPosition)
        {
            lastPosition = transform.position;

            Vector3 playerShadowPosition = transform.position + Vector3.down * (transform.localScale.y / 2f);
            Shader.SetGlobalVector(playerShadowPositionID, playerShadowPosition);
        }
    }

    #endregion Lifecycle


    #region Private Methods ======================================================================= Private Methods

    private void OnDrawGizmosSelected()
    {
        float radius = relativeShadowRadius * transform.localScale.x;
        float distance = relativeShadowDistance * transform.localScale.y;

        Vector3 shadowOrigin = transform.position + Vector3.down * (transform.localScale.y / 2f);
        Vector3 shadowLeft = shadowOrigin + Vector3.left * radius;
        Vector3 shadowRight = shadowOrigin + Vector3.right * radius;
        Vector3 shadowDown = shadowOrigin + Vector3.down * distance;

        Gizmos.color = new(0f, 1f, 0f, 0.4f);
        Gizmos.DrawLine(shadowLeft, shadowRight);
        Gizmos.DrawLine(shadowRight, shadowDown);
        Gizmos.DrawLine(shadowLeft, shadowDown);
    }

    #endregion Private Methods
}
