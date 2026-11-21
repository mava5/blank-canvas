
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;
using Utility;


public abstract class BaseSettingKey
{
    public bool IsLocked => isSecondaryLocked || isManuallyLocked;

    public readonly string Name = "";
    public readonly string ReadableName = "";
    public string Category => category;
    public string SubCategory => subCategory;

    public bool RequiresRestart { get; private set; } = false;
    public bool CausesHeavyOperation { get; private set; } = false;


    private string category = "";
    private string subCategory = "";

    private Func<bool> isSecondaryLockedFunc = null;
    private bool isSecondaryLocked = false;

    private List<BaseSettingKey> dependentSettingKeys = new();

    private bool isManuallyLocked = false;


    public IEvent<bool> OnLockedStateChanged => onLockedStateChanged;
    private readonly EventCaller<bool> onLockedStateChanged = new();

    public IEvent OnRefreshed => onRefreshed;
    private readonly EventCaller onRefreshed = new();


    #region Lifecycle ============================================================================= Lifecycle

    protected BaseSettingKey(string name)
    {
        Name = name;

        int parenthesesCount = 0;

        foreach (char c in Name)
        {
            switch (c)
            {
                case '(': parenthesesCount++; break;
                case ')': parenthesesCount--; break;
                case '_': ReadableName += " "; break;
                default: if (parenthesesCount <= 0) { ReadableName += c; } break;
            }
        }

        InitializeCategories();
    }

    internal abstract class Builder
    {
        /// <summary> Tells if modifying this setting requires the game to be restarted. </summary>
        internal bool RequiresRestart = false;

        /// <summary> Tells if modifying this setting takes time. </summary>
        internal bool CausesHeavyOperation = false;

        /// <summary>
        /// <br/>   Returned bool tells if setting is locked.
        /// <br/>   Setting is locked if this returns true or it's manually locked with <see cref="Lock"/>.
        /// </summary>
        internal Func<bool> IsSecondaryLockedFunc = null;

        /// <summary> List of other settings, which when changed, cause this setting to refresh. </summary>
        internal List<BaseSettingKey> SettingKeyDependencies = new();

        protected readonly BaseSettingKey baseSettingKey = null;

        protected Builder(string name, BaseSettingKey passedKey) => baseSettingKey = passedKey;

        public static implicit operator BaseSettingKey(Builder builder) =>
            builder.FinishAs<BaseSettingKey>();

        protected virtual void OnFinish()
        {
            baseSettingKey.RequiresRestart = RequiresRestart;
            baseSettingKey.CausesHeavyOperation = CausesHeavyOperation;
            baseSettingKey.isSecondaryLockedFunc = IsSecondaryLockedFunc;

            foreach (BaseSettingKey settingKeyDependency in SettingKeyDependencies)
            { settingKeyDependency.dependentSettingKeys.Add(baseSettingKey); }
        }

        protected T FinishAs<T>() where T : BaseSettingKey
        {
            OnFinish();
            baseSettingKey.Refresh(refreshDependentSettings: false);
            return (T)baseSettingKey;
        }
    }

    #endregion Lifecycle


    #region Public Methods ======================================================================== Public Methods

    public abstract void Reset();

    public void Lock()
    {
        if (isManuallyLocked) { return; }

        isManuallyLocked = true;
        onLockedStateChanged.Invoke(isManuallyLocked);

        Refresh(refreshDependentSettings: true);
    }

    public void Unlock()
    {
        if (!isManuallyLocked) { return; }

        isManuallyLocked = false;
        onLockedStateChanged.Invoke(isManuallyLocked);

        Refresh(refreshDependentSettings: true);
    }

    public void Refresh(bool refreshDependentSettings = true)
    {
        if (refreshDependentSettings)
        {
            int refreshCount = 0;
            TryRecrusiveRefresh(ref refreshCount);
        }
        else
        {
            OnRefresh();
        }
    }

    public void RefreshDependentSettings()
    {
        int refreshCount = 0;

        foreach (BaseSettingKey settingKey in dependentSettingKeys)
        { settingKey.TryRecrusiveRefresh(ref refreshCount); }
    }

    #endregion Public Methods


    #region Protected Methods ===================================================================== Protected Methods

    protected bool TryRecrusiveRefresh(ref int refreshCount)
    {
        if (refreshCount > 50)
        {
            Util.LogWarning<BaseSettingKey>("Exceeded recursive refresh count!");
            return false;
        }

        OnRefresh();
        onRefreshed.Invoke();

        foreach (BaseSettingKey settingKey in dependentSettingKeys)
        { if (!settingKey.TryRecrusiveRefresh(ref refreshCount)) { return false; } }

        return true;
    }

    protected virtual void OnRefresh()
    {
        bool wasLocked = IsLocked;
        isSecondaryLocked = isSecondaryLockedFunc?.Invoke() ?? false;

        if (wasLocked != IsLocked)
        { onLockedStateChanged.Invoke(IsLocked); }
    }

    #endregion Protected Methods


    #region Private Methods ======================================================================= Private Methods

    private void InitializeCategories() => TryRecursiveFindCategoriesInType(typeof(GameSettings));

    private bool TryRecursiveFindCategoriesInType(Type type)
    {
        // Go through current type's fields and look for this setting key
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (ReferenceEquals(field.GetValue(null), this))
            {
                category = type.DeclaringType?.Name;
                subCategory = type.Name;
                return true;
            }
        }

        // Go through nested classes recursively until categories were found
        foreach (Type nestedType in type.GetNestedTypes(BindingFlags.Public))
        { if (TryRecursiveFindCategoriesInType(nestedType)) { return true; } }

        return false;
    }

    #endregion Private Methods
}


/// <summary>
/// <br/>   Base type for a key matched to a specific setting.
/// <br/>   Contains <see cref="onValueChanged"/> for keeping up with value changes.
/// <br/>   Should be used for types: <see cref="bool"/>
/// </summary>
public class SettingKey<T> : BaseSettingKey
{
    public T DefaultValue { get; private set; } = default;

    private Func<T> defaultValueFunc = null;
    private Action<T> valueChangedFunc = null;

    public IEvent<T> OnValueChanged => onValueChanged;
    private readonly EventCaller<T> onValueChanged = new();


    #region Lifecycle ============================================================================= Lifecycle

    protected SettingKey(string name) : base(name) { }

    internal new class Builder : BaseSettingKey.Builder
    {
        /// <summary> Returns the default value for the setting. </summary>
        internal Func<T> DefaultValueFunc = null;
        
        /// <summary> Called when the setting's value changes. </summary> 
        internal Action<T> ValueChangedFunc = null;

        internal Builder(string name, SettingKey<T> passedKey = null) :
            base(name, passedKey ?? new(name)) { }

        public static implicit operator SettingKey<T>(Builder builder) =>
            builder.FinishAs<SettingKey<T>>();

        protected override void OnFinish()
        {
            base.OnFinish();

            var settingKey = (SettingKey<T>)baseSettingKey;
            settingKey.defaultValueFunc = DefaultValueFunc;
            settingKey.valueChangedFunc = ValueChangedFunc;

            settingKey.onValueChanged.Always += settingKey.valueChangedFunc;
        }
    }

    #endregion Lifecycle


    #region Public Methods ======================================================================== Public Methods

    public virtual bool IsValueValid(T value) => true;

    public virtual T Get()
    {
        object result = DefaultValue switch
        {
            int d => PlayerPrefs.GetInt(Name, d),
            bool d => PlayerPrefs.GetInt(Name, d ? 1 : 0) != 0,
            float d => PlayerPrefs.GetFloat(Name, d),
            string d => PlayerPrefs.GetString(Name, d),
            Vector2 d => GetPlayerPrefsVector2(Name, d),
            Vector2Int d => GetPlayerPrefsVector2Int(Name, d),
            _ => throw new ArgumentException($"[SettingKey]: Unsupported type: {typeof(T)}!")
        };

        return IsValueValid((T)result) ? (T)result : DefaultValue;
    }

    public virtual void Set(T value, bool notify = true)
    {
        if (!IsValueValid(value) || IsLocked || value.Equals(Get())) { return; }

        bool valueChanged = false;

        switch (value)
        {
            case int v: PlayerPrefs.SetInt(Name, v); valueChanged = true; break;
            case bool v: PlayerPrefs.SetInt(Name, v ? 1 : 0); valueChanged = true; break;
            case float v: PlayerPrefs.SetFloat(Name, v); valueChanged = true; break;
            case string v: PlayerPrefs.SetString(Name, v); valueChanged = true; break;
            case Vector2 v: SetPlayerPrefsVector2(Name, v); valueChanged = true; break;
            case Vector2Int v: SetPlayerPrefsVector2Int(Name, v); valueChanged = true; break;
            default: throw new ArgumentException($"[SettingKey]: Unsupported type: {typeof(T)}!");
        }

        if (valueChanged)
        {
            if (notify)
            { onValueChanged.Invoke(value); }

            Refresh(refreshDependentSettings: true);
        }
    }

    public override sealed void Reset() => Set(DefaultValue);

    public void SubscribeUntilDestroyed(Action<T> callbackMethod, ref Action onObjectDestroy, bool initialize = false)
    {
        onValueChanged.Always += callbackMethod;

        onObjectDestroy += () =>
        {
            if (onValueChanged != null)
            { onValueChanged.Always -= callbackMethod; }
        };

        if (initialize)
        { callbackMethod.Invoke(Get()); }
    }

    #endregion Public Methods


    #region Protected Methods ===================================================================== Protected Methods

    protected override void OnRefresh()
    {
        base.OnRefresh();

        DefaultValue = defaultValueFunc != null ? defaultValueFunc.Invoke() : default;
    }

    #endregion Protected Methods


    #region Private Methods ======================================================================= Private Methods

    private Vector2 GetPlayerPrefsVector2(string name, Vector2 defaultValue)
    {
        return new(PlayerPrefs.GetFloat(name + "_X", defaultValue.x),
            PlayerPrefs.GetFloat(name + "_Y", defaultValue.y));
    }

    private Vector2Int GetPlayerPrefsVector2Int(string name, Vector2Int defaultValue)
    {
        return new(PlayerPrefs.GetInt(name + "_X", defaultValue.x),
            PlayerPrefs.GetInt(name + "_Y", defaultValue.y));
    }

    private void SetPlayerPrefsVector2(string name, Vector2 value)
    {
        PlayerPrefs.SetFloat(name + "_X", value.x);
        PlayerPrefs.SetFloat(name + "_Y", value.y);
    }

    private void SetPlayerPrefsVector2Int(string name, Vector2Int value)
    {
        PlayerPrefs.SetInt(name + "_X", value.x);
        PlayerPrefs.SetInt(name + "_Y", value.y);
    }

    #endregion Private Methods
}


/// <summary>
/// <br/>   Only allows setting value to be set in range [min, max].
/// <br/>   Should be used for types: <see cref="int"/>, <see cref="float"/>
/// </summary>
public class RangeSettingKey<T> : SettingKey<T> where T : IComparable<T>
{
    public T Min { get; private set; } = default;
    public T Max { get; private set; } = default;

    private Func<T> minValueFunc = null;
    private Func<T> maxValueFunc = null;


    #region Lifecycle ============================================================================= Lifecycle

    protected RangeSettingKey(string name) : base(name) { }

    internal new class Builder : SettingKey<T>.Builder
    {
        /// <summary> Returns the minimum value allowed for this setting. </summary>
        internal Func<T> MinValueFunc = null;

        /// <summary> Returns the maximum value allowed for this setting. </summary>
        internal Func<T> MaxValueFunc = null;

        internal Builder(string name, RangeSettingKey<T> passedKey = null) :
            base(name, passedKey ?? new(name)) { }

        public static implicit operator RangeSettingKey<T>(Builder builder) =>
            builder.FinishAs<RangeSettingKey<T>>();

        protected override void OnFinish()
        {
            base.OnFinish();

            var settingKey = (RangeSettingKey<T>)baseSettingKey;
            settingKey.minValueFunc = MinValueFunc;
            settingKey.maxValueFunc = MaxValueFunc;
        }
    }

    #endregion Lifecycle


    #region Public Methods ======================================================================== Public Methods

    public override bool IsValueValid(T value) => value.CompareTo(Min) >= 0 && value.CompareTo(Max) <= 0;

    #endregion Public Methods


    #region Protected Methods ===================================================================== Protected Methods

    protected override void OnRefresh()
    {
        base.OnRefresh();

        Min = minValueFunc != null ? minValueFunc.Invoke() : default;
        Max = maxValueFunc != null ? maxValueFunc.Invoke() : default;
    }

    #endregion Protected Methods
}


/// <summary>
/// <br/>   Only allows setting value to be set to pre-determined values.
/// <br/>   Should be used for types: <see cref="int"/>, <see cref="float"/>, <see cref="string"/>
/// </summary>
public class LimitedSettingKey<T> : SettingKey<T>
{
    public List<T> AllowedValues { get; private set; } = new();

    private Func<List<T>> allowedValuesFunc = null;
    private Func<T, T, float> valueClosenessFunc = null;

    public IEvent<List<T>> OnAllowedValuesChanged => onAllowedValuesChanged;
    private readonly EventCaller<List<T>> onAllowedValuesChanged = new();


    #region Lifecycle ============================================================================= Lifecycle

    protected LimitedSettingKey(string name) : base(name) { }

    internal new class Builder : SettingKey<T>.Builder
    {
        /// <summary> Returns list of values the setting is allowed to use. </summary>
        internal Func<List<T>> AllowedValuesFunc = null;

        /// <summary> Used to find the closest allowed value to current one. </summary>
        internal Func<T, T, float> ValueClosenessFunc = null;

        internal Builder(string name, LimitedSettingKey<T> passedKey = null) :
            base(name, passedKey ?? new(name)) { }

        public static implicit operator LimitedSettingKey<T>(Builder builder) =>
            builder.FinishAs<LimitedSettingKey<T>>();

        protected override void OnFinish()
        {
            base.OnFinish();

            var settingKey = (LimitedSettingKey<T>)baseSettingKey;
            settingKey.allowedValuesFunc = AllowedValuesFunc;
            settingKey.valueClosenessFunc = ValueClosenessFunc;
        }
    }

    #endregion Lifecycle


    #region Public Methods ======================================================================== Public Methods

    public override bool IsValueValid(T value) => AllowedValues.Contains(value);

    public override void Set(T value, bool notify = true) => base.Set(GetClosestValueTo(value), notify);

    public void SetByIndex(int index, bool notify = true)
    {
        if (index < 0 || index > AllowedValues.Count) { return; }

        base.Set(AllowedValues[index], notify);
    }

    public T GetClosestValueTo(T value)
    {
        if (value == null || AllowedValues == null || AllowedValues.Count == 0) { return DefaultValue; }
        
        if (AllowedValues.Count == 1) { return AllowedValues[0]; }

        if (valueClosenessFunc == null)
        { return AllowedValues.Contains(value) ? value : AllowedValues[0]; }

        T currentValue = Get();
        float bestCloseness = float.MaxValue;
        int bestIndex = -1;

        for (int i = 0; i < AllowedValues.Count; i++)
        {
            float closeness = valueClosenessFunc.Invoke(currentValue, AllowedValues[i]);

            if (closeness < bestCloseness)
            {
                bestCloseness = closeness;
                bestIndex = i;
            }
        }

        return bestIndex == -1 ? DefaultValue : AllowedValues[bestIndex];
    }

    #endregion Public Methods


    #region Protected Methods ===================================================================== Protected Methods

    protected override void OnRefresh()
    {
        base.OnRefresh();

        List<T> oldAllowedValues = new(AllowedValues);
        AllowedValues = allowedValuesFunc?.Invoke() ?? new();
        Set(Get());

        if (!AllowedValues.SequenceEqual(oldAllowedValues))
        { onAllowedValuesChanged?.Invoke(AllowedValues); }
    }

    #endregion Protected Methods
}
