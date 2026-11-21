using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu_OLD : MonoBehaviour
{
    [SerializeField] GameObject pauseMenu;
    

    private void Awake()
    {
        pauseMenu.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseMenu.activeSelf)
            {
                pauseMenu.SetActive(false);
                Services.Game.Unpause(true);
            }
            else
            {
                pauseMenu.SetActive(true);
                Services.Game.Pause();
            }
        }
    }

    public void OnGameResumePress()
    {
        pauseMenu.SetActive(false);
        Services.Game.Unpause();
    }

    public void OnSaveGamePress()
    {

    }

    public void OnGameExitPress()
    {
        SceneManager.LoadScene("MainMenuTesting");
    }
}
