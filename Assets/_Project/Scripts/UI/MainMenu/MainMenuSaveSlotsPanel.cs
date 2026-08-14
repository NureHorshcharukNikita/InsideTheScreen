using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuSaveSlotsPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private MainMenuSaveSlotRow[] rows;
    [SerializeField] private Button backButton;

    private MainMenuController controller;
    private MainMenuSaveSlotMode mode;

    private void Awake()
    {
        if (backButton != null)
            backButton.onClick.AddListener(Hide);

        BindRows();
    }

    public void Show(MainMenuController owner, MainMenuSaveSlotMode slotMode)
    {
        controller = owner;
        mode = slotMode;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        if (controller != null)
            controller.HideSaveSlots();
    }

    private void BindRows()
    {
        if (rows == null)
            return;

        for (int i = 0; i < rows.Length; i++)
        {
            int slotIndex = i;
            Button button = rows[i] != null ? rows[i].Button : null;
            if (button == null)
                continue;

            button.onClick.AddListener(() => SelectSlot(slotIndex));
        }
    }

    private void Refresh()
    {
        if (titleText != null)
            titleText.text = mode == MainMenuSaveSlotMode.NewGame ? "Select Slot" : "Continue";

        if (rows == null)
            return;

        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i] == null)
                continue;

            bool occupied = PersistentSessionSave.TryGetSlotInfo(i, out SessionSaveData data);
            rows[i].SetText(GetSlotText(i, occupied, data));
            rows[i].SetInteractable(mode == MainMenuSaveSlotMode.NewGame || occupied);
        }
    }

    private string GetSlotText(int slotIndex, bool occupied, SessionSaveData data)
    {
        string prefix = $"Slot {slotIndex + 1}";

        if (occupied)
            return $"{prefix}: {FormatSaveDate(data)}";

        if (mode == MainMenuSaveSlotMode.NewGame)
            return $"{prefix}: New Game";

        return $"{prefix}: Empty";
    }

    private static string FormatSaveDate(SessionSaveData data)
    {
        if (data == null || data.savedAtTicks <= 0)
            return "Saved";

        DateTime savedAt = new DateTime(data.savedAtTicks);
        return savedAt.ToString("HH:mm dd.MM.yyyy");
    }

    private void SelectSlot(int slotIndex)
    {
        if (controller == null)
            return;

        if (mode == MainMenuSaveSlotMode.NewGame)
            controller.StartNewGameInSlot(slotIndex);
        else
            controller.ContinueGameFromSlot(slotIndex);
    }
}
