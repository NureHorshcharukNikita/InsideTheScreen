using UnityEngine;
using UnityEngine.EventSystems;

public class BattleBackgroundClick : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private BattleSystem battleSystem;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging)
            return;

        battleSystem.DeselectCard();
    }
}