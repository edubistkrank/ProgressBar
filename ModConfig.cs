using Nautilus.Json;
using Nautilus.Options;
using Nautilus.Options.Attributes;
using UnityEngine.UI;

namespace ProgressBar;

[Menu("ProgressBar")]
internal sealed class ModConfig : ConfigFile
{
    internal const int PercentDisplayModeCount = 3;
    internal const int ItemCountModeCount = 4;
    internal const int BarClosureModeCount = 8;
    internal const int StandardColorCount = 9;
    internal const int BarColorModeCount = 10;
    internal const int PercentColorStyleCount = 13;

    [Toggle("Enable numpad hotkeys", Tooltip = "STYLE:\n[0] Percentage\n[.] Item count\n[1] Power\n[2] Bar\n[3] Closers\n\nCOLORS:\n[4] Power ON\n[5] Bar\n[6] Closers\n[7] Power OFF\n[8] Percentage\n[9] Item count", Order = -10)]
    public bool EnableNumpadHotkeys { get; set; } = false;

    [Toggle("<color=#FFAC09FF>Style</color> <alpha=#00>----------------------------------------------------------------------------</alpha>", Order = 0)]
    [OnGameObjectCreated(nameof(ConfigureStyleHeader))]
    public bool StyleSection { get; set; }

    [Choice(Label = "Power", Options = new[] { "Original", "Bracket", "Compact", "Dot", "Dot Ring", "Double Ring", "Dot Circle", "Square", "Square Ring", "Diamond", "Diamond Ring", "Round Square", "Warning", "Info", "Heart", "Star", "Code", "Yin Yang", "X", "X", "X", "X", "X", "X", "X", "X", "X", "X", "X", "X" }, Tooltip = "Choose ACTIVE/INACTIVE indicator style.", Order = 10)]
    public int ReactorStatusStyleIndex { get; set; } = 0;

    [Choice(Label = "Bar", Options = new[] { "Solid", "Compact", "Spaced", "Pipe", "Diamond", "Parallelogram", "Circle", "Circle Ring", "Arrow", "Slash", "Parentheses", "Bracket", "Exclamation", "Hashtag", "Asterik", "Dash", "Equal", "Colon", "Dot", "Degree" }, Tooltip = "Choose one of 20 bar styles.", Order = 11)]
    public int BarStyleIndex { get; set; } = 0;

    [Choice(Label = "Closers", Options = new[] { "None", "Default  |...|", "Brackets [...]", "Match bar symbols", "Box A (top/bottom)", "Box B (corners)", "Box C (top/bottom + sides)", "Box D (corners + sides)" }, Tooltip = "Choose how bar borders/closers are rendered.", Order = 12)]
    public int BarClosureMode { get; set; } = 1;

    [Choice(Label = "Item count", Options = new[] { "Hidden", "X/N", "X", "Circles" }, Tooltip = "Choose Hidden, X/N, X or circles.", Order = 13)]
    public int BioItemCountMode { get; set; } = 1;

    [Choice(Label = "Percentage", Options = new[] { "Hidden", "X%", "Text" }, Tooltip = "Choose Hidden, X% or Text.", Order = 14)]
    public int PercentDisplayMode { get; set; } = 1;

    [Toggle("<color=#FFAC09FF>Colors</color> <alpha=#00>----------------------------------------------------------------------------</alpha>", Order = 100)]
    [OnGameObjectCreated(nameof(ConfigureColorsHeader))]
    public bool ColorsSection { get; set; }

    [Choice(Label = "Power OFF", Options = new[] { "Blue", "Cyan", "Green", "Yellow", "Orange", "Red", "Magenta", "White", "Gray" }, Tooltip = "Choose indicator color when reactor is OFF.", Order = 110)]
    public int PowerOffColorModeIndex { get; set; } = 4;

    [Choice(Label = "Power ON", Options = new[] { "Blue", "Cyan", "Green", "Yellow", "Orange", "Red", "Magenta", "White", "Gray" }, Tooltip = "Choose indicator color when reactor is ON.", Order = 111)]
    public int PowerOnColorModeIndex { get; set; } = 2;

    [Choice(Label = "Bar", Options = new[] { "Blue", "Cyan", "Green", "Yellow", "Orange", "Red", "Magenta", "White", "Gray", "Follow % Color" }, Tooltip = "Choose bar fill color or Follow % Color.", Order = 112)]
    public int BarColorModeIndex { get; set; } = 0;

    [Choice(Label = "Closers", Options = new[] { "Blue", "Cyan", "Green", "Yellow", "Orange", "Red", "Magenta", "White", "Gray", "Follow % Color" }, Tooltip = "Choose bar closer color or Follow % Color.", Order = 113)]
    public int BarCloserColorModeIndex { get; set; } = 0;

    [Choice(Label = "Item count", Options = new[] { "Blue", "Cyan", "Green", "Yellow", "Orange", "Red", "Magenta", "White", "Gray" }, Tooltip = "Choose item count text color.", Order = 114)]
    public int ItemCountColorModeIndex { get; set; } = 7;

    [Choice(Label = "Percentage", Options = new[] { "Animated Traffic", "Animated Subnautica Blue", "Animated Ice", "Animated Neon", "Blue", "Cyan", "Green", "Yellow", "Orange", "Red", "Magenta", "White", "Gray" }, Tooltip = "Choose percentage color palette (styles 1-4 are dynamic, rest are fixed colors).", Order = 115)]
    public int PercentColorStyleIndex { get; set; } = 0;

    public int PercentageColorModeIndex { get; set; } = 0;

    private void ConfigureStyleHeader(object sender, GameObjectCreatedEventArgs args)
    {
        ConfigureSectionHeader(args);
    }

    private void ConfigureColorsHeader(object sender, GameObjectCreatedEventArgs args)
    {
        ConfigureSectionHeader(args);
    }

    private static void ConfigureSectionHeader(GameObjectCreatedEventArgs args)
    {
        if (args?.Value == null)
        {
            return;
        }

        Toggle toggle = args.Value.GetComponentInChildren<Toggle>(true);
        if (toggle == null)
        {
            return;
        }

        toggle.interactable = false;

        if (toggle.graphic != null)
        {
            toggle.graphic.enabled = false;
        }

        if (toggle.targetGraphic != null)
        {
            toggle.targetGraphic.enabled = false;
        }
    }
}
