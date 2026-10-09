using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Level select popup: green = completed (tick), orange = current/next (pulsing), grey = locked (padlock).
/// Buttons are created at runtime from a hidden template.
/// </summary>
public class LevelSelectPanel : MonoBehaviour
{
    [Header("Levels")]
    [SerializeField] private int levelCount = 16;
    [SerializeField] private string[] levelTitles;
    [SerializeField] private string gameSceneName = "GamePlay";

    [Header("UI")]
    [SerializeField] private RectTransform content;
    [SerializeField] private GameObject buttonTemplate;
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Button closeButton;

    [Header("Look")]
    [SerializeField] private Sprite completedSprite;
    [SerializeField] private Sprite currentSprite;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite checkIcon;
    [SerializeField] private Sprite lockIcon;

    private readonly List<GameObject> buttons = new List<GameObject>();
    private PopupAnimator popup;
    private bool loading;

    public void Configure(int count, string[] titles)
    {
        levelCount = count;
        levelTitles = titles;
    }

    private void Awake()
    {
        popup = GetComponent<PopupAnimator>();
        if (buttonTemplate != null) buttonTemplate.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        if (popup != null) popup.Close();
        else gameObject.SetActive(false);
    }

    private int Count
    {
        get
        {
            int saved = LevelProgress.LevelCount;
            return saved > 0 ? saved : levelCount;
        }
    }

    private void OnEnable()
    {
        loading = false;
        Refresh();
        StartCoroutine(IntroRoutine());
    }

    private void Refresh()
    {
        if (content == null || buttonTemplate == null) return;

        int count = Count;
        int unlocked = LevelProgress.Unlocked;

        while (buttons.Count < count)
        {
            GameObject b = Instantiate(buttonTemplate, content);
            b.name = "Level " + (buttons.Count + 1);
            int index = buttons.Count;
            Button btn = b.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => OnLevelClicked(index));
            buttons.Add(b);
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            GameObject b = buttons[i];
            b.SetActive(i < count);
            if (i >= count) continue;

            bool completed = i < unlocked;
            bool locked = i > unlocked;

            Image face = Find<Image>(b, "NT_Btn");
            if (face != null) face.sprite = completed ? completedSprite : locked ? lockedSprite : currentSprite;

            TMP_Text number = Find<TMP_Text>(b, "NT_Number");
            if (number != null)
            {
                number.text = (i + 1).ToString();
                number.color = completed ? Color.white
                    : locked ? new Color(0.62f, 0.55f, 0.46f, 1f)
                    : new Color(0.42f, 0.28f, 0.13f, 1f);
            }

            TMP_Text caption = Find<TMP_Text>(b, "NT_Caption");
            if (caption != null)
            {
                caption.text = locked ? "LOCKED" : (levelTitles != null && i < levelTitles.Length ? levelTitles[i] : "");
                caption.alpha = locked ? 0.6f : 1f;
            }

            Transform badge = b.transform.Find("NT_Badge");
            if (badge != null)
            {
                badge.gameObject.SetActive(completed || locked);
                Image icon = Find<Image>(b, "NT_Badge/NT_BadgeIcon");
                if (icon != null) icon.sprite = completed ? checkIcon : lockIcon;
            }

            UIButtonJuice juice = b.GetComponent<UIButtonJuice>();
            if (juice != null) juice.SetIdlePulse(!completed && !locked, 0.06f);
        }

        if (progressText != null)
        {
            int done = Mathf.Min(unlocked, count);
            progressText.text = $"{done} / {count} COMPLETED";
        }
    }

    private IEnumerator IntroRoutine()
    {
        int count = Mathf.Min(Count, buttons.Count);
        for (int i = 0; i < count; i++)
        {
            StartCoroutine(Pop(buttons[i].transform, 0.2f + i * 0.03f));
        }

        // scroll so the current level is visible
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (scroll != null && count > 0)
        {
            GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
            int cols = grid != null && grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount ? grid.constraintCount : 4;
            int rows = Mathf.CeilToInt(count / (float)cols);
            int row = Mathf.Clamp(LevelProgress.Unlocked, 0, count - 1) / cols;
            scroll.verticalNormalizedPosition = rows <= 1 ? 1f : Mathf.Clamp01(1f - row / (float)(rows - 1));
        }
    }

    private IEnumerator Pop(Transform t, float delay)
    {
        // keep it hidden while waiting (coroutines run after Update, so this wins over UIButtonJuice)
        float wait = 0f;
        while (wait < delay)
        {
            wait += Time.unscaledDeltaTime;
            t.localScale = Vector3.zero;
            yield return null;
        }
        float time = 0f;
        while (time < 0.35f)
        {
            time += Time.unscaledDeltaTime;
            t.localScale = Vector3.one * Tween.OutBack(Mathf.Clamp01(time / 0.35f), 2f);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    private void OnLevelClicked(int index)
    {
        if (loading) return;

        if (LevelProgress.IsLocked(index))
        {
            SfxPlayer.Play(SfxPlayer.Sfx.Wrong);
            StartCoroutine(Shake(buttons[index].transform));
            return;
        }

        loading = true;
        LevelProgress.RequestLevel(index);
        SceneFader.LoadScene(gameSceneName);
    }

    private IEnumerator Shake(Transform t)
    {
        float time = 0f;
        while (time < 0.35f)
        {
            time += Time.unscaledDeltaTime;
            t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 50f) * 10f * (1f - time / 0.35f));
            yield return null;
        }
        t.localRotation = Quaternion.identity;
    }

    private static T Find<T>(GameObject root, string path) where T : Component
    {
        Transform t = root.transform.Find(path);
        return t != null ? t.GetComponent<T>() : null;
    }
}
