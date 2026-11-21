
using UnityEngine;


public class HubLightingSettings : MonoBehaviour
{
    public Color Sourec0Color
    {
        get => source0Color;
        set
        {
            source0Color = value;
            UpdateGlobalValues();
        }
    }

    public float Sourec0Power
    {
        get => source0Power;
        set
        {
            source0Power = Mathf.Clamp(value, 0f, 2f);
            UpdateGlobalValues();
        }
    }

    public Color Sourec1Color
    {
        get => source1Color;
        set
        {
            source1Color = value;
            UpdateGlobalValues();
        }
    }

    public float Sourec1Power
    {
        get => source1Power;
        set
        {
            source1Power = Mathf.Clamp(value, 0f, 2f);
            UpdateGlobalValues();
        }
    }

    public Color Sourec2Color
    {
        get => source2Color;
        set
        {
            source2Color = value;
            UpdateGlobalValues();
        }
    }

    public float Sourec2Power
    {
        get => source2Power;
        set
        {
            source2Power = Mathf.Clamp(value, 0f, 2f);
            UpdateGlobalValues();
        }
    }

    [SerializeField] private Color source0Color = Color.white;
    [SerializeField, Range(0f, 2f)] private float source0Power = 1.0f;
    [Space]
    [SerializeField] private Color source1Color = Color.white;
    [SerializeField, Range(0f, 2f)] private float source1Power = 1.0f;
    [Space]
    [SerializeField] private Color source2Color = Color.white;
    [SerializeField, Range(0f, 2f)] private float source2Power = 1.0f;

    protected virtual void Awake() => UpdateGlobalValues();

    private void UpdateGlobalValues()
    {
        Shader.SetGlobalColor("_G_Lighting_Source0_Color", source0Color);
        Shader.SetGlobalColor("_G_Lighting_Source1_Color", source1Color);
        Shader.SetGlobalColor("_G_Lighting_Source2_Color", source2Color);
        Shader.SetGlobalFloat("_G_Lighting_Source0_Power", source0Power);
        Shader.SetGlobalFloat("_G_Lighting_Source1_Power", source1Power);
        Shader.SetGlobalFloat("_G_Lighting_Source2_Power", source2Power);
    }

#if UNITY_EDITOR

    protected virtual void OnValidate() => UpdateGlobalValues();

#endif
}
