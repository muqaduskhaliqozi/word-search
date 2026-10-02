using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class LetterTile : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
{
    [Header("Tile Settings")]
    [SerializeField] private string letter;
    [SerializeField] private int row;
    [SerializeField] private int column;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI letterText;

    [Header("Highlight Visuals")]
    [SerializeField] private GameObject selectionHighlight;
    [SerializeField] private GameObject foundHighlight;
    [SerializeField] private GameObject hintHighlight;

    public string Letter => letter;
    public int Row => row;
    public int Column => column;

    public bool IsSelected { get; private set; }
    public bool IsFound { get; private set; }
    public bool IsHinted { get; private set; }

    private WordSearchLevel levelController;

    private void Awake()
    {
        UpdateVisualText();
    }

    public void InitTile(WordSearchLevel level)
    {
        levelController = level;
    }

    public void SetLetter(string newLetter, int r, int c)
    {
        letter = newLetter;
        row = r;
        column = c;
        UpdateVisualText();
    }

    private void UpdateVisualText()
    {
        if (letterText != null && !string.IsNullOrEmpty(letter))
        {
            letterText.text = letter.ToUpper();
        }
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(selected);
        }
    }

    public void SetFound(bool found)
    {
        IsFound = found;
        if (foundHighlight != null)
        {
            foundHighlight.SetActive(found);
        }

        if (found)
        {
            SetSelected(false);
            SetHint(false);
        }
    }

    public void SetHint(bool hinted)
    {
        IsHinted = hinted;
        if (hintHighlight != null)
        {
            hintHighlight.SetActive(hinted);
        }
    }

    public void ResetState()
    {
        SetSelected(false);
        SetFound(false);
        SetHint(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (levelController != null)
        {
            levelController.OnTilePointerDown(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (levelController != null)
        {
            levelController.OnTilePointerEnter(this);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (levelController != null)
        {
            levelController.OnTilePointerUp(this);
        }
    }

    private void OnValidate()
    {
        UpdateVisualText();
        gameObject.name = $"Tile_{row}_{column}_{(string.IsNullOrEmpty(letter) ? "?" : letter)}";
    }
}
