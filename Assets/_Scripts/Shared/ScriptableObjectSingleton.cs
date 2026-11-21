
using System;
using System.Collections.Generic;

using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

using Utility;
using UtilityExtensions;



/// <summary> Used for creating a <see cref="ScriptableObject"/> singleton. </summary>
public abstract class ScriptableObjectSingleton<TDerived> : ScriptableObject
    where TDerived : ScriptableObjectSingleton<TDerived>
{
    private enum ArgState
    {
        None,
        Missing,
        Extra,
        Wrong,
        Valid,
    }


    /// <summary> Determines if the singleton also has a persistent container for components or other objects. </summary>
    protected abstract bool RequiresPersistentContainer { get; }

    /// <summary> The type and name of the arguments expected by the ScriptableObject. </summary>
    protected abstract TypeAndName[] ExpectedArgs { get; }

    protected GameObject persistentContainer = null;


    protected virtual void OnValidate() { }

    public static async UniTask<TDerived> CreateFromResourcesAsync(string path, params object[] args)
    {
        TDerived scriptableObject = null;
        GameObject container = null;

        try
        {
            scriptableObject = Instantiate(Resources.Load<TDerived>(path));

            if (!scriptableObject.ArgsAreValid())
            { throw new ControlledException("Args are invalid!"); }

            if (scriptableObject.RequiresPersistentContainer)
            {
                container = new($"[{typeof(TDerived).Name}]");
                DontDestroyOnLoad(container);
                scriptableObject.persistentContainer = container;
            }

            await scriptableObject.InitializeAsync(args);

            return scriptableObject;
        }
        catch (Exception ex)
        {
            ex.LogWithContext<TDerived>();

            if (scriptableObject != null)
            {
                scriptableObject.OnInitializationFailed();
                Destroy(scriptableObject);
            }

            if (container != null)
            { Destroy(container.gameObject); }

            return null;
        }
    }

    public static async UniTask<TDerived> CreateFromAddressablesAsync(string key, params object[] args)
    {
        AsyncOperationHandle<TDerived> handle = default;
        TDerived scriptableObject = null;
        GameObject container = null;

        try
        {
            handle = Addressables.LoadAssetAsync<TDerived>(key);

            await handle;

            scriptableObject = Instantiate(handle.Result);
            Addressables.Release(handle);

            if (!scriptableObject.ArgsAreValid())
            { throw new ControlledException("Args are invalid!"); }

            if (scriptableObject.RequiresPersistentContainer)
            {
                container = new($"[{typeof(TDerived).Name}]");
                DontDestroyOnLoad(container);
                scriptableObject.persistentContainer = container;
            }

            await scriptableObject.InitializeAsync(args);

            return scriptableObject;
        }
        catch (Exception ex)
        {
            ex.LogWithContext<TDerived>();

            if (scriptableObject != null)
            {
                scriptableObject.OnInitializationFailed();
                Destroy(scriptableObject);
            }

            if (container != null)
            { Destroy(container); }

            if (handle.IsValid())
            { Addressables.Release(handle); }

            return null;
        }
    }

    /// <remarks> <see cref="ControlledException"/> </remarks>
    protected abstract UniTask<TDerived> InitializeAsync(params object[] args);

    protected abstract void OnInitializationFailed();

    private bool ArgsAreValid(params object[] args)
    {
        if (ExpectedArgs == null || ExpectedArgs.Length == 0) { return true; }

        int expectedCount = ExpectedArgs.Length;
        Dictionary<ArgState, int> stateCounts = new();
        List<(Type inputType, Type expectedType, string name, ArgState state)> argStates = new();

        // Add: Extra, Wrong or Valid
        for (int i = 0; i < args.Length; i++)
        {
            Type inputType = args[i].GetType();
            Type expectedType = ExpectedArgs.Length > i ? ExpectedArgs[i].Type : null;
            string name = ExpectedArgs.Length > i ? ExpectedArgs[i].Name : null;

            ArgState state = true switch
            {
                true when i >= ExpectedArgs.Length => ArgState.Extra,
                true when inputType != ExpectedArgs[i].Type => ArgState.Wrong,
                _ => ArgState.Valid
            };

            stateCounts.TryAdd(state, 0);
            stateCounts[state]++;
            argStates.Add((inputType, expectedType, name, state));
        }

        // Add: Missing
        for (int i = args.Length; i < ExpectedArgs.Length; i++)
        { argStates.Add((null, ExpectedArgs[i].Type, ExpectedArgs[i].Name, ArgState.Missing)); }

        // Log faulty arguments
        if (stateCounts.GetValueOrDefault(ArgState.Extra) > 0 ||
            stateCounts.GetValueOrDefault(ArgState.Wrong) > 0 ||
            stateCounts.GetValueOrDefault(ArgState.Missing) > 0)
        {
            string toLog = "";
            int counter = 0;

            foreach ((Type inputType, Type expectedType, string name, ArgState state) in argStates)
            {
                string inputTypeName = inputType?.Name ?? "NULL";
                string expectedTypeName = expectedType?.Name ?? "NULL";
                string argName = name ?? "NULL";

                toLog += $"[{counter} | {argName} | {state}]: Got {inputTypeName}, Expected {expectedTypeName}";
                counter++;
            }

            Util.LogWarning<TDerived>(toLog);

            return false;
        }

        return true;
    }
}
