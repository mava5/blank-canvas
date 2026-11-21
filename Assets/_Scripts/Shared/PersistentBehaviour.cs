
using System;
using System.Threading;

using UnityEngine;
using Cysharp.Threading.Tasks;

using Utility;
using UtilityExtensions;


/// <summary> Used for creating a persistent <see cref="MonoBehaviour"/> with DontDestroyOnLoad. </summary>
public abstract class PersistentBehaviour<TDerived> : MonoBehaviour
    where TDerived : PersistentBehaviour<TDerived>
{
    public static async UniTask<TDerived> CreateAsync(GameObject parent = null, params object[] args)
    {
        TDerived component = null;
        bool isOwnGameObject = parent == null;

        if (isOwnGameObject)
        {
            parent = new($"[{typeof(TDerived).Name}]");
            DontDestroyOnLoad(parent);
        }
        else if (parent.scene.name != "DontDestroyOnLoad")
        { Util.LogWarning<TDerived>($"{parent.name} not marked DontDestroyOnLoad."); }

        try
        {
            component = parent.AddComponent<TDerived>();
            return await component.InitializeAsync(component.GetCancellationTokenOnDestroy(), args);
        }
        catch (Exception ex)
        {
            ex.LogWithContext<TDerived>();

            if (component != null)
            {
                component.OnInitializationFailed();
                if (isOwnGameObject) { Destroy(parent); }
                else { if (component != null) { Destroy(component); } }
            }

            return null;
        }
    }

    /// <remarks> <see cref="ControlledException"/> </remarks>
    protected abstract UniTask<TDerived> InitializeAsync(CancellationToken token, params object[] args);

    protected abstract void OnInitializationFailed();
}
