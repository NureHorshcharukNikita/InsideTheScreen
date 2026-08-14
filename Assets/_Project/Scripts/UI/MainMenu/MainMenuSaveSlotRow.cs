using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuSaveSlotRow : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

    public Button Button => button;

    private void Reset()
    {
        button = GetComponent<Button>();
        label = GetComponentInChildren<TMP_Text>();
    }

    public void SetText(string text)
    {
        if (label != null)
            label.text = text;
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }
}
