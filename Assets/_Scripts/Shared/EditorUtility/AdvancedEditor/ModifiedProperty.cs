
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditorInternal;

using UnityEngine;


namespace UtilityEditor
{
    public class ModifiedProperty
    {
        public readonly SerializedProperty Property = null;
        public readonly GUIContent Label = null;

        public ReorderableList ReorderableList = null;

        public bool IsShown => forceShowState ?? shownPredicates.All(p => p.Invoke());
        public bool IsEnabled => forceEnableState ?? enabledPredicates.All(p => p.Invoke());


        private bool? forceShowState = null;
        private bool? forceEnableState = null;

        private readonly List<Func<bool>> shownPredicates = new();
        private readonly List<Func<bool>> enabledPredicates = new();


        public ModifiedProperty(SerializedProperty property)
        {
            Property = property;
            Label = new(property.displayName);
        }


        public void SetForceShowState(bool? value) => forceShowState = value;
        public void SetForceEnableState(bool? value) => forceEnableState = value;

        /// <param name="predicate"> Inputs the current state and outputs the new state </param>
        public void AddShownPredicate(Func<bool> predicate) => shownPredicates.Add(predicate);

        /// <param name="predicate"> Inputs the current state and outputs the new state </param>
        public void AddEnabledPredicate(Func<bool> predicate) => enabledPredicates.Add(predicate);
    }
}

#endif
