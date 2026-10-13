using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Runs the whole Floor 5 rooftop sequence:
///   1. Opening cutscene - locks input and walks the player to the overlook point.
///   2. Old Man dialogue - built-in typewriter driver, no dialogue plugin needed.
///   3. Final choice - Truth ending (open the shutters) or Control ending (take the seat).
///
/// Setup (F5 scene):
///   - Add this component to an empty GameObject, e.g. "RooftopSequence".
///   - Assign Overlook Point (an empty Transform where the player stops).
///   - Assign the dialogue panel and its TMP texts (the existing DialogueUI panel works).
///   - Assign the choice panel and its two Buttons.
///   - Everything else (animators, particles, audio, fade) is optional and null-checked.
///   - Add "Ending_Truth" and "Ending_Control" to File > Build Profiles > Scene List.
/// </summary>
public class FinalRooftopSceneManager : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Inspector references
    // ------------------------------------------------------------------

    [Header("Player")]
    [Tooltip("Leave empty to find the persistent object tagged 'Player' at runtime.")]
    [SerializeField] private Transform player;
    [Tooltip("Optional. If set, the player is snapped here before the walk starts.")]
    [SerializeField] private Transform playerStartPoint;
    [Tooltip("Where the player walks to before the Old Man speaks.")]
    [SerializeField] private Transform overlookPoint;
    [SerializeField] private float walkSpeed = 2.5f;
    [Tooltip("Pause between stopping at the overlook and the first line of dialogue.")]
    [SerializeField] private float pauseBeforeDialogue = 0.8f;
    [Tooltip("Bool parameter on the player's Animator. The project's Player.controller uses 'isWalking'.")]
    [SerializeField] private string walkingBoolName = "isWalking";

    [Header("Camera")]
    [Tooltip("Leave empty to use Camera.main.")]
    [SerializeField] private Camera targetCamera;

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [Tooltip("Optional. Shown when a line has finished typing (e.g. a small arrow).")]
    [SerializeField] private GameObject continuePrompt;
    [Tooltip("Optional. Portrait image in the dialogue panel.")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private Sprite oldManPortrait;

    [Header("Dialogue Content")]
    [SerializeField] private string speakerName = "Old Man";
    [SerializeField, TextArea(2, 5)] private string[] oldManLines =
    {
        "Welcome, Scholar. Do you like the view? It is your inheritance. The sun has never disappeared, yet this truth must remain hidden.",
        "One hundred and twenty-three years ago, this valley was choked with smoke and corpses. A people living in a cave will worship whoever holds the match.",
        "I was you once—full of faith and doubt. But someone must tend the machine. Someone must guard the truth. Now, it is your turn. Take my place and maintain order... or destroy the shutters and shatter this world."
    };
    [Tooltip("Characters typed per second.")]
    [SerializeField] private float typingSpeed = 40f;
    [Tooltip("Optional blip played every few characters while typing.")]
    [SerializeField] private AudioSource typingAudioSource;
    [SerializeField] private int charactersPerBlip = 3;

    [Header("Choice UI")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Button truthButton;
    [SerializeField] private Button controlButton;
    [SerializeField] private string truthButtonLabel = "Overload Core & Open Shutters";
    [SerializeField] private string controlButtonLabel = "Take the Overseer's Seat";

    [Header("Truth Ending")]
    [SerializeField] private Animator shutterAnimator;
    [SerializeField] private string openShuttersTrigger = "OpenShutters";
    [SerializeField] private ParticleSystem rubbleParticles;
    [SerializeField] private AudioSource collapseAudio;
    [SerializeField] private float shakeDuration = 3f;
    [SerializeField] private float shakeMagnitude = 0.3f;
    [Tooltip("Extra time after the shake before the fade starts.")]
    [SerializeField] private float truthHoldTime = 1f;
    [SerializeField] private string truthSceneName = "Ending_Truth";

    [Header("Control Ending")]
    [SerializeField] private AudioSource consoleSealAudio;
    [Tooltip("Time to let the door/seal sound play before fading.")]
    [SerializeField] private float controlHoldTime = 1.2f;
    [SerializeField] private string controlSceneName = "Ending_Control";

    [Header("Fade")]
    [Tooltip("Full-screen black Image with a CanvasGroup. If empty, one is created at runtime.")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 2f;
    [Tooltip("Also fade all game audio to silence during the final fade.")]
    [SerializeField] private bool fadeAudio = true;

    [Header("Scene Cleanup")]
    [Tooltip("Destroy DontDestroyOnLoad objects (Player, UICanvas, Elevator...) before loading an ending, " +
             "so they don't appear in the ending scene. The ending scene then needs its own Camera.")]
    [SerializeField] private bool destroyPersistentObjectsOnEnding = true;

    // ------------------------------------------------------------------
    // Runtime state
    // ------------------------------------------------------------------

    private Rigidbody2D playerBody;
    private Animator playerAnimator;
    private PlayerInput playerInput;
    private InteractionDetector[] interactionDetectors;
    private bool choiceMade;

    // Cached animator parameter hashes so we only set parameters that exist
    private static readonly int InputXHash = Animator.StringToHash("InputX");
    private static readonly int InputYHash = Animator.StringToHash("InputY");
    private static readonly int LastInputXHash = Animator.StringToHash("LastInputX");
    private static readonly int LastInputYHash = Animator.StringToHash("LastInputY");

    // ------------------------------------------------------------------
    // Unity lifecycle
    // ------------------------------------------------------------------

    private void Awake()
    {
        // Hide UI until each phase needs it
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
        if (continuePrompt != null) continuePrompt.SetActive(false);
    }

    // Start runs after SceneManager.sceneLoaded, so PauseController has already
    // reset its flag and ElevatorTransition has already placed the player.
    private void Start()
    {
        ResolvePlayer();
        LockPlayer(true);
        StartCoroutine(RunSequence());
    }

    private void OnDisable()
    {
        // Safety net: never leave the player locked if this object goes away mid-sequence
        if (!choiceMade) LockPlayer(false);
    }

    // ------------------------------------------------------------------
    // Main sequence
    // ------------------------------------------------------------------

    private IEnumerator RunSequence()
    {
        // A. Opening cutscene
        yield return WalkPlayerToOverlook();
        yield return new WaitForSeconds(pauseBeforeDialogue);

        // B. Old Man dialogue
        yield return PlayDialogue(oldManLines);

        // C. Final choice
        ShowChoices();
    }

    // ------------------------------------------------------------------
    // A. Opening cutscene
    // ------------------------------------------------------------------

    private void ResolvePlayer()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        if (player == null)
        {
            Debug.LogWarning("[FinalRooftop] No player found. The cutscene walk will be skipped.");
            return;
        }

        playerBody = player.GetComponent<Rigidbody2D>();
        playerAnimator = player.GetComponent<Animator>();
        playerInput = player.GetComponent<PlayerInput>();
        interactionDetectors = player.GetComponentsInChildren<InteractionDetector>(true);
    }

    /// <summary>Stops player-driven movement, interaction and menus.</summary>
    private void LockPlayer(bool locked)
    {
        // PlayerMovement, MenuController etc. all respect this flag
        PauseController.SetPause(locked);

        // PlayerInput also drives interaction callbacks, so switch it off entirely
        if (playerInput != null) playerInput.enabled = !locked;

        // InteractionDetector polls the E key directly, so disable it too
        if (interactionDetectors != null)
        {
            foreach (InteractionDetector detector in interactionDetectors)
            {
                if (detector != null) detector.enabled = !locked;
            }
        }

        if (locked && playerBody != null) playerBody.linearVelocity = Vector2.zero;
    }

    private IEnumerator WalkPlayerToOverlook()
    {
        if (player == null) yield break;

        if (playerStartPoint != null) TeleportPlayer(playerStartPoint.position);

        if (overlookPoint == null)
        {
            Debug.LogWarning("[FinalRooftop] Overlook Point is not assigned. Skipping the walk.");
            yield break;
        }

        Vector2 target = overlookPoint.position;
        Vector2 direction = (target - (Vector2)player.position).normalized;
        SetWalkingAnimation(true, direction);

        // Set the position directly each physics step. Velocity stays zero, so
        // PlayerMovement (which zeroes velocity while paused) won't cancel the walk animation.
        while (Vector2.Distance(player.position, target) > 0.02f)
        {
            Vector2 next = Vector2.MoveTowards(player.position, target, walkSpeed * Time.fixedDeltaTime);
            TeleportPlayer(next);
            yield return new WaitForFixedUpdate();
        }

        TeleportPlayer(target);
        SetWalkingAnimation(false, direction);
    }

    private void TeleportPlayer(Vector2 position)
    {
        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.position = position;
        }
        player.position = position;
    }

    private void SetWalkingAnimation(bool walking, Vector2 direction)
    {
        if (playerAnimator == null) return;

        SetBoolIfExists(playerAnimator, Animator.StringToHash(walkingBoolName), walking);
        SetFloatIfExists(playerAnimator, InputXHash, walking ? direction.x : 0f);
        SetFloatIfExists(playerAnimator, InputYHash, walking ? direction.y : 0f);
        // Last input keeps the idle pose facing the walk direction
        SetFloatIfExists(playerAnimator, LastInputXHash, direction.x);
        SetFloatIfExists(playerAnimator, LastInputYHash, direction.y);
    }

    // ------------------------------------------------------------------
    // B. Dialogue (built-in typewriter driver)
    // ------------------------------------------------------------------

    private IEnumerator PlayDialogue(string[] lines)
    {
        if (lines == null || lines.Length == 0) yield break;

        if (dialoguePanel == null || dialogueText == null)
        {
            // Still playable without UI: log the lines so testing isn't blocked
            Debug.LogWarning("[FinalRooftop] Dialogue UI not assigned. Logging lines instead.");
            foreach (string line in lines) Debug.Log($"{speakerName}: {line}");
            yield break;
        }

        dialoguePanel.SetActive(true);
        if (speakerNameText != null) speakerNameText.text = speakerName;
        if (portraitImage != null && oldManPortrait != null) portraitImage.sprite = oldManPortrait;

        foreach (string line in lines)
        {
            yield return TypeLine(line);

            // Wait for the player to read and press continue
            if (continuePrompt != null) continuePrompt.SetActive(true);
            yield return null; // don't reuse the press that finished typing
            yield return new WaitUntil(AdvancePressed);
            if (continuePrompt != null) continuePrompt.SetActive(false);
            yield return null;
        }

        dialoguePanel.SetActive(false);
    }

    private IEnumerator TypeLine(string line)
    {
        // TMP reveal keeps word wrapping stable while typing
        dialogueText.text = line;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();
        int total = dialogueText.textInfo.characterCount;

        float delay = typingSpeed > 0f ? 1f / typingSpeed : 0f;
        yield return null; // ignore any key held from the previous step

        for (int i = 1; i <= total; i++)
        {
            // Pressing continue mid-line shows the full line straight away
            if (AdvancePressed()) break;

            dialogueText.maxVisibleCharacters = i;

            if (typingAudioSource != null && charactersPerBlip > 0 && i % charactersPerBlip == 0)
            {
                typingAudioSource.Play();
            }

            yield return new WaitForSeconds(delay);
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
    }

    /// <summary>Space, Enter, E or left click advances dialogue.</summary>
    private static bool AdvancePressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null &&
            (keyboard.spaceKey.wasPressedThisFrame ||
             keyboard.enterKey.wasPressedThisFrame ||
             keyboard.eKey.wasPressedThisFrame))
        {
            return true;
        }

        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
    }

    // ------------------------------------------------------------------
    // C. Final choice
    // ------------------------------------------------------------------

    private void ShowChoices()
    {
        if (choicePanel == null || truthButton == null || controlButton == null)
        {
            Debug.LogError("[FinalRooftop] Choice panel or buttons are not assigned. The ending cannot be chosen.");
            return;
        }

        SetButtonLabel(truthButton, truthButtonLabel);
        SetButtonLabel(controlButton, controlButtonLabel);

        truthButton.onClick.RemoveAllListeners();
        controlButton.onClick.RemoveAllListeners();
        truthButton.onClick.AddListener(() => OnChoiceSelected(true));
        controlButton.onClick.AddListener(() => OnChoiceSelected(false));

        truthButton.interactable = true;
        controlButton.interactable = true;
        choicePanel.SetActive(true);

        // Select a button so keyboard / gamepad navigation works immediately
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(truthButton.gameObject);
    }

    private static void SetButtonLabel(Button button, string label)
    {
        if (string.IsNullOrEmpty(label)) return;

        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) { tmp.text = label; return; }

        Text legacy = button.GetComponentInChildren<Text>(true);
        if (legacy != null) legacy.text = label;
    }

    private void OnChoiceSelected(bool truth)
    {
        if (choiceMade) return; // block double clicks
        choiceMade = true;

        truthButton.interactable = false;
        controlButton.interactable = false;
        choicePanel.SetActive(false);

        StartCoroutine(truth ? TruthEndingSequence() : ControlEndingSequence());
    }

    // ------------------------------------------------------------------
    // Truth ending: open the shutters, collapse, fade, load
    // ------------------------------------------------------------------

    private IEnumerator TruthEndingSequence()
    {
        if (shutterAnimator != null) shutterAnimator.SetTrigger(openShuttersTrigger);
        if (rubbleParticles != null) rubbleParticles.Play();
        if (collapseAudio != null) collapseAudio.Play();

        yield return CameraShake(shakeDuration, shakeMagnitude);
        yield return new WaitForSeconds(truthHoldTime);

        yield return FadeOutAndLoad(truthSceneName);
    }

    // ------------------------------------------------------------------
    // Control ending: seal the console, fade, load
    // ------------------------------------------------------------------

    private IEnumerator ControlEndingSequence()
    {
        if (consoleSealAudio != null) consoleSealAudio.Play();

        yield return new WaitForSeconds(controlHoldTime);

        yield return FadeOutAndLoad(controlSceneName);
    }

    // ------------------------------------------------------------------
    // Camera shake
    // ------------------------------------------------------------------

    /// <summary>
    /// Shakes the camera for <paramref name="duration"/> seconds.
    /// The CinemachineBrain is paused during the shake, otherwise it would
    /// overwrite the camera position every frame.
    /// </summary>
    public IEnumerator CameraShake(float duration, float magnitude)
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null || duration <= 0f) yield break;

        CinemachineBrain brain = cam.GetComponent<CinemachineBrain>();
        bool brainWasEnabled = brain != null && brain.enabled;
        if (brain != null) brain.enabled = false;

        Transform camTransform = cam.transform;
        Vector3 origin = camTransform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Ease out so the quake settles rather than stopping dead
            float strength = magnitude * (1f - elapsed / duration);
            Vector2 offset = Random.insideUnitCircle * strength;
            camTransform.position = origin + new Vector3(offset.x, offset.y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        camTransform.position = origin;
        if (brain != null) brain.enabled = brainWasEnabled;
    }

    // ------------------------------------------------------------------
    // Fade and scene loading
    // ------------------------------------------------------------------

    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        CanvasGroup fade = GetOrCreateFadeGroup();
        float startAlpha = fade.alpha;
        float startVolume = AudioListener.volume;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            fade.alpha = Mathf.Lerp(startAlpha, 1f, k);
            if (fadeAudio) AudioListener.volume = Mathf.Lerp(startVolume, 0f, k);
            yield return null;
        }
        fade.alpha = 1f;

        LoadEnding(sceneName, startVolume);
    }

    private void LoadEnding(string sceneName, float volumeToRestore)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[FinalRooftop] Scene '{sceneName}' is not in the Build Profile scene list. Add it to load the ending.");
            AudioListener.volume = volumeToRestore;
            return;
        }

        // AudioListener.volume is global, so restore it once the ending scene is up
        s_VolumeToRestore = volumeToRestore;
        SceneManager.sceneLoaded -= RestoreVolumeOnLoad;
        SceneManager.sceneLoaded += RestoreVolumeOnLoad;

        PauseController.SetPause(false);
        if (destroyPersistentObjectsOnEnding) DestroyPersistentObjects();

        SceneManager.LoadScene(sceneName);
    }

    private static float s_VolumeToRestore = 1f;

    private static void RestoreVolumeOnLoad(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= RestoreVolumeOnLoad;
        AudioListener.volume = s_VolumeToRestore;
    }

    /// <summary>Removes the carried-over gameplay objects (Player, HUD, Elevator...).</summary>
    private void DestroyPersistentObjects()
    {
        PersistentObject[] persistent = FindObjectsByType<PersistentObject>(FindObjectsInactive.Include);
        foreach (PersistentObject obj in persistent)
        {
            // Never destroy the object running this coroutine's fade canvas
            if (obj != null && !transform.IsChildOf(obj.transform)) Destroy(obj.gameObject);
        }
    }

    /// <summary>Uses the assigned fade group, or builds a full-screen black overlay.</summary>
    private CanvasGroup GetOrCreateFadeGroup()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            fadeCanvasGroup.blocksRaycasts = true;
            return fadeCanvasGroup;
        }

        GameObject canvasObject = new GameObject("RooftopFadeCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue; // above every other UI

        GameObject imageObject = new GameObject("Black");
        imageObject.transform.SetParent(canvasObject.transform, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = Color.black;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fadeCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = true;
        return fadeCanvasGroup;
    }

    // ------------------------------------------------------------------
    // Animator helpers (avoid console spam for missing parameters)
    // ------------------------------------------------------------------

    private static bool HasParameter(Animator animator, int hash, AnimatorControllerParameterType type)
    {
        if (animator.runtimeAnimatorController == null) return false;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.nameHash == hash && p.type == type) return true;
        }
        return false;
    }

    private static void SetBoolIfExists(Animator animator, int hash, bool value)
    {
        if (HasParameter(animator, hash, AnimatorControllerParameterType.Bool)) animator.SetBool(hash, value);
    }

    private static void SetFloatIfExists(Animator animator, int hash, float value)
    {
        if (HasParameter(animator, hash, AnimatorControllerParameterType.Float)) animator.SetFloat(hash, value);
    }
}
