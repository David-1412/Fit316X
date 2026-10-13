using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Runs the whole Floor 5 rooftop sequence:
///   1. Opening cutscene - locks input and walks the player to the overlook point.
///   2. Old Man dialogue - built-in typewriter driver, no dialogue plugin needed.
///   3. Final choice - Truth ending (open the shutters) or Control ending (take the seat).
///
/// ZERO SETUP: when the scene named "F5" loads, this component is created automatically
/// if the scene doesn't already contain one. Anything not assigned in the Inspector is
/// built at runtime:
///   - Overlook point: a GameObject named "OverlookPoint" in the scene, otherwise a point
///     a few units in front of where the player arrives.
///   - Old Man: a GameObject named "OldMan" / "NPC_OldMan" in the scene, otherwise
///     spawned from Resources/NPC_OldMan next to the overlook point.
///   - Dialogue box, choice buttons and fade overlay: created in code.
///   - Ending scenes: if "Ending_Truth" / "Ending_Control" are not in the Build Profile
///     scene list, a built-in ending screen is shown instead.
///
/// To customise, add this component to a GameObject in F5 and fill in any of the fields.
/// </summary>
public class FinalRooftopSceneManager : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Auto bootstrap
    // ------------------------------------------------------------------

    /// <summary>Scene in which the rooftop sequence starts automatically.</summary>
    private const string RooftopSceneName = "F5";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterBootstrap()
    {
        SceneManager.sceneLoaded -= OnAnySceneLoaded;
        SceneManager.sceneLoaded += OnAnySceneLoaded;
    }

    private static void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != RooftopSceneName) return;

        // A manually placed (customised) manager takes priority
        if (FindAnyObjectByType<FinalRooftopSceneManager>() != null) return;

        GameObject host = new GameObject("RooftopSequence (auto)");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<FinalRooftopSceneManager>();
    }

    // ------------------------------------------------------------------
    // Inspector references (all optional)
    // ------------------------------------------------------------------

    [Header("Player")]
    [Tooltip("Leave empty to find the persistent object tagged 'Player' at runtime.")]
    [SerializeField] private Transform player;
    [Tooltip("Optional. If set, the player is snapped here before the walk starts.")]
    [SerializeField] private Transform playerStartPoint;
    [Tooltip("Where the player walks to. Leave empty to use a scene object named 'OverlookPoint', " +
             "or a point offset from the player's arrival position.")]
    [SerializeField] private Transform overlookPoint;
    [Tooltip("Used only when no overlook point exists: offset from the player's arrival position.")]
    [SerializeField] private Vector2 defaultOverlookOffset = new Vector2(0f, 3f);
    [SerializeField] private float walkSpeed = 2.5f;
    [Tooltip("Pause between stopping at the overlook and the first line of dialogue.")]
    [SerializeField] private float pauseBeforeDialogue = 0.8f;
    [Tooltip("Bool parameter on the player's Animator. The project's Player.controller uses 'isWalking'.")]
    [SerializeField] private string walkingBoolName = "isWalking";

    [Header("Old Man")]
    [Tooltip("Leave empty to use a scene object named 'OldMan' or 'NPC_OldMan', or spawn one from Resources.")]
    [SerializeField] private GameObject oldMan;
    [SerializeField] private string oldManResourcePath = "NPC_OldMan";
    [Tooltip("Where a spawned Old Man stands, relative to the overlook point.")]
    [SerializeField] private Vector2 oldManOffset = new Vector2(1.5f, 0.5f);

    [Header("Camera")]
    [Tooltip("Leave empty to use Camera.main.")]
    [SerializeField] private Camera targetCamera;

    [Header("Dialogue Content")]
    [SerializeField] private string speakerName = "Old Man";
    [SerializeField, TextArea(2, 5)] private string[] oldManLines =
    {
        "Welcome, Scholar. Do you like the view? It is your inheritance. The sun has never disappeared, yet this truth must remain hidden.",
        "One hundred and twenty-three years ago, this valley was choked with smoke and corpses. A people living in a cave will worship whoever holds the match.",
        "I was you once—full of faith and doubt. But someone must tend the machine. Someone must guard the truth. Now, it is your turn. Take my place and maintain order... or destroy the shutters and shatter this world."
    };
    [Tooltip("Optional. Leave empty to use the Old Man's portrait from his NPC Dialogue asset.")]
    [SerializeField] private Sprite oldManPortrait;
    [Tooltip("Seconds between letters (same as NPCDialogue.typingSpeed).")]
    [SerializeField] private float typingSpeed = 0.05f;
    [Tooltip("Voice blip played on every letter. Leave empty to use the Old Man's voice from his NPC Dialogue asset.")]
    [SerializeField] private AudioClip voiceSound;
    [SerializeField] private float voicePitch = 0.5f;

    [Header("Choice UI (built at runtime if empty)")]
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
    [SerializeField] private string truthEndingTitle = "THE TRUTH";
    [SerializeField, TextArea(3, 8)] private string truthEndingText =
        "The shutters groan open, and sunlight pours into the city below for the first time in a century. " +
        "By nightfall, the Cult's hold is broken.\n\n" +
        "The tower does not survive the morning. Neither do you.\n\n" +
        "Far below, beneath the rubble, the people look up—and see the sky.";

    [Header("Control Ending")]
    [SerializeField] private AudioSource consoleSealAudio;
    [Tooltip("Time to let the door/seal sound play before fading.")]
    [SerializeField] private float controlHoldTime = 1.2f;
    [SerializeField] private string controlSceneName = "Ending_Control";
    [SerializeField] private string controlEndingTitle = "ORDER";
    [SerializeField, TextArea(3, 8)] private string controlEndingText =
        "The console seals with a heavy click. The Old Man's chair is still warm.\n\n" +
        "Below, the city waits in the dark for its ration of light.\n\n" +
        "Today, you decide who receives it.";

    [Header("Fade")]
    [Tooltip("Full-screen black CanvasGroup. If empty, one is created at runtime.")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 2f;
    [Tooltip("Also fade all game audio to silence during the final fade.")]
    [SerializeField] private bool fadeAudio = true;

    [Header("After The Ending")]
    [Tooltip("Scene loaded from the built-in ending screen. Ignored if it isn't in the Build Profile scene list.")]
    [SerializeField] private string menuSceneName = "StartMenu";
    [Tooltip("Destroy DontDestroyOnLoad objects (Player, UICanvas, Elevator...) before leaving F5, " +
             "so they don't appear in the next scene. That scene then needs its own Camera.")]
    [SerializeField] private bool destroyPersistentObjectsOnEnding = true;

    // ------------------------------------------------------------------
    // Runtime state
    // ------------------------------------------------------------------

    private Rigidbody2D playerBody;
    private Animator playerAnimator;
    private PlayerInput playerInput;
    private InteractionDetector[] interactionDetectors;
    private Canvas runtimeCanvas;
    private bool choiceMade;

    private static float s_VolumeToRestore = 1f;

    // Cached animator parameter hashes so we only set parameters that exist
    private static readonly int InputXHash = Animator.StringToHash("InputX");
    private static readonly int InputYHash = Animator.StringToHash("InputY");
    private static readonly int LastInputXHash = Animator.StringToHash("LastInputX");
    private static readonly int LastInputYHash = Animator.StringToHash("LastInputY");

    // UI colours for the runtime-built interface
    private static readonly Color PanelColour = new Color(0.05f, 0.05f, 0.08f, 0.9f);
    private static readonly Color ButtonColour = new Color(0.16f, 0.16f, 0.2f, 1f);
    private static readonly Color AccentColour = new Color(1f, 0.85f, 0.45f, 1f);

    // ------------------------------------------------------------------
    // Unity lifecycle
    // ------------------------------------------------------------------

    private void Awake()
    {
        // Hide assigned UI until each phase needs it
        if (choicePanel != null) choicePanel.SetActive(false);
    }

    // Start runs after SceneManager.sceneLoaded, so PauseController has already
    // reset its flag and ElevatorTransition has already placed the player.
    private void Start()
    {
        ResolvePlayer();
        LockPlayer(true);

        EnsureEventSystem();
        EnsureOverlookPoint();
        EnsureOldMan();
        EnsureChoiceUI();

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
    // Scene setup (fills in anything not assigned in the Inspector)
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

    private void EnsureOverlookPoint()
    {
        if (overlookPoint != null) return;

        GameObject named = GameObject.Find("OverlookPoint");
        if (named != null)
        {
            overlookPoint = named.transform;
            return;
        }

        Vector2 origin = playerStartPoint != null ? (Vector2)playerStartPoint.position
                       : player != null ? (Vector2)player.position
                       : Vector2.zero;

        GameObject point = new GameObject("OverlookPoint (auto)");
        point.transform.SetParent(transform, false);
        point.transform.position = origin + defaultOverlookOffset;
        overlookPoint = point.transform;
    }

    private void EnsureOldMan()
    {
        if (oldMan == null) oldMan = GameObject.Find("OldMan");
        if (oldMan == null) oldMan = GameObject.Find("NPC_OldMan");
        if (oldMan != null)
        {
            NPC sceneNpc = oldMan.GetComponentInChildren<NPC>(true);
            if (sceneNpc != null)
            {
                TakeVoiceFrom(sceneNpc.dialogueData);
                sceneNpc.enabled = false; // the cutscene drives this conversation
            }
            MatchPlayerSorting(oldMan);
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(oldManResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning($"[FinalRooftop] No Old Man in the scene and Resources/{oldManResourcePath} not found.");
            return;
        }

        // Instantiate under an inactive holder so no Awake/Start runs before we strip
        // the wandering and talk-to behaviour - this Old Man is driven by the cutscene.
        // DestroyImmediate is needed: a deferred Destroy would let them wake up first.
        GameObject holder = new GameObject("OldManHolder");
        holder.SetActive(false);
        oldMan = Instantiate(prefab, holder.transform);
        oldMan.name = "OldMan";

        foreach (NPC npc in oldMan.GetComponentsInChildren<NPC>(true))
        {
            TakeVoiceFrom(npc.dialogueData);
            DestroyImmediate(npc);
        }
        foreach (WaypointMover mover in oldMan.GetComponentsInChildren<WaypointMover>(true)) DestroyImmediate(mover);

        Vector3 position = overlookPoint.position + (Vector3)oldManOffset;
        oldMan.transform.SetParent(null, false);
        SceneManager.MoveGameObjectToScene(oldMan, gameObject.scene);
        Destroy(holder);

        // The prefab's sprite sits on a child "NPC" object; place that child at the target
        NPC_Visual(oldMan).position = position;
        MatchPlayerSorting(oldMan);
    }

    /// <summary>
    /// Draws the Old Man on the same sorting layer and order as the player,
    /// so ground tilemaps on lower layers don't cover him.
    /// </summary>
    private void MatchPlayerSorting(GameObject target)
    {
        SpriteRenderer playerSprite = player != null ? player.GetComponentInChildren<SpriteRenderer>() : null;
        int layerId = playerSprite != null ? playerSprite.sortingLayerID : SortingLayer.NameToID("Player");
        int order = playerSprite != null ? playerSprite.sortingOrder : 0;

        foreach (SpriteRenderer sprite in target.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sprite.sortingLayerID = layerId;
            sprite.sortingOrder = order;
        }

        // A SortingGroup overrides its children's settings, so match that too
        foreach (UnityEngine.Rendering.SortingGroup group in target.GetComponentsInChildren<UnityEngine.Rendering.SortingGroup>(true))
        {
            group.sortingLayerID = layerId;
            group.sortingOrder = order;
        }
    }

    /// <summary>
    /// Reuses the Old Man's Floor 1 voice blip and portrait (OldManDialogue.asset)
    /// for anything not set in the Inspector, so he sounds the same on both floors.
    /// </summary>
    private void TakeVoiceFrom(NPCDialogue data)
    {
        if (data == null) return;
        if (voiceSound == null)
        {
            voiceSound = data.voiceSound;
            voicePitch = data.voicePitch;
        }
        if (oldManPortrait == null) oldManPortrait = data.npcPortrait;
    }

    /// <summary>The Transform that actually holds the Old Man's sprite.</summary>
    private static Transform NPC_Visual(GameObject root)
    {
        SpriteRenderer sprite = root.GetComponentInChildren<SpriteRenderer>(true);
        return sprite != null ? sprite.transform : root.transform;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem (auto)");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    private void EnsureChoiceUI()
    {
        if (choicePanel != null && truthButton != null && controlButton != null) return;

        Canvas canvas = GetRuntimeCanvas();

        // Centred panel with a title and two stacked buttons
        RectTransform panel = CreateUIObject("ChoicePanel", canvas.transform);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(1000f, 420f);
        panel.gameObject.AddComponent<Image>().color = PanelColour;

        TMP_Text title = CreateText("Title", panel, 40f, AccentColour, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(30f, -100f), new Vector2(-30f, -25f));
        title.text = "What will you do?";
        title.fontStyle = FontStyles.Bold;

        truthButton = CreateButton("TruthButton", panel, new Vector2(0f, 0f));
        controlButton = CreateButton("ControlButton", panel, new Vector2(0f, -140f));

        // Up/down keys move between the two buttons
        Navigation truthNav = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = controlButton, selectOnUp = controlButton };
        Navigation controlNav = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = truthButton, selectOnDown = truthButton };
        truthButton.navigation = truthNav;
        controlButton.navigation = controlNav;

        choicePanel = panel.gameObject;
        choicePanel.SetActive(false);
    }

    private Button CreateButton(string name, Transform parent, Vector2 anchoredPosition)
    {
        RectTransform rect = CreateUIObject(name, parent);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(860f, 110f);
        rect.anchoredPosition = anchoredPosition + new Vector2(0f, 10f);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = Color.white; // tinted by the Button's ColorBlock

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colours = button.colors;
        colours.normalColor = ButtonColour;
        colours.highlightedColor = new Color(0.35f, 0.3f, 0.18f, 1f);
        colours.selectedColor = new Color(0.35f, 0.3f, 0.18f, 1f);
        colours.pressedColor = AccentColour;
        colours.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.6f);
        button.colors = colours;

        TMP_Text label = CreateText("Label", rect, 36f, Color.white, TextAlignmentOptions.Center);
        SetRect(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    private Canvas GetRuntimeCanvas()
    {
        if (runtimeCanvas != null) return runtimeCanvas;

        GameObject canvasObject = new GameObject("RooftopUI (auto)");
        canvasObject.transform.SetParent(transform, false);

        runtimeCanvas = canvasObject.AddComponent<Canvas>();
        runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        runtimeCanvas.sortingOrder = 500; // above the game HUD

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return runtimeCanvas;
    }

    // ------------------------------------------------------------------
    // A. Opening cutscene
    // ------------------------------------------------------------------

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
        if (overlookPoint == null) yield break;

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

        // Same dialogue box, typewriter and voice blip as the Floor 1 NPCs
        DialogueBox box = DialogueBox.Instance;
        box.Open(speakerName, oldManPortrait);

        foreach (string line in lines)
        {
            yield return box.TypeLine(line, typingSpeed, voiceSound, voicePitch);
            yield return box.WaitForContinue();
        }

        box.Close();
    }

    // ------------------------------------------------------------------
    // C. Final choice
    // ------------------------------------------------------------------

    private void ShowChoices()
    {
        SetButtonLabel(truthButton, truthButtonLabel);
        SetButtonLabel(controlButton, controlButtonLabel);

        truthButton.onClick.RemoveAllListeners();
        controlButton.onClick.RemoveAllListeners();
        truthButton.onClick.AddListener(() => OnChoiceSelected(true));
        controlButton.onClick.AddListener(() => OnChoiceSelected(false));

        truthButton.interactable = true;
        controlButton.interactable = true;
        choicePanel.SetActive(true);

        // Keyboard (Up/Down + Enter/Space/E) and mouse work even if the EventSystem
        // carried over from an earlier floor isn't receiving input
        ChoiceInput.Attach(choicePanel, new[] { truthButton, controlButton }, new Color(0.35f, 0.3f, 0.18f, 1f));
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
        ChoiceInput choiceInput = choicePanel.GetComponent<ChoiceInput>();
        if (choiceInput != null) choiceInput.Clear();
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

        yield return FadeOut();
        yield return LoadEnding(truthSceneName, truthEndingTitle, truthEndingText);
    }

    // ------------------------------------------------------------------
    // Control ending: seal the console, fade, load
    // ------------------------------------------------------------------

    private IEnumerator ControlEndingSequence()
    {
        if (consoleSealAudio != null) consoleSealAudio.Play();

        yield return new WaitForSeconds(controlHoldTime);

        yield return FadeOut();
        yield return LoadEnding(controlSceneName, controlEndingTitle, controlEndingText);
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

    private IEnumerator FadeOut()
    {
        CanvasGroup fade = GetOrCreateFadeGroup();
        float startAlpha = fade.alpha;
        float startVolume = AudioListener.volume;
        s_VolumeToRestore = startVolume;
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
    }

    /// <summary>
    /// Loads the ending scene if it's in the build, otherwise shows the built-in ending screen.
    /// </summary>
    private IEnumerator LoadEnding(string sceneName, string title, string body)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            LeaveFloor(sceneName);
            yield break;
        }

        Debug.Log($"[FinalRooftop] Scene '{sceneName}' is not in the Build Profile scene list. Showing the built-in ending screen.");
        AudioListener.volume = s_VolumeToRestore;
        yield return ShowEndingScreen(title, body);
    }

    private IEnumerator ShowEndingScreen(string title, string body)
    {
        // Own top-most canvas so it sits above whatever fade overlay is in use
        GameObject canvasObject = new GameObject("EndingScreen (auto)");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        CanvasGroup group = canvasObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        RectTransform background = CreateUIObject("Background", canvasObject.transform);
        SetRect(background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        background.gameObject.AddComponent<Image>().color = Color.black;

        TMP_Text titleText = CreateText("Title", canvasObject.transform, 72f, AccentColour, TextAlignmentOptions.Center);
        SetRect(titleText.rectTransform, new Vector2(0.1f, 0.68f), new Vector2(0.9f, 0.85f), Vector2.zero, Vector2.zero);
        titleText.text = title;
        titleText.fontStyle = FontStyles.Bold;
        titleText.characterSpacing = 12f;

        TMP_Text bodyText = CreateText("Body", canvasObject.transform, 36f, Color.white, TextAlignmentOptions.Top);
        SetRect(bodyText.rectTransform, new Vector2(0.18f, 0.22f), new Vector2(0.82f, 0.64f), Vector2.zero, Vector2.zero);
        bodyText.text = body;

        bool canReturn = Application.CanStreamedLevelBeLoaded(menuSceneName);
        TMP_Text hint = CreateText("Hint", canvasObject.transform, 26f, new Color(1f, 1f, 1f, 0.6f), TextAlignmentOptions.Center);
        SetRect(hint.rectTransform, new Vector2(0.1f, 0.06f), new Vector2(0.9f, 0.14f), Vector2.zero, Vector2.zero);
        hint.text = canReturn ? "Press any key to return to the menu" : "Thank you for playing";

        // Fade the card in
        for (float t = 0f; t < 1.5f; t += Time.unscaledDeltaTime)
        {
            group.alpha = t / 1.5f;
            yield return null;
        }
        group.alpha = 1f;

        if (!canReturn) yield break;

        yield return new WaitForSeconds(1f); // stop an accidental skip
        yield return new WaitUntil(AnyInputPressed);
        LeaveFloor(menuSceneName);
    }

    private static bool AnyInputPressed()
    {
        return (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
               (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
    }

    private void LeaveFloor(string sceneName)
    {
        // AudioListener.volume is global, so restore it once the next scene is up
        SceneManager.sceneLoaded -= RestoreVolumeOnLoad;
        SceneManager.sceneLoaded += RestoreVolumeOnLoad;

        PauseController.SetPause(false);
        if (destroyPersistentObjectsOnEnding) DestroyPersistentObjects();

        SceneManager.LoadScene(sceneName);
    }

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

        GameObject canvasObject = new GameObject("RooftopFade (auto)");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue - 1; // above all UI except the ending screen

        RectTransform black = CreateUIObject("Black", canvasObject.transform);
        SetRect(black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        black.gameObject.AddComponent<Image>().color = Color.black;

        fadeCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = true;
        return fadeCanvasGroup;
    }

    // ------------------------------------------------------------------
    // UI building helpers
    // ------------------------------------------------------------------

    private static RectTransform CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static TMP_Text CreateText(string name, Transform parent, float size, Color colour, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateUIObject(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.color = colour;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
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
