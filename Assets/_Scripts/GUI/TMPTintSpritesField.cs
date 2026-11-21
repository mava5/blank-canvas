
using TMPro;
using UnityEngine;


[ExecuteAlways]
public class TMPTintSpritesField : MonoBehaviour
{
    [SerializeField] private bool tintAllSprites = false;

    void Awake()
    { if (TryGetComponent<TextMeshProUGUI>(out var tmp)) { tmp.tintAllSprites = tintAllSprites; } }

    void OnValidate() => Awake();
}
