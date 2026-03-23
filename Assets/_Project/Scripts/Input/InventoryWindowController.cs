using UnityEngine;

public class InventoryWindowController : MonoBehaviour
{
    [SerializeField] private GameObject inventoryWindow;

    private bool isOpen = false;

    void Start()
    {
        inventoryWindow.SetActive(false);
        CloseInventory();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && isOpen)
        {
            CloseInventory();
        }
    }

    public void ToggleInventory()
    {
        if (isOpen)
            CloseInventory();
        else
            OpenInventory();
    }

    public void OpenInventory()
    {
        isOpen = true;
        inventoryWindow.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseInventory()
    {
        isOpen = false;
        inventoryWindow.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}