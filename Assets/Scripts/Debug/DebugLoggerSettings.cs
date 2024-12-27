using UnityEngine;
using System;
using System.Collections.Generic;
using static DebugLogger;

[CreateAssetMenu(fileName = "DebugLoggerSettings", menuName = "Debug/Logger Settings")]
public class DebugLoggerSettings : ScriptableObject {
    [SerializeField] private List<TagSettings> tagSettings = new List<TagSettings>();
    [SerializeField] private List<ClassFilter> classFilters = new List<ClassFilter>();
    [SerializeField] private bool showStackTrace = false;
    [SerializeField] private bool whitelistMode = true;
    [SerializeField] private bool tagWhitelistMode = true;

    public List<TagSettings> TagSettings => tagSettings;
    public List<ClassFilter> ClassFilters => classFilters;
    public bool ShowStackTrace => showStackTrace;
    public bool WhitelistMode => whitelistMode;
    public bool TagWhitelistMode => tagWhitelistMode;

    public void Initialize() {
        if (tagSettings == null || tagSettings.Count == 0) {
            tagSettings = new List<TagSettings>();
            foreach (LogTag tag in Enum.GetValues(typeof(LogTag))) {
                if (tag != LogTag.None && tag != LogTag.All &&
                    DefaultColors.ContainsKey(tag)) {
                    tagSettings.Add(new TagSettings {
                        tag = tag,
                        isEnabled = true,
                        color = DefaultColors[tag]
                    });
                }
            }
        }

        if (classFilters == null) {
            classFilters = new List<ClassFilter>();
        }
    }
}