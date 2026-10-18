using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Keyboard and mouse control for a set of choice buttons that works even when the
/// UI EventSystem isn't receiving input (e.g. after a floor change). Arrow keys or
/// WASD move the highlight, Enter/Space/E confirm, and clicking a button picks it.
/// Buttons still work through the EventSystem as normal; callers should ignore a
/// second pick of the same choice.
/// </summary>
public class ChoiceInput : MonoBehaviour
{
    private readonly List<Button> buttons = new List<Button>();
    private readonly List<ColorBlock> originalColours = new List<ColorBlock>();
    private Color highlightColour = new Color(0.35f, 0.3f, 0.18f, 1f);
    private int selected;
    private int readyFrame;

    /// <summary>Attaches (or reuses) the input handler on <paramref name="host"/>.</summary>
    public static ChoiceInput Attach(GameObject host, IList<Button> choices, Color highlight)
    {
        ChoiceInput input = host.GetComponent<ChoiceInput>();
        if (input == null) input = host.AddComponent<ChoiceInput>();
        input.Setup(choices, highlight);
        return input;
    }

    public void Setup(IList<Button> choices, Color highlight)
    {
        RestoreColours();
        buttons.Clear();
        originalColours.Clear();
        foreach (Button button in choices)
        {
            if (button == null) continue;
            buttons.Add(button);
            originalColours.Add(button.colors);
        }

        highlightColour = highlight;
        readyFrame = Time.frameCount + 1; // ignore the key press that opened the choices
        Select(0);
    }

    public void Clear()
    {
        RestoreColours();
        buttons.Clear();
        originalColours.Clear();
    }

    private void OnDisable()
    {
        RestoreColours();
    }

    private void Update()
    {
        if (buttons.Count == 0 || Time.frameCount <= readyFrame) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame ||
                keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
            {
                Select((selected - 1 + buttons.Count) % buttons.Count);
            }
            else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame ||
                     keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
            {
                Select((selected + 1) % buttons.Count);
            }
            else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame ||
                     keyboard.spaceKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)
            {
                Click(selected);
                return;
            }
        }

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 pointer = mouse.position.ReadValue();
        for (int i = 0; i < buttons.Count; i++)
        {
            Button button = buttons[i];
            if (button == null || !button.isActiveAndEnabled) continue;

            RectTransform rect = (RectTransform)button.transform;
            if (!RectTransformUtility.RectangleContainsScreenPoint(rect, pointer, CanvasCamera(button))) continue;

            if (i != selected) Select(i); // hover moves the highlight
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Click(i);
                return;
            }
        }
    }

    private void Select(int index)
    {
        if (buttons.Count == 0) return;
        selected = Mathf.Clamp(index, 0, buttons.Count - 1);

        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] == null) continue;
            ColorBlock colours = originalColours[i];
            if (i == selected) colours.normalColor = highlightColour;
            buttons[i].colors = colours;
        }

        if (EventSystem.current != null && buttons[selected] != null)
            EventSystem.current.SetSelectedGameObject(buttons[selected].gameObject);
    }

    private void Click(int index)
    {
        if (index < 0 || index >= buttons.Count) return;
        Button button = buttons[index];
        if (button == null || !button.isActiveAndEnabled || !button.interactable) return;
        button.onClick.Invoke();
    }

    private void RestoreColours()
    {
        for (int i = 0; i < buttons.Count && i < originalColours.Count; i++)
        {
            if (buttons[i] != null) buttons[i].colors = originalColours[i];
        }
    }

    private static Camera CanvasCamera(Component component)
    {
        Canvas canvas = component.GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        canvas = canvas.rootCanvas;
        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }
}
