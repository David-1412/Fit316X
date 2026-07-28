using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] float fadeDuration = 0.5f;
    [SerializeField] CinemachineCamera vcam;

    CinemachinePositionComposer composer;
    Vector3 originalDamping;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (vcam != null)
        {
            composer = vcam.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachinePositionComposer;
            if (composer != null)
                originalDamping = composer.Damping;
        }
    }

    async Task Fade(float targetTransparency)
    {
        float start = canvasGroup.alpha, t = 0;
        while(t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, targetTransparency, t / fadeDuration);
            await Task.Yield();
        }
        canvasGroup.alpha = targetTransparency;
    }

    public async Task FadeOut()
    {
        await Fade(1); //Fade to black
        SetDamping(Vector3.zero); //turn off damping
    }

    public async Task FadeIn()
    {
        await Fade(0); //Fade to transparent
        SetDamping(originalDamping);
    }

    void SetDamping(Vector3 d)
    {
        if (!composer) return;
        composer.Damping = d;
    }
}
