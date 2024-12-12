using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;

[Flags]
public enum LogTag {
    None = 0,
    UI = 1 << 0,
    Actions = 1 << 1,
    Effects = 1 << 2,
    Creatures = 1 << 3,
    Players = 1 << 4,
    Cards = 1 << 5,
    Combat = 1 << 6,
    Initialization = 1 << 7,
    Network = 1 << 8,
    Economy = 1 << 9,
    All = ~0
}

public class LoggerSettings {
    private static readonly Dictionary<LogTag, string> DefaultTagColors = new Dictionary<LogTag, string>
    {
        { LogTag.UI, "#80FFFF" },
        { LogTag.Actions, "#FFE066" },
        { LogTag.Effects, "#FF99FF" },
        { LogTag.Creatures, "#90EE90" },
        { LogTag.Players, "#ADD8E6" },
        { LogTag.Cards, "#E0E0E0" },
        { LogTag.Combat, "#FF9999" },
        { LogTag.Initialization, "#DEB887" },
        { LogTag.Network, "#98FB98" },
        { LogTag.Economy, "#DDA0DD" }
    };

    public static LoggerSettings Default => new LoggerSettings {
        EnabledTags = LogTag.All,
        TagColors = new Dictionary<LogTag, string>(DefaultTagColors)
    };

    public LogTag EnabledTags { get; set; } = LogTag.All;

    public Dictionary<LogTag, string> TagColors { get; set; }

    public LoggerSettings() {
        TagColors = new Dictionary<LogTag, string>(DefaultTagColors);
    }

    public void EnableTags(LogTag tags) {
        EnabledTags |= tags;
    }

    public void DisableTags(LogTag tags) {
        EnabledTags &= ~tags;
    }

    public void SetTagColor(LogTag tag, string hexColor) {
        TagColors[tag] = hexColor;
    }
}

public static class DebugLogger {
    private static readonly LoggerSettings _settings;

    // Static constructor to initialize settings with only UI tags enabled
    static DebugLogger() {
        _settings = new LoggerSettings {
            EnabledTags = LogTag.UI, // Only enable UI tags
            TagColors = new Dictionary<LogTag, string>
            {
                { LogTag.UI, "#80FFFF" },
                { LogTag.Actions, "#FFE066" },
                { LogTag.Effects, "#FF99FF" },
                { LogTag.Creatures, "#90EE90" },
                { LogTag.Players, "#ADD8E6" },
                { LogTag.Cards, "#E0E0E0" },
                { LogTag.Combat, "#FF9999" },
                { LogTag.Initialization, "#DEB887" },
                { LogTag.Network, "#98FB98" },
                { LogTag.Economy, "#DDA0DD" }
            }
        };
    }

    public static void Log(
        object message,
        LogTag tags = LogTag.All,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "") {
        if (!ShouldLog(tags)) return;

        string formattedMessage = FormatMessage(message, tags, memberName, sourceFilePath);
        Debug.Log(formattedMessage);
    }

    public static void LogWarning(
        object message,
        LogTag tags,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "") {
        if (!ShouldLog(tags)) return;

        string formattedMessage = FormatMessage(message, tags, memberName, sourceFilePath);
        Debug.LogWarning(formattedMessage);
    }

    public static void LogError(
        object message,
        LogTag tags,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "") {
        if (!ShouldLog(tags)) return;

        string formattedMessage = FormatMessage(message, tags, memberName, sourceFilePath);
        Debug.LogError(formattedMessage);
    }

    private static bool ShouldLog(LogTag tags) {
        return (_settings.EnabledTags & tags) != 0;
    }

    private static string FormatMessage(
        object message,
        LogTag tags,
        string memberName,
        string sourceFilePath) {
        string className = GetClassName(sourceFilePath);
        string tagList = GetTagList(tags);
        string coloredTags = ColorizeTags(tagList, tags);

        return $"{className}: [{coloredTags}] {message}";
    }

    private static string GetClassName(string sourceFilePath) {
        if (string.IsNullOrEmpty(sourceFilePath)) return "Unknown";
        return System.IO.Path.GetFileNameWithoutExtension(sourceFilePath);
    }

    private static string GetTagList(LogTag tags) {
        return string.Join("|",
            Enum.GetValues(typeof(LogTag))
                .Cast<LogTag>()
                .Where(tag => tag != LogTag.None && tag != LogTag.All && (tags & tag) == tag)
                .Select(tag => tag.ToString())
        );
    }

    private static string ColorizeTags(string tagList, LogTag tags) {
        string[] individualTags = tagList.Split('|');

        return string.Join("|", individualTags.Select(tag =>
            Enum.TryParse(tag, out LogTag currentTag) && _settings.TagColors.TryGetValue(currentTag, out string color)
                ? $"<color={color}>{tag}</color>"
                : tag
        ));
    }
}