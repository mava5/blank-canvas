
using System;

using UnityEngine;

using URandom = UnityEngine.Random;


public class FootstepAudio : MonoBehaviour
{
    [Header("Source:")]
    [SerializeField] private AudioSource audioSource = null;
    [SerializeField, Range(0f, 5f)] private float legSeparation = 0f;

    [Header("Audio:")]
    [SerializeField, Range(0f, 5f)] private float stepCooldown = 0.5f;
    [SerializeField, Range(0f, 50f)] private float minVelocity = 2f;
    [SerializeField, Range(0f, 50f)] private float maxVelocity = 10f;
    [SerializeField, Range(1f, 5f)] private float maxRechargeSpeed = 2f;
    [SerializeField] private SurfaceAudio[] surfaceAudios;

    private float stepCooldownLeft = 0f;
    private bool forLeftLegNext = false;
    private GameObject groundObject = null;
    private float velocity = 0f;

    private AudioSource rightAudioSource = null;


    #region Lifecycle ============================================================================= Lifecycle

    protected virtual void Awake()
    {
        forLeftLegNext = URandom.value > 0.5f;
        stepCooldownLeft = URandom.value * stepCooldown * 0.5f;

        rightAudioSource = Instantiate(audioSource.gameObject).GetComponent<AudioSource>();
        rightAudioSource.transform.parent = audioSource.transform.parent;

        Vector3 localOrigin = audioSource.transform.localPosition;
        Vector3 localOffset = new(legSeparation / 2f, 0f, 0f);
        audioSource.transform.localPosition = localOrigin - localOffset;
        rightAudioSource.transform.localPosition = localOrigin + localOffset;
    }

    protected virtual void Update()
    {
        if (groundObject == null || velocity < 0.01f) { return; }

        float velocityAlpha = Mathf.InverseLerp(minVelocity, maxVelocity, Mathf.Clamp01(velocity));
        stepCooldownLeft -= Mathf.Lerp(1f, maxRechargeSpeed, velocityAlpha) * Time.deltaTime;

        if (stepCooldownLeft > 0f) { return; }

        stepCooldownLeft = stepCooldown;
        forLeftLegNext = !forLeftLegNext;
        PlayFootstep(groundObject, forLeftLegNext, volumeMultiplier: 0.9f);
    }

    #endregion Lifecycle


    #region Public Methods ======================================================================== Public Methods

    public void SetVelocity(float value) => velocity = value;

    public void OnJump() => groundObject = null;

    public void OnLand(GameObject groundObject, bool playLandingSound)
    {
        if (groundObject == null) { return; }

        this.groundObject = groundObject;

        if (playLandingSound)
        {
            PlayFootstep(this.groundObject, false, volumeMultiplier: 1.2f);
            PlayFootstep(this.groundObject, true, volumeMultiplier: 1.2f);
        }
    }

    public void PlayFootstep(GameObject groundObject, bool forLeftLeg, float volumeMultiplier = 1f)
    {
        SurfaceType type = SurfaceType.Stone;

        if (groundObject == null || !groundObject.TryGetComponent<FootstepSurface>(out var surface)) { return; }

        type = surface.SurfaceType;

        if (type == SurfaceType.Stone)
        { volumeMultiplier *= 1.75f; }

        if (TryGetRandomClip(type, out AudioClip audioClip))
        {
            if (forLeftLeg)
            {
                audioSource.pitch = URandom.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(audioClip, URandom.Range(0.95f, 1.05f) * volumeMultiplier);
            }
            else
            {
                rightAudioSource.pitch = URandom.Range(0.95f, 1.05f);
                rightAudioSource.PlayOneShot(audioClip, URandom.Range(0.95f, 1.05f) * volumeMultiplier);
            }
        }
    }

    #endregion Public Methods


    #region Private Methods ======================================================================= Private Methods

    private bool TryGetRandomClip(SurfaceType type, out AudioClip audioClip)
    {
        audioClip = null;

        foreach (var surface in surfaceAudios)
        {
            if (surface.SurfaceType != type) { continue; }

            if (surface.AudioClips.Length == 0) { return false; }

            audioClip = surface.AudioClips[URandom.Range(0, surface.AudioClips.Length)];
            return true;
        }

        return false;
    }

    #endregion Private Methods


#if UNITY_EDITOR

    protected virtual void OnDrawGizmosSelected()
    {
        if (audioSource == null) { return; }

        Vector3 localOrigin = audioSource.transform.localPosition;
        Vector3 localOffset = new(legSeparation / 2f, 0f, 0f);
        Vector3 left = audioSource.transform.parent.TransformPoint(localOrigin - localOffset);
        Vector3 right = audioSource.transform.parent.TransformPoint(localOrigin + localOffset);
        Color originalColor = Gizmos.color;

        Gizmos.color = Color.lightCyan;
        Gizmos.DrawLine(left, right);
        Gizmos.color = originalColor;
    }

#endif
}


[Serializable]
public class SurfaceAudio
{
    public SurfaceType SurfaceType = SurfaceType.Stone;
    public AudioClip[] AudioClips = new AudioClip[0];
}
