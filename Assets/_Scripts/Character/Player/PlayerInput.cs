
using System;
using System.Collections.Generic;

using UnityEngine;


/// <summary> Handles <see cref="Player"/> input based on controls settings. </summary>
public class PlayerInput : CharacterInput3D
{
    internal bool Initialized { get; private set; } = true;


    public Dictionary<int, Touch> validTouches = new();

    private Action onDestroy;


    #region Lifecycle ============================================================================= Lifecycle

    internal void Initialize()
    {

    }

    private void OnDestroy() => onDestroy?.Invoke();

    public void Update()
    {
        Look = Input.mousePositionDelta;
        Movement = new(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        WantsToJump = Input.GetKeyDown(KeyCode.Space);
    }

    #endregion Lifecycle
}
