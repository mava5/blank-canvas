
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor;
using UnityEditorInternal;

using UnityEngine;

using UtilityEnum;
using UtilityExtensions;


namespace UtilityEditor
{
    /// <summary>
    /// <br/>   Implements logic for:
    /// <br/>   • <see cref="IConditionalShowAttribute"/>
    /// <br/>   • <see cref="IConditionalEnableAttribute"/>
    /// <br/>   • <see cref="LabelAttribute"/>
    /// <br/>   • <see cref="TrimmedLabelAttribute"/>
    /// <br/>   • <see cref="FoldoutAttribute"/>
    /// <br/>   • <see cref="EndFoldoutAttribute"/>
    /// <br/>   • <see cref="HideComponentAttribute"/>
    /// <br/>   • <see cref="RenameableListAttribute"/>
    /// </summary>
    /// <remarks>
    /// <br/>   Optionally add within UNITY_EDITOR: <c>
    /// <br/>   [<see cref="SerializeField"/>] protected bool showHiddenComponents = false;
    /// <br/>   [<see cref="SerializeField"/>] protected List&lt;bool&gt; foldoutStates = new(); </c>
    /// <br/>   
    /// <br/>   These will allow persistence for some Inspector states.
    /// <br/>   They would otherwise reset after deselecting object.
    /// </remarks>
    // [SerializeField] protected bool showHiddenComponents = false;
    // [SerializeField] protected List<bool> foldoutStates = new();
    public abstract class AdvancedEditor : Editor
    {
        protected class FoldoutData
        {
            public readonly string Name = null;
            public bool IsOpen = true;
            public bool IsShown = true;
            public List<ModifiedProperty> Properties = new();

            public FoldoutData(string name) { Name = name; }
        }

        protected GUIStyle foldoutStyle = null;
        protected GUIStyle foldoutTitleStyle = null;
        protected GUIStyle foldoutTitleBackgroundStyle = null;
        protected GUIStyle centeredLabel = null;
        protected GUIStyle headerLabel = null;
        private bool stylesInitialized = false;

        private ModifiedObject modifiedObject = null;

        private SerializedProperty memberScriptProperty = null;

        private SerializedProperty showHiddenComponentsProperty = null;
        private HideComponentAttribute hideComponentsAttribute = null;
        private List<Component> hiddenComponents = new();

        private SerializedProperty foldoutStatesProperty = null;
        private List<FoldoutData> foldoutDatas = new();


        private bool drawingStopRequested = false;
        private bool foldoutDrawingStopRequested = false;


        #region Lifecycle ============================================================================= Lifecycle

        private void OnEnable()
        {
            modifiedObject = new(serializedObject, target);
            
            if (modifiedObject.ObjectType != ObjType.ScriptableObject)
            {
                showHiddenComponentsProperty = serializedObject.FindProperty("showHiddenComponents");
                hideComponentsAttribute = modifiedObject.TargetType.GetCustomAttribute<HideComponentAttribute>();
            }

            memberScriptProperty = serializedObject.FindProperty("m_Script");
            foldoutStatesProperty = serializedObject.FindProperty("foldoutStates");

            OnEnableInternal();

            InitializeFoldoutDatas();
            UpdateComponentsToHideVisibility();
        }

        private void OnDisable()
        {
            OnDisableInternal();
            //AttemptShowHiddenComponents();
        }

        public override sealed void OnInspectorGUI()
        {
            drawingStopRequested = false;
            foldoutDrawingStopRequested = false;

            if (!stylesInitialized)
            {
                foldoutStyle = new("Wizard Box")
                {
                    padding = new(17, 5, 5, 5)
                };
                foldoutTitleStyle = new("Foldout")
                {
                    padding = new(16, 0, 0, 0),
                    margin = new(8, 0, 0, 0),
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                foldoutTitleBackgroundStyle = new("GameViewBackground")
                {
                    padding = new(0, 0, 1, 0)
                };
                centeredLabel = new("CenteredLabel")
                {
                    fontStyle = FontStyle.Bold
                };
                headerLabel = new(EditorStyles.boldLabel);

                OnInitializeStyles();

                stylesInitialized = true;
            }

            if (!drawingStopRequested) { OnInspectorGUIFirst(); }
            serializedObject.Update();
            if (!drawingStopRequested) { OnInspectorGUIBegin(); }

            if (memberScriptProperty != null)
            {
                using (new EditorGUI.DisabledGroupScope(true))
                { EditorGUILayout.PropertyField(memberScriptProperty); }
                EditorGUILayout.Space();
            }

            if (showHiddenComponentsProperty != null && hideComponentsAttribute != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(showHiddenComponentsProperty);
                EditorGUILayout.Space();

                if (EditorGUI.EndChangeCheck())
                { UpdateComponentsToHideVisibility(); }
            }

            if (!drawingStopRequested) { DrawFoldouts(foldoutDatas); }

            if (!drawingStopRequested) { OnInspectorGUIEnd(); }
            serializedObject.ApplyModifiedProperties();
            if (!drawingStopRequested) { OnInspectorGUILast(); }
        }

        protected virtual void OnEnableInternal() { }
        protected virtual void OnDisableInternal() { }

        /// <summary>
        /// <br/>   Called for very first <see cref="OnInspectorGUI()"/>.
        /// <br/>   Can be used to initialize <see cref="GUIStyle"/> variables.
        /// </summary>
        protected virtual void OnInitializeStyles() { }

        /// <summary> Called when about to draw a property's field. </summary>
        /// <returns> <see cref="bool"/>: Should property field still be drawn </returns>
        /// <remarks> Default: Returns true </remarks>
        protected virtual bool OnPropertyFieldBegin(ModifiedProperty modifiedProperty) { return true; }

        /// <summary> Called after drawing a property's field. </summary>
        protected virtual void OnPropertyFieldEnd(ModifiedProperty modifiedProperty) { }

        /// <summary><c>
        /// <br/>   ...
        /// <br/>   <see cref="OnInspectorGUIFirst"/>; --- This
        /// <br/>   <see cref="SerializedObject.Update"/>;
        /// <br/>   <see cref="OnInspectorGUIBegin"/>;
        /// <br/>   ...
        /// </c></summary>
        protected virtual void OnInspectorGUIFirst() { }

        /// <summary><c>
        /// <br/>   ...
        /// <br/>   <see cref="OnInspectorGUIFirst"/>;
        /// <br/>   <see cref="SerializedObject.Update"/>;
        /// <br/>   <see cref="OnInspectorGUIBegin"/>; --- This
        /// <br/>   ...
        /// </c></summary>
        protected virtual void OnInspectorGUIBegin() { }

        /// <summary><c>
        /// <br/>   ...
        /// <br/>   <see cref="OnInspectorGUIEnd"/>; --- This
        /// <br/>   <see cref="SerializedObject.ApplyModifiedProperties"/>;
        /// <br/>   <see cref="OnInspectorGUILast"/>;
        /// <br/>   ...
        /// </c></summary>
        protected virtual void OnInspectorGUIEnd() { }

        /// <summary><c>
        /// <br/>   ...
        /// <br/>   <see cref="OnInspectorGUIEnd"/>;
        /// <br/>   <see cref="SerializedObject.ApplyModifiedProperties"/>;
        /// <br/>   <see cref="OnInspectorGUILast"/>; --- This
        /// <br/>   ...
        /// </c></summary>
        protected virtual void OnInspectorGUILast() { }

        #endregion Lifecycle


        #region Field Processing ====================================================================== Field Processing

        private void InitializeFoldoutDatas()
        {
            foldoutDatas.Clear();

            // Initialize foldouts based on serialized properties and their attributes
            SerializedProperty iteratedProperty = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iteratedProperty.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iteratedProperty.name == "m_Script" ||
                    iteratedProperty.name == "showHiddenComponents" ||
                    iteratedProperty.name == "foldoutStates") { continue; }

                FieldInfo fieldInfo = serializedObject.targetObject.GetType().GetField(iteratedProperty.name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fieldInfo == null) { continue; }

                ModifiedProperty modifiedProperty = new(iteratedProperty.Copy());

                foreach (Attribute attribute in fieldInfo.GetCustomAttributes())
                { ProcessAttribute(fieldInfo, attribute, modifiedProperty); }

                // Start with a non-foldout if no new one was created
                if (foldoutDatas.Count == 0) { foldoutDatas.Add(new(null)); }

                if (fieldInfo.GetCustomAttribute<EndFoldoutAttribute>()?.AfterNext == true)
                {
                    foldoutDatas[^1].Properties.Add(modifiedProperty);
                    foldoutDatas.Add(new(null));
                }
                else
                {
                    // Add property to latest foldout
                    foldoutDatas[^1].Properties.Add(modifiedProperty);
                }

                // Show foldout when it has any visible properties
                if (foldoutDatas[^1].Name != null && modifiedProperty.IsShown)
                { foldoutDatas[^1].IsShown = true; }
            }

            // Merge foldouts of same names together
            List<FoldoutData> mergedFoldouts = new();
            foreach (FoldoutData foldout in foldoutDatas)
            {
                FoldoutData mergedFoldout = null;
                if (foldout.Name != null && mergedFoldouts.Any(f => { mergedFoldout = f; return f.Name == foldout.Name; }))
                {
                    if (foldout.IsShown) { mergedFoldout.IsShown = true; }
                    mergedFoldout.Properties.AddRange(foldout.Properties);
                    continue;
                }

                mergedFoldouts.Add(foldout);
            }
            foldoutDatas = mergedFoldouts;

            // Update foldout states
            if (foldoutStatesProperty != null)
            {
                serializedObject.Update();
                foldoutStatesProperty.ResizeArray(foldoutDatas.Count, fillValue: false);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                for (int i = 0; i < foldoutDatas.Count; i++)
                { foldoutDatas[i].IsOpen = foldoutStatesProperty.GetArrayElementAtIndex(i).boolValue; }
            }
        }

        private void ProcessAttribute(FieldInfo fieldInfo, Attribute attribute, ModifiedProperty modifiedProperty)
        {
            var underlyingProperty = modifiedProperty.Property;

            if (attribute is IConditionalShowAttribute conditionalShowAttr)
            { modifiedProperty.AddShownPredicate(() => conditionalShowAttr.IsShown(modifiedObject)); }

            else if (attribute is IConditionalEnableAttribute conditionalEnableAttr)
            { modifiedProperty.AddEnabledPredicate(() => conditionalEnableAttr.IsEnabled(modifiedObject)); }

            else if (attribute is LabelAttribute labelAttr)
            { labelAttr.ProcessLabel(modifiedProperty.Label); }

            else if (attribute is TrimmedLabelAttribute trimmedLabelAttr)
            { trimmedLabelAttr.ProcessLabel(fieldInfo, modifiedProperty.Label); }

            else if (attribute is FoldoutAttribute foldoutAttr)
            {
                foldoutDatas.Add(new(foldoutAttr.Name));
                foldoutDatas[^1].IsShown = false;
            }

            else if (attribute is EndFoldoutAttribute endFoldoutAttr)
            {
                if (!endFoldoutAttr.AfterNext)
                { foldoutDatas.Add(new(null)); }
            }

            else if (attribute is RenameableListAttribute renameableListAttr &&
                underlyingProperty.isArray &&
                underlyingProperty.propertyType != SerializedPropertyType.String)
            {
                Type baseType = fieldInfo.FieldType.GetGenericArguments()[0];
                List<Type> validTypes = new();

                // Cache valid types
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type type in assembly.GetTypes())
                    { if (!type.IsAbstract && baseType.IsAssignableFrom(type)) { validTypes.Add(type); } }
                }

                ReorderableList list = new(serializedObject, underlyingProperty, true, true, true, true);
                modifiedProperty.ReorderableList = list;

                list.drawHeaderCallback = rect =>
                { EditorGUI.LabelField(rect, modifiedProperty.Label); };

                list.elementHeightCallback = index =>
                {
                    var element = underlyingProperty.GetArrayElementAtIndex(index);
                    float height = EditorGUI.GetPropertyHeight(element, true);
                    height += element.isExpanded ? 10 : 0;
                    return height;
                };

                // Change add button "+" to open a context menu with all available derived classes
                list.onAddDropdownCallback = (rect, list) =>
                {
                    GenericMenu menu = new();

                    foreach (Type validType in validTypes)
                    {
                        menu.AddItem(new GUIContent(validType.Name), false, () =>
                        {
                            serializedObject.Update();

                            int newIndex = underlyingProperty.arraySize++;
                            var newElement = underlyingProperty.GetArrayElementAtIndex(newIndex);
                            newElement.managedReferenceValue = Activator.CreateInstance(validType);
                            newElement.FindPropertyRelative("name").stringValue = validType.Name;

                            serializedObject.ApplyModifiedProperties();
                        });
                    }

                    menu.ShowAsContext();
                };

                // Draw element normally, but with a text field in place of the regular title
                list.drawElementCallback = (rect, index, active, focused) =>
                {
                    var element = modifiedProperty.Property.GetArrayElementAtIndex(index);

                    // Draw a TextField for a modifiable element name
                    var nameProperty = element.FindPropertyRelative("name");
                    Rect textRect = new(rect.x + 15, rect.y + 1, rect.width - 15, EditorGUIUtility.singleLineHeight);
                    nameProperty.stringValue = EditorGUI.TextField(textRect, nameProperty.stringValue);

                    // Use event when clicking on text field
                    // This way the click won't be directed to the element foldout
                    if (Event.current.type == EventType.MouseDown && textRect.Contains(Event.current.mousePosition))
                    { Event.current.Use(); }

                    // Draw the element foldout without a label
                    float propertyHeight = EditorGUI.GetPropertyHeight(element, true);
                    Rect propertyRect = new(rect.x, rect.y, rect.width, propertyHeight);
                    EditorGUI.PropertyField(propertyRect, element, GUIContent.none, true);
                };
            }
        }

        #endregion Field Processing


        #region Drawing =============================================================================== Drawing

        /// <summary> Stops the inspector drawing process. </summary>
        protected void StopDrawing() => drawingStopRequested = true;

        /// <summary> Stops drawing the current foldout. </summary>
        protected void StopDrawingFoldout() => foldoutDrawingStopRequested = true;

        private void DrawFoldouts(List<FoldoutData> foldoutDatas)
        {
            for (int i = 0; i < foldoutDatas.Count; i++)
            {
                FoldoutData foldout = foldoutDatas[i];

                // Not a foldout
                if (foldout.Name == null)
                {
                    foreach (ModifiedProperty modifiedProperty in foldout.Properties)
                    {
                        DrawModifiedProperty(modifiedProperty);
                        if (drawingStopRequested) { return; }
                    }
                    EditorGUILayout.Space();
                    continue;
                }

                if (foldout.Properties.Count == 0 || !foldout.IsShown) { continue; }

                // Draw foldout title
                EditorGUILayout.BeginVertical(foldoutTitleBackgroundStyle);
                foldout.IsOpen = EditorGUILayout.Foldout(foldout.IsOpen, foldout.Name, true, foldoutTitleStyle);
                EditorGUILayout.EndVertical();

                if (foldoutStatesProperty != null)
                { foldoutStatesProperty.GetArrayElementAtIndex(i).boolValue = foldout.IsOpen; }

                if (!foldout.IsOpen) { EditorGUILayout.Space(); continue; }

                // Draw foldout contents
                EditorGUILayout.BeginVertical(foldoutStyle);
                foreach (ModifiedProperty modifiedProperty in foldout.Properties)
                {
                    DrawModifiedProperty(modifiedProperty);
                    EditorGUILayout.Space(0); // Use default field spacing
                    if (foldoutDrawingStopRequested) { break; }
                    if (drawingStopRequested) { return; }
                }
                foldoutDrawingStopRequested = false;
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        private void DrawModifiedProperty(ModifiedProperty modifiedProperty)
        {
            if (!modifiedProperty.IsShown) { return; }

            if (!OnPropertyFieldBegin(modifiedProperty)) { return; }

            using (new EditorGUI.DisabledScope(!modifiedProperty.IsEnabled))
            {
                if (modifiedProperty.ReorderableList != null)
                { modifiedProperty.ReorderableList.DoLayoutList(); }
                else
                { EditorGUILayout.PropertyField(modifiedProperty.Property, modifiedProperty.Label, true); }
            }

            OnPropertyFieldEnd(modifiedProperty);
        }

        #endregion Drawing


        #region Private Methods ======================================================================= Private Methods

        private void UpdateComponentsToHideVisibility()
        {
            if (hideComponentsAttribute == null || showHiddenComponentsProperty == null ||
                modifiedObject.ObjectType == ObjType.ScriptableObject) { return; }

            if (showHiddenComponentsProperty.boolValue)
            {
                AttemptShowHiddenComponents();
            }
            else
            {
                foreach (Type typeToHide in hideComponentsAttribute.TypesToHide)
                {
                    if (!modifiedObject.TargetComponent.TryGetComponent(typeToHide, out Component componentToHide)) { continue; }

                    hiddenComponents.Add(componentToHide);
                    componentToHide.hideFlags |= HideFlags.HideInInspector;
                }
            }
        }

        private void AttemptShowHiddenComponents()
        {
            foreach (Component component in hiddenComponents)
            { if (component != null) { component.hideFlags &= ~HideFlags.HideInInspector; } }
        }

        #endregion Private Methods
    }
}

#endif
