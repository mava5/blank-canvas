
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;


public interface IService { }

public static partial class Services
{
    public static bool TryAssign<T>(T serviceProvider) where T : IService
    {
        if (serviceProvider == null)
        { Debug.LogWarning($"[Services]: Trying to assign {typeof(T).Name} as null."); return false; }

        int target = GetServiceCount(serviceProvider);
        int final = 0;

        foreach (var field in typeof(Services).GetFields(BindingFlags.Static | BindingFlags.Public))
        {
            if (!field.FieldType.IsAssignableFrom(typeof(T))) { continue; }

            var currentValue = (T)field.GetValue(null);

            if (currentValue is UnityEngine.Object currentObj)
            {
                if (serviceProvider is UnityEngine.Object previousObj && currentObj == previousObj)
                { Debug.LogWarning($"[Services]: {typeof(T).Name} is already assigned to {currentObj.name}."); final++; continue; }

                if (currentObj != null)
                { Debug.LogWarning($"[Services]: Trying to reassign {typeof(T).Name} from {currentObj.name}."); continue; }
            }
            else
            {
                if (EqualityComparer<T>.Default.Equals(currentValue, serviceProvider))
                { Debug.LogWarning($"[Services]: {typeof(T).Name} is already assigned to this."); final++; continue; }

                if (currentValue != null)
                { Debug.LogWarning($"[Services]: Trying to reassign {typeof(T).Name}."); continue; }
            }

            final++;
            field.SetValue(null, serviceProvider);
        }

        // Method was successful if all services of provider were assigned
        return target == final;
    }

    public static int GetServiceCount(object provider)
    {
        return provider.GetType().GetInterfaces().
            Where(i => typeof(IService).IsAssignableFrom(i) && i != typeof(IService)).Count();
    }
}
