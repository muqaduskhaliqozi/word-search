using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

public class WordTarget : MonoBehaviour
{
    [Header("Word Settings")]
    [SerializeField] private string targetWord;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI wordText;
    [SerializeField] private GameObject completedVisual;

    [Header("Solution Assignment")]
    [SerializeField] private List<LetterTile> solutionTiles = new List<LetterTile>();

    public string TargetWord => targetWord;
    public bool IsCompleted { get; private set; }
    public List<LetterTile> SolutionTiles => solutionTiles;

    private void Awake()
    {
        UpdateVisualText();
        SetCompleted(false);
    }

    private void UpdateVisualText()
    {
        if (wordText != null && !string.IsNullOrEmpty(targetWord))
        {
            wordText.text = targetWord.ToUpper();
        }
    }

    public void SetCompleted(bool completed)
    {
        IsCompleted = completed;
        if (completedVisual != null)
        {
            completedVisual.SetActive(completed);
        }
    }

    public bool ValidatePath(List<LetterTile> selectedTiles)
    {
        if (IsCompleted) return false;
        if (selectedTiles == null || selectedTiles.Count == 0) return false;

        // 1. Build string from selected tiles
        StringBuilder sb = new StringBuilder();
        foreach (var tile in selectedTiles)
        {
            if (tile != null && !string.IsNullOrEmpty(tile.Letter))
            {
                sb.Append(tile.Letter.ToUpper());
            }
        }
        string selectedStr = sb.ToString();

        // 2. Build reverse string
        char[] charArr = selectedStr.ToCharArray();
        Array.Reverse(charArr);
        string reversedStr = new string(charArr);

        string targetUpper = !string.IsNullOrEmpty(targetWord) ? targetWord.ToUpper() : "";

        // 3. String Match (Forward or Reverse)
        if (!string.IsNullOrEmpty(targetUpper) && (selectedStr == targetUpper || reversedStr == targetUpper))
        {
            return true;
        }

        // 4. Exact solutionTiles Match (Forward or Reverse)
        if (solutionTiles != null && solutionTiles.Count > 0 && solutionTiles.Count == selectedTiles.Count)
        {
            bool forwardMatch = true;
            for (int i = 0; i < solutionTiles.Count; i++)
            {
                if (selectedTiles[i] != solutionTiles[i])
                {
                    forwardMatch = false;
                    break;
                }
            }
            if (forwardMatch) return true;

            bool reverseMatch = true;
            int count = solutionTiles.Count;
            for (int i = 0; i < count; i++)
            {
                if (selectedTiles[i] != solutionTiles[count - 1 - i])
                {
                    reverseMatch = false;
                    break;
                }
            }
            if (reverseMatch) return true;
        }

        return false;
    }

    private void OnValidate()
    {
        UpdateVisualText();
        gameObject.name = $"WordTarget_{(string.IsNullOrEmpty(targetWord) ? "Empty" : targetWord)}";
    }
}
