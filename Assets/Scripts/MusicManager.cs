using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Plays the game's music and cross-fades between tracks.
///
/// ZERO SETUP: created automatically before the first scene loads and kept across scenes.
/// Tracks come from Resources/MusicLibrary. The background track plays on every scene;
/// gameplay scripts swap in other tracks for moments (cutscenes, puzzle solved, endings).
///
/// Volume is saved in PlayerPrefs and set by a "MusicSlider" on the Settings page. If a
/// scene only has the "SfxSlider", a Music slider is added beside it at runtime.
/// </summary>
public class MusicManager : MonoBehaviour
{
    private const string VolumeKey = "MusicVolume";
    private const float DefaultVolume = 0.5f;
    private const float DefaultFade = 1f;
    private const string SfxSliderName = "SfxSlider";
    private const string MusicSliderName = "MusicSlider";

    private static MusicManager s_Instance;

    private MusicLibrary library;
    private readonly AudioSource[] sources = new AudioSource[2];
    private readonly float[] weights = new float[2];
    private int activeSource;
    private float volume = DefaultVolume;
    private Coroutine fadeRoutine;
    private Coroutine stingerRoutine;

    // The scene's own track, and where it was when a moment interrupted it
    private AudioClip ambientClip;
    private float ambientResumeTime;
    private bool overrideActive;

    public static MusicLibrary Library => s_Instance != null ? s_Instance.library : null;
    public static float Volume => s_Instance != null ? s_Instance.volume : PlayerPrefs.GetFloat(VolumeKey, DefaultVolume);

    // ------------------------------------------------------------------
    // Auto bootstrap
    // ------------------------------------------------------------------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (s_Instance != null) return;
        GameObject host = new GameObject("MusicManager (auto)");
        DontDestroyOnLoad(host);
        host.AddComponent<MusicManager>();
    }

    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        s_Instance = this;

        library = Resources.Load<MusicLibrary>("MusicLibrary");
        if (library == null) Debug.LogWarning("[Music] Resources/MusicLibrary not found - no music will play.");

        volume = PlayerPrefs.GetFloat(VolumeKey, DefaultVolume);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f; // 2D: same volume wherever the camera is
            source.ignoreListenerPause = true;
            sources[i] = source;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (s_Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        s_Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return;

        // An ending scene keeps the ending track started on the rooftop
        if (!scene.name.StartsWith("Ending"))
        {
            CancelStinger();
            overrideActive = false;
            SetAmbient(library != null ? library.background : null);
        }

        EnsureMusicSliders();
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    /// <summary>Sets the music volume (0-1) and saves it.</summary>
    public static void SetVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumeKey, value);
        if (s_Instance == null) return;
        s_Instance.volume = value;
        s_Instance.ApplyVolumes();
    }

    /// <summary>Plays a track until <see cref="StopMoment"/>, e.g. for a cutscene.</summary>
    public static void PlayMoment(AudioClip clip, float fade = DefaultFade)
    {
        if (s_Instance == null || clip == null) return;
        s_Instance.CancelStinger();
        s_Instance.StartOverride(clip, fade);
    }

    /// <summary>Plays a track for a few seconds, then returns to the background.</summary>
    public static void PlayStinger(AudioClip clip, float seconds, float fade = DefaultFade)
    {
        if (s_Instance == null || clip == null) return;
        s_Instance.CancelStinger();
        s_Instance.StartOverride(clip, fade);
        s_Instance.stingerRoutine = s_Instance.StartCoroutine(s_Instance.EndStingerAfter(seconds, fade));
    }

    /// <summary>Ends a cutscene or other moment and fades back to the background.</summary>
    public static void StopMoment(float fade = DefaultFade)
    {
        if (s_Instance == null) return;
        s_Instance.CancelStinger();
        s_Instance.ReturnToAmbient(fade);
    }

    /// <summary>Fades the music out completely.</summary>
    public static void FadeOut(float fade = DefaultFade)
    {
        if (s_Instance == null) return;
        s_Instance.CancelStinger();
        s_Instance.overrideActive = true; // stays silent until something else plays
        s_Instance.CrossFadeTo(null, fade, 0f);
    }

    // Shortcuts for the moments used around the game
    public static void PlayCutscene(bool shortVersion = false)
    {
        if (Library == null) return;
        PlayMoment(shortVersion && Library.shortCutscene != null ? Library.shortCutscene : Library.cutscene);
    }

    public static void PlayPuzzleSolved()
    {
        if (Library == null) return;
        PlayStinger(Library.puzzleSolved, Library.puzzleSolvedSeconds);
    }

    public static void PlayFinalChoice()
    {
        if (Library == null) return;
        PlayMoment(Library.finalChoice);
    }

    public static void PlayEnding(bool truth, float fade = DefaultFade)
    {
        if (Library == null) return;
        PlayMoment(truth ? Library.truthEnding : Library.controlEnding, fade);
    }

    // ------------------------------------------------------------------
    // Track switching
    // ------------------------------------------------------------------

    private void SetAmbient(AudioClip clip)
    {
        if (ambientClip != clip) ambientResumeTime = 0f;
        ambientClip = clip;
        if (!overrideActive) CrossFadeTo(clip, DefaultFade, ambientResumeTime);
    }

    private void StartOverride(AudioClip clip, float fade)
    {
        // Remember where the background was so it picks up from there afterwards
        AudioSource current = sources[activeSource];
        if (!overrideActive && current.clip == ambientClip && current.isPlaying)
            ambientResumeTime = current.time;

        overrideActive = true;
        CrossFadeTo(clip, fade, 0f);
    }

    private void ReturnToAmbient(float fade)
    {
        if (!overrideActive) return;
        overrideActive = false;
        CrossFadeTo(ambientClip, fade, ambientResumeTime);
    }

    private IEnumerator EndStingerAfter(float seconds, float fade)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, seconds - fade));
        stingerRoutine = null;
        ReturnToAmbient(fade);
    }

    private void CancelStinger()
    {
        if (stingerRoutine == null) return;
        StopCoroutine(stingerRoutine);
        stingerRoutine = null;
    }

    private void CrossFadeTo(AudioClip clip, float fade, float startTime)
    {
        AudioSource current = sources[activeSource];
        if (clip != null && current.clip == clip && current.isPlaying && weights[activeSource] > 0f)
        {
            // Already playing - just make sure it is fading in, not out
            StartFade(activeSource, fade);
            return;
        }

        int next = clip == null ? activeSource : 1 - activeSource;
        if (clip != null)
        {
            AudioSource source = sources[next];
            source.clip = clip;
            source.time = startTime < clip.length ? startTime : 0f;
            weights[next] = 0f;
            source.volume = 0f;
            source.Play();
            activeSource = next;
            StartFade(next, fade);
        }
        else
        {
            StartFade(-1, fade);
        }
    }

    /// <summary>Fades <paramref name="target"/> up to full and every other source down (-1: all down).</summary>
    private void StartFade(int target, float fade)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(Fade(target, fade));
    }

    private IEnumerator Fade(int target, float duration)
    {
        float[] start = (float[])weights.Clone();
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            for (int i = 0; i < weights.Length; i++)
                weights[i] = Mathf.Lerp(start[i], i == target ? 1f : 0f, k);
            ApplyVolumes();
            yield return null;
        }

        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = i == target ? 1f : 0f;
            if (i != target) sources[i].Stop();
        }
        ApplyVolumes();
        fadeRoutine = null;
    }

    private void ApplyVolumes()
    {
        for (int i = 0; i < sources.Length; i++)
            sources[i].volume = weights[i] * volume;
    }

    // ------------------------------------------------------------------
    // Settings slider
    // ------------------------------------------------------------------

    /// <summary>Hooks up every Music slider, adding one next to the SFX slider where missing.</summary>
    private void EnsureMusicSliders()
    {
        foreach (Slider slider in FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (slider.name == MusicSliderName)
            {
                BindSlider(slider);
            }
            else if (slider.name == SfxSliderName && slider.transform.parent != null
                     && slider.transform.parent.Find(MusicSliderName) == null)
            {
                BindSlider(CreateMusicSlider(slider));
            }
        }
    }

    private static void BindSlider(Slider slider)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(Volume);
        slider.onValueChanged.RemoveListener(SetVolume);
        slider.onValueChanged.AddListener(SetVolume);
    }

    private static Slider CreateMusicSlider(Slider sfxSlider)
    {
        RectTransform sfxRect = (RectTransform)sfxSlider.transform;
        Transform page = sfxRect.parent;

        Slider musicSlider = Instantiate(sfxSlider, page);
        musicSlider.name = MusicSliderName;
        musicSlider.onValueChanged = new Slider.SliderEvent(); // drop the SFX slider's listeners
        RectTransform musicRect = (RectTransform)musicSlider.transform;
        musicRect.SetSiblingIndex(sfxRect.GetSiblingIndex());

        // Music on top, SFX one row below, each with a label on its left
        float row = Mathf.Max(sfxRect.rect.height, 20f) * 3f;
        musicRect.anchoredPosition = sfxRect.anchoredPosition;
        sfxRect.anchoredPosition -= new Vector2(0f, row);

        TMP_Text style = page.GetComponentInChildren<TMP_Text>(true);
        AddLabel("MusicLabel", "Music", musicRect, style);
        if (page.Find("SfxLabel") == null) AddLabel("SfxLabel", "Sound Effects", sfxRect, style);
        return musicSlider;
    }

    private static void AddLabel(string name, string text, RectTransform slider, TMP_Text style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(slider.parent, false);
        rect.anchorMin = slider.anchorMin;
        rect.anchorMax = slider.anchorMax;
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(260f, 40f);
        rect.anchoredPosition = slider.anchoredPosition
            - new Vector2(slider.rect.width * slider.pivot.x + 20f, 0f);

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 28;
        label.alignment = TextAlignmentOptions.Right;
        label.color = style != null ? style.color : Color.white;
        if (style != null) label.font = style.font;
        label.raycastTarget = false;
    }
}
