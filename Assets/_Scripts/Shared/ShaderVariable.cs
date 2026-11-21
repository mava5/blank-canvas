
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif

using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using SVT = ShaderVariableType;
using SV = ShaderVariable;
using MPB = UnityEngine.MaterialPropertyBlock;


public enum ShaderVariableType
{
    None,
    Int,
    Bool,
    Float,
    Color,
    Vector2,
    Vector3,
    Vector4,
}

/// <summary>
/// <br/>   An wrapper around a dictionary of <see cref="ShaderVariable"/>.
/// <br/>   Abstractifies and optimizes lerping between these variables.
/// </summary>
public class SVCollection
{
    public Renderer Renderer = null;

    private readonly MPB mpb = null;
    private readonly Dictionary<string, SV> variableRows = new();

    public SVCollection(Renderer renderer, List<SV> shaderVariables)
    {
        if (renderer == null || shaderVariables.Count == 0) { return; }

        Renderer = renderer;
        mpb = new();
        Renderer.GetPropertyBlock(mpb);

        foreach (SV variableRow in shaderVariables)
        {
            if (variableRow == null || !variableRow.InMPB(mpb)) { continue; }

            variableRows.TryAdd(variableRow.Name, null);
            variableRows[variableRow.Name] = variableRow;
        }
    }


    #region Public Methods ======================================================================== Public Methods

    public void UpdateMPB()
    { if (Renderer != null) { Renderer.GetPropertyBlock(mpb); } }

    public void UpdateRenderer()
    { if (Renderer != null) { Renderer.SetPropertyBlock(mpb); } }


    public void SetMultiple(string state)
    {
        if (Renderer == null) { return; }
        foreach (string key in variableRows.Keys) { SetSingle(key, state); }
    }

    public void SetSingle(string key, string state)
    {
        if (Renderer == null || !variableRows.ContainsKey(key)) { return; }
        SV variable = variableRows[key];
        if (variable.Values.TryGetValue(state, out object value)) { Set(key, variable.Type, value); }
    }
    

    public void LerpMultiple(string startState, string endState, float t)
    {
        if (Renderer == null) { return; }
        foreach (string key in variableRows.Keys) { LerpSingle(key, startState, endState, t); }
    }

    public void LerpSingle(string key, string startState, string endState, float t)
    {
        if (Renderer == null || !variableRows.ContainsKey(key)) { return; }
        SV variable = variableRows[key];
        if (variable.Values.TryGetValue(startState, out object a) &&
            variable.Values.TryGetValue(endState, out object b)) { Lerp(key, variable.Type, a, b, t); }
    }


    public void LerpMultipleFromCurrent(string endState, float t)
    {
        if (Renderer == null) { return; }
        foreach (string key in variableRows.Keys) { LerpSingleFromCurrent(key, endState, t); }
    }

    public void LerpSingleFromCurrent(string key, string endState, float t)
    {
        if (Renderer == null || !variableRows.ContainsKey(key)) { return; }
        SV variable = variableRows[key];
        if (variable.Values.TryGetValue(endState, out object b)) { LerpCurrent(key, variable.Type, b, t); }
    }

    #endregion Public Methods


    #region Private Methods ======================================================================= Private Methods

    private void Set(string key, SVT type, object value)
    {
        switch (type)
        {
            case SVT.Int: mpb.SetInteger(key, (int)value); break;
            case SVT.Bool: mpb.SetInteger(key, ((bool)value) ? 1 : 0); break;
            case SVT.Float: mpb.SetFloat(key, (float)value); break;
            case SVT.Color: mpb.SetColor(key, (Color)value); break;
            case SVT.Vector2: mpb.SetVector(key, (Vector2)value); break;
            case SVT.Vector3: mpb.SetVector(key, (Vector3)value); break;
            case SVT.Vector4: mpb.SetVector(key, (Vector4)value); break;
        }
    }

    private void Lerp(string key, SVT type, object a, object b, float t)
    {
        switch (type)
        {
            case SVT.Int: mpb.SetInteger(key, Mathf.RoundToInt(Mathf.Lerp((int)a, (int)b, t))); break;
            case SVT.Bool: mpb.SetInteger(key, ((t <= 0.5f) ? (bool)a : (bool)b) ? 1 : 0); break;
            case SVT.Float: mpb.SetFloat(key, Mathf.Lerp((float)a, (float)b, t)); break;
            case SVT.Color: mpb.SetColor(key, Color.Lerp((Color)a, (Color)b, t)); break;
            case SVT.Vector2: mpb.SetVector(key, Vector2.Lerp((Vector2)a, (Vector2)b, t)); break;
            case SVT.Vector3: mpb.SetVector(key, Vector3.Lerp((Vector3)a, (Vector3)b, t)); break;
            case SVT.Vector4: mpb.SetVector(key, Vector4.Lerp((Vector4)a, (Vector4)b, t)); break;
        }
    }

    private void LerpCurrent(string key, SVT type, object b, float t)
    {
        switch (type)
        {
            case SVT.Int: mpb.SetInteger(key, Mathf.RoundToInt(Mathf.Lerp(mpb.GetInteger(key), (int)b, t))); break;
            case SVT.Bool: mpb.SetInteger(key, ((t <= 0.5f) ? (mpb.GetInteger(key) == 1) : (bool)b) ? 1 : 0); break;
            case SVT.Float: mpb.SetFloat(key, Mathf.Lerp(mpb.GetFloat(key), (float)b, t)); break;
            case SVT.Color: mpb.SetColor(key, Color.Lerp(mpb.GetColor(key), (Color)b, t)); break;
            case SVT.Vector2: mpb.SetVector(key, Vector2.Lerp(mpb.GetVector(key), (Vector2)b, t)); break;
            case SVT.Vector3: mpb.SetVector(key, Vector3.Lerp(mpb.GetVector(key), (Vector3)b, t)); break;
            case SVT.Vector4: mpb.SetVector(key, Vector4.Lerp(mpb.GetVector(key), (Vector4)b, t)); break;
        }
    }

    #endregion Private Methods
}

/// <summary>
/// <br/>   A Json serialized non-generic data type for different shader supported variable types.
/// <br/>   Enables the creation of lists for values of different types.
/// <br/>   Specify variable states with <see cref="SVStatesAttribute"/>.
/// </summary>
[Serializable]
public class ShaderVariable
{
    public string Name => name;
    public SVT Type => type;
    public List<string> States => states;

#if UNITY_EDITOR
    public Dictionary<string, object> Values
    {
        get
        {
            if (deserializedValues == null || !Application.isPlaying)
            { deserializedValues = GetDeserializedValues(); }
            return deserializedValues;
        }
    }
#else
    public Dictionary<string, object> Values => deserializedValues ??= GetDeserializedValues();
#endif

    [SerializeField] private string name = "";
    [SerializeField] private SVT type = SVT.None;
    [SerializeField] private List<string> valueJsons = new();
    [SerializeField] private List<string> states = new();

    private Dictionary<string, object> deserializedValues = null;


    public bool InMPB(MPB mpb)
    {
        return type switch
        {
            SVT.Int => mpb.HasInteger(name),
            SVT.Bool => mpb.HasInteger(name),
            SVT.Float => mpb.HasFloat(name),
            SVT.Color => mpb.HasColor(name),
            SVT.Vector2 => mpb.HasVector(name),
            SVT.Vector3 => mpb.HasVector(name),
            SVT.Vector4 => mpb.HasVector(name),
            _ => false
        };
    }


    #region Deserialization ========================================================================= Deserialization

    private Dictionary<string, object> GetDeserializedValues()
    {
        Dictionary<string, object> result = new();

        for (int i = 0; i < states.Count; i++)
        { result.TryAdd(states[i], GetDeserializedValue(type, valueJsons[i])); }

        return result;
    }

    private static object GetDeserializedValue(SVT type, string json)
    {
        return type switch
        {
            SVT.Int => JsonUtility.FromJson<IntWrapper>(json)?.V,
            SVT.Bool => JsonUtility.FromJson<BoolWrapper>(json)?.V,
            SVT.Float => JsonUtility.FromJson<FloatWrapper>(json)?.V,
            SVT.Color => JsonUtility.FromJson<ColorWrapper>(json)?.V,
            SVT.Vector2 => JsonUtility.FromJson<Vector2Wrapper>(json)?.V,
            SVT.Vector3 => JsonUtility.FromJson<Vector3Wrapper>(json)?.V,
            SVT.Vector4 => JsonUtility.FromJson<Vector4Wrapper>(json)?.V,
            _ => null
        };
    }

    private static string GetValueJson(SVT type, object newValue)
    {
        return type switch
        {
            SVT.Int => JsonUtility.ToJson(new IntWrapper((int)newValue)),
            SVT.Bool => JsonUtility.ToJson(new BoolWrapper((bool)newValue)),
            SVT.Float => JsonUtility.ToJson(new FloatWrapper((float)newValue)),
            SVT.Color => JsonUtility.ToJson(new ColorWrapper((Color)newValue)),
            SVT.Vector2 => JsonUtility.ToJson(new Vector2Wrapper((Vector2)newValue)),
            SVT.Vector3 => JsonUtility.ToJson(new Vector3Wrapper((Vector3)newValue)),
            SVT.Vector4 => JsonUtility.ToJson(new Vector4Wrapper((Vector4)newValue)),
            _ => null
        };
    }

    [Serializable] private class IntWrapper { public int V; public IntWrapper(int v) => V = v; }
    [Serializable] private class BoolWrapper { public bool V; public BoolWrapper(bool v) => V = v; }
    [Serializable] private class FloatWrapper { public float V; public FloatWrapper(float v) => V = v; }
    [Serializable] private class ColorWrapper { public Color V; public ColorWrapper(Color v) => V = v; }
    [Serializable] private class Vector2Wrapper { public Vector2 V; public Vector2Wrapper(Vector2 v) => V = v; }
    [Serializable] private class Vector3Wrapper { public Vector3 V; public Vector3Wrapper(Vector3 v) => V = v; }
    [Serializable] private class Vector4Wrapper { public Vector4 V; public Vector4Wrapper(Vector4 v) => V = v; }

    #endregion Deserialization


#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SV))]
    public class ShaderVariableDrawer : PropertyDrawer
    {
        private const float PADDING = 2f;
        private const float TYPE_WIDTH = 80f;
        private GUIContent noContent = null;

        private List<string> states = new() { "Default" };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            noContent = GUIContent.none;
            SVStatesAttribute statesAttribute = fieldInfo.GetCustomAttribute<SVStatesAttribute>();

            if (statesAttribute != null && statesAttribute.States.Count > 0)
            { states = statesAttribute.States; }

            if ((SVT)property.FindPropertyRelative("type").enumValueIndex == SVT.None)
            { return EditorGUIUtility.singleLineHeight; }

            return (1 + states.Count) * (EditorGUIUtility.singleLineHeight + PADDING);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            Rect typeRect = new(rect) { width = TYPE_WIDTH };
            Rect nameRect = new(rect) { x = rect.x + TYPE_WIDTH + PADDING, width = rect.width - TYPE_WIDTH - PADDING };


            EditorGUI.BeginChangeCheck();
            SerializedProperty typeProperty = property.FindPropertyRelative("type");
            EditorGUI.PropertyField(typeRect, typeProperty, noContent);
            bool typeChanged = EditorGUI.EndChangeCheck();

            SerializedProperty nameProperty = property.FindPropertyRelative("name");
            EditorGUI.PropertyField(nameRect, nameProperty, noContent);


            SerializedProperty statesProperty = property.FindPropertyRelative("states");

            while (statesProperty.arraySize > states.Count)
            { statesProperty.DeleteArrayElementAtIndex(statesProperty.arraySize - 1); }

            while (statesProperty.arraySize < states.Count)
            {
                statesProperty.InsertArrayElementAtIndex(statesProperty.arraySize);
                statesProperty.GetArrayElementAtIndex(statesProperty.arraySize - 1).stringValue = states[statesProperty.arraySize - 1];
            }


            SerializedProperty valueJsonsProperty = property.FindPropertyRelative("valueJsons");

            while (valueJsonsProperty.arraySize > states.Count)
            { valueJsonsProperty.DeleteArrayElementAtIndex(valueJsonsProperty.arraySize - 1); }

            while (valueJsonsProperty.arraySize < states.Count)
            {
                valueJsonsProperty.InsertArrayElementAtIndex(valueJsonsProperty.arraySize);
                valueJsonsProperty.GetArrayElementAtIndex(valueJsonsProperty.arraySize - 1).stringValue = "";
            }


            SVT type = (SVT)typeProperty.enumValueIndex;
            Rect valueRect = new(rect);

            for (int i = 0; i < states.Count; i++)
            {
                SerializedProperty jsonProperty = valueJsonsProperty.GetArrayElementAtIndex(i);
                string json = typeChanged ? "" : jsonProperty.stringValue;
                valueRect.y += PADDING + valueRect.height;
                jsonProperty.stringValue = DrawValueField(valueRect, states[i], type, json);
            }
        }

        private string DrawValueField(Rect rect, string label, SVT type, string json)
        {
            return type switch
            {
                SVT.Int => GetValueJson(type, EditorGUI.IntField(rect, label, (int?)GetDeserializedValue(type, json) ?? 0)),
                SVT.Bool => GetValueJson(type, EditorGUI.Toggle(rect, label, (bool?)GetDeserializedValue(type, json) ?? false)),
                SVT.Float => GetValueJson(type, EditorGUI.FloatField(rect, label, (float?)GetDeserializedValue(type, json) ?? 0f)),
                SVT.Color => GetValueJson(type, EditorGUI.ColorField(rect, label, (Color?)GetDeserializedValue(type, json) ?? Color.white)),
                SVT.Vector2 => GetValueJson(type, EditorGUI.Vector2Field(rect, label, (Vector2?)GetDeserializedValue(type, json) ?? Vector2.zero)),
                SVT.Vector3 => GetValueJson(type, EditorGUI.Vector3Field(rect, label, (Vector3?)GetDeserializedValue(type, json) ?? Vector3.zero)),
                SVT.Vector4 => GetValueJson(type, EditorGUI.Vector4Field(rect, label, (Vector4?)GetDeserializedValue(type, json) ?? Vector4.zero)),
                _ => null
            };
        }
    }
#endif
}


[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class SVStatesAttribute : PropertyAttribute
{
    public readonly List<string> States = new();
    public SVStatesAttribute(params string[] states) => States = states.ToList();
}
