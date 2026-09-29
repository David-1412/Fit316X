#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-click builders for colour-mixing light puzzles.
///
/// Tools > Light Puzzle > Build Colour-Mix Gate
///     One goal (White + Blue) that hides one gate.
///
/// Tools > Light Puzzle > Build 3-Colour Door
///     Three colour gates. Each stays lit once unlocked, so the player can
///     re-aim the pedestals at the next one. When all three are lit, the Door
///     disappears and the player can walk through.
///
/// Everything is created with Undo (Ctrl+Z removes it). Save the scene afterwards (Ctrl+S).
/// </summary>
public static class ColourMixPuzzleBuilder
{
    const string GoalPrefabPath       = "Assets/Prefabs/LightBeamPuzzle/LightGoal_Blue.prefab";
    const string ControllerPrefabPath = "Assets/Prefabs/LightBeamPuzzle/LightPuzzleController.prefab";

    // Gem colours in this project: White (1,1,1)  Blue (0,0,1)  Red (1,0,0)  Green (0,1,0)
    struct GateSpec { public string name; public Color color; public string recipe; }

    static readonly GateSpec[] ThreeGates =
    {
        new GateSpec { name = "Gate_LightBlue", color = new Color(0.5f, 0.5f, 1f), recipe = "White + Blue" },
        new GateSpec { name = "Gate_Purple",    color = new Color(0.5f, 0f,   0.5f), recipe = "Red + Blue" },
        new GateSpec { name = "Gate_Yellow",    color = new Color(0.5f, 0.5f, 0f), recipe = "Red + Green" },
    };

    // ─────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Light Puzzle/Build 3-Colour Door")]
    public static void BuildThreeGateDoor()
    {
        if (!CheckScene(out var scene) || !LoadPrefabs(out var goalPrefab, out var controllerPrefab)) return;

        Undo.SetCurrentGroupName("Build 3-Colour Door");
        int undoGroup = Undo.GetCurrentGroup();

        OfferToRemoveSingleGatePuzzle();

        // Three gates in a row, centred on the Scene view, with the door above them
        Vector3 centre = SceneViewCentre();
        var goals = new List<LightGoal>();
        for (int i = 0; i < ThreeGates.Length; i++)
        {
            var spec = ThreeGates[i];
            Vector3 pos = centre + new Vector3((i - 1) * 3f, 0f, 0f);
            var goal = CreateGoal(goalPrefab, scene, spec.name, pos, spec.color, stayLit: true);
            goals.Add(goal);
        }

        var door = CreateBlock(scene, "Door", centre + new Vector3(0f, 4f, 0f),
                               new Vector3(3f, 1f, 1f), new Color(0.45f, 0.3f, 0.2f), goals[0]);

        var controller = GetOrCreateController(controllerPrefab, scene);
        controller.goals = goals.ToArray();
        UnityEventTools.AddBoolPersistentListener(controller.OnPuzzleSolved, door.SetActive, false);
        PrefabUtility.RecordPrefabInstancePropertyModifications(controller);

        Finish(undoGroup, scene, goals[1].gameObject);

        string recipes = "";
        foreach (var g in ThreeGates) recipes += $"\n  {g.name}: {g.recipe}";
        Debug.Log($"[ColourMixPuzzle] Built 3-colour door in '{scene.name}'.{recipes}\n" +
                  "Move the gates and Door where you want them, then save (Ctrl+S).");
    }

    // ─────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Light Puzzle/Build Colour-Mix Gate")]
    public static void BuildSingleGate()
    {
        if (!CheckScene(out var scene) || !LoadPrefabs(out var goalPrefab, out var controllerPrefab)) return;

        Undo.SetCurrentGroupName("Build Colour-Mix Gate");
        int undoGroup = Undo.GetCurrentGroup();

        Color mix = ThreeGates[0].color; // White + Blue
        Vector3 goalPos = MidpointOfFirstTwoPedestals();
        var goal = CreateGoal(goalPrefab, scene, "LightGoal_Mix", goalPos, mix, stayLit: false);

        var gate = CreateBlock(scene, "ColourGate", goalPos + new Vector3(0f, 4f, 0f),
                               new Vector3(3f, 1f, 1f), mix, goal);

        var controller = GetOrCreateController(controllerPrefab, scene);
        controller.goals = new[] { goal };
        UnityEventTools.AddBoolPersistentListener(controller.OnPuzzleSolved, gate.SetActive, false);
        PrefabUtility.RecordPrefabInstancePropertyModifications(controller);

        Finish(undoGroup, scene, goal.gameObject);
        Debug.Log($"[ColourMixPuzzle] Built in '{scene.name}': goal at {goalPos}. Move ColourGate into your doorway, then save (Ctrl+S).");
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    static bool CheckScene(out Scene scene)
    {
        scene = EditorSceneManager.GetActiveScene();
        return scene.name.Contains("Puzzle 6") ||
               EditorUtility.DisplayDialog("Light Puzzle",
                   $"The open scene is '{scene.name}', not 'Puzzle 6'. Build the puzzle here anyway?",
                   "Build here", "Cancel");
    }

    static bool LoadPrefabs(out GameObject goalPrefab, out GameObject controllerPrefab)
    {
        goalPrefab       = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPrefabPath);
        controllerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ControllerPrefabPath);
        if (goalPrefab != null && controllerPrefab != null) return true;

        EditorUtility.DisplayDialog("Light Puzzle",
            "Couldn't find LightGoal_Blue or LightPuzzleController in Prefabs/LightBeamPuzzle.", "OK");
        return false;
    }

    static LightGoal CreateGoal(GameObject prefab, Scene scene, string name, Vector3 pos, Color color, bool stayLit)
    {
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        obj.name = name;
        obj.transform.position = pos;

        var goal = obj.GetComponent<LightGoal>();
        goal.requiredColor       = color;
        goal.activeColor         = color;
        goal.colorTolerance      = 0.15f; // a single colour or the wrong pair won't pass
        goal.mixBeams            = true;
        goal.requiredBeamCount   = 2;
        goal.showMixPreview      = true;
        goal.stayLitOnceUnlocked = stayLit;
        PrefabUtility.RecordPrefabInstancePropertyModifications(goal);
        return goal;
    }

    /// Solid block with a collider (blocks the player). Uses the goal's sorting layer so it's visible.
    static GameObject CreateBlock(Scene scene, string name, Vector3 pos, Vector3 scale, Color color, LightGoal sortLike)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        if (go.scene != scene) SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position   = pos;
        go.transform.localScale = scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.color  = color;
        var refSr = sortLike != null ? sortLike.GetComponentInChildren<SpriteRenderer>() : null;
        if (refSr != null)
        {
            sr.sortingLayerID = refSr.sortingLayerID;
            sr.sortingOrder   = refSr.sortingOrder;
        }

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = false;
        col.size = Vector2.one;
        return go;
    }

    /// Only one LightPuzzleController is allowed per scene, so reuse it if there is one.
    static LightPuzzleController GetOrCreateController(GameObject prefab, Scene scene)
    {
        var controller = Object.FindAnyObjectByType<LightPuzzleController>();
        if (controller != null)
        {
            Undo.RecordObject(controller, "Update LightPuzzleController");
            return controller;
        }
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Undo.RegisterCreatedObjectUndo(obj, "Create LightPuzzleController");
        return obj.GetComponent<LightPuzzleController>();
    }

    /// If the earlier single-gate puzzle is in the scene, offer to remove it so it doesn't clash.
    static void OfferToRemoveSingleGatePuzzle()
    {
        var oldGoal = GameObject.Find("LightGoal_Mix");
        var oldGate = GameObject.Find("ColourGate");
        if (oldGoal == null && oldGate == null) return;

        if (!EditorUtility.DisplayDialog("Light Puzzle",
                "This scene already has the single-gate puzzle (LightGoal_Mix / ColourGate).\n\n" +
                "Remove it? The new door uses its own three gates.",
                "Remove it", "Keep it"))
            return;

        var controller = Object.FindAnyObjectByType<LightPuzzleController>();
        if (controller != null && oldGate != null)
        {
            Undo.RecordObject(controller, "Remove old gate listener");
            for (int i = controller.OnPuzzleSolved.GetPersistentEventCount() - 1; i >= 0; i--)
                if (controller.OnPuzzleSolved.GetPersistentTarget(i) == oldGate)
                    UnityEventTools.RemovePersistentListener(controller.OnPuzzleSolved, i);
        }
        if (oldGoal != null) Undo.DestroyObjectImmediate(oldGoal);
        if (oldGate != null) Undo.DestroyObjectImmediate(oldGate);
    }

    static Vector3 MidpointOfFirstTwoPedestals()
    {
        var machines = Object.FindObjectsByType<BeamMachine>(FindObjectsSortMode.None);
        if (machines.Length < 2) return SceneViewCentre();
        System.Array.Sort(machines, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        Vector3 p = (machines[0].transform.position + machines[1].transform.position) * 0.5f;
        p.z = 0f;
        return p;
    }

    static Vector3 SceneViewCentre()
    {
        var view = SceneView.lastActiveSceneView;
        Vector3 p = view != null ? view.pivot : Vector3.zero;
        p.z = 0f;
        return p;
    }

    static void Finish(int undoGroup, Scene scene, GameObject select)
    {
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = select;
        EditorGUIUtility.PingObject(select);
    }
}
#endif
