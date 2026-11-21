
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;

using BasicB = SettingKey<bool>;

using LimitV2I = LimitedSettingKey<UnityEngine.Vector2Int>;
using LimitS = LimitedSettingKey<string>;

using RangeF = RangeSettingKey<float>;
using RangeI = RangeSettingKey<int>;

using ReadOnlySettingDictionary =
    System.Collections.Generic.IReadOnlyDictionary<string,
        System.Collections.Generic.IReadOnlyDictionary<string,
            System.Collections.Generic.IReadOnlyList<BaseSettingKey>>>;

using SettingDictionary =
    System.Collections.Generic.Dictionary<string,
        System.Collections.Generic.Dictionary<string,
            System.Collections.Generic.List<BaseSettingKey>>>;


/// <summary>
/// <br/>   A wrapper around <see cref="PlayerPrefs"/>.
/// <br/>   Adds auto completion for setting names as well as type safety.
/// <br/>   Contains each <see cref="SettingKey{T}"/> within subcategories.
/// </summary>
public static class GameSettings
{
    public static class Controls
    {
        public static readonly RangeF Sensitivity = new RangeF.Builder("Sensitivity")
        {
            DefaultValueFunc = () => 0.5f,
            MinValueFunc = () => 0f,
            MaxValueFunc = () => 1f,
        };
    }

    public static class Audio
    {
        public static readonly RangeF MusicVolume = new RangeF.Builder("Music_Volume")
        {
            DefaultValueFunc = () => 0.5f,
            MinValueFunc = () => 0f,
            MaxValueFunc = () => 1f,
        };

        public static readonly RangeF SFXVolume = new RangeF.Builder("SFX_Volume")
        {
            DefaultValueFunc = () => 0.5f,
            MinValueFunc = () => 0f,
            MaxValueFunc = () => 1f,
        };

        public static readonly RangeF AmbianceVolume = new RangeF.Builder("Ambiance_Volume")
        {
            DefaultValueFunc = () => 0.5f,
            MinValueFunc = () => 0f,
            MaxValueFunc = () => 1f,
        };
    }

    public static class Display
    {
        private static readonly FullScreenMode defaultDisplayMode = FullScreenMode.FullScreenWindow;

        private static readonly Dictionary<FullScreenMode, string> allowedDisplayModes = new()
            {
                { FullScreenMode.FullScreenWindow, "Borderless windowed" },
                { FullScreenMode.Windowed, "Windowed" },
            };

        public static readonly LimitS DisplayMode = new LimitS.Builder("Display_Mode")
        {
            DefaultValueFunc = () => allowedDisplayModes[defaultDisplayMode],
            AllowedValuesFunc = () => allowedDisplayModes.Values.ToList(),
            ValueChangedFunc = _ => UpdateScreen(),
        };

        public static readonly LimitV2I Resolution = new LimitV2I.Builder("Resolution")
        {
            DefaultValueFunc = () => new(Screen.currentResolution.width, Screen.currentResolution.height),
            AllowedValuesFunc = () =>
            {
                float targetAspectRatio = (float)Screen.currentResolution.width / Screen.currentResolution.height;
                float tolerance = 0.1f;

                return Screen.resolutions
                    .Where(res => Mathf.Abs(((float)res.width / res.height) - targetAspectRatio) < tolerance)
                    .GroupBy(res => new { res.width, res.height })
                    .Select(group => new Vector2Int(group.First().width, group.First().height))
                    .OrderBy(res => res.y)
                    .ThenBy(res => res.x)
                    .ToList();
            },
            ValueClosenessFunc = (own, other) => Mathf.Abs(own.x - other.x) + Mathf.Abs(own.y - other.y),
            ValueChangedFunc = _ => UpdateScreen(),
        };

        public static readonly BasicB VSync = new BasicB.Builder("VSync")
        {
            DefaultValueFunc = () => false,
            ValueChangedFunc = isVSyncOn => QualitySettings.vSyncCount = isVSyncOn ? 1 : 0,
        };

        public static readonly RangeI FrameRate = new RangeI.Builder("Frame_Rate")
        {
            DefaultValueFunc = () => 60,
            MinValueFunc = () => 30,
            MaxValueFunc = () => 240,
            SettingKeyDependencies = new() { VSync },
            ValueChangedFunc = frameRate => Application.targetFrameRate = frameRate,
        };

        private static void UpdateScreen()
        {
            Vector2Int resolution = Resolution.Get();
            string displayModeName = DisplayMode.Get();
            FullScreenMode displayMode = defaultDisplayMode;

            foreach ((FullScreenMode mode, string modeName) in allowedDisplayModes)
            {
                if (modeName == displayModeName)
                {
                    displayMode = mode;
                    break;
                }
            }

            Screen.SetResolution(resolution.x, resolution.y, displayMode);
        }
    }


    public readonly static ReadOnlySettingDictionary SortedSettings = CreateReadOnlyDictionary();


    #region Private Methods ======================================================================= Private Methods

    private static ReadOnlySettingDictionary CreateReadOnlyDictionary()
    {
        SettingDictionary tempDictionary = new();
        RecursiveIterateType(typeof(GameSettings), tempDictionary);

        var tabs = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<BaseSettingKey>>>();
        foreach (var tab in tempDictionary)
        {
            var sections = new Dictionary<string, IReadOnlyList<BaseSettingKey>>();
            foreach (var section in tab.Value)
            {
                sections[section.Key] = section.Value.AsReadOnly();
            }
            tabs[tab.Key] = sections;
        }
        return tabs;
    }

    private static void RecursiveIterateType(Type type, SettingDictionary sortedSettings)
    {
        // Go through current type's fields
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is BaseSettingKey setting)
            {
                sortedSettings.TryAdd(setting.Category, new());
                sortedSettings[setting.Category].TryAdd(setting.SubCategory, new());
                sortedSettings[setting.Category][setting.SubCategory].Add(setting);
            }
        }

        // Go through nested classes recursively
        foreach (Type nestedType in type.GetNestedTypes(BindingFlags.Public))
        { RecursiveIterateType(nestedType, sortedSettings); }
    }

    #endregion Private Methods
}