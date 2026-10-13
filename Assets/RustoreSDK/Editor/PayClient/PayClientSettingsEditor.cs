#if UNITY_EDITOR
using RuStore.Editor.Manifest;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RuStore.Editor {

    [CustomEditor(typeof(PayClientSettings))]
    public class PayClientSettingsEditor : UnityEditor.Editor {

        private enum EntryPoint {
            UnityPlayerActivity,
            UnityPlayerGameActivity,
            CustomMainActivity,
        }

        private enum DeeplinkActivity {
            Default,
            Custom,
        }

        private const string UNITY_PLAYER_ACTIVITY_CLASS = "com.unity3d.player.UnityPlayerActivity";
        private const string UNITY_PLAYER_GAME_ACTIVITY_CLASS = "com.unity3d.player.UnityPlayerGameActivity";
        private const string INTENT_FILTER_ACTIVITY_DEFAULT_CLASS = "ru.rustore.unitysdk.RuStoreDeeplinkActivityDefault";

        private static readonly GUIContent labelActivity = new("Activity");
        private static readonly GUIContent labelGameActivity = new("GameActivity");
        private static readonly GUIContent labelCustomMainActivity = new("CustomMainActivity");

        private static readonly GUIContent labelDeeplinkActivity = new("Default");
        private static readonly GUIContent labelCustomDeeplinkActivity = new("Custom");

        private string _payClientLabelText = "Pay Client SDK";
        private string _applicationEntryPointLabelText = "Application Entry Point";
        private string _deeplinkActivityLabelText = "Deeplink Activity";
        private int _sectionSpace = 10;

        private SerializedProperty _consoleApplicationId;
        private SerializedProperty _deeplinkScheme;
        private SerializedProperty _customMainActivityClass;
        private SerializedProperty _unityMainActivityClass;
        private SerializedProperty _customDeeplinkActivityClass;
        private SerializedProperty _rustoreDeeplinkActivityClass;

        private EntryPoint _selectedApplicationEntryPoint;
        private DeeplinkActivity _selectedDeeplinkActivity;

        private bool _loggedMissingProps = false;

        private void OnEnable() {
            _consoleApplicationId = FindPropSafe(nameof(PayClientSettings.consoleApplicationId));
            _deeplinkScheme = FindPropSafe(nameof(PayClientSettings.deeplinkScheme));

            _customMainActivityClass = FindPropSafe(nameof(PayClientSettings.customMainActivityClass));
            _unityMainActivityClass = FindPropSafe(nameof(PayClientSettings.unityMainActivityClass));

            _customDeeplinkActivityClass = FindPropSafe(nameof(PayClientSettings.customDeeplinkActivityClass));
            _rustoreDeeplinkActivityClass = FindPropSafe(nameof(PayClientSettings.rustoreDeeplinkActivityClass));

            serializedObject.Update();
            _selectedApplicationEntryPoint = ResolveApplicationEntryPoint(_unityMainActivityClass.stringValue);
            _selectedDeeplinkActivity = ResolveDeeplinkActivity(_rustoreDeeplinkActivityClass.stringValue);
            serializedObject.ApplyModifiedProperties();

            CheckForMigration();
        }

        private SerializedProperty FindPropSafe(string name)
        {
            var prop = serializedObject.FindProperty(name);
            if (prop == null && !_loggedMissingProps) {
                _loggedMissingProps = true;
                Debug.LogError($"PayClientSettingsEditor: SerializedProperty '{name}' not found in {target?.GetType().FullName}.");
            }

            return prop;
        }

        private void CheckForMigration() {
            if (target is PayClientSettings settings && settings.schemeVersion < PayClientSettings.CURRENT_SCHEME_VERSION) {
                var window = EditorWindow.GetWindow<PayClientSettingsMigrationWindow>();
                window.Show(settings);
            }
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            DrawPayClientSdkSection();
            EditorGUILayout.Space(_sectionSpace);
            DrawApplicationEntryPointSection();
            EditorGUILayout.Space(_sectionSpace);
            DrawDeeplinkActivitySection();
            DrawManifestToolsSection();

            if (EditorGUI.EndChangeCheck()) {
                ApplySettings();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPayClientSdkSection() {
            EditorGUILayout.LabelField(_payClientLabelText, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_consoleApplicationId);
            EditorGUILayout.PropertyField(_deeplinkScheme);
        }

        private void DrawApplicationEntryPointSection() {
            EditorGUILayout.LabelField(_applicationEntryPointLabelText, EditorStyles.boldLabel);

            _selectedApplicationEntryPoint = DrawCheckAsRadioRight(_selectedApplicationEntryPoint, EntryPoint.UnityPlayerActivity, labelActivity);
            _selectedApplicationEntryPoint = DrawCheckAsRadioRight(_selectedApplicationEntryPoint, EntryPoint.UnityPlayerGameActivity, labelGameActivity);
            _selectedApplicationEntryPoint = DrawCheckAsRadioRight(_selectedApplicationEntryPoint, EntryPoint.CustomMainActivity, labelCustomMainActivity);

            using (new EditorGUI.DisabledScope(_selectedApplicationEntryPoint != EntryPoint.CustomMainActivity)) {
                EditorGUILayout.PropertyField(_customMainActivityClass);
            }
        }

        private void DrawDeeplinkActivitySection() {
            EditorGUILayout.LabelField(_deeplinkActivityLabelText, EditorStyles.boldLabel);

            _selectedDeeplinkActivity = DrawCheckAsRadioRight(_selectedDeeplinkActivity, DeeplinkActivity.Default, labelDeeplinkActivity);
            _selectedDeeplinkActivity = DrawCheckAsRadioRight(_selectedDeeplinkActivity, DeeplinkActivity.Custom, labelCustomDeeplinkActivity);

            using (new EditorGUI.DisabledScope(_selectedDeeplinkActivity != DeeplinkActivity.Custom)) {
                EditorGUILayout.PropertyField(_customDeeplinkActivityClass);
            }
        }

        private static T DrawCheckAsRadioRight<T>(T current, T value, GUIContent label) where T : struct, Enum {
            bool isOn = EqualityComparer<T>.Default.Equals(current, value);

            Rect rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, label);

            bool newIsOn = EditorGUI.Toggle(rect, isOn);

            return (!isOn && newIsOn) ? value : current;
        }

        private void DrawManifestToolsSection() {
            EditorGUILayout.Space(_sectionSpace);
            EditorGUILayout.LabelField("Android Manifest", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Verify Manifest")) {
                    var settings = PayClientSettingsRepository.LoadFromInspector(serializedObject, target);
                    PayClientManifestPatcher.Instance.VerifyProjectManifest(settings);
                }

                if (GUILayout.Button("Patch Manifest")) {
                    var settings = PayClientSettingsRepository.LoadFromInspector(serializedObject, target);
                    PayClientManifestPatcher.Instance.PatchProjectManifest(settings);
                }
            }
        }

        private EntryPoint ResolveApplicationEntryPoint(string activityClass) {
            var cls = activityClass?.Trim();

            if (string.Equals(cls, UNITY_PLAYER_ACTIVITY_CLASS, StringComparison.Ordinal))
                return EntryPoint.UnityPlayerActivity;

            if (string.Equals(cls, UNITY_PLAYER_GAME_ACTIVITY_CLASS, StringComparison.Ordinal))
                return EntryPoint.UnityPlayerGameActivity;

            return EntryPoint.CustomMainActivity;
        }
        
        private DeeplinkActivity ResolveDeeplinkActivity(string activityClass) {
            var cls = activityClass?.Trim();

            if (string.Equals(cls, INTENT_FILTER_ACTIVITY_DEFAULT_CLASS, StringComparison.Ordinal))
                return DeeplinkActivity.Default;

            return DeeplinkActivity.Custom;
        }

        private void ApplySettings() {
            string activityClass = _selectedApplicationEntryPoint switch {
                EntryPoint.UnityPlayerActivity => UNITY_PLAYER_ACTIVITY_CLASS,
                EntryPoint.UnityPlayerGameActivity => UNITY_PLAYER_GAME_ACTIVITY_CLASS,
                EntryPoint.CustomMainActivity => _customMainActivityClass.stringValue,
                _ => _unityMainActivityClass.stringValue
            };

            if (!string.Equals(_unityMainActivityClass.stringValue, activityClass, StringComparison.Ordinal)) {
                _unityMainActivityClass.stringValue = activityClass;
            }

            string deeplinkActivityClass = _selectedDeeplinkActivity switch {
                DeeplinkActivity.Default => INTENT_FILTER_ACTIVITY_DEFAULT_CLASS,
                DeeplinkActivity.Custom => _customDeeplinkActivityClass.stringValue,
                _ => _rustoreDeeplinkActivityClass.stringValue
            };

            if (!string.Equals(_rustoreDeeplinkActivityClass.stringValue, deeplinkActivityClass, StringComparison.Ordinal)) {
                _rustoreDeeplinkActivityClass.stringValue = deeplinkActivityClass;
            }
        }
    }
}
#endif
