using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void OnNewGamePress()
    {
        SceneManager.LoadScene("PlayerMovement");
    }

    public void OnLoadGamePress()
    {

    }

    public void OnOptionsPress()
    {

    }

    public void OnQuitGamePress()
    {
        Application.Quit();
    }
}
