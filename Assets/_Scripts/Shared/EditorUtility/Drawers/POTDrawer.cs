
#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using UnityEngine;

using Utility;
using UtilityEnum;
using UtilityExtensions;


namespace UtilityEditor
{
    public class POTAttribute : PropertyAttribute
    {
        public readonly int Min = 1;
        public readonly int Max = int.MaxValue.PreviousPOT();

        public POTAttribute() { }

        public POTAttribute(int min, int max)
        {
            Min = Math.Max(min, 1);
            Max = Math.Max(max, Min).PreviousPOT();
        }

        public POTAttribute(POT min, POT max)
        {
            Min = Math.Max(Util.POTValue(min), 1);
            Max = Math.Max(Util.POTValue(max), Min).PreviousPOT();
        }
    }


#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(POTAttribute))]
    public class POTDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.LabelField(position, label.text, "POTAttribute works only with integers!");
                return;
            }

            if (!TryGetMinMax(property, attribute, out Vector2Int range))
            {
                EditorGUI.LabelField(position, label.text, "POT range couldn't be fetched!");
                return;
            }

            int min = range.x;
            int max = range.y;
            int minExp = Mathf.RoundToInt(Mathf.Log(min, 2));
            int maxExp = Mathf.RoundToInt(Mathf.Log(max, 2));

            int currentValue = Mathf.Clamp(Mathf.ClosestPowerOfTwo(property.intValue), min, max);
            int currentExp = Mathf.RoundToInt(Mathf.Log(currentValue, 2));

            label = EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, label);

            float fieldWidth = EditorGUIUtility.fieldWidth;
            Rect sliderRect = new Rect(position.x, position.y, position.width - fieldWidth - 5, position.height);
            Rect fieldRect = new Rect(position.x + position.width - fieldWidth, position.y, fieldWidth, position.height);

            EditorGUI.BeginChangeCheck();

            int newExp = EditorGUI.IntSlider(sliderRect, currentExp, minExp, maxExp);
            int newPOT = (int)Mathf.Pow(2, newExp);

            int newInputPOT = EditorGUI.IntField(fieldRect, newPOT);

            if (EditorGUI.EndChangeCheck())
            {
                if (newInputPOT != newPOT)
                {
                    int snapped = Mathf.ClosestPowerOfTwo(newInputPOT);
                    property.intValue = Mathf.Clamp(snapped, min, max);
                }
                else
                {
                    property.intValue = newPOT;
                }
            }

            EditorGUI.EndProperty();
        }

        protected virtual bool TryGetMinMax(SerializedProperty property, PropertyAttribute attribute, out Vector2Int range)
        {
            range = default;

            if (attribute is POTAttribute potAttribute)
            {
                range = new(potAttribute.Min, potAttribute.Max);
                return true;
            }

            return false;
        }
    }

#endif
}
