using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WordSearchLevel))]
public class WordSearchLevelEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WordSearchLevel level = (WordSearchLevel)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Editor Setup Helper Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Auto-Find & Assign Children (Tiles & Target Words)"))
        {
            Undo.RecordObject(level, "Auto Assign Level References");

            // Find all LetterTile components in children
            LetterTile[] tiles = level.GetComponentsInChildren<LetterTile>(true);
            var tilesField = typeof(WordSearchLevel).GetField("letterTiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (tilesField != null)
            {
                tilesField.SetValue(level, new System.Collections.Generic.List<LetterTile>(tiles));
            }

            // Find all WordTarget components in children
            WordTarget[] targets = level.GetComponentsInChildren<WordTarget>(true);
            var targetsField = typeof(WordSearchLevel).GetField("targetWords", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (targetsField != null)
            {
                targetsField.SetValue(level, new System.Collections.Generic.List<WordTarget>(targets));
            }

            EditorUtility.SetDirty(level);
            Debug.Log($"[WordSearchLevelEditor] Found {tiles.Length} LetterTiles and {targets.Length} WordTargets.");
        }
    }
}
