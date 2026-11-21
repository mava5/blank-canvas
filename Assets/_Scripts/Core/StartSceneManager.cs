
using UnityEngine;

using UnityEngine.SceneManagement;


public class StartSceneManager : MonoBehaviour
{
    void Update()
    {
        if (!Game.Instance.Initialized) { return; }

        SceneManager.LoadScene("_HubScene");
    }
}
