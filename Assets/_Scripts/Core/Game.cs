
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Experimental.Rendering;

using ObjectDrawing;


/// <summary>
/// <br/>   Created when game starts and destroyed when it ends:
/// <br/>   • Sets up persistent singletons
/// <br/>   • Loads persistent assets
/// </summary>
[DefaultExecutionOrder(0)]
public class Game : MonoBehaviour, IGameService
{
#if UNITY_EDITOR

    [InitializeOnLoad]
    private static class PlayModeBootstrapper
    {
        private static string BOOTSTRAP_SCENE_PATH = "Assets/_Scenes/Editor/BootsrapScene.unity";

        static PlayModeBootstrapper() => EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // Prompt developer to save the scene
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                { EditorApplication.isPlaying = false; return; }

                // Save the currently open scene that is going to be played
                string activeScenePath = EditorSceneManager.GetActiveScene().path;
                EditorPrefs.SetString("LastOpenedScene", activeScenePath);

                // Force Unity to load the bootstrap scene first
                var bootstrapAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(BOOTSTRAP_SCENE_PATH);
                EditorSceneManager.playModeStartScene = bootstrapAsset;
            }
        }

        public static void LoadPlayScene()
        {
            string lastOpenedScenePath = EditorPrefs.GetString("LastOpenedScene");

            if (!string.IsNullOrEmpty(lastOpenedScenePath))
            { EditorSceneManager.LoadSceneInPlayMode(lastOpenedScenePath, new(LoadSceneMode.Single)); }
        }
    }

#endif


    public static Game Instance { get; private set; } = null;

    public bool Initialized { get; private set; } = false;
    public bool IsPaused { get; private set; } = false;


    #region Events ========================================================================================== Events

    public IEvent OnGamePaused => onGamePaused;
    private readonly EventCaller onGamePaused = new();

    public IEvent OnGameUnpaused => onGameUnpaused;
    private readonly EventCaller onGameUnpaused = new();

    #endregion Events


    #region Lifecycle ======================================================================================= Lifecycle

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartGame()
    { DontDestroyOnLoad(new GameObject("[Game]", typeof(Game))); }

    private async void Awake()
    {
        Instance = this; Services.TryAssign(this);

        //GameSettings.Visuals.Graphics.FPS.Update();
        //GameSettings.Visuals.Graphics.VSync.Update();

        await GUIAudioManager.CreateAsync();
        await HubAssetManager.CreateFromResourcesAsync("HubAssetManager");
        await GlobalShaderSettings.CreateFromResourcesAsync("GlobalShaderSettings");
        await DrawingSystem.CreateFromResourcesAsync("DrawingSystem", TextureBrush.MAX_COLORS);

        // TODO: Get a better place to enable vsync
        //GameSettings.Visuals.Graphics.VSync.Set(true);

        //await AttemptWithPopupAsync(GraphicsManager.CreateAsync(), "Failed to initialize graphics!");

        Initialized = true;

#if UNITY_EDITOR
        PlayModeBootstrapper.LoadPlayScene();
#endif
    }

    #endregion Lifecycle


    #region Public Methods ================================================================================== Public Methods

    public void Pause()
    {
        if (IsPaused) { return; }

        IsPaused = true;
        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        onGamePaused?.Invoke();
    }

    public void Unpause(bool lockCursor = false)
    {
        if (!IsPaused) { return; }

        IsPaused = false;
        Time.timeScale = 1f;
        
        if (lockCursor)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        onGameUnpaused?.Invoke();
    }

    public void TogglePause()
    { if (IsPaused) { Unpause(); } else { Pause(); } }

    #endregion Public Methods


    #region Private Methods ================================================================================= Private Methods

    /// <summary> Attempts to perform given async operation until successful. Shows error pop-up when unsuccessful. </summary>
    private async Task AttemptWithPopupAsync(Task<bool> tryTask, string failText)
    {
        //PopUpInfo info = new(PopUpType.Error, "Loading Error!", failText, new()
        //{ new("Retry", GUIStyle.PositiveColor), new("Quit", GUIStyle.NegativeColor, () => { Application.Quit(); } });

        while (!await tryTask) { /*await PopUp.ShowAsync(info);*/ }
    }

    #endregion Private Methods
}
