
using TMPro;

using UnityEngine;


public class Notebook : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI pageText = null;
    [SerializeField] private TextMeshProUGUI controlsText = null;

    private Player player = null;


    private void Awake()
    {
        player = GetComponent<Player>();
    }

    public void SetPageText(string value)
    {
        pageText.text = value;
    }

    public void SetControlsTexts(params string[] texts)
    {
        if (texts == null || texts.Length == 0) { controlsText.text = ""; }

        controlsText.text = string.Join(", ", texts);
    }
}
