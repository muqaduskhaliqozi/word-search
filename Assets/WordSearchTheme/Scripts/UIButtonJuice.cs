using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Springy press / release scale for any UI element, plus optional idle "breathing".
/// </summary>
[DisallowMultipleComponent]
public class UIButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
{
    [SerializeField] private Transform target;
    [SerializeField] private float pressedScale = 0.88f;
    [SerializeField] private float stiffness = 420f;
    [SerializeField] private float damping = 18f;
    [SerializeField] private bool playClickSound = true;

    [Header("Idle Breathing")]
    [SerializeField] private bool idlePulse = false;
    [SerializeField] private float pulseAmount = 0.05f;
    [SerializeField] private float pulseSpeed = 2.2f;

    private Selectable selectable;
    private Vector3 baseScale = Vector3.one;
    private float current = 1f;
    private float velocity;
    private bool pressed;
    private bool inside;
    private float phase;

    public void SetIdlePulse(bool on, float amount = 0.05f)
    {
        idlePulse = on;
        pulseAmount = amount;
    }

    private void Awake()
    {
        if (target == null) target = transform;
        // Always spring around scale 1. (Reading localScale here is unsafe: an intro animation such as
        // UIPopIn may already have shrunk the object, which left buttons stuck small.)
        baseScale = Vector3.one;
        selectable = GetComponent<Selectable>();
        phase = Random.value * 10f;
    }

    private void OnEnable()
    {
        current = 1f;
        velocity = 0f;
        pressed = false;
        if (target != null) target.localScale = baseScale;
    }

    private bool Interactable => selectable == null || selectable.IsInteractable();

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!Interactable) return;
        pressed = true;
        inside = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (pressed && inside && Interactable)
        {
            velocity += 6f; // release kick -> overshoot
            if (playClickSound) SfxPlayer.Play(SfxPlayer.Sfx.Click);
        }
        pressed = false;
    }

    public void OnPointerEnter(PointerEventData eventData) { inside = true; }
    public void OnPointerExit(PointerEventData eventData) { inside = false; }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        float goal = (pressed && inside) ? pressedScale : 1f;

        velocity += (goal - current) * stiffness * dt;
        velocity *= Mathf.Exp(-damping * dt);
        current += velocity * dt;

        float pulse = 1f;
        if (idlePulse && !pressed)
        {
            phase += dt * pulseSpeed;
            pulse = 1f + Mathf.Sin(phase) * pulseAmount;
        }

        target.localScale = baseScale * (current * pulse);
    }
}
