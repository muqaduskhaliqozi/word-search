using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Tiny dependency-free tween helper (coroutine based, unscaled time).
/// </summary>
public static class Tween
{
    public static Coroutine Run(MonoBehaviour host, float duration, Action<float> step, Action done = null, float delay = 0f)
    {
        if (host == null || !host.isActiveAndEnabled)
        {
            step?.Invoke(1f);
            done?.Invoke();
            return null;
        }
        return host.StartCoroutine(Routine(duration, step, done, delay));
    }

    private static IEnumerator Routine(float duration, Action<float> step, Action done, float delay)
    {
        if (delay > 0f)
        {
            float d = 0f;
            while (d < delay)
            {
                d += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        float t = 0f;
        duration = Mathf.Max(0.0001f, duration);
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            step?.Invoke(Mathf.Clamp01(t / duration));
            yield return null;
        }
        step?.Invoke(1f);
        done?.Invoke();
    }

    // ------------------------------------------------------------ easing
    public static float OutCubic(float t) { t = 1f - t; return 1f - t * t * t; }
    public static float InCubic(float t) { return t * t * t; }
    public static float InOutSine(float t) { return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f; }

    public static float OutBack(float t, float s = 1.70158f)
    {
        t -= 1f;
        return t * t * ((s + 1f) * t + s) + 1f;
    }

    public static float OutElastic(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        const float c4 = (2f * Mathf.PI) / 3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }

    /// <summary>0 -> 1 -> 0 bump, nice for "punch" effects.</summary>
    public static float Punch(float t)
    {
        return Mathf.Sin(t * Mathf.PI) * (1f - t * 0.35f);
    }
}
