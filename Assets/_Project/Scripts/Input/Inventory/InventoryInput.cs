using UnityEngine;

public class InventoryInput : MonoBehaviour
{
    [SerializeField] private InventoryWindow inventoryWindow;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            inventoryWindow.Toggle();
    }
}