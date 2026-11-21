
using System;
using UnityEngine;


/// <summary>
/// <br/>   Targeted field begins a foldout group of given name.
/// <br/>   Foldout group ends when encountering:
/// <br/>   • Another <see cref="FoldoutAttribute"/> 
/// <br/>   • <see cref="EndFoldoutAttribute"/> 
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class FoldoutAttribute : PropertyAttribute
{
    public readonly string Name = "";
    public FoldoutAttribute(string name) { Name = name; }
}

/// <summary> Ends previous <see cref="FoldoutAttribute"/>. </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class EndFoldoutAttribute : PropertyAttribute
{
    public readonly bool AfterNext = false;
    public EndFoldoutAttribute(bool afterNext = false) { AfterNext = afterNext; }
}
