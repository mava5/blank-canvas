
using UnityEngine;
using UnityEngine.SceneManagement;


public class WinScreenManager : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            SceneManager.LoadScene("_HubScene_Level0");
        }
    }
}
