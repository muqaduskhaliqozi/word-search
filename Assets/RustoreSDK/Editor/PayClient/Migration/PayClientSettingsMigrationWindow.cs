#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RuStore.Editor {

    internal class PayClientSettingsMigrationWindow : EditorWindow, IPayClientSettingsMigrationWindow {

        private PayClientSettings _settings;

        // Window
        private const string WINDOW_TITLE = "PayClientSettings Migration";
        private const string HEADER_MESSAGE = "Your PayClientSettings asset is outdated and needs to be migrated.";

        // Version Info Section
        private const string VERSION_INFO_LABEL = "Version Info";
        private const string CURRENT_VERSION_LABEL = "Current version: {0}";
        private const string TARGET_VERSION_LABEL = "Target version: {0}";
        private const string DATA_TO_PRESERVE_LABEL = "These values will be saved:";
        private const string MIGRATION_HELP_MESSAGE = "Choose an action:\n• Migrate - Preserve your data and update the asset\n• Delete & Recreate - Create a fresh asset with default values\n• Cancel - Keep the old asset (some features may not work)";

        // Buttons
        private const string MIGRATE_BUTTON = "Migrate";
        private const string DELETE_RECREATE_BUTTON = "Delete & Recreate";
        private const string CANCEL_BUTTON = "Cancel";

        // Confirm Delete Dialog
        private const string CONFIRM_DELETE_TITLE = "Confirm Delete";
        private const string CONFIRM_DELETE_MESSAGE = "Are you sure you want to delete and recreate the settings?\n\nAll data will be permanently lost!";
        private const string CONFIRM_DELETE_OK = "Delete & Recreate";
        private const string CONFIRM_DELETE_CANCEL = "Cancel";

        private Vector2 _scrollPosition;

        public event Action OnMigrationComplete;

        public void Show(PayClientSettings settings) {
            _settings = settings;

            titleContent = new GUIContent(WINDOW_TITLE);
            minSize = new Vector2(400, 300);

            ShowModal();
        }

        private void OnGUI() {
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(HEADER_MESSAGE, MessageType.Warning);

            EditorGUILayout.Space(10);

            using (var scope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                EditorGUILayout.LabelField(VERSION_INFO_LABEL, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(string.Format(CURRENT_VERSION_LABEL, _settings?.schemeVersion ?? 0));
                EditorGUILayout.LabelField(string.Format(TARGET_VERSION_LABEL, PayClientSettings.CURRENT_SCHEME_VERSION));
            }

            EditorGUILayout.Space(10);

            DrawDataToPreserveSection();

            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(MIGRATION_HELP_MESSAGE, MessageType.Info);

            EditorGUILayout.Space(10);

            GUILayout.FlexibleSpace();

            using (new EditorGUILayout.HorizontalScope()) {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(MIGRATE_BUTTON, GUILayout.Width(100))) {
                    MigrateSettings();
                }

                if (GUILayout.Button(DELETE_RECREATE_BUTTON, GUILayout.Width(120))) {
                    if (EditorUtility.DisplayDialog(CONFIRM_DELETE_TITLE, CONFIRM_DELETE_MESSAGE, CONFIRM_DELETE_OK, CONFIRM_DELETE_CANCEL)) {
                        DeleteAndRecreate();
                    }
                }

                if (GUILayout.Button(CANCEL_BUTTON, GUILayout.Width(80))) {
                    Close();
                }
            }

            EditorGUILayout.Space(10);
        }

        private void DrawDataToPreserveSection() {
            using (var scope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                EditorGUILayout.LabelField(DATA_TO_PRESERVE_LABEL, EditorStyles.boldLabel);

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(150));

                EditorGUI.BeginDisabledGroup(true);

                // Динамически отображаем все публичные поля
                if (_settings != null) {
                    var fields = typeof(PayClientSettings)
                        .GetFields(BindingFlags.Public | BindingFlags.Instance)
                        .Where(f => !f.IsInitOnly)
                        .Where(f => f.GetCustomAttribute<HideInInspector>() == null);

                    foreach (var field in fields) {
                        var value = field.GetValue(_settings);
                        string displayValue = value?.ToString() ?? "";
                        EditorGUILayout.TextField(field.Name, displayValue);
                    }
                }

                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndScrollView();
            }
        }

        private void MigrateSettings() {
            if (_settings == null) {
                Debug.LogError("PayClientSettingsMigrationWindow: Settings is null");
                Close();
                return;
            }
            
            var migrationData = new PayClientSettingsMigrationData(_settings);

            PayClientSettingsMigrations.RunMigrations(
                migrationData,
                _settings.schemeVersion,
                PayClientSettings.CURRENT_SCHEME_VERSION
            );

            string assetPath = AssetDatabase.GetAssetPath(_settings);

            // Удаляем старый ассет
            AssetDatabase.DeleteAsset(assetPath);

            var newSettings = ScriptableObject.CreateInstance<PayClientSettings>();

            migrationData.ApplyTo(newSettings);
            newSettings.schemeVersion = PayClientSettings.CURRENT_SCHEME_VERSION;

            string directory = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            AssetDatabase.CreateAsset(newSettings, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = newSettings;

            Debug.Log($"PayClientSettings migrated from version {_settings.schemeVersion} to {PayClientSettings.CURRENT_SCHEME_VERSION}.");

            OnMigrationComplete?.Invoke();
            Close();
        }

        private void DeleteAndRecreate() {
            if (_settings == null) {
                Debug.LogError("PayClientSettingsMigrationWindow: Settings is null");
                Close();
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(_settings);

            AssetDatabase.DeleteAsset(assetPath);

            var newSettings = ScriptableObject.CreateInstance<PayClientSettings>();
            newSettings.schemeVersion = PayClientSettings.CURRENT_SCHEME_VERSION;

            string directory = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            AssetDatabase.CreateAsset(newSettings, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = newSettings;

            Debug.Log("PayClientSettings recreated with default values.");

            OnMigrationComplete?.Invoke();
            Close();
        }

    }
}
#endif
