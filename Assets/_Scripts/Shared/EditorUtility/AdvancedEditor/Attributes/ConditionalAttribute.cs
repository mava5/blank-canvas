
#if UNITY_EDITOR
using System.Reflection;
using Utility;
#endif

using System;

using UnityEngine;

using UtilityEnum;


namespace UtilityEditor
{
    public interface IConditionalShowAttribute
    { public bool IsShown(ModifiedObject modifiedObject); }

    public interface IConditionalEnableAttribute
    { public bool IsEnabled(ModifiedObject modifiedObject); }


    #region Variable ================================================================================================== Variable

    /// <remarks> Requires <see cref="AdvancedEditor"/> </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public abstract class ConditionalVariableCheckingAttribute : PropertyAttribute
    {
        protected readonly string variableName = "";
        protected readonly object targetValue = null;

        protected ConditionalVariableCheckingAttribute(string variableName, object targetValue)
        {
            this.variableName = variableName;
            this.targetValue = targetValue;
        }

#if UNITY_EDITOR
        protected bool Predicate(ModifiedObject modifiedObject)
        {
            object target = modifiedObject.Object.targetObject;
            Type type = target.GetType();

            MemberInfo memberInfo = null;

            // Target variable is a property
            if ((memberInfo = type.GetProperty(variableName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)) != null)
            { return Equals(targetValue, ((PropertyInfo)memberInfo).GetValue(target)); }

            // Target variable is a field
            else if ((memberInfo = type.GetField(variableName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)) != null)
            { return Equals(targetValue, ((FieldInfo)memberInfo).GetValue(target)); }

            else { Util.LogWarning<ConditionalVariableCheckingAttribute>($"Couldn't find \"{variableName}\"!"); return true; }
        }
#else
        protected bool Predicate(ModifiedObject modifiedObject) => true;
#endif
    }

    /// <summary> Only shows serialized field when given field's value is equal to given object. </summary> <inheritdoc/>
    public class ShowIfAttribute : ConditionalVariableCheckingAttribute, IConditionalShowAttribute
    {
        /// <summary> Shows field if given boolean property is true. </summary>
        public ShowIfAttribute(string variableName) : base(variableName, true) { }

        /// <summary> Shows field if given property matches given value. </summary>
        public ShowIfAttribute(string variableName, object targetValue) : base(variableName, targetValue) { }

        public bool IsShown(ModifiedObject modifiedObject) => Predicate(modifiedObject);
    }

    /// <summary> Only shows serialized field when given field's value is not equal to given object. </summary> <inheritdoc/>
    public class ShowIfNotAttribute : ConditionalVariableCheckingAttribute, IConditionalShowAttribute
    {
        /// <summary> Shows field if given boolean property is false. </summary>
        public ShowIfNotAttribute(string variableName) : base(variableName, true) { }

        /// <summary> Shows field if given property doesn't match given value. </summary>
        public ShowIfNotAttribute(string variableName, object targetValue) : base(variableName, targetValue) { }

        public bool IsShown(ModifiedObject modifiedObject) => !Predicate(modifiedObject);
    }

    /// <summary> Only enables serialized field when given field's value is equal to given object. </summary> <inheritdoc/>
    public class EnableIfAttribute : ConditionalVariableCheckingAttribute, IConditionalEnableAttribute
    {
        /// <summary> Enables field if given boolean property is true. </summary>
        public EnableIfAttribute(string variableName) : base(variableName, true) { }

        /// <summary> Enables field if given property matches given value. </summary>
        public EnableIfAttribute(string variableName, object targetValue) : base(variableName, targetValue) { }

        public bool IsEnabled(ModifiedObject modifiedObject) => Predicate(modifiedObject);
    }

    /// <summary> Only enables serialized field when given field's value is not equal to given object. </summary> <inheritdoc/>
    public class EnableIfNotAttribute : ConditionalVariableCheckingAttribute, IConditionalEnableAttribute
    {
        /// <summary> Enables field if given boolean property is false. </summary>
        public EnableIfNotAttribute(string variableName) : base(variableName, true) { }

        /// <summary> Enables field if given property doesn't match given value. </summary>
        public EnableIfNotAttribute(string variableName, object targetValue) : base(variableName, targetValue) { }

        public bool IsEnabled(ModifiedObject modifiedObject) => !Predicate(modifiedObject);
    }

    #endregion Variable


    #region Object Type =============================================================================================== Object Type

    /// <remarks> Requires <see cref="AdvancedEditor"/> </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public abstract class ConditionalObjectTypeCheckingAttribute : PropertyAttribute
    {
        protected readonly ObjType typeMask = 0;

        protected ConditionalObjectTypeCheckingAttribute(ObjType typeMask)
        { this.typeMask = typeMask; }

#if UNITY_EDITOR
        protected bool Predicate(ModifiedObject modifiedObject)
        { return (modifiedObject.ObjectType & typeMask) != 0; }
#else
        protected bool Predicate(ModifiedObject modifiedObject) => true;
#endif
    }

    /// <summary> Creates a white list of object types for which the field will be shown. </summary> <inheritdoc/>
    public class ShowFor : ConditionalObjectTypeCheckingAttribute, IConditionalShowAttribute
    {
        public ShowFor(ObjType typeMask) : base(typeMask) { }
        public bool IsShown(ModifiedObject modifiedObject) => Predicate(modifiedObject);
    }

    /// <summary> Creates a black list of object types for which the field will be hidden. </summary> <inheritdoc/>
    public class HideFor : ConditionalObjectTypeCheckingAttribute, IConditionalShowAttribute
    {
        public HideFor(ObjType typeMask) : base(typeMask) { }
        public bool IsShown(ModifiedObject modifiedObject) => !Predicate(modifiedObject);
    }

    /// <summary> Creates a white list of object types for which the field will be enabled. </summary> <inheritdoc/>
    public class EnableFor : ConditionalObjectTypeCheckingAttribute, IConditionalEnableAttribute
    {
        public EnableFor(ObjType typeMask) : base(typeMask) { }
        public bool IsEnabled(ModifiedObject modifiedObject) => Predicate(modifiedObject);
    }

    /// <summary> Creates a black list of object types for which the field will be disabled. </summary> <inheritdoc/>
    public class DisableFor : ConditionalObjectTypeCheckingAttribute, IConditionalEnableAttribute
    {
        public DisableFor(ObjType typeMask) : base(typeMask) { }
        public bool IsEnabled(ModifiedObject modifiedObject) => !Predicate(modifiedObject);
    }

    #endregion Object Type
}
