#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using static DebugLogger;

[CustomEditor(typeof(DebugLogger))]
public class DebugLoggerEditor : Editor {
    private SerializedProperty settingsProp;
    private Editor settingsEditor;

    private void OnEnable() {
        settingsProp = serializedObject.FindProperty("settings");
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        EditorGUILayout.PropertyField(settingsProp);

        if (settingsProp.objectReferenceValue != null) {
            CreateCachedEditor(settingsProp.objectReferenceValue, null, ref settingsEditor);
            settingsEditor.OnInspectorGUI();
        }

        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(DebugLoggerSettings))]
public class DebugLoggerSettingsEditor : Editor {
    private SerializedProperty tagSettingsProp;
    private SerializedProperty classFiltersProp;
    private SerializedProperty showStackTraceProp;
    private SerializedProperty whitelistModeProp;
    private SerializedProperty tagWhitelistModeProp;
    private bool showTagSettings = true;
    private bool showClassFilters = true;
    private Vector2 classListScrollPosition;
    private Vector2 tagListScrollPosition;
    private string classSearchString = "";
    private string tagSearchString = "";
    private bool allTagsSelected = false;
    private bool allClassesSelected = false;

    private bool AreAllTagsEnabled() {
        if (tagSettingsProp == null || tagSettingsProp.arraySize == 0) return false;

        for (int i = 0; i < tagSettingsProp.arraySize; i++) {
            var tagSetting = tagSettingsProp.GetArrayElementAtIndex(i);
            if (tagSetting != null) {
                var isEnabledProp = tagSetting.FindPropertyRelative("isEnabled");
                if (isEnabledProp != null && !isEnabledProp.boolValue) {
                    return false;
                }
            }
        }
        return true;
    }

    private bool AreAllTagsDisabled() {
        if (tagSettingsProp == null || tagSettingsProp.arraySize == 0) return true;

        for (int i = 0; i < tagSettingsProp.arraySize; i++) {
            var tagSetting = tagSettingsProp.GetArrayElementAtIndex(i);
            if (tagSetting != null) {
                var isEnabledProp = tagSetting.FindPropertyRelative("isEnabled");
                if (isEnabledProp != null && isEnabledProp.boolValue) {
                    return false;
                }
            }
        }
        return true;
    }

    private bool AreAllClassesEnabled() {
        foreach (var className in AvailableClasses) {
            if (!IsClassEnabled(className)) {
                return false;
            }
        }
        return true;
    }

    private bool AreAllClassesDisabled() {
        foreach (var className in AvailableClasses) {
            if (IsClassEnabled(className)) {
                return false;
            }
        }
        return true;
    }

    private void OnEnable() {
        tagSettingsProp = serializedObject.FindProperty("tagSettings");
        classFiltersProp = serializedObject.FindProperty("classFilters");
        showStackTraceProp = serializedObject.FindProperty("showStackTrace");
        whitelistModeProp = serializedObject.FindProperty("whitelistMode");
        tagWhitelistModeProp = serializedObject.FindProperty("tagWhitelistMode");

        // Initialize default tag settings if empty
        if (!Application.isPlaying && (tagSettingsProp.arraySize == 0 || tagSettingsProp == null)) {
            InitializeDefaultTagSettings();
        }

        // Sync initial states
        allTagsSelected = AreAllTagsEnabled();
        allClassesSelected = AreAllClassesEnabled();
    }

    private void InitializeDefaultTagSettings() {
        serializedObject.Update();
        tagSettingsProp.ClearArray();

        var orderedTags = new[]
        {
            LogTag.UI,
            LogTag.Actions,
            LogTag.Effects,
            LogTag.Creatures,
            LogTag.Players,
            LogTag.Cards,
            LogTag.Combat,
            LogTag.Initialization
        };

        foreach (var tag in orderedTags) {
            if (DefaultColors.ContainsKey(tag)) {
                tagSettingsProp.InsertArrayElementAtIndex(tagSettingsProp.arraySize);
                var element = tagSettingsProp.GetArrayElementAtIndex(tagSettingsProp.arraySize - 1);

                element.FindPropertyRelative("tagValue").intValue = (int)tag;
                element.FindPropertyRelative("isEnabled").boolValue = true;
                element.FindPropertyRelative("color").colorValue = DefaultColors[tag];
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Logger Settings", EditorStyles.boldLabel);

        // Stack Trace Toggle
        EditorGUILayout.PropertyField(showStackTraceProp);

        // Tag Settings
        EditorGUILayout.Space();
        showTagSettings = EditorGUILayout.Foldout(showTagSettings, "Tag Settings", true);
        if (showTagSettings) {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(tagWhitelistModeProp, new GUIContent("Whitelist"));

            string buttonText = "Select All";
            if (AreAllTagsEnabled()) {
                buttonText = "Deselect All";
            } else if (!AreAllTagsDisabled()) {
                // Keep current state if in mixed state
                buttonText = allTagsSelected ? "Deselect All" : "Select All";
            }

            if (GUILayout.Button(buttonText, GUILayout.Width(100))) {
                ToggleAllTags();
            }
            EditorGUILayout.EndHorizontal();

            // Tag search bar
            tagSearchString = EditorGUILayout.TextField("Search", tagSearchString ?? "");

            // Tag list with scrollview
            EditorGUILayout.LabelField("Available Tags");
            tagListScrollPosition = EditorGUILayout.BeginScrollView(tagListScrollPosition, GUILayout.Height(200));
            if (tagSettingsProp != null) {
                for (int i = 0; i < tagSettingsProp.arraySize; i++) {
                    SerializedProperty tagSetting = tagSettingsProp.GetArrayElementAtIndex(i);
                    if (tagSetting == null) continue;

                    SerializedProperty tagValueProp = tagSetting.FindPropertyRelative("tagValue");
                    SerializedProperty isEnabledProp = tagSetting.FindPropertyRelative("isEnabled");
                    SerializedProperty colorProp = tagSetting.FindPropertyRelative("color");
                    if (tagValueProp == null || isEnabledProp == null || colorProp == null) continue;

                    var currentTag = (DebugLogger.LogTag)tagValueProp.intValue;
                    string tagName = currentTag.ToString();

                    if (!string.IsNullOrEmpty(tagSearchString) &&
                        !tagName.ToLower().Contains(tagSearchString.ToLower())) {
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal();
                    float minTextWidth = GUI.skin.toggle.CalcSize(new GUIContent(tagName)).x + 15;
                    bool newEnabled = EditorGUILayout.ToggleLeft(tagName, isEnabledProp.boolValue, GUILayout.Width(minTextWidth));
                    if (newEnabled != isEnabledProp.boolValue) {
                        isEnabledProp.boolValue = newEnabled;
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.PropertyField(colorProp, GUIContent.none, GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Reset Colors")) {
                ResetTagColors();
            }
            EditorGUI.indentLevel--;
        }

        // Class Filters
        EditorGUILayout.Space();
        showClassFilters = EditorGUILayout.Foldout(showClassFilters, "Class Filters", true);
        if (showClassFilters) {
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(whitelistModeProp, new GUIContent("Whitelist"));

            string buttonText = "Select All";
            if (AreAllClassesEnabled()) {
                buttonText = "Deselect All";
            } else if (!AreAllClassesDisabled()) {
                // Keep current state if in mixed state
                buttonText = allClassesSelected ? "Deselect All" : "Select All";
            }

            if (GUILayout.Button(buttonText, GUILayout.Width(100))) {
                ToggleAllClasses();
            }
            EditorGUILayout.EndHorizontal();

            classSearchString = EditorGUILayout.TextField("Search", classSearchString ?? "");

            EditorGUILayout.LabelField("Available Classes");
            classListScrollPosition = EditorGUILayout.BeginScrollView(classListScrollPosition, GUILayout.Height(300));

            var filteredClasses = DebugLogger.AvailableClasses
                .Where(c => string.IsNullOrEmpty(classSearchString) ||
                           c.ToLower().Contains(classSearchString.ToLower()));

            foreach (var className in filteredClasses) {
                bool isEnabled = IsClassEnabled(className);
                bool newEnabled = EditorGUILayout.ToggleLeft(className, isEnabled);

                if (newEnabled != isEnabled) {
                    if (newEnabled) {
                        AddClassFilter(className);
                    } else {
                        RemoveClassFilter(className);
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void ToggleAllTags() {
        if (tagSettingsProp == null) return;

        // Set the target state based on current actual state
        bool targetState = !AreAllTagsEnabled();
        allTagsSelected = targetState;

        for (int i = 0; i < tagSettingsProp.arraySize; i++) {
            var tagSetting = tagSettingsProp.GetArrayElementAtIndex(i);
            if (tagSetting != null) {
                var isEnabledProp = tagSetting.FindPropertyRelative("isEnabled");
                if (isEnabledProp != null) {
                    isEnabledProp.boolValue = targetState;
                }
            }
        }
    }

    private void ResetTagColors() {
        if (tagSettingsProp == null) return;

        for (int i = 0; i < tagSettingsProp.arraySize; i++) {
            SerializedProperty tagSetting = tagSettingsProp.GetArrayElementAtIndex(i);
            if (tagSetting == null) continue;

            SerializedProperty tagValueProp = tagSetting.FindPropertyRelative("tagValue");
            SerializedProperty colorProp = tagSetting.FindPropertyRelative("color");

            if (tagValueProp == null || colorProp == null) continue;

            var currentTag = (LogTag)tagValueProp.intValue;
            if (DefaultColors.TryGetValue(currentTag, out Color defaultColor)) {
                colorProp.colorValue = defaultColor;
            }
        }
    }

    private bool IsClassEnabled(string className) {
        if (classFiltersProp == null) return false;

        for (int i = 0; i < classFiltersProp.arraySize; i++) {
            var filter = classFiltersProp.GetArrayElementAtIndex(i);
            var classNameProp = filter.FindPropertyRelative("className");
            var isEnabledProp = filter.FindPropertyRelative("isEnabled");

            if (classNameProp.stringValue == className) {
                return isEnabledProp.boolValue;
            }
        }

        // If we're in whitelist mode and the class isn't found, it should be disabled
        // If we're in blacklist mode and the class isn't found, it should be enabled
        return !whitelistModeProp.boolValue;
    }

    private void AddClassFilter(string className) {
        // Check if class filter already exists
        for (int i = 0; i < classFiltersProp.arraySize; i++) {
            var filter = classFiltersProp.GetArrayElementAtIndex(i);
            if (filter.FindPropertyRelative("className").stringValue == className) {
                filter.FindPropertyRelative("isEnabled").boolValue = true;
                return;
            }
        }

        // Add new filter only if it doesn't exist
        classFiltersProp.InsertArrayElementAtIndex(classFiltersProp.arraySize);
        var newFilter = classFiltersProp.GetArrayElementAtIndex(classFiltersProp.arraySize - 1);
        newFilter.FindPropertyRelative("className").stringValue = className;
        newFilter.FindPropertyRelative("isEnabled").boolValue = true;
    }

    private void RemoveClassFilter(string className) {
        for (int i = 0; i < classFiltersProp.arraySize; i++) {
            var filter = classFiltersProp.GetArrayElementAtIndex(i);
            if (filter.FindPropertyRelative("className").stringValue == className) {
                filter.FindPropertyRelative("isEnabled").boolValue = false;
                return;
            }
        }

        // Only add a disabled filter in whitelist mode
        if (whitelistModeProp.boolValue) {
            classFiltersProp.InsertArrayElementAtIndex(classFiltersProp.arraySize);
            var newFilter = classFiltersProp.GetArrayElementAtIndex(classFiltersProp.arraySize - 1);
            newFilter.FindPropertyRelative("className").stringValue = className;
            newFilter.FindPropertyRelative("isEnabled").boolValue = false;
        }
    }

    private void ToggleAllClasses() {
        // Set the target state based on current actual state
        bool targetState = !AreAllClassesEnabled();
        allClassesSelected = targetState;

        // Instead of clearing the array, we'll update existing entries
        // and only add new ones if necessary
        foreach (var className in DebugLogger.AvailableClasses) {
            bool found = false;

            // First try to find and update existing entry
            for (int i = 0; i < classFiltersProp.arraySize; i++) {
                var filter = classFiltersProp.GetArrayElementAtIndex(i);
                var classNameProp = filter.FindPropertyRelative("className");

                if (classNameProp.stringValue == className) {
                    filter.FindPropertyRelative("isEnabled").boolValue = targetState;
                    found = true;
                    break;
                }
            }

            // If entry wasn't found, add it only if necessary
            if (!found && targetState != !whitelistModeProp.boolValue) {
                classFiltersProp.InsertArrayElementAtIndex(classFiltersProp.arraySize);
                var newFilter = classFiltersProp.GetArrayElementAtIndex(classFiltersProp.arraySize - 1);
                newFilter.FindPropertyRelative("className").stringValue = className;
                newFilter.FindPropertyRelative("isEnabled").boolValue = targetState;
            }
        }
    }
}
#endif