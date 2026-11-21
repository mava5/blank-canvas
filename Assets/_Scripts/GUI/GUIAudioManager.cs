
using System.Threading;

using UnityEngine;
using Cysharp.Threading.Tasks;


public class GUIAudioManager : PersistentBehaviour<GUIAudioManager>
{
    private static AudioSource audioSource = null;


    #region Initialization ======================================================================== Initialization

    protected override async UniTask<GUIAudioManager> InitializeAsync(CancellationToken token, params object[] args)
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        return this;
    }

    protected override void OnInitializationFailed() { }

    #endregion Initialization


    #region Public Methods ======================================================================== Public Methods

    public static void PlayOneShot(AudioClip audioClip) => audioSource.PlayOneShot(audioClip);

    #endregion Public Methods
}
