using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Collider2D))]
public class RandomCardPickup : MonoBehaviour
{
    [Header("Drop")]
    [Tooltip("Stable pickup id saved in the player session. Auto-filled for scene objects.")]
    [SerializeField] private string pickupId;
    [SerializeField] private List<CardData> cardPool = new();
    [SerializeField, Min(1)] private int minCards = 1;
    [SerializeField, Min(1)] private int maxCards = 3;
    [SerializeField] private bool destroyAfterPickup = true;

    [Header("Pickup Text")]
    [SerializeField] private Vector3 textOffset = new(0f, 1.5f, 0f);
    [SerializeField] private float textDelay = 0.25f;
    [SerializeField] private float textLifetime = 1.2f;
    [SerializeField] private float textRiseDistance = 0.5f;
    [SerializeField] private float textFontSize = 3f;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private int textSortingOrder = 100;

    private bool isPickedUp;
    private string runtimePickupId;

    private void Awake()
    {
        runtimePickupId = CreateRuntimePickupId();
    }

    private void Start()
    {
        if (CollectedPickups.IsCollected(runtimePickupId))
            Destroy(gameObject);
    }

    private void Reset()
    {
        AutoFillPickupId();

        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    private void OnValidate()
    {
        AutoFillPickupId();

        minCards = Mathf.Max(1, minCards);
        maxCards = Mathf.Max(minCards, maxCards);

        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isPickedUp)
            return;

        PlayerCharacter player = other.GetComponent<PlayerCharacter>();
        if (player == null || player.InventoryData == null)
            return;

        List<CardData> availableCards = GetAvailableCards();
        if (availableCards.Count == 0)
            return;

        isPickedUp = true;
        CollectedPickups.MarkCollected(runtimePickupId);
        DisablePickupCollision();

        int cardsToDrop = Random.Range(minCards, maxCards + 1);
        List<CardData> pickedCards = new();

        for (int i = 0; i < cardsToDrop; i++)
        {
            CardData card = availableCards[Random.Range(0, availableCards.Count)];
            player.InventoryData.AddCard(card);
            ExplorationPlayerSession.AddToDeckIfRoom(card, 1);
            pickedCards.Add(card);
        }

        StartCoroutine(ShowPickedCards(other.transform, pickedCards));
    }

    private IEnumerator ShowPickedCards(Transform target, IReadOnlyList<CardData> pickedCards)
    {
        if (destroyAfterPickup)
            HidePickupVisuals();

        for (int i = 0; i < pickedCards.Count; i++)
        {
            ShowPickedCardText(target, pickedCards[i]);

            if (textDelay > 0f)
                yield return new WaitForSeconds(textDelay);
        }

        if (destroyAfterPickup)
            yield return new WaitForSeconds(textLifetime);

        if (destroyAfterPickup)
            Destroy(gameObject);
    }

    private void ShowPickedCardText(Transform target, CardData card)
    {
        GameObject textObject = new("PickupCardText");
        textObject.transform.SetParent(target, false);
        textObject.transform.localPosition = textOffset;

        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.text = $"+ {GetCardDisplayName(card)}";
        text.fontSize = textFontSize;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;
        text.sortingOrder = textSortingOrder;

        StartCoroutine(AnimatePickupText(text, textObject.transform.localPosition));
    }

    private IEnumerator AnimatePickupText(TextMeshPro text, Vector3 startLocalPosition)
    {
        float elapsed = 0f;
        Color startColor = text.color;
        Transform textTransform = text.transform;

        while (elapsed < textLifetime)
        {
            elapsed += Time.deltaTime;
            float progress = textLifetime > 0f ? Mathf.Clamp01(elapsed / textLifetime) : 1f;

            textTransform.localPosition = Vector3.Lerp(
                startLocalPosition,
                startLocalPosition + Vector3.up * textRiseDistance,
                progress);

            Color color = startColor;
            color.a = Mathf.Lerp(startColor.a, 0f, progress);
            text.color = color;

            yield return null;
        }

        Destroy(text.gameObject);
    }

    private void DisablePickupCollision()
    {
        foreach (Collider2D pickupCollider in GetComponents<Collider2D>())
            pickupCollider.enabled = false;
    }

    private void HidePickupVisuals()
    {
        foreach (Renderer pickupRenderer in GetComponentsInChildren<Renderer>())
            pickupRenderer.enabled = false;
    }

    private string GetCardDisplayName(CardData card)
    {
        if (card == null)
            return "Card";

        return string.IsNullOrWhiteSpace(card.CardName) ? card.name : card.CardName;
    }

    private List<CardData> GetAvailableCards()
    {
        List<CardData> availableCards = new();

        foreach (CardData card in cardPool)
        {
            if (card != null)
                availableCards.Add(card);
        }

        return availableCards;
    }

    private string CreateRuntimePickupId()
    {
        if (!string.IsNullOrWhiteSpace(pickupId))
            return pickupId.Trim();

        return CreateReadablePickupId();
    }

    private void AutoFillPickupId()
    {
#if UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(pickupId) || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
            return;

        pickupId = CreateReadablePickupId();
        EditorUtility.SetDirty(this);
#endif
    }

    private string CreateReadablePickupId()
    {
        string scenePrefix = CreateIdPart(gameObject.scene.name);
        string objectName = CreateIdPart(gameObject.name);

        if (string.IsNullOrEmpty(scenePrefix))
            scenePrefix = "scene";
        if (string.IsNullOrEmpty(objectName))
            objectName = "pickup";

        return $"{scenePrefix}_{objectName}_{transform.GetSiblingIndex() + 1:00}";
    }

    private static string CreateIdPart(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        List<char> chars = new();
        bool previousWasSeparator = false;

        string trimmedValue = value.Trim();
        for (int i = 0; i < trimmedValue.Length; i++)
        {
            char character = trimmedValue[i];
            bool shouldSplitBeforeUppercase = i > 0
                                              && char.IsUpper(character)
                                              && char.IsLetterOrDigit(trimmedValue[i - 1])
                                              && !previousWasSeparator;

            if (shouldSplitBeforeUppercase)
            {
                chars.Add('_');
                previousWasSeparator = true;
            }

            if (char.IsLetterOrDigit(character))
            {
                chars.Add(char.ToLowerInvariant(character));
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator)
            {
                chars.Add('_');
                previousWasSeparator = true;
            }
        }

        while (chars.Count > 0 && chars[^1] == '_')
            chars.RemoveAt(chars.Count - 1);

        return new string(chars.ToArray());
    }
}
