using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Self-contained sound effects. Clips are synthesised at runtime, so no audio files are needed.
/// Drop real clips into the overrides array later if you want custom sounds.
/// </summary>
public class SfxPlayer : MonoBehaviour
{
    public enum Sfx { Click, Select, Found, Wrong, Complete, Star, Popup, Hint, Shuffle }

    private const string SoundKey = "WS_Sound";
    private const int Rate = 44100;

    private static SfxPlayer instance;
    private readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
    private AudioSource source;

    public static bool SoundOn
    {
        get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
        set { PlayerPrefs.SetInt(SoundKey, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static void Play(Sfx sfx, float pitch = 1f, float volume = 1f)
    {
        if (!SoundOn || !Application.isPlaying) return;
        if (instance == null)
        {
            GameObject go = new GameObject("[SfxPlayer]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SfxPlayer>();
        }
        instance.PlayInternal(sfx, pitch, volume);
    }

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    private void PlayInternal(Sfx sfx, float pitch, float volume)
    {
        if (!clips.TryGetValue(sfx, out AudioClip clip))
        {
            clip = Build(sfx);
            clips[sfx] = clip;
        }
        source.pitch = pitch;
        source.PlayOneShot(clip, volume);
    }

    // ------------------------------------------------------------------ synthesis
    private static AudioClip Build(Sfx sfx)
    {
        switch (sfx)
        {
            case Sfx.Click: return Make("click", 0.08f, (t, i) => Tone(t, 900f - t * 4000f) * Env(t, 0.002f, 0.08f) * 0.5f);
            case Sfx.Select: return Make("select", 0.07f, (t, i) => Tone(t, 1200f) * Env(t, 0.002f, 0.07f) * 0.35f);
            case Sfx.Found: return Make("found", 0.45f, (t, i) => Chime(t, 784f, 0f) + Chime(t, 1175f, 0.09f));
            case Sfx.Wrong: return Make("wrong", 0.28f, (t, i) => Square(t, 220f - t * 300f) * Env(t, 0.005f, 0.28f) * 0.18f);
            case Sfx.Complete:
                return Make("complete", 1.1f, (t, i) =>
                    Chime(t, 523f, 0f) + Chime(t, 659f, 0.12f) + Chime(t, 784f, 0.24f) + Chime(t, 1047f, 0.36f) * 1.2f);
            case Sfx.Star: return Make("star", 0.5f, (t, i) => Chime(t, 1319f, 0f) + Chime(t, 1976f, 0.02f) * 0.5f);
            case Sfx.Popup: return Make("popup", 0.25f, (t, i) => Tone(t, 300f + t * 1800f) * Env(t, 0.01f, 0.25f) * 0.35f);
            case Sfx.Hint: return Make("hint", 0.6f, (t, i) => Chime(t, 1568f, 0f) + Chime(t, 2093f, 0.08f) + Chime(t, 2637f, 0.16f));
            case Sfx.Shuffle: return Make("shuffle", 0.35f, (t, i) => Noise(i) * Env(t, 0.03f, 0.35f) * 0.15f + Tone(t, 500f + Mathf.Sin(t * 60f) * 200f) * Env(t, 0.01f, 0.3f) * 0.15f);
        }
        return Make("none", 0.01f, (t, i) => 0f);
    }

    private delegate float Wave(float t, int i);

    private static AudioClip Make(string name, float seconds, Wave wave)
    {
        int n = Mathf.CeilToInt(seconds * Rate);
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            data[i] = Mathf.Clamp(wave(t, i), -1f, 1f);
        }
        AudioClip clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static float Tone(float t, float f) => Mathf.Sin(2f * Mathf.PI * f * t);
    private static float Square(float t, float f) => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * t));

    private static uint seed = 12345;
    private static float Noise(int i)
    {
        seed = seed * 1664525u + 1013904223u;
        return (seed >> 9) / 4194304f - 1f;
    }

    private static float Env(float t, float attack, float length)
    {
        if (t < attack) return t / attack;
        float k = Mathf.Clamp01((t - attack) / Mathf.Max(0.0001f, length - attack));
        return (1f - k) * (1f - k);
    }

    private static float Chime(float t, float f, float start)
    {
        float lt = t - start;
        if (lt < 0f) return 0f;
        float env = Mathf.Exp(-lt * 7f) * Mathf.Clamp01(lt / 0.004f);
        return (Tone(lt, f) * 0.6f + Tone(lt, f * 2f) * 0.2f + Tone(lt, f * 3f) * 0.08f) * env * 0.35f;
    }
}
