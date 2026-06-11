using HarmonyLib;
using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace ProgressBar;

internal static class ReactorProgressPatches
{
    private const float NuclearRodCharge = 20000f;
    private const int BioReactorMaxSlots = 16;
    private const int NuclearReactorMaxSlots = 4;
    private const string NuclearRodFilledSymbol = "●";
    private const string NuclearRodEmptySymbol = "○";
    private const string NuclearRodSpentSymbol = "♽";
    private const string NuclearRodHalfSymbol = "◐";
    private const string EmptyColor = "#1B2B38";
    private const string InvisibleColor = "#00000000";
    private static int bioItemCountMode = 1;
    private static int percentDisplayMode = 1;
    private static int barStyleIndex;
    private static int barClosureMode = 0; // 0=None, 1=Default (▕▏), 2=Brackets ([]), 3=Match bar, 4-7=Box A-D
    private static int percentColorStyleIndex;
    private static int reactorStatusStyleIndex;
    private static int barColorModeIndex = 0; // Index into BarColorOptions
    private static int barCloserColorModeIndex = 0; // Index into BarCloserColorOptions
    private static int powerOffColorModeIndex = 4; // Red by default
    private static int powerOnColorModeIndex = 2; // Green by default
    private static int itemCountColorModeIndex = 7; // White by default
    private static int cachedBioItemCountMode = int.MinValue;
    private static int cachedPercentDisplayMode = int.MinValue;
    private static int cachedBarStyleIndex = int.MinValue;
    private static int cachedBarClosureMode = int.MinValue;
    private static int cachedPercentColorStyleIndex = int.MinValue;
    private static int cachedReactorStatusStyleIndex = int.MinValue;
    private static int cachedBarColorModeIndex = int.MinValue;
    private static int cachedBarCloserColorModeIndex = int.MinValue;
    private static int cachedPowerOffColorModeIndex = int.MinValue;
    private static int cachedPowerOnColorModeIndex = int.MinValue;
    private static int cachedItemCountColorModeIndex = int.MinValue;

    // Color palette (chromatic order: Blue, Cyan, Green, Yellow, Orange, Red, Magenta, White, Gray)
    private static readonly string[] BarColorPalette = new[]
    {
        "#2EA8FF", // Blue (default Subnautica)
        "#00E5FF", // Cyan
        "#00FF00", // Green
        "#FFFF00", // Yellow
        "#FFA500", // Orange
        "#FF0000", // Red
        "#FF00FF", // Magenta
        "#FFFFFF", // White
        "#808080"  // Gray
    };

    // Same palette for closers
    private static readonly string[] BarCloserColorPalette = BarColorPalette;

    // One-line editable symbol lists for ACTIVE/INACTIVE styles.
    // Edit unicode symbols here separated by commas. Count is determined dynamically.
    private const string ReactorStatusActiveSymbolsCsv = "ACTIVE,[ACTIVE],ON,●,◉,◎,◌,■,▣,◇,◈,▢,⚠,ⓘ,♥,★,⌘,☯";
    private const string ReactorStatusInactiveSymbolsCsv = "INACTIVE,[INACTIVE],OFF,●,◉,◎,◌,■,▣,◇,◈,▢,⚠,ⓘ,♥,★,⌘,☯";

    private static readonly string[] ReactorStatusActiveSymbols = SplitCsvSymbols(ReactorStatusActiveSymbolsCsv);
    private static readonly string[] ReactorStatusInactiveSymbols = SplitCsvSymbols(ReactorStatusInactiveSymbolsCsv);

    // Dynamically set ReactorStatusStylesCount based on actual symbols provided
    private static readonly int DynamicReactorStatusStylesCount = ReactorStatusActiveSymbols.Length;

    // Bar interior symbols (filled char for each style, comma-separated).
    // Styles 1-3: predefined (Solid, Compact, Pipe)
    // Add more symbols separated by commas as needed. Count is determined dynamically.
    private const string BarFilledSymbolsCsv = "█,▌,▏,|,◆,▱,●,◉,▶,/,),],!,#,*,-,=,:,.,º";
    // Bar empty symbols (empty char for each style, comma-separated).
    private const string BarEmptySymbolsCsv = "█,▌,▏,|,◇,▱,○,◎,▷,/,),],!,#,*,-,=,:,.,º";

    private static readonly string[] BarFilledSymbols = SplitCsvSymbols(BarFilledSymbolsCsv);
    private static readonly string[] BarEmptySymbols = SplitCsvSymbols(BarEmptySymbolsCsv);

    // Dynamically set BarStylesCount based on actual symbols provided
    private static readonly int DynamicBarStylesCount = BarFilledSymbols.Length;

    // Generic closers (not per-style, applied based on closure mode choice)
    private const string DefaultLeftCloser = "▕";
    private const string DefaultRightCloser = "▏";
    private const string BracketLeftCloser = "[";
    private const string BracketRightCloser = "]";

    internal static void ApplyRuntimeConfig(ModConfig config)
    {
        if (config == null)
        {
            return;
        }

        SetBioItemCountMode(config.BioItemCountMode);
        SetPercentDisplayMode(config.PercentDisplayMode);
        SetBarStyle(config.BarStyleIndex);
        SetBarClosureMode(config.BarClosureMode);
        SetPercentColorStyle(config.PercentColorStyleIndex);
        SetReactorStatusStyle(config.ReactorStatusStyleIndex);
        SetBarColorMode(config.BarColorModeIndex);
        SetBarCloserColorMode(config.BarCloserColorModeIndex);
        SetPowerOffColor(config.PowerOffColorModeIndex);
        SetPowerOnColor(config.PowerOnColorModeIndex);
        SetItemCountColor(config.ItemCountColorModeIndex);
    }

    internal static void SetBioItemCountMode(int mode)
    {
        bioItemCountMode = Clamp(mode, 0, 3);
    }

    internal static void SetPercentDisplayMode(int mode)
    {
        percentDisplayMode = Clamp(mode, 0, 2);
    }

    internal static void SetBarStyle(int styleIndex)
    {
        barStyleIndex = Clamp(styleIndex, 0, DynamicBarStylesCount - 1);
    }

    internal static void SetBarClosureMode(int mode)
    {
        barClosureMode = Clamp(mode, 0, 7);
    }

    internal static void SetPercentColorStyle(int styleIndex)
    {
        int maxIndex = 3 + BarColorPalette.Length;
        percentColorStyleIndex = Clamp(styleIndex, 0, maxIndex);
    }

    internal static void SetReactorStatusStyle(int styleIndex)
    {
        reactorStatusStyleIndex = Clamp(styleIndex, 0, DynamicReactorStatusStylesCount - 1);
    }

    internal static void SetBarColorMode(int modeIndex)
    {
        barColorModeIndex = Clamp(modeIndex, 0, BarColorPalette.Length);
    }

    internal static void SetBarCloserColorMode(int modeIndex)
    {
        barCloserColorModeIndex = Clamp(modeIndex, 0, BarCloserColorPalette.Length);
    }

    internal static void SetSyncBarToPercentColor(bool sync)
    {
        // This method is kept for backward compatibility but does nothing now
        // Color selection is handled by barColorModeIndex and barCloserColorModeIndex
    }

    internal static void SetPowerOffColor(int colorIndex)
    {
        powerOffColorModeIndex = Clamp(colorIndex, 0, BarColorPalette.Length - 1);
    }

    internal static void SetPowerOnColor(int colorIndex)
    {
        powerOnColorModeIndex = Clamp(colorIndex, 0, BarColorPalette.Length - 1);
    }

    internal static void SetItemCountColor(int colorIndex)
    {
        itemCountColorModeIndex = Clamp(colorIndex, 0, BarColorPalette.Length - 1);
    }

    internal static int GetPowerStyleCount()
    {
        return DynamicReactorStatusStylesCount;
    }

    internal static int GetBarStyleCount()
    {
        return DynamicBarStylesCount;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    [HarmonyPatch(typeof(BaseBioReactor), "Update")]
    private static class BaseBioReactor_Update_Patch
    {
        private static void Postfix(BaseBioReactor __instance)
        {
            SyncRuntimeConfig();

            if (__instance == null)
            {
                return;
            }

            BaseBioReactorGeometry geometry = GetGeometry(__instance);
            TextMeshProUGUI text = geometry?.text;
            if (text == null)
            {
                return;
            }

            if (!TryGetBioCurrentItem(__instance, out float currentCharge))
            {
                text.text = BuildStatusText(Language.main.Get("BaseBioReactorInactive"), false);
                return;
            }

            float toConsume = (float)AccessTools.Field(typeof(BaseBioReactor), "_toConsume").GetValue(__instance);
            float remaining = Mathf.Clamp(currentCharge - toConsume, 0f, currentCharge);
            float percent = Mathf.Clamp01(remaining / currentCharge);
            int percentValue = Mathf.RoundToInt(percent * 100f);
            int currentItems = GetBioItemCount(__instance);
            string itemCountText = BuildBioItemCountText(currentItems, percentValue);

            text.text = $"{BuildStatusText(Language.main.Get("BaseBioReactorActive"), true)}{itemCountText}\n\n{BuildProgressSection(percent, percentValue)}";
        }
    }

    [HarmonyPatch(typeof(BaseNuclearReactor), "Update")]
    private static class BaseNuclearReactor_Update_Patch
    {
        private static void Postfix(BaseNuclearReactor __instance)
        {
            SyncRuntimeConfig();

            if (__instance == null)
            {
                return;
            }

            BaseNuclearReactorGeometry geometry = GetGeometry(__instance);
            TextMeshProUGUI text = geometry?.text;
            if (text == null)
            {
                return;
            }

            if (!TryHasActiveNuclearRod(__instance))
            {
                text.text = BuildStatusText(Language.main.Get("BaseNuclearReactorInactive"), false);
                return;
            }

            float toConsume = (float)AccessTools.Field(typeof(BaseNuclearReactor), "_toConsume").GetValue(__instance);
            float remaining = Mathf.Clamp(NuclearRodCharge - toConsume, 0f, NuclearRodCharge);
            float percent = Mathf.Clamp01(remaining / NuclearRodCharge);
            int percentValue = Mathf.RoundToInt(percent * 100f);
            int currentItems = GetNuclearRodCount(__instance);
            string itemCountText = BuildNuclearItemCountText(__instance, currentItems, percentValue);

            text.text = $"{BuildStatusText(Language.main.Get("BaseNuclearReactorActive"), true)}{itemCountText}\n\n{BuildProgressSection(percent, percentValue)}";
        }
    }

    private static string BuildProgressBar(float percent, int percentValue)
    {
        int styleIdx = Mathf.Clamp(barStyleIndex, 0, DynamicBarStylesCount - 1);
        int segments = GetVisualSegments(styleIdx);
        int filled = Mathf.Clamp(Mathf.RoundToInt(percent * segments), 0, segments);

        // Get style-specific interior symbols
        string filledSymbol = BarFilledSymbols[styleIdx];
        string emptySymbol = BarEmptySymbols[styleIdx];

        // Get generic closers based on closure mode
        string leftCloser = GetLeftCloser(styleIdx);
        string rightCloser = GetRightCloser(styleIdx);

        // Determine bar fill color (with percent value for "Follow % Color" mode)
        string fillColor = GetBarColor(percentValue);
        string closerColor = GetBarCloserColor(percentValue);

        // Standard bar using filled/empty symbols from CSV
        string coreBar = $"<color={closerColor}>{leftCloser}</color><color={fillColor}>{new string(filledSymbol[0], filled)}</color><color={EmptyColor}>{new string(emptySymbol[0], segments - filled)}</color><color={closerColor}>{rightCloser}</color>";

        return AddHorizontalMargins(coreBar);
    }

    private static string BuildProgressSection(float percent, int percentValue)
    {
        if (barClosureMode < 4)
        {
            return $"<size=75%>{BuildProgressBar(percent, percentValue)}</size>{BuildPercentText(percentValue)}";
        }

        int styleIdx = Mathf.Clamp(barStyleIndex, 0, DynamicBarStylesCount - 1);
        int segments = GetVisualSegments(styleIdx);
        int filled = Mathf.Clamp(Mathf.RoundToInt(percent * segments), 0, segments);
        string filledSymbol = BarFilledSymbols[styleIdx];
        string emptySymbol = BarEmptySymbols[styleIdx];
        string fillColor = GetBarColor(percentValue);
        string borderColor = GetBarCloserColor(percentValue);

        int borderDashCount = GetBorderDashCount(styleIdx, segments);
        string horizontal = new string('─', borderDashCount);
        string horizontalInsideCorners = new string('─', Mathf.Max(borderDashCount, 1));
        string horizontalWithSides = new string('─', borderDashCount + 1);
        string topBorder;
        string middleBar;
        string bottomBorder;

        switch (barClosureMode)
        {
            case 4: // Box A: top/bottom only, no corners, no sides
                topBorder = AddHorizontalMargins($"<color={borderColor}>{horizontal}</color>");
                middleBar = AddHorizontalMargins($"<color={fillColor}>{new string(filledSymbol[0], filled)}</color><color={EmptyColor}>{new string(emptySymbol[0], segments - filled)}</color>");
                bottomBorder = AddHorizontalMargins($"<color={borderColor}>{horizontal}</color>");
                break;
            case 5: // Box B: top/bottom with corners, no sides
                topBorder = AddHorizontalMargins($"<color={borderColor}>┌{horizontalInsideCorners}┐</color>");
                middleBar = AddHorizontalMargins($"<color={fillColor}>{new string(filledSymbol[0], filled)}</color><color={EmptyColor}>{new string(emptySymbol[0], segments - filled)}</color>");
                bottomBorder = AddHorizontalMargins($"<color={borderColor}>└{horizontalInsideCorners}┘</color>");
                break;
            case 6: // Box C: top/bottom only + sides (no corners)
                topBorder = AddHorizontalMargins($"<color={borderColor}>{horizontalWithSides}</color>");
                middleBar = AddHorizontalMargins($"<color={borderColor}>│</color><color={fillColor}>{new string(filledSymbol[0], filled)}</color><color={EmptyColor}>{new string(emptySymbol[0], segments - filled)}</color><color={borderColor}>│</color>");
                bottomBorder = AddHorizontalMargins($"<color={borderColor}>{horizontalWithSides}</color>");
                break;
            case 7: // Box D: top/bottom with corners + sides (actual)
                topBorder = AddHorizontalMargins($"<color={borderColor}>┌{horizontal}┐</color>");
                middleBar = AddHorizontalMargins($"<color={borderColor}>│</color><color={fillColor}>{new string(filledSymbol[0], filled)}</color><color={EmptyColor}>{new string(emptySymbol[0], segments - filled)}</color><color={borderColor}>│</color>");
                bottomBorder = AddHorizontalMargins($"<color={borderColor}>└{horizontal}┘</color>");
                break;
            default:
                return $"<size=75%>{BuildProgressBar(percent, percentValue)}</size>{BuildPercentText(percentValue)}";
        }

        return $"<size=75%><line-height=80%>{topBorder}\n{middleBar}\n{bottomBorder}</line-height></size>{BuildPercentText(percentValue)}";
    }

    private static int GetVisualSegments(int styleIdx)
    {
        const int baseSegments = 14;
        char symbol = BarFilledSymbols[styleIdx][0];
        float widthFactor = GetSymbolWidthFactor(symbol);
        int adjusted = Mathf.RoundToInt(baseSegments / widthFactor);
        return Mathf.Clamp(adjusted, baseSegments, 24);
    }

    private static float GetSymbolWidthFactor(char symbol)
    {
        switch (symbol)
        {
            case '|':
            case '!':
            case ':':
            case '.':
            case 'º':
                return 0.55f;
            case '/':
            case '-':
            case '=':
            case '*':
                return 0.7f;
            case '▏':
            case '▌':
                return 0.8f;
            default:
                return 1f;
        }
    }

    private static int GetBorderDashCount(int styleIdx, int fallbackSegments)
    {
        switch (styleIdx)
        {
            case 3:  // Style 4
            case 9:  // Style 10
            case 12: // Style 13
            case 15: // Style 16
            case 17: // Style 18
            case 18: // Style 19
                return 7;
            case 10: // Style 11
            case 11: // Style 12
                return 5;
            case 13: // Style 14
            case 14: // Style 15
                return 9;
            case 16: // Style 17
                return 11;
            case 19: // Style 20
                return 13;
            default:
                return fallbackSegments;
        }
    }

    private static string GetBarColor(int percentValue = 50)
    {
        // If it's the last index, use "Follow % Color" mode
        if (barColorModeIndex == BarColorPalette.Length)
        {
            return GetPercentColor(percentValue);
        }

        // Otherwise use the selected color from the palette
        if (barColorModeIndex >= 0 && barColorModeIndex < BarColorPalette.Length)
        {
            return BarColorPalette[barColorModeIndex];
        }

        return BarColorPalette[0]; // Default to first color
    }

    private static string GetBarCloserColor(int percentValue = 50)
    {
        // If it's the last index, use "Follow % Color" mode
        if (barCloserColorModeIndex == BarCloserColorPalette.Length)
        {
            return GetPercentColor(percentValue);
        }

        // Otherwise use the selected color from the palette
        if (barCloserColorModeIndex >= 0 && barCloserColorModeIndex < BarCloserColorPalette.Length)
        {
            return BarCloserColorPalette[barCloserColorModeIndex];
        }

        return BarCloserColorPalette[0]; // Default to first color
    }

    private static string GetLeftCloser(int styleIdx)
    {
        return barClosureMode switch
        {
            0 => "", // None
            1 => DefaultLeftCloser, // Default (▕)
            2 => BracketLeftCloser, // Brackets ([)
            3 => BarFilledSymbols[styleIdx], // Match bar
            _ => ""
        };
    }

    private static string GetRightCloser(int styleIdx)
    {
        return barClosureMode switch
        {
            0 => "", // None
            1 => DefaultRightCloser, // Default (▏)
            2 => BracketRightCloser, // Brackets (])
            3 => BarFilledSymbols[styleIdx], // Match bar (use filled symbol)
            _ => ""
        };
    }

    private static string AddHorizontalMargins(string bar)
    {
        return $"<color={InvisibleColor}>██</color>{bar}<color={InvisibleColor}>██</color>";
    }

    private static string BuildPercentText(int percentValue)
    {
        string percentText = string.Empty;

        if (percentDisplayMode != 0)
        {
            string label = percentDisplayMode == 2 ? GetPercentWord(percentValue) : $"{percentValue}%";
            percentText = $"\n<size=75%><color={GetPercentColor(percentValue)}>{label}</color></size>";
        }

        return percentText;
    }

    private static string BuildBioItemCountText(int currentItems, int currentItemPercent)
    {
        switch (bioItemCountMode)
        {
            case 0:
                return string.Empty;
            case 2:
                return $"\n<size=70%><color={BarColorPalette[itemCountColorModeIndex]}>{currentItems}</color></size>";
            case 3:
                return $"\n<size=70%><color={BarColorPalette[itemCountColorModeIndex]}>{BuildBioRodIndicatorSymbols(currentItems, currentItemPercent)}</color></size>";
            default:
                return $"\n<size=70%><color={BarColorPalette[itemCountColorModeIndex]}>{currentItems}/{BioReactorMaxSlots}</color></size>";
        }
    }

    private static string BuildNuclearItemCountText(BaseNuclearReactor reactor, int currentItems, int currentRodPercent)
    {
        switch (bioItemCountMode)
        {
            case 0:
                return string.Empty;
            case 2:
                return $"\n<size=70%><color={BarColorPalette[itemCountColorModeIndex]}>{currentItems}</color></size>";
            case 3:
                return $"\n<size=70%><color={BarColorPalette[itemCountColorModeIndex]}>{BuildNuclearRodIndicatorSymbols(GetNuclearEquipment(reactor), currentRodPercent, reactor != null && reactor.producingPower)}</color></size>";
            default:
                return $"\n<size=70%><color={BarColorPalette[itemCountColorModeIndex]}>{currentItems}/{NuclearReactorMaxSlots}</color></size>";
        }
    }

    private static string GetPercentColor(int percent)
    {
        // For animated styles (0-3), return color based on percentage
        switch (percentColorStyleIndex)
        {
            case 0:
                if (percent >= 75)
                {
                    return "#5DFF7A";
                }

                if (percent >= 50)
                {
                    return "#FFE066";
                }

                if (percent >= 25)
                {
                    return "#FFB347";
                }

                return "#FF6B6B";
            case 1:
                if (percent >= 75)
                {
                    return "#7DD3FF";
                }

                if (percent >= 50)
                {
                    return "#4DBBFF";
                }

                if (percent >= 25)
                {
                    return "#2EA8FF";
                }

                return "#1D7FCC";
            case 2:
                if (percent >= 75)
                {
                    return "#CFF6FF";
                }

                if (percent >= 50)
                {
                    return "#96E6FF";
                }

                if (percent >= 25)
                {
                    return "#67D6FF";
                }

                return "#39B7E8";
            case 3:
                if (percent >= 75)
                {
                    return "#7CFFEA";
                }

                if (percent >= 50)
                {
                    return "#66FFC2";
                }

                if (percent >= 25)
                {
                    return "#FFD166";
                }

                return "#FF6B9A";
            // For fixed colors (indices 4+), use the palette directly
            default:
                if (percentColorStyleIndex >= 4 && percentColorStyleIndex < 4 + BarColorPalette.Length)
                {
                    return BarColorPalette[percentColorStyleIndex - 4];
                }

                // Fallback to first animated style if index is out of range
                if (percent >= 75)
                {
                    return "#CFF6FF";
                }

                if (percent >= 50)
                {
                    return "#96E6FF";
                }

                if (percent >= 25)
                {
                    return "#67D6FF";
                }

                return "#39B7E8";
        }
    }

    private static string GetPercentWord(int percent)
    {
        if (percent >= 95)
        {
            return "FULL";
        }

        if (percent >= 75)
        {
            return "HIGH";
        }

        if (percent >= 50)
        {
            return "MID";
        }

        if (percent >= 25)
        {
            return "LOW";
        }

        return "CRITICAL";
    }

    private static string BuildStatusText(string baseText, bool isActive)
    {
        string color = isActive ? BarColorPalette[powerOnColorModeIndex] : BarColorPalette[powerOffColorModeIndex];
        if (reactorStatusStyleIndex <= 0)
        {
            return $"<color={color}>{baseText}</color>";
        }

        string symbol = GetStatusSymbol(reactorStatusStyleIndex, isActive);
        return $"<size=120%><color={color}>{symbol}</color></size>";
    }

    private static string GetStatusSymbol(int styleIndex, bool isActive)
    {
        if (styleIndex < 0)
        {
            styleIndex = 0;
        }

        if (styleIndex >= DynamicReactorStatusStylesCount)
        {
            styleIndex = DynamicReactorStatusStylesCount - 1;
        }

        string[] symbols = isActive ? ReactorStatusActiveSymbols : ReactorStatusInactiveSymbols;
        string symbol = symbols[styleIndex];
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return "X";
        }

        return symbol;
    }

    private static string[] SplitCsvSymbols(string csv)
    {
        string[] raw = csv.Split(',');
        string[] result = new string[raw.Length];

        for (int i = 0; i < raw.Length; i++)
        {
            result[i] = raw[i].Trim();
        }

        return result;
    }

    private static void SyncRuntimeConfig()
    {
        ModConfig config = Plugin.Settings;
        if (config == null)
        {
            return;
        }

        if (cachedBioItemCountMode != config.BioItemCountMode)
        {
            cachedBioItemCountMode = config.BioItemCountMode;
            SetBioItemCountMode(config.BioItemCountMode);
        }

        if (cachedPercentDisplayMode != config.PercentDisplayMode)
        {
            cachedPercentDisplayMode = config.PercentDisplayMode;
            SetPercentDisplayMode(config.PercentDisplayMode);
        }

        if (cachedBarStyleIndex != config.BarStyleIndex)
        {
            cachedBarStyleIndex = config.BarStyleIndex;
            SetBarStyle(config.BarStyleIndex);
        }

        if (cachedBarClosureMode != config.BarClosureMode)
        {
            cachedBarClosureMode = config.BarClosureMode;
            SetBarClosureMode(config.BarClosureMode);
        }

        if (cachedPercentColorStyleIndex != config.PercentColorStyleIndex)
        {
            cachedPercentColorStyleIndex = config.PercentColorStyleIndex;
            SetPercentColorStyle(config.PercentColorStyleIndex);
        }

        if (cachedReactorStatusStyleIndex != config.ReactorStatusStyleIndex)
        {
            cachedReactorStatusStyleIndex = config.ReactorStatusStyleIndex;
            SetReactorStatusStyle(config.ReactorStatusStyleIndex);
        }

        if (cachedBarColorModeIndex != config.BarColorModeIndex)
        {
            cachedBarColorModeIndex = config.BarColorModeIndex;
            SetBarColorMode(config.BarColorModeIndex);
        }

        if (cachedBarCloserColorModeIndex != config.BarCloserColorModeIndex)
        {
            cachedBarCloserColorModeIndex = config.BarCloserColorModeIndex;
            SetBarCloserColorMode(config.BarCloserColorModeIndex);
        }

        if (cachedPowerOffColorModeIndex != config.PowerOffColorModeIndex)
        {
            cachedPowerOffColorModeIndex = config.PowerOffColorModeIndex;
            SetPowerOffColor(config.PowerOffColorModeIndex);
        }

        if (cachedPowerOnColorModeIndex != config.PowerOnColorModeIndex)
        {
            cachedPowerOnColorModeIndex = config.PowerOnColorModeIndex;
            SetPowerOnColor(config.PowerOnColorModeIndex);
        }

        if (cachedItemCountColorModeIndex != config.ItemCountColorModeIndex)
        {
            cachedItemCountColorModeIndex = config.ItemCountColorModeIndex;
            SetItemCountColor(config.ItemCountColorModeIndex);
        }
    }

    private static bool TryGetBioCurrentItem(BaseBioReactor reactor, out float charge)
    {
        charge = 0f;
        if (!reactor.producingPower)
        {
            return false;
        }

        ItemsContainer container = (ItemsContainer)AccessTools.Property(typeof(BaseBioReactor), "container").GetValue(reactor, null);
        if (container == null || container.count <= 0)
        {
            return false;
        }

        foreach (InventoryItem inventoryItem in (IEnumerable)container)
        {
            Pickupable pickupable = inventoryItem?.item;
            if (pickupable == null)
            {
                continue;
            }

            charge = BaseBioReactor.GetCharge(pickupable.GetTechType());
            if (charge > 0f)
            {
                return true;
            }
        }

        charge = 0f;
        return false;
    }

    private static int GetBioItemCount(BaseBioReactor reactor)
    {
        ItemsContainer container = (ItemsContainer)AccessTools.Property(typeof(BaseBioReactor), "container").GetValue(reactor, null);
        if (container == null)
        {
            return 0;
        }

        return container.count;
    }

    private static int GetNuclearRodCount(BaseNuclearReactor reactor)
    {
        Equipment equipment = GetNuclearEquipment(reactor);
        if (equipment == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 1; i <= NuclearReactorMaxSlots; i++)
        {
            InventoryItem item = equipment.GetItemInSlot("NuclearReactor" + i);
            Pickupable pickupable = item?.item;
            if (pickupable != null && pickupable.GetTechType() == TechType.ReactorRod)
            {
                count++;
            }
        }

        return count;
    }

    private static Equipment GetNuclearEquipment(BaseNuclearReactor reactor)
    {
        if (reactor == null)
        {
            return null;
        }

        return (Equipment)AccessTools.Property(typeof(BaseNuclearReactor), "equipment").GetValue(reactor, null);
    }

    private static string BuildNuclearRodIndicatorSymbols(Equipment equipment, int currentRodPercent, bool producingPower)
    {
        if (equipment == null)
        {
            return string.Concat(NuclearRodEmptySymbol, NuclearRodEmptySymbol, NuclearRodEmptySymbol, NuclearRodEmptySymbol);
        }

        int activeCount = 0;
        int spentCount = 0;

        for (int i = 1; i <= NuclearReactorMaxSlots; i++)
        {
            InventoryItem item = equipment.GetItemInSlot("NuclearReactor" + i);
            Pickupable pickupable = item?.item;
            if (pickupable == null)
            {
                continue;
            }

            TechType techType = pickupable.GetTechType();
            if (techType == TechType.ReactorRod)
            {
                activeCount++;
                continue;
            }

            if (techType == TechType.DepletedReactorRod)
            {
                spentCount++;
            }
        }

        int emptyCount = Mathf.Clamp(NuclearReactorMaxSlots - activeCount - spentCount, 0, NuclearReactorMaxSlots);
        int rightGroupCount = activeCount + emptyCount;
        string[] symbols = new string[rightGroupCount];
        int index = 0;

        for (int i = 0; i < activeCount; i++)
        {
            bool isConsumingRod = i == activeCount - 1;
            symbols[index++] = isConsumingRod && producingPower
                ? GetProgressSectorSymbol(currentRodPercent, NuclearRodSpentSymbol)
                : NuclearRodFilledSymbol;
        }

        for (int i = 0; i < emptyCount; i++)
        {
            symbols[index++] = NuclearRodEmptySymbol;
        }

        string rightGroup = string.Concat(symbols);
        if (spentCount <= 0)
        {
            return rightGroup;
        }

        string spentGroup = new string(NuclearRodSpentSymbol[0], spentCount);
        return rightGroupCount > 0
            ? $"{spentGroup} / {rightGroup}"
            : spentGroup;
    }

    private static string BuildBioRodIndicatorSymbols(int currentItems, int currentItemPercent)
    {
        int clamped = Mathf.Clamp(currentItems, 0, BioReactorMaxSlots);
        string[] symbols = new string[BioReactorMaxSlots];
        for (int i = 0; i < BioReactorMaxSlots; i++)
        {
            if (i < clamped - 1)
            {
                symbols[i] = NuclearRodFilledSymbol;
                continue;
            }

            if (i == clamped - 1)
            {
                symbols[i] = GetProgressSectorSymbol(currentItemPercent, NuclearRodEmptySymbol);
                continue;
            }

            symbols[i] = NuclearRodEmptySymbol;
        }

        return string.Concat(symbols);
    }

    private static string GetProgressSectorSymbol(int percent, string zeroSymbol)
    {
        if (percent <= 0)
        {
            return zeroSymbol;
        }

        if (percent > 50)
        {
            return NuclearRodFilledSymbol;
        }

        return NuclearRodHalfSymbol;
    }

    private static bool TryHasActiveNuclearRod(BaseNuclearReactor reactor)
    {
        if (!reactor.producingPower)
        {
            return false;
        }

        Equipment equipment = (Equipment)AccessTools.Property(typeof(BaseNuclearReactor), "equipment").GetValue(reactor, null);
        if (equipment == null)
        {
            return false;
        }

        for (int i = 1; i <= NuclearReactorMaxSlots; i++)
        {
            InventoryItem item = equipment.GetItemInSlot("NuclearReactor" + i);
            Pickupable pickupable = item?.item;
            if (pickupable != null && pickupable.GetTechType() == TechType.ReactorRod)
            {
                return true;
            }
        }

        return false;
    }

    private static BaseBioReactorGeometry GetGeometry(BaseBioReactor reactor)
    {
        if (reactor == null)
        {
            return null;
        }

        Base baseComponent = reactor.GetComponentInParent<Base>();
        if (baseComponent == null)
        {
            return null;
        }

        Base.Face face = (Base.Face)AccessTools.Property(typeof(BaseBioReactor), "moduleFace").GetValue(reactor, null);
        IBaseModuleGeometry geometry = baseComponent.GetModuleGeometry(face);
        return geometry as BaseBioReactorGeometry;
    }

    private static BaseNuclearReactorGeometry GetGeometry(BaseNuclearReactor reactor)
    {
        if (reactor == null)
        {
            return null;
        }

        Base baseComponent = reactor.GetComponentInParent<Base>();
        if (baseComponent == null)
        {
            return null;
        }

        Base.Face face = (Base.Face)AccessTools.Property(typeof(BaseNuclearReactor), "moduleFace").GetValue(reactor, null);
        IBaseModuleGeometry geometry = baseComponent.GetModuleGeometry(face);
        return geometry as BaseNuclearReactorGeometry;
    }
}
