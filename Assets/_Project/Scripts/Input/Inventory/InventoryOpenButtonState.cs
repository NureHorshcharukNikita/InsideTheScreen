using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class InventoryOpenButtonState : MonoBehaviour
{
    [SerializeField] private Button button;

    private void Reset()
    {
        ResolveButton();
    }

    private void Awake()
    {
        ResolveButton();
        Refresh();
    }

    private void OnEnable()
    {
        ResolveButton();
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void ResolveButton()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    private void Refresh()
    {
        if (button != null)
            button.interactable = GameStateManager.CanOpenInventory();
    }
}
