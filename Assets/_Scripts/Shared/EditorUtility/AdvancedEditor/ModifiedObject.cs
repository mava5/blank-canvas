
#if UNITY_EDITOR

using System;
using UnityEditor;

using UnityEngine;

using UtilityEnum;


namespace UtilityEditor
{
    public class ModifiedObject
    {
        public readonly SerializedObject Object = null;
        public readonly ObjType ObjectType = ObjType.None;
        public readonly Type TargetType = null;
        public readonly Component TargetComponent = null;

        internal ModifiedObject(SerializedObject serializedObject, UnityEngine.Object target)
        {
            Object = serializedObject;
            TargetType = target.GetType();

            if (typeof(ScriptableObject).IsAssignableFrom(target.GetType()))
            {
                ObjectType = ObjType.ScriptableObject;
                return;
            }

            TargetComponent = (Component)target;

            bool isPartOfPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(target);
            bool isPartOfPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(target);
            bool hasPrefabSource = PrefabUtility.GetCorrespondingObjectFromSource(target) != null;

            switch (true)
            {
                // Normal object not related to any prefabs
                case true when !isPartOfPrefabAsset && !isPartOfPrefabInstance:
                    ObjectType = ObjType.Instance; break;

                // Instantiated copy of a prefab
                case true when isPartOfPrefabInstance:
                    ObjectType = ObjType.PrefabCopy; break;

                // Prefab itself
                case true when isPartOfPrefabAsset && !hasPrefabSource:
                    ObjectType = ObjType.Prefab; break;
            }
        }
    }
}

#else

namespace UtilityEditor
{
    public class ModifiedObject { }
}

#endif
