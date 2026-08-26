using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// BeamMachine — dynamically detects any GemItem in the inventory.
/// Add a GemItem component to any item prefab to make it placeable here.
/// No hardcoded gem names — just attach GemItem to any prefab with a colour.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BeamMachine : MonoBehaviour, IMultiInteractable
{
    [Header("Visuals")]
    public SpriteRenderer placedItemSprite;
    public Transform nozzleTransform;

    [Header("Beam Settings")]
    public float maxBeamDistance = 50f;
    public LayerMask obstacleLayer;
    public float beamWidth = 0.1f;

    // Internal state
    private string currentGemName = "";
    private Color  currentColor   = Color.white;
    private int    rotationState  = 0;  // 0=Up 1=Right 2=Down 3=Left
    private bool   isMachineOn    = false;

    // LineRenderer pool
    private List<LineRenderer> segmentRenderers = new List<LineRenderer>();

    // ── Unity ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (placedItemSprite != null) placedItemSprite.sprite = null;
    }

    private void Update()
    {
        LightGoal.ResetAll();

        if (!string.IsNullOrEmpty(currentGemName) && isMachineOn)
            DrawBeamChain();
        else
            HideAllSegments();
    }

    // ── Beam Drawing ──────────────────────────────────────────────────────
    private void DrawBeamChain()
    {
        foreach (var lr in segmentRenderers) lr.gameObject.SetActive(false);

        int segIndex = 0;
        Queue<(Vector2 start, Vector2 dir)> beamsToProcess = new Queue<(Vector2, Vector2)>();

        Vector2 nozzleDir   = NozzleDirection();
        Vector2 nozzleStart = (Vector2)nozzleTransform.position + nozzleDir * 0.5f;
        beamsToProcess.Enqueue((nozzleStart, nozzleDir));

        int safetyLimit = 20;
        while (beamsToProcess.Count > 0 && safetyLimit-- > 0)
        {
            var (start, dir) = beamsToProcess.Dequeue();

            // Cast against ALL layers — component checks decide what to do
            RaycastHit2D[] hits = Physics2D.RaycastAll(start, dir, maxBeamDistance);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            RaycastHit2D bestHit = default;
            foreach (var h in hits)
            {
                if (h.collider == null)                              continue;
                if (h.collider.gameObject == this.gameObject)        continue;
                if (h.collider.transform.IsChildOf(this.transform))  continue;
                if (h.collider.CompareTag("Player"))                 continue;
                if (h.collider.isTrigger)                            continue;
                bestHit = h;
                break;
            }

            LineRenderer seg = GetOrCreateSegment(segIndex++);
            seg.startColor = currentColor;
            seg.endColor   = new Color(currentColor.r, currentColor.g, currentColor.b, 0.4f);
            seg.startWidth = beamWidth;
            seg.endWidth   = beamWidth;
            seg.SetPosition(0, start);

            if (bestHit.collider != null)
            {
                seg.SetPosition(1, bestHit.point);

                // ── Redirector ────────────────────────────────────────────
                BeamRedirector redirector = bestHit.collider.GetComponent<BeamRedirector>();
                if (redirector != null)
                {
                    Vector2 nextDir   = redirector.GetOutputDirection();
                    Vector2 nextStart = (Vector2)redirector.transform.position + nextDir * 0.6f;
                    beamsToProcess.Enqueue((nextStart, nextDir));
                    Debug.Log($"[Beam] Hit redirector '{redirector.name}', continuing {nextDir}");
                }

                // ── Splitter ──────────────────────────────────────────────
                BeamSplitter splitter = bestHit.collider.GetComponent<BeamSplitter>();
                if (splitter != null)
                {
                    List<Vector2> splitDirs = splitter.GetOutputDirections(dir);
                    foreach (Vector2 splitDir in splitDirs)
                    {
                        Vector2 splitStart = (Vector2)splitter.transform.position + splitDir * 0.6f;
                        beamsToProcess.Enqueue((splitStart, splitDir));
                    }
                    Debug.Log($"[Beam] Hit splitter '{splitter.name}' ({splitter.splitterType}), emitting {splitDirs.Count} beams");
                }

                // ── Goal ──────────────────────────────────────────────────
                LightGoal goal = bestHit.collider.GetComponent<LightGoal>();
                if (goal != null)
                {
                    goal.ReceiveBeam(currentColor);
                    Debug.Log($"[Beam] Hit goal '{goal.name}' with color {currentColor}");
                }
            }
            else
            {
                seg.SetPosition(1, start + dir * maxBeamDistance);
            }
        }
    }

    private void HideAllSegments()
    {
        foreach (var lr in segmentRenderers) lr.gameObject.SetActive(false);
    }

    // ── LineRenderer Pool ─────────────────────────────────────────────────
    private Material beamMaterial;

    private LineRenderer GetOrCreateSegment(int index)
    {
        if (index >= segmentRenderers.Count)
        {
            GameObject obj = new GameObject($"BeamSegment_{index}");
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;

            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.positionCount    = 2;
            lr.useWorldSpace    = true;
            lr.sortingLayerName = "Collision";
            lr.sortingOrder     = 5000;
            lr.textureMode      = LineTextureMode.Tile;

            if (beamMaterial == null)
            {
                beamMaterial = new Material(Shader.Find("Sprites/Default"));
                beamMaterial.name = "BeamMaterial";
            }
            lr.material = beamMaterial;

            segmentRenderers.Add(lr);
        }

        if (segmentRenderers[index].sharedMaterial == null)
            segmentRenderers[index].material = beamMaterial;

        segmentRenderers[index].gameObject.SetActive(true);
        return segmentRenderers[index];
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private Vector2 NozzleDirection()
    {
        return rotationState switch
        {
            1 => Vector2.right,
            2 => Vector2.down,
            3 => Vector2.left,
            _ => Vector2.up
        };
    }

    private void ShowPopup(string message)
    {
        if (ItemPickupUIController.Instance != null)
            ItemPickupUIController.Instance.ShowItemPickup(message, null);
        else
            Debug.Log(message);
    }

    // ── Gem Discovery ─────────────────────────────────────────────────────
    private struct GemEntry
    {
        public string name;
        public Color  color;
        public Sprite sprite;
    }

    /// <summary>
    /// Returns all gem-like items in the player inventory.
    /// Priority: GemItem component → name-based color inference.
    /// </summary>
    private List<GemEntry> GetGemsInInventory()
    {
        var result = new List<GemEntry>();
        var inv = InventoryController.Instance;
        if (inv == null || inv.inventoryPanel == null)
        {
            Debug.LogWarning("[BeamMachine] InventoryController.Instance or inventoryPanel is null!");
            return result;
        }

        foreach (Transform slotTransform in inv.inventoryPanel.transform)
        {
            var slot = slotTransform.GetComponent<Slot>();
            if (slot?.currentItem == null) continue;

            var item = slot.currentItem.GetComponent<Item>();
            if (item == null || item.quantity <= 0) continue;

            var sr = slot.currentItem.GetComponent<SpriteRenderer>()
                  ?? slot.currentItem.GetComponentInChildren<SpriteRenderer>();

            // ── Option A: GemItem component (explicit, preferred) ─────────
            var gemComp = slot.currentItem.GetComponent<GemItem>();
            if (gemComp != null)
            {
                result.Add(new GemEntry { name = item.Name, color = gemComp.gemColor, sprite = sr?.sprite });
                continue;
            }

            // ── Option B: Name-based detection (works without any setup) ──
            string lower = item.Name.ToLower();
            if (lower.Contains("gem") || lower.Contains("crystal") || lower.Contains("orb"))
            {
                result.Add(new GemEntry
                {
                    name   = item.Name,
                    color  = ColorFromItemName(item.Name),
                    sprite = sr?.sprite
                });
            }
        }

        Debug.Log($"[BeamMachine] Found {result.Count} gem(s) in inventory.");
        return result;
    }

    /// <summary>Infers beam colour from an item's name. Extend freely.</summary>
    private static Color ColorFromItemName(string name)
    {
        string n = name.ToLower();
        if (n.Contains("red"))                      return Color.red;
        if (n.Contains("blue"))                     return Color.cyan;
        if (n.Contains("green"))                    return Color.green;
        if (n.Contains("yellow"))                   return Color.yellow;
        if (n.Contains("purple") || n.Contains("violet")) return new Color(0.6f, 0f, 1f);
        if (n.Contains("orange"))                   return new Color(1f, 0.5f, 0f);
        if (n.Contains("white"))                    return Color.white;
        if (n.Contains("pink"))                     return new Color(1f, 0.4f, 0.7f);
        return Color.white; // unknown gem — white beam
    }

    // ── IMultiInteractable ────────────────────────────────────────────────
    public bool CanInteract() => true;

    public void Interact()
    {
        var options = GetInteractionOptions();
        if (options.Count > 0) options[0].OnSelect?.Invoke();
    }

    public List<InteractionOption> GetInteractionOptions()
    {
        var options = new List<InteractionOption>();

        // Always available
        options.Add(new InteractionOption { Name = "Rotate Machine", OnSelect = RotateMachine });

        if (isMachineOn)
            options.Add(new InteractionOption { Name = "Turn Off", OnSelect = () => { isMachineOn = false; SoundEffectManager.Play("PickUp"); } });
        else
            options.Add(new InteractionOption { Name = "Turn On",  OnSelect = TryTurnOn });

        if (string.IsNullOrEmpty(currentGemName))
        {
            var gems = GetGemsInInventory();
            if (gems.Count == 0)
            {
                options.Add(new InteractionOption
                {
                    Name     = "Put Gem (none in inventory)",
                    OnSelect = () => ShowPopup("No gems in inventory!")
                });
            }
            else
            {
                foreach (var gem in gems)
                {
                    var captured = gem;
                    options.Add(new InteractionOption
                    {
                        Name     = $"Put {captured.name}",
                        OnSelect = () => PlaceGem(captured)
                    });
                }
            }
        }
        else
        {
            options.Add(new InteractionOption
            {
                Name     = $"Remove {currentGemName}",
                OnSelect = TryTakeGem
            });
        }

        return options;
    }

    // ── Machine Actions ───────────────────────────────────────────────────
    private void RotateMachine()
    {
        rotationState = (rotationState + 1) % 4;
        if (nozzleTransform != null)
            nozzleTransform.rotation = Quaternion.Euler(0, 0, -rotationState * 90f);
        SoundEffectManager.Play("PickUp");
    }

    private void TryTurnOn()
    {
        if (string.IsNullOrEmpty(currentGemName))
            ShowPopup("Need a gem inside first!");
        else
        {
            isMachineOn = true;
            SoundEffectManager.Play("PickUp");
        }
    }

    private void TryTakeGem()
    {
        if (string.IsNullOrEmpty(currentGemName)) { ShowPopup("Machine is empty!"); return; }
        TakeGem();
    }

    private void PlaceGem(GemEntry gem)
    {
        if (!string.IsNullOrEmpty(currentGemName)) { ShowPopup("Machine is already full!"); return; }

        InventoryController.Instance?.RemoveItemByName(gem.name, 1);
        currentGemName = gem.name;
        currentColor   = gem.color;

        if (placedItemSprite != null && gem.sprite != null)
            placedItemSprite.sprite = gem.sprite;

        if (nozzleTransform != null)
            nozzleTransform.rotation = Quaternion.Euler(0, 0, -rotationState * 90f);

        SoundEffectManager.Play("PickUp");
        Debug.Log($"[BeamMachine] Placed '{currentGemName}', beam color = {currentColor}");
    }


    private void TakeGem()
    {
        var dict = FindAnyObjectByType<ItemDictionary>();
        if (dict == null) return;

        var prefab = dict.GetItemPrefabByName(currentGemName);
        if (prefab != null && InventoryController.Instance != null)
        {
            if (InventoryController.Instance.AddItem(prefab))
            {
                ItemPickupUIController.Instance?.ShowItemPickup(
                    prefab.GetComponent<Item>().Name,
                    prefab.GetComponent<SpriteRenderer>()?.sprite);

                currentGemName = "";
                isMachineOn    = false;
                if (placedItemSprite != null) placedItemSprite.sprite = null;
                HideAllSegments();
            }
        }
    }
}
