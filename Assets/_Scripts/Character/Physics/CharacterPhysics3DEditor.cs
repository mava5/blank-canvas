
//#if UNITY_EDITOR

//using System;
//using System.Reflection;

//using UnityEditor;
//using UnityEditorInternal;

//using UnityEngine;


//[CustomEditor(typeof(CharacterPhysics3D), true)]
//public class CharacterPhysics2DEditor : Editor
//{
//    private readonly Type baseType = typeof(PhysicsDriver3D);

//    private SerializedProperty drivers = null;
//    private ReorderableList list = null;


//    private void OnEnable()
//    {
//        drivers = serializedObject.FindProperty("drivers");

//        list = new(serializedObject, drivers)
//        {
//            draggable = true,
//            displayAdd = true,
//            displayRemove = true,

//            drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Drivers"); },
//            elementHeightCallback = index =>
//            {
//                var element = drivers.GetArrayElementAtIndex(index);
//                float height = EditorGUI.GetPropertyHeight(element, true);
//                height += element.isExpanded ? 10 : 0;

//                return height;
//            },

//            // Change add button "+" to open a context menu with all available physics drivers
//            onAddDropdownCallback = (rect, list) =>
//            {
//                GenericMenu menu = new GenericMenu();

//                foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
//                {
//                    if (type.IsAbstract || !baseType.IsAssignableFrom(type)) { continue; }

//                    menu.AddItem(new GUIContent(type.Name), false, () =>
//                    {
//                        serializedObject.Update();

//                        int newIndex = drivers.arraySize++;
//                        SerializedProperty newElement = drivers.GetArrayElementAtIndex(newIndex);
//                        newElement.managedReferenceValue = Activator.CreateInstance(type);
//                        newElement.FindPropertyRelative("name").stringValue = type.Name;

//                        serializedObject.ApplyModifiedProperties();
//                    });
//                }

//                menu.ShowAsContext();
//            },

//            // Draw element normally, but with a text field in place of the regular title
//            drawElementCallback = (rect, index, active, focused) =>
//            {
//                var element = drivers.GetArrayElementAtIndex(index);

//                // Draw a TextField for a modifiable element name
//                var nameProperty = element.FindPropertyRelative("name");
//                Rect textRect = new(rect.x + 15, rect.y + 1, rect.width - 15, EditorGUIUtility.singleLineHeight);
//                nameProperty.stringValue = EditorGUI.TextField(textRect, nameProperty.stringValue);

//                // Use event when clicking on text field
//                // This way the click won't be directed to the element foldout
//                if (Event.current.type == EventType.MouseDown && textRect.Contains(Event.current.mousePosition))
//                { Event.current.Use(); }

//                // Draw the element foldout without a label
//                float propertyHeight = EditorGUI.GetPropertyHeight(element, true);
//                Rect propertyRect = new(rect.x, rect.y, rect.width, propertyHeight);
//                EditorGUI.PropertyField(propertyRect, element, GUIContent.none, true);
//            },
//        };
//    }

//    public override void OnInspectorGUI()
//    {
//        serializedObject.Update();

//        // Show all serialized fields other than "m_Script" and "drivers"
//        SerializedProperty iterator = serializedObject.GetIterator();
//        bool enterChildren = true;
//        while (iterator.NextVisible(enterChildren))
//        {
//            enterChildren = false;
//            if (iterator.name != "m_Script" && iterator.name != "drivers")
//            { EditorGUILayout.PropertyField(iterator, true); }
//        }

//        EditorGUILayout.Space();

//        list.DoLayoutList();

//        serializedObject.ApplyModifiedProperties();
//    }
//}

//#endif
