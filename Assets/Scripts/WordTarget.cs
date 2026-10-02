using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WordTarget : MonoBehaviour
{
    [Header("Word Settings")]
    [SerializeField] private string targetWord;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI wordText;
    [SerializeField] private GameObject completedVisual;

    [Header("Chip Style (optional)")]
    [SerializeField] private Image chipImage;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite foundSprite;
    [SerializeField] private Color normalTextColor = new Color(0.07f, 0.11f, 0.25f, 1f);
    [SerializeField] private Color foundTextColor = new Color(1f, 1f, 1f, 1f);

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
        bool becameCompleted = completed && !IsCompleted;
        IsCompleted = completed;

        if (completedVisual != null)
        {
            completedVisual.SetActive(completed);
        }

        if (chipImage != null)
        {
            Sprite s = completed ? foundSprite : normalSprite;
            if (s != null) chipImage.sprite = s;
        }

        if (wordText != null && chipImage != null)
        {
            wordText.color = completed ? foundTextColor : normalTextColor;
        }

        if (!completed)
        {
            StopAllCoroutines();
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            if (completedVisual != null) completedVisual.transform.localScale = Vector3.one;
        }
        else if (becameCompleted && isActiveAndEnabled && Application.isPlaying)
        {
            StopAllCoroutines();
            StartCoroutine(FoundRoutine());
        }
    }

    /// <summary>Little "look at me" wiggle (used by hints / shuffle).</summary>
    public void PlayNudge(float delay = 0f)
    {
        if (!isActiveAndEnabled || !Application.isPlaying) return;
        StartCoroutine(NudgeRoutine(delay));
    }

    private IEnumerator FoundRoutine()
    {
        Transform strike = completedVisual != null ? completedVisual.transform : null;
        float t = 0f;
        const float dur = 0.5f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            transform.localScale = Vector3.one * (1f + Tween.Punch(k) * 0.3f);
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI * 3f) * 6f * (1f - k));
            if (strike != null) strike.localScale = new Vector3(Tween.OutCubic(k), 1f, 1f);
            yield return null;
        }
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
    }

    private IEnumerator NudgeRoutine(float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        float t = 0f;
        const float dur = 0.35f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            transform.localScale = Vector3.one * (1f + Tween.Punch(k) * 0.15f);
            yield return null;
        }
        transform.localScale = Vector3.one;
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
