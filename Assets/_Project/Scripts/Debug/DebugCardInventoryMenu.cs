using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DebugCardInventoryMenu : MonoBehaviour
{
    private const float MaxWindowWidth = 1180f;
    private const float MaxWindowHeight = 760f;
    private const float ScreenPadding = 22f;
    private const float CategoryWidth = 190f;
    private const float RowHeight = 108f;

    private static DebugCardInventoryMenu instance;

    private readonly List<CardData> cards = new();
    private readonly List<CardCategory> categories = new();
    private readonly List<CardData> filteredCards = new();
    private readonly Dictionary<CardData, int> inventoryCounts = new();

    private Rect windowRect;
    private Vector2 cardsScroll;
    private Vector2 categoriesScroll;
    private CardCategory? selectedCategory;
    private string statusMessage = string.Empty;
    private bool isVisible;
    private PlayerCharacter player;
    private InventoryWindow inventoryWindow;

    private GUIStyle titleStyle;
    private GUIStyle categoryStyle;
    private GUIStyle selectedCategoryStyle;
    private GUIStyle cardNameStyle;
    private GUIStyle mutedStyle;
    private GUIStyle descriptionStyle;

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeMenu()
    {
        if (instance != null)
            return;

        GameObject menuObject = new("DebugCardInventoryMenu");
        DontDestroyOnLoad(menuObject);
        instance = menuObject.AddComponent<DebugCardInventoryMenu>();
    }
#endif

    private void Awake()
    {
        CenterWindow();
        LoadCards();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        if (WasTogglePressed())
        {
            isVisible = !isVisible;
            if (isVisible)
            {
                CenterWindow();
                RefreshRuntimeReferences();
                LoadCards();
            }
        }
    }

    private void OnGUI()
    {
        if (!isVisible)
            return;

        EnsureStyles();
        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "Debug Card Inventory");
    }

    private void DrawWindow(int windowId)
    {
        Color previousColor = GUI.color;
        GUI.color = new Color(0.08f, 0.065f, 0.055f, 0.98f);
        GUI.DrawTexture(new Rect(0f, 18f, windowRect.width, windowRect.height - 18f), Texture2D.whiteTexture);
        GUI.color = previousColor;

        using (new GUILayout.VerticalScope())
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label($"{GetWindowTitle()} ({filteredCards.Count})", titleStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", GUILayout.Width(90f), GUILayout.Height(28f)))
                {
                    RefreshRuntimeReferences();
                    LoadCards();
                }
                if (GUILayout.Button("Close", GUILayout.Width(80f), GUILayout.Height(28f)))
                    isVisible = false;
            }

            GUILayout.Space(8f);

            using (new GUILayout.HorizontalScope())
            {
                DrawCategoriesPanel();
                GUILayout.Space(10f);
                DrawCardsPanel();
            }

            GUILayout.Space(6f);
            GUILayout.Label(statusMessage, mutedStyle, GUILayout.Height(22f));
        }

        GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 34f));
    }

    private void DrawCategoriesPanel()
    {
        using (new GUILayout.VerticalScope(GUI.skin.box, GUILayout.Width(CategoryWidth), GUILayout.ExpandHeight(true)))
        {
            GUILayout.Label("Categories", titleStyle);
            categoriesScroll = GUILayout.BeginScrollView(categoriesScroll);

            DrawCategoryButton("All", null);
            foreach (CardCategory category in categories)
                DrawCategoryButton(GetCategoryLabel(category), category);

            GUILayout.EndScrollView();
        }
    }

    private void DrawCategoryButton(string label, CardCategory? category)
    {
        GUIStyle style = selectedCategory == category ? selectedCategoryStyle : categoryStyle;
        if (GUILayout.Button(label, style, GUILayout.Height(34f)))
        {
            selectedCategory = category;
            cardsScroll = Vector2.zero;
            RebuildFilteredCards();
        }
    }

    private void DrawCardsPanel()
    {
        using (new GUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
        {
            cardsScroll = GUILayout.BeginScrollView(cardsScroll);

            foreach (CardData card in filteredCards)
                DrawCardRow(card);

            GUILayout.EndScrollView();
        }
    }

    private void DrawCardRow(CardData card)
    {
        if (card == null)
            return;

        using (new GUILayout.HorizontalScope(GUI.skin.box, GUILayout.MinHeight(RowHeight)))
        {
            DrawCardIcon(card);
            int ownedCount = GetInventoryCount(card);

            using (new GUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                GUILayout.Label(GetCardName(card), cardNameStyle);
                GUILayout.Label($"Owned: {ownedCount}   Cost: {card.Cost}   Category: {GetCategories(card)}", mutedStyle);
                GUILayout.Label(GetCardStats(card), mutedStyle);
                GUILayout.Label(GetCardDescription(card), descriptionStyle);
            }

            using (new GUILayout.VerticalScope(GUILayout.Width(120f)))
            {
                GUILayout.Space(10f);
                if (GUILayout.Button("Add +1", GUILayout.Height(36f)))
                    AddCard(card);

                bool wasEnabled = GUI.enabled;
                GUI.enabled = ownedCount > 0;
                if (GUILayout.Button("Remove -1", GUILayout.Height(36f)))
                    RemoveCard(card);
                GUI.enabled = wasEnabled;
            }
        }
    }

    private void DrawCardIcon(CardData card)
    {
        Texture texture = null;
#if UNITY_EDITOR
        if (card.Icon != null)
            texture = AssetPreview.GetAssetPreview(card.Icon) ?? AssetPreview.GetMiniThumbnail(card.Icon);
#endif

        Rect iconRect = GUILayoutUtility.GetRect(72f, 72f, GUILayout.Width(72f), GUILayout.Height(72f));
        if (texture != null)
            GUI.DrawTexture(iconRect, texture, ScaleMode.ScaleToFit);
        else
            GUI.Box(iconRect, string.Empty);
    }

    private void AddCard(CardData card)
    {
        RefreshRuntimeReferences();
        if (player == null || player.InventoryData == null)
        {
            statusMessage = "Player inventory not found.";
            return;
        }

        player.InventoryData.AddCard(card);

        inventoryWindow?.RebuildCategoriesAndRefresh();
        RefreshInventoryCounts();

        statusMessage = $"Added {GetCardName(card)} to inventory.";
    }

    private void RemoveCard(CardData card)
    {
        RefreshRuntimeReferences();
        if (player == null || player.InventoryData == null)
        {
            statusMessage = "Player inventory not found.";
            return;
        }

        if (!player.InventoryData.Cards.Contains(card))
        {
            statusMessage = $"{GetCardName(card)} is not in inventory.";
            return;
        }

        player.InventoryData.RemoveCard(card);

        inventoryWindow?.RebuildCategoriesAndRefresh();
        RefreshInventoryCounts();

        statusMessage = $"Removed {GetCardName(card)} from inventory.";
    }

    private void LoadCards()
    {
        cards.Clear();
        categories.Clear();

#if UNITY_EDITOR
        foreach (string guid in AssetDatabase.FindAssets("t:CardData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (card != null)
                cards.Add(card);
        }
#endif

        cards.Sort((left, right) => string.Compare(GetCardName(left), GetCardName(right), System.StringComparison.OrdinalIgnoreCase));
        categories.AddRange(cards
            .Where(card => card != null && card.Categories != null)
            .SelectMany(card => card.Categories)
            .Distinct()
            .OrderBy(category => category));
        RebuildFilteredCards();
        RefreshInventoryCounts();
    }

    private void RebuildFilteredCards()
    {
        filteredCards.Clear();
        if (selectedCategory == null)
        {
            filteredCards.AddRange(cards);
            return;
        }

        filteredCards.AddRange(cards.Where(card => card != null
                                                   && card.Categories != null
                                                   && card.Categories.Contains(selectedCategory.Value)));
    }

    private void RefreshInventoryCounts()
    {
        inventoryCounts.Clear();
        if (player == null || player.InventoryData == null || player.InventoryData.Cards == null)
            return;

        foreach (CardData card in player.InventoryData.Cards)
        {
            if (card == null)
                continue;

            inventoryCounts.TryGetValue(card, out int count);
            inventoryCounts[card] = count + 1;
        }
    }

    private int GetInventoryCount(CardData card)
    {
        return card != null && inventoryCounts.TryGetValue(card, out int count) ? count : 0;
    }

    private void RefreshRuntimeReferences()
    {
        player = FindFirstObjectByType<PlayerCharacter>();
        inventoryWindow = FindFirstObjectByType<InventoryWindow>(FindObjectsInactive.Include);
        RefreshInventoryCounts();
    }

    private void CenterWindow()
    {
        float width = Mathf.Min(MaxWindowWidth, Mathf.Max(320f, Screen.width - ScreenPadding * 2f));
        float height = Mathf.Min(MaxWindowHeight, Mathf.Max(320f, Screen.height - ScreenPadding * 2f));
        windowRect = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height);
    }

    private string GetWindowTitle()
    {
        return selectedCategory.HasValue ? GetCategoryLabel(selectedCategory.Value) : "All Cards";
    }

    private static bool WasTogglePressed()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
            return true;

        string input = Input.inputString;
        return input.Contains("`") || input.Contains("ё") || input.Contains("Ё");
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        categoryStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 14,
            padding = new RectOffset(10, 8, 0, 0)
        };

        selectedCategoryStyle = new GUIStyle(categoryStyle)
        {
            fontStyle = FontStyle.Bold
        };
        selectedCategoryStyle.normal.textColor = new Color(0.9f, 0.96f, 1f, 1f);

        cardNameStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        mutedStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            normal = { textColor = new Color(0.78f, 0.76f, 0.72f, 1f) }
        };

        descriptionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            wordWrap = true,
            normal = { textColor = new Color(0.92f, 0.9f, 0.86f, 1f) }
        };
    }

    private static string GetCardName(CardData card)
    {
        if (card == null)
            return "Missing Card";

        return string.IsNullOrWhiteSpace(card.CardName) ? card.name : card.CardName;
    }

    private static string GetCardDescription(CardData card)
    {
        if (card == null || string.IsNullOrWhiteSpace(card.Description))
            return "No description.";

        return card.Description;
    }

    private static string GetCategories(CardData card)
    {
        if (card == null || card.Categories == null || card.Categories.Count == 0)
            return "None";

        return string.Join(", ", card.Categories.Select(GetCategoryLabel));
    }

    private static string GetCardStats(CardData card)
    {
        if (card == null)
            return string.Empty;

        List<string> parts = new();

        if (card.InstantEffects != null)
        {
            foreach (CardInstantEffectEntry effect in card.InstantEffects)
            {
                string effectName = effect?.Effect != null
                    ? GetEffectName(effect.Effect)
                    : "Instant Effect";
                parts.Add($"{effectName}: {effect?.Value ?? 0}");
            }
        }

        if (card.StatusApplications != null)
        {
            foreach (CardStatusApplicationEntry status in card.StatusApplications)
            {
                string statusName = status?.Status != null
                    ? GetStatusName(status.Status)
                    : "Status";
                parts.Add($"{statusName}: {status?.Value ?? 0} for {status?.Duration ?? 0} turns");
            }
        }

        return parts.Count > 0 ? string.Join(" | ", parts) : "No gameplay effects.";
    }

    private static string GetEffectName(InstantBattleEffect effect)
    {
        return string.IsNullOrWhiteSpace(effect.EffectName) ? effect.name : effect.EffectName;
    }

    private static string GetStatusName(BattleStatus status)
    {
        return string.IsNullOrWhiteSpace(status.StatusName) ? status.name : status.StatusName;
    }

    private static string GetCategoryLabel(CardCategory category)
    {
        string text = category.ToString();
        for (int i = text.Length - 1; i > 0; i--)
        {
            if (char.IsUpper(text[i]) && !char.IsWhiteSpace(text[i - 1]))
                text = text.Insert(i, " ");
        }

        return text;
    }
}
