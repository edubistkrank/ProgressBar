using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;
using System;
using UnityEngine;

namespace ProgressBar;

[BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
public sealed class Plugin : BaseUnityPlugin
{
    private Harmony harmony;
    private readonly BepInEx.Configuration.KeyboardShortcut cycleStylePercentShortcut = new BepInEx.Configuration.KeyboardShortcut(KeyCode.Keypad0);
    private readonly BepInEx.Configuration.KeyboardShortcut cycleStyleItemCountShortcut = new BepInEx.Configuration.KeyboardShortcut(KeyCode.KeypadPeriod);
    private readonly BepInEx.Configuration.KeyboardShortcut cycleStylePowerShortcut = new BepInEx.Configuration.KeyboardShortcut(KeyCode.Keypad1);
    private readonly BepInEx.Configuration.KeyboardShortcut cycleStyleBarShortcut = new BepInEx.Configuration.KeyboardShortcut(KeyCode.Keypad2);
    private readonly BepInEx.Configuration.KeyboardShortcut cycleStyleClosersShortcut = new BepInEx.Configuration.KeyboardShortcut(KeyCode.Keypad3);
    private readonly BepInEx.Configuration.KeyboardShortcut cycleColorPowerOnShortcut = new BepInEx.Configuration.KeyboardShortcut(KeyCode.Keypad4);
    private readonly BepInEx.Configuration.KeyboardShortcut cycleColorBarShortcut = new BepInEx.Configuration.KeyboardShortcut(KeyCode.Keypad5);
    private readonly BepInEx.Configuration.KeyboardShortcut cycleColorClosersShortcut = new BepInEx.Configuration.KeyboardShortcut(KeyCode.Keypad6);
    private readonly BepInEx.Configuration.KeyboardShortcut cycleColorPowerOffShortcut = new BepInEx.Configuration.KeyboardShortcut(KeyCode.Keypad7);
    private readonly BepInEx.Configuration.KeyboardShortcut cycleColorPercentShortcut = new BepInEx.Configuration.KeyboardShortcut(KeyCode.Keypad8);
    private readonly BepInEx.Configuration.KeyboardShortcut cycleColorItemCountShortcut = new BepInEx.Configuration.KeyboardShortcut(KeyCode.Keypad9);
    internal static ManualLogSource Log { get; private set; }
    internal static ModConfig Settings { get; private set; }

    private void Awake()
    {
        Log = Logger;
        Settings = OptionsPanelHandler.RegisterModOptions<ModConfig>();

        ReactorProgressPatches.ApplyRuntimeConfig(Settings);
        harmony = new Harmony(PluginInfo.Guid);
        harmony.PatchAll();
        Log.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loaded.");
    }

    private void OnDestroy()
    {
        harmony?.UnpatchSelf();
    }

    private void Update()
    {
        if (Settings == null || !Settings.EnableNumpadHotkeys)
        {
            return;
        }

        bool changed = false;

        changed |= CycleOption(cycleStylePercentShortcut.IsDown(), () => Settings.PercentDisplayMode, value => Settings.PercentDisplayMode = value, ModConfig.PercentDisplayModeCount, ReactorProgressPatches.SetPercentDisplayMode);
        changed |= CycleOption(cycleStyleItemCountShortcut.IsDown(), () => Settings.BioItemCountMode, value => Settings.BioItemCountMode = value, ModConfig.ItemCountModeCount, ReactorProgressPatches.SetBioItemCountMode);
        changed |= CycleOption(cycleStylePowerShortcut.IsDown(), () => Settings.ReactorStatusStyleIndex, value => Settings.ReactorStatusStyleIndex = value, ReactorProgressPatches.GetPowerStyleCount(), ReactorProgressPatches.SetReactorStatusStyle);
        changed |= CycleOption(cycleStyleBarShortcut.IsDown(), () => Settings.BarStyleIndex, value => Settings.BarStyleIndex = value, ReactorProgressPatches.GetBarStyleCount(), ReactorProgressPatches.SetBarStyle);
        changed |= CycleOption(cycleStyleClosersShortcut.IsDown(), () => Settings.BarClosureMode, value => Settings.BarClosureMode = value, ModConfig.BarClosureModeCount, ReactorProgressPatches.SetBarClosureMode);
        changed |= CycleOption(cycleColorPowerOnShortcut.IsDown(), () => Settings.PowerOnColorModeIndex, value => Settings.PowerOnColorModeIndex = value, ModConfig.StandardColorCount, ReactorProgressPatches.SetPowerOnColor);
        changed |= CycleOption(cycleColorBarShortcut.IsDown(), () => Settings.BarColorModeIndex, value => Settings.BarColorModeIndex = value, ModConfig.BarColorModeCount, ReactorProgressPatches.SetBarColorMode);
        changed |= CycleOption(cycleColorClosersShortcut.IsDown(), () => Settings.BarCloserColorModeIndex, value => Settings.BarCloserColorModeIndex = value, ModConfig.BarColorModeCount, ReactorProgressPatches.SetBarCloserColorMode);
        changed |= CycleOption(cycleColorPowerOffShortcut.IsDown(), () => Settings.PowerOffColorModeIndex, value => Settings.PowerOffColorModeIndex = value, ModConfig.StandardColorCount, ReactorProgressPatches.SetPowerOffColor);
        changed |= CycleOption(cycleColorPercentShortcut.IsDown(), () => Settings.PercentColorStyleIndex, value => Settings.PercentColorStyleIndex = value, ModConfig.PercentColorStyleCount, ReactorProgressPatches.SetPercentColorStyle);
        changed |= CycleOption(cycleColorItemCountShortcut.IsDown(), () => Settings.ItemCountColorModeIndex, value => Settings.ItemCountColorModeIndex = value, ModConfig.StandardColorCount, ReactorProgressPatches.SetItemCountColor);

        if (changed)
        {
            Settings.Save();
        }
    }

    private static bool CycleOption(bool isDown, Func<int> getValue, Action<int> setValue, int count, Action<int> apply)
    {
        if (!isDown)
        {
            return false;
        }

        int nextValue = Next(getValue(), count);
        setValue(nextValue);
        apply(nextValue);
        return true;
    }

    private static int Next(int current, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        if (current < 0 || current >= count)
        {
            return 0;
        }

        return (current + 1) % count;
    }
}
