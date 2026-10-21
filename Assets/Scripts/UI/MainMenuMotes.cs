using UnityEngine;
using UnityEngine.UI;

/// <summary>Small squares of light drifting upward behind the start menu, and a slow glow pulse.</summary>
public class MainMenuMotes : MonoBehaviour
{
    public RawImage glow;

    private const int Count = 40;
    private RectTransform[] motes;
    private float[] speeds;
    private float[] phases;
    private Image[] images;

    private void Start()
    {
        motes = new RectTransform[Count];
        speeds = new float[Count];
        phases = new float[Count];
        images = new Image[Count];
        for (int i = 0; i < Count; i++)
        {
            GameObject go = new GameObject("Mote", typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(Random.value, Random.value);
            float size = Random.Range(4f, 10f);
            rect.sizeDelta = new Vector2(size, size);
            images[i] = go.AddComponent<Image>();
            images[i].raycastTarget = false;
            motes[i] = rect;
            speeds[i] = Random.Range(0.01f, 0.035f);
            phases[i] = Random.value * Mathf.PI * 2f;
        }
    }

    private void Update()
    {
        float t = Time.unscaledTime;
        for (int i = 0; i < Count; i++)
        {
            Vector2 a = motes[i].anchorMin;
            a.y += speeds[i] * Time.unscaledDeltaTime;
            a.x += Mathf.Sin(t * 0.6f + phases[i]) * 0.004f * Time.unscaledDeltaTime;
            if (a.y > 1.05f) { a.y = -0.05f; a.x = Random.value; }
            motes[i].anchorMin = motes[i].anchorMax = a;

            // Twinkle, and fade near the top and bottom of the screen
            float edge = Mathf.Clamp01(Mathf.Min(a.y + 0.05f, 1.05f - a.y) * 5f);
            float twinkle = 0.55f + 0.45f * Mathf.Sin(t * 2f + phases[i]);
            images[i].color = new Color(1f, 0.85f, 0.5f, 0.75f * edge * twinkle);
        }

        if (glow != null)
        {
            Color c = glow.color;
            c.a = 0.28f + 0.06f * Mathf.Sin(t * 0.8f);
            glow.color = c;
        }
    }
}
