using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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

    [Header("Animation")]
    [SerializeField] private float selectedScale = 1.1f;
    [SerializeField] private Color normalLetterColor = new Color(0.07f, 0.11f, 0.25f, 1f);
    [SerializeField] private Color selectedLetterColor = new Color(0.07f, 0.11f, 0.25f, 1f);

    public string Letter => letter;
    public int Row => row;
    public int Column => column;

    public bool IsSelected { get; private set; }
    public bool IsFound { get; private set; }
    public bool IsHinted { get; private set; }

    private WordSearchLevel levelController;

    // --- animation state (spring + one-shot effects) ---
    private float scale = 1f;
    private float scaleVel;
    private float shakeTime;
    private float bounceDelay = -1f;
    private float bounceTime = -1f;
    private float introDelay = -1f;
    private float introTime = -1f;
    private Graphic hintGraphic;
    private float hintPhase;

    private void Awake()
    {
        UpdateVisualText();
        if (hintHighlight != null) hintGraphic = hintHighlight.GetComponent<Graphic>();
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
        if (selected && !IsSelected)
        {
            scaleVel += 7f; // little pop when the drag reaches this tile
        }

        IsSelected = selected;
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(selected);
        }
        if (letterText != null)
        {
            letterText.color = selected ? selectedLetterColor : normalLetterColor;
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
        hintPhase = 0f;
    }

    public void ResetState()
    {
        SetSelected(false);
        SetFound(false);
        SetHint(false);
        scale = 1f;
        scaleVel = 0f;
        shakeTime = 0f;
        bounceTime = -1f;
        bounceDelay = -1f;
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
    }

    // ------------------------------------------------------------------ effects

    /// <summary>Scale-in when a level starts.</summary>
    public void PlayIntro(float delay)
    {
        if (!Application.isPlaying) return;
        introDelay = delay;
        introTime = 0f;
        transform.localScale = Vector3.zero;
    }

    /// <summary>Happy hop used when a word is found (call with increasing delay for a wave).</summary>
    public void PlayFoundBounce(float delay)
    {
        bounceDelay = delay;
        bounceTime = 0f;
    }

    public void PlayShake()
    {
        shakeTime = 0.35f;
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

        // intro pop
        if (introTime >= 0f)
        {
            if (introDelay > 0f)
            {
                introDelay -= dt;
                transform.localScale = Vector3.zero;
                return;
            }
            introTime += dt;
            float k = Mathf.Clamp01(introTime / 0.35f);
            transform.localScale = Vector3.one * Tween.OutBack(k, 2f);
            if (k >= 1f) introTime = -1f;
            return;
        }

        // spring toward the selected / idle scale
        float goal = IsSelected ? selectedScale : 1f;
        scaleVel += (goal - scale) * 380f * dt;
        scaleVel *= Mathf.Exp(-16f * dt);
        scale += scaleVel * dt;

        float extra = 1f;
        if (bounceTime >= 0f)
        {
            if (bounceDelay > 0f) bounceDelay -= dt;
            else
            {
                bounceTime += dt;
                float k = Mathf.Clamp01(bounceTime / 0.4f);
                extra += Tween.Punch(k) * 0.28f;
                if (k >= 1f) bounceTime = -1f;
            }
        }

        transform.localScale = Vector3.one * (scale * extra);

        float rot = 0f;
        if (shakeTime > 0f)
        {
            shakeTime -= dt;
            rot = Mathf.Sin(shakeTime * 60f) * 10f * (shakeTime / 0.35f);
        }
        transform.localRotation = Quaternion.Euler(0f, 0f, rot);

        // pulsing hint
        if (IsHinted && hintGraphic != null)
        {
            hintPhase += dt * 5f;
            Color c = hintGraphic.color;
            c.a = 0.45f + Mathf.Sin(hintPhase) * 0.35f;
            hintGraphic.color = c;
            transform.localScale = Vector3.one * (scale * extra * (1f + Mathf.Sin(hintPhase) * 0.05f));
        }
    }

    // ------------------------------------------------------------------ input

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
