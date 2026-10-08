using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Keeps the EventSystem selection in step with the mouse in every UI: when the pointer moves,
// the button under it becomes the selected one, and moving it off all buttons clears the
// selection. Without this a clicked button stays "selected" and lit while another one is
// hovered, so two buttons glow at once. Keyboard navigation is untouched because nothing
// changes while the mouse stands still.
public sealed class PointerSelectionSync : MonoBehaviour
{
    private const float MoveThresholdSqr = 0.25f;

    private static readonly List<RaycastResult> Hits = new();

    private Vector2 lastPointerPosition;
    private bool hasPointerPosition;
    private Selectable pressedButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject host = new(nameof(PointerSelectionSync));
        DontDestroyOnLoad(host);
        host.AddComponent<PointerSelectionSync>();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        EventSystem eventSystem = EventSystem.current;
        if (mouse == null || eventSystem == null)
            return;

        Vector2 position = mouse.position.ReadValue();
        bool moved = !hasPointerPosition || (position - lastPointerPosition).sqrMagnitude > MoveThresholdSqr;
        lastPointerPosition = position;
        hasPointerPosition = true;

        TrackPressedButton(mouse, eventSystem, position);

        // Dragging a slider or a scrollbar must not move the selection.
        if (!moved || mouse.leftButton.isPressed || mouse.rightButton.isPressed || eventSystem.alreadySelecting)
            return;

        GameObject selected = eventSystem.currentSelectedGameObject;
        if (KeepsSelection(selected))
            return;

        Selectable hovered = FindHoveredSelectable(eventSystem, position);
        GameObject target = hovered != null ? hovered.gameObject : null;
        if (target != selected)
            eventSystem.SetSelectedGameObject(target);
    }

    // Unity keeps a button in its pressed (lit) state until the mouse is released, even after the
    // pointer has left it, so dragging from one button to another lit both. Once the held pointer
    // leaves the pressed button, its press is released and the selection follows the pointer.
    // Releasing over another button still does not click either of them.
    private void TrackPressedButton(Mouse mouse, EventSystem eventSystem, Vector2 position)
    {
        if (!mouse.leftButton.isPressed)
        {
            pressedButton = null;
            return;
        }

        Selectable hovered = FindHoveredSelectable(eventSystem, position);
        if (mouse.leftButton.wasPressedThisFrame)
        {
            pressedButton = hovered is Button or Toggle ? hovered : null;
            return;
        }

        if (pressedButton == null || hovered == pressedButton)
            return;

        ExecuteEvents.Execute(pressedButton.gameObject,
            new PointerEventData(eventSystem) { button = PointerEventData.InputButton.Left },
            ExecuteEvents.pointerUpHandler);
        pressedButton = null;

        if (!eventSystem.alreadySelecting)
            eventSystem.SetSelectedGameObject(hovered != null ? hovered.gameObject : null);
    }

    // A text field being typed in and a key waiting for a new binding keep their selection.
    private static bool KeepsSelection(GameObject selected)
    {
        if (selected == null)
            return false;

        if (IsTextField(selected))
            return true;

        SettingsKeyBindingRow row = selected.GetComponentInParent<SettingsKeyBindingRow>();
        return row != null && row.IsWaitingForKey;
    }

    private static Selectable FindHoveredSelectable(EventSystem eventSystem, Vector2 position)
    {
        PointerEventData pointer = new(eventSystem) { position = position };
        Hits.Clear();
        eventSystem.RaycastAll(pointer, Hits);
        if (Hits.Count == 0)
            return null;

        // Only the topmost hit counts, so a dialog over a menu hides the buttons behind it.
        Selectable selectable = Hits[0].gameObject.GetComponentInParent<Selectable>();
        if (selectable == null || !selectable.IsInteractable())
            return null;

        // Selecting a text field would start typing; hovering it only clears the selection.
        return IsTextField(selectable.gameObject) ? null : selectable;
    }

    private static bool IsTextField(GameObject gameObject)
    {
        return gameObject.TryGetComponent(out TMP_InputField _) || gameObject.TryGetComponent(out InputField _);
    }
}
