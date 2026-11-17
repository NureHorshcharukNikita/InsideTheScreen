using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Something the pointer can show the player (UiPointer), found by its id, e.g. deck_add on the Add to
// deck button or battle_strength on the strength counter. Only targets that are shown count.
[DisallowMultipleComponent]
public sealed class UiPointerTarget : MonoBehaviour
{
    private static readonly List<UiPointerTarget> Shown = new();

    [Tooltip("The name hints point at it by, e.g. battle_end_turn.")]
    [SerializeField] private string id;
    [Tooltip("It pulses while the pointer shows it. Off for something that moves or resizes itself.")]
    [SerializeField] private bool pulse = true;

    public string Id => id;
    public bool Pulse => pulse;

    private void OnEnable()
    {
        Shown.Add(this);
    }

    private void OnDisable()
    {
        Shown.Remove(this);
    }

    public static bool TryFind(string id, out UiPointerTarget target)
    {
        target = null;
        if (string.IsNullOrWhiteSpace(id))
            return false;

        foreach (UiPointerTarget shown in Shown)
        {
            if (shown != null && shown.id == id && shown.isActiveAndEnabled && !shown.IsDisabledButton())
            {
                target = shown;
                return true;
            }
        }

        return false;
    }

    // A button that can't be pressed now is not shown, e.g. Add to deck with the Crystal Dust picked.
    private bool IsDisabledButton()
    {
        return TryGetComponent(out Selectable button) && !button.IsInteractable();
    }
}
