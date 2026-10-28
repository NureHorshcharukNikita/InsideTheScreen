using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The crafting tab of the inventory window. On the right the recipes (Crafting) known so far
// (CraftingProgress: a recipe shows up once one of its cards has been found), A to Z, with a
// search (gone while none is known); the ones that cannot be made now say "Missing materials", and one not clicked yet has
// the dot of an unread quest. The chosen one lies on the workbench: the icon of the card it
// makes, how many times (- and +, up to what the inventory allows) and Craft. Below, the cards it
// takes, each with how many are at hand and how many are needed. The tab opens with none chosen.
// Made, the card comes up out of the workbench, big in the middle, then flies into the backpack tab,
// shrinking as it goes; the tab bumps, and a plate in the top right corner, where "Game saved"
// shows too, says what was made. No crafting in a battle.
public sealed class CraftingPageView : MonoBehaviour
{
    private static readonly Color TextColor = new(0.97f, 0.94f, 0.88f, 1f);
    private static readonly Color MutedTextColor = new(0.78f, 0.7f, 0.64f, 1f);
    private static readonly Color DimTextColor = new(0.58f, 0.51f, 0.47f, 1f);
    private static readonly Color UnavailableRowColor = new(0.62f, 0.62f, 0.62f, 1f);
    private const string EnoughHex = "#8EDB72";
    private const string MissingHex = "#E5534B";
    // The plate saying what was made: as the "Game saved" one (InventoryWindow), brown-gold
    // instead of green, with the card's icon on its left. In canvas units.
    private static readonly Color ToastColor = new(0.58f, 0.4f, 0.14f, 0.98f);
    private static readonly Vector2 ToastMargin = new(-34f, -34f);
    private const float ToastHeight = 58f;
    private const float ToastPadding = 18f;
    private const float ToastIconSize = 40f;
    private const float ToastIconGap = 12f;
    private const float ToastFontSize = 26f;
    private const float ToastSlideSeconds = 0.22f;
    private const float ToastHoldSeconds = 1.25f;
    // The made card, in canvas units: as big as a card in the inventory grid.
    private static readonly Vector2 FlyingCardSize = new(180f, 280f);
    private const float CardRiseSeconds = 0.3f;
    private const float CardHoldSeconds = 0.45f;
    private const float CardFlySeconds = 0.6f;
    private const float CardBigScale = 1.15f;
    private const float CardSmallScale = 0.12f;
    // How far above the workbench icon the card stops, and how high its way to the tab arcs.
    private const float CardRise = 40f;
    private const float CardArc = 120f;
    private const float TabBumpSeconds = 0.25f;
    private const float TabBump = 0.18f;

    [SerializeField] private PlayerCharacter player;
    [SerializeField] private List<CraftingRecipe> recipes = new();
    [Tooltip("The card shown for each ingredient (Card Slot).")]
    [SerializeField] private CardView cardViewPrefab;

    [Header("Workbench")]
    [Tooltip("The icon of the card it makes (Preserve Aspect). Hidden while no recipe is chosen.")]
    [SerializeField] private Image resultIcon;
    [SerializeField] private TMP_Text emptyText;
    [SerializeField] private Button decreaseButton;
    [SerializeField] private Button increaseButton;
    [SerializeField] private TMP_InputField quantityInput;
    [SerializeField] private Button craftButton;
    [Tooltip("The font of what was made: a copy shows on a plate in the top right corner once the card has flown in.")]
    [SerializeField] private TMP_Text craftedText;
    [Tooltip("Where a made card flies to: the backpack tab at the top of the window.")]
    [SerializeField] private RectTransform inventoryTab;
    [Tooltip("Only there while a recipe lies on the workbench, e.g. the quantity, Craft and the materials title.")]
    [SerializeField] private List<GameObject> recipeOnly = new();

    [Header("Materials")]
    [SerializeField] private RectTransform materialsRow;
    [Tooltip("One ingredient: a CardHolder for the card, a CountText (at hand / needed) and a NoteText (a tool, or what does instead).")]
    [SerializeField] private RectTransform materialTemplate;
    [Tooltip("The plus between two ingredients.")]
    [SerializeField] private RectTransform plusTemplate;

    [Header("Recipes")]
    [SerializeField] private CardSearchField search;
    [Tooltip("The bar around the search. Hidden while no recipe is known yet: there is nothing to search.")]
    [SerializeField] private GameObject searchBar;
    [Tooltip("One recipe: UnreadIndicator (the new dot), Icon, TextColumn/Title, TextColumn/Subtitle. A RightArea (the card's cost) is hidden: the cost stays unknown.")]
    [SerializeField] private RectTransform rowTemplate;
    [SerializeField] private Sprite rowSprite;
    [SerializeField] private Sprite selectedRowSprite;
    [Tooltip("Shown when no recipe is listed. Its text says why: none known yet, or none found by the search.")]
    [SerializeField] private GameObject noRecipesState;

    private readonly List<RecipeRow> rows = new();
    private readonly List<GameObject> materials = new();
    // The made cards on their way and the plate saying what was made, gone if the tab closes.
    private readonly List<GameObject> showingCrafted = new();
    private RectTransform craftedToast;
    private CraftingRecipe selected;
    private int quantity = 1;
    private Vector3 tabScale = Vector3.one;

    private sealed class RecipeRow
    {
        public CraftingRecipe Recipe;
        public GameObject Root;
        public Image Background;
        public Image Icon;
        public TMP_Text Title;
        public TMP_Text Subtitle;
        public GameObject NewDot;
        public bool Known;
        public bool CanCraft;
    }

    private InventoryData Inventory => player != null ? player.InventoryData : ExplorationPlayerSession.RuntimeInventory;

    private bool InBattle => gameObject.scene.name == SceneNames.Battle;

    private void Awake()
    {
        player ??= FindFirstObjectByType<PlayerCharacter>(FindObjectsInactive.Include);
        HideTemplate(rowTemplate);
        HideTemplate(materialTemplate);
        HideTemplate(plusTemplate);

        decreaseButton?.onClick.AddListener(() => SetQuantity(quantity - 1));
        increaseButton?.onClick.AddListener(() => SetQuantity(quantity + 1));
        craftButton?.onClick.AddListener(CraftSelected);
        if (quantityInput != null)
        {
            quantityInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            quantityInput.onEndEdit.AddListener(text => SetQuantity(int.TryParse(text, out int value) ? value : 1));
        }

        if (search != null)
            search.QueryChanged += _ => ApplySearch();

        if (inventoryTab != null)
            tabScale = inventoryTab.localScale;
    }

    private void OnEnable()
    {
        if (craftedText != null)
            craftedText.gameObject.SetActive(false);

        search?.Clear();
        selected = null;
        BuildRows();
        RefreshRows();

        quantity = 1;
        foreach (RecipeRow row in rows)
            ApplyRowLook(row);

        ShowSelected();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        foreach (GameObject shown in showingCrafted)
        {
            if (shown != null)
                Destroy(shown);
        }

        showingCrafted.Clear();
        if (craftedToast != null)
            Destroy(craftedToast.gameObject);
        if (inventoryTab != null)
            inventoryTab.localScale = tabScale;
        if (resultIcon != null)
            resultIcon.enabled = true;
    }

    private void BuildRows()
    {
        if (rows.Count > 0 || rowTemplate == null)
            return;

        foreach (CraftingRecipe recipe in recipes)
        {
            if (recipe == null || recipe.Result == null)
                continue;

            GameObject root = Instantiate(rowTemplate.gameObject, rowTemplate.parent, false);
            root.name = $"Recipe_{recipe.Result.CardName}";
            root.SetActive(true);
            RecipeRow row = new()
            {
                Recipe = recipe,
                Root = root,
                Background = root.GetComponent<Image>(),
                Icon = FindComponent<Image>(root.transform, "Icon"),
                Title = FindComponent<TMP_Text>(root.transform, "TextColumn/Title"),
                Subtitle = FindComponent<TMP_Text>(root.transform, "TextColumn/Subtitle"),
                NewDot = root.transform.Find("UnreadIndicator")?.gameObject
            };

            // What the card costs in a battle stays unknown until it is made: no cost on the row,
            // and the text gets its room.
            root.transform.Find("RightArea")?.gameObject.SetActive(false);

            if (row.Icon != null)
            {
                row.Icon.sprite = recipe.Result.Icon;
                row.Icon.preserveAspect = true;
                row.Icon.enabled = recipe.Result.Icon != null;
            }

            if (row.Title != null)
                row.Title.text = recipe.Result.CardName;
            if (row.Subtitle != null)
                row.Subtitle.gameObject.SetActive(true);

            if (root.TryGetComponent(out Button button))
            {
                Navigation navigation = button.navigation;
                navigation.mode = Navigation.Mode.None;
                button.navigation = navigation;
                button.onClick.AddListener(() => Select(recipe));
            }

            rows.Add(row);
        }
    }

    // The known ones, A to Z by the card they make.
    private void RefreshRows()
    {
        InventoryData inventory = Inventory;
        foreach (RecipeRow row in rows)
        {
            row.Known = CraftingProgress.IsKnown(row.Recipe, inventory);
            row.CanCraft = !InBattle && Crafting.HowManyTimes(inventory, row.Recipe) > 0;
            if (row.Subtitle != null)
            {
                string description = row.Recipe.Result.ShortDescription;
                row.Subtitle.text = row.CanCraft
                    ? description
                    : $"{description}\n<color={MissingHex}>Missing materials</color>";
            }
        }

        rows.Sort((a, b) => string.Compare(a.Recipe.Result.CardName, b.Recipe.Result.CardName, StringComparison.OrdinalIgnoreCase));

        for (int i = 0; i < rows.Count; i++)
        {
            rows[i].Root.transform.SetSiblingIndex(rowTemplate.GetSiblingIndex() + 1 + i);
            ApplyRowLook(rows[i]);
        }

        ApplySearch();
    }

    // The chosen row has the lighted panel; one that cannot be made now is dimmed; one not clicked
    // yet has the dot.
    private void ApplyRowLook(RecipeRow row)
    {
        if (row.NewDot != null)
            row.NewDot.SetActive(CraftingProgress.IsNew(row.Recipe, Inventory));

        bool isSelected = row.Recipe == selected;
        if (row.Background != null)
        {
            Sprite normal = rowSprite != null ? rowSprite : row.Background.sprite;
            row.Background.sprite = isSelected && selectedRowSprite != null ? selectedRowSprite : normal;
            row.Background.color = row.CanCraft || isSelected ? Color.white : UnavailableRowColor;
        }

        if (row.Title != null)
            row.Title.color = row.CanCraft ? TextColor : MutedTextColor;
        if (row.Subtitle != null)
            row.Subtitle.color = row.CanCraft ? MutedTextColor : DimTextColor;
        if (row.Icon != null)
            row.Icon.color = row.CanCraft ? Color.white : MutedTextColor;
    }

    private void ApplySearch()
    {
        string query = search != null ? search.Query : string.Empty;
        int shown = 0;
        bool anyKnown = false;
        foreach (RecipeRow row in rows)
        {
            bool listed = row.Known && CardSearchField.Matches(row.Recipe.Result.CardName, query);
            row.Root.SetActive(listed);
            anyKnown |= row.Known;
            if (listed)
                shown++;
        }

        if (searchBar != null)
            searchBar.SetActive(anyKnown);

        if (noRecipesState == null)
            return;

        noRecipesState.SetActive(shown == 0);
        if (noRecipesState.TryGetComponent(out TMP_Text text))
            text.text = anyKnown ? "No recipes found" : "Gather materials to learn recipes";
    }

    private void Select(CraftingRecipe recipe)
    {
        if (selected != recipe)
            quantity = 1;

        CraftingProgress.MarkOpened(recipe);
        selected = recipe;
        foreach (RecipeRow row in rows)
            ApplyRowLook(row);

        ShowSelected();
    }

    private void SetQuantity(int value)
    {
        quantity = value;
        ShowSelected();
    }

    private void ShowSelected()
    {
        InventoryData inventory = Inventory;
        CraftingRecipe shown = InBattle ? null : selected;
        int canMake = shown != null ? Crafting.HowManyTimes(inventory, shown) : 0;
        quantity = Mathf.Clamp(quantity, 1, Mathf.Max(1, canMake));

        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(shown == null);
            emptyText.text = InBattle ? "No crafting during a battle" : "Select a recipe";
        }

        foreach (GameObject part in recipeOnly)
        {
            if (part != null)
                part.SetActive(shown != null);
        }

        ShowResult(shown);
        ShowMaterials(shown, inventory);

        if (quantityInput != null)
            quantityInput.SetTextWithoutNotify(quantity.ToString());
        if (decreaseButton != null)
            decreaseButton.interactable = quantity > 1;
        if (increaseButton != null)
            increaseButton.interactable = quantity < canMake;
        if (craftButton != null)
            craftButton.interactable = canMake >= quantity && canMake > 0;
    }

    // Just the icon of the card it makes, not the whole card.
    private void ShowResult(CraftingRecipe recipe)
    {
        if (resultIcon == null)
            return;

        Sprite icon = recipe != null ? recipe.Result.Icon : null;
        resultIcon.sprite = icon;
        resultIcon.gameObject.SetActive(icon != null);
    }

    private void ShowMaterials(CraftingRecipe recipe, InventoryData inventory)
    {
        foreach (GameObject material in materials)
        {
            if (material != null)
                Destroy(material);
        }

        materials.Clear();
        if (recipe == null || materialsRow == null || materialTemplate == null)
            return;

        bool first = true;
        foreach (CraftingRecipe.Ingredient ingredient in recipe.Ingredients)
        {
            if (ingredient?.card == null)
                continue;

            if (!first && plusTemplate != null)
                materials.Add(Clone(plusTemplate));
            first = false;

            CardData card = Crafting.Choose(inventory, ingredient, quantity);
            bool tool = Crafting.IsTool(ingredient, card);
            int owned = Crafting.Owned(inventory, card);
            int needed = Crafting.Needed(ingredient, card, quantity);

            GameObject slot = Clone(materialTemplate);
            materials.Add(slot);

            RectTransform holder = slot.transform.Find("CardHolder") as RectTransform;
            if (holder != null && cardViewPrefab != null)
                PlaceCard(holder).Setup(card, null, 0, null);

            TMP_Text countText = FindComponent<TMP_Text>(slot.transform, "CountPlate/CountText");
            if (countText != null)
                countText.text = $"<color={(owned >= needed ? EnoughHex : MissingHex)}>{owned}</color> / {needed}";

            TMP_Text noteText = FindComponent<TMP_Text>(slot.transform, "NoteText");
            if (noteText != null)
            {
                CardData other = card == ingredient.orInstead ? ingredient.card : ingredient.orInstead;
                string note = tool ? "Not used up" : other != null ? $"or {other.CardName}" : string.Empty;
                noteText.text = note;
                noteText.gameObject.SetActive(note.Length > 0);
            }
        }
    }

    private void CraftSelected()
    {
        if (selected == null || InBattle || !Crafting.Craft(Inventory, selected, quantity))
            return;

        int made = selected.ResultCount * quantity;
        CardData result = selected.Result;
        RefreshRows();
        ShowSelected();
        ShowCrafted(result, made);
    }

    // Over everything in the window: its root canvas.
    private RectTransform OverlayLayer
    {
        get
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            return canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
        }
    }

    private void ShowCrafted(CardData card, int made)
    {
        RectTransform layer = OverlayLayer;
        if (layer == null)
            return;

        if (cardViewPrefab != null)
            StartCoroutine(FlyCrafted(card, made, layer));
        else
            StartCoroutine(ShowCraftedToast(card, made, layer));
    }

    // Unscaled, like all of it: the game is paused while the inventory is open.
    private IEnumerator FlyCrafted(CardData card, int made, RectTransform layer)
    {
        float unit = layer.lossyScale.y;
        CardView view = Instantiate(cardViewPrefab, layer, false);
        GameObject flyer = view.gameObject;
        showingCrafted.Add(flyer);
        RectTransform rect = (RectTransform)view.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = FlyingCardSize;
        rect.SetAsLastSibling();
        LayOver(flyer);
        view.Setup(card, made > 1 ? made : (int?)null, 0, null);

        // Not in the way of clicks while it flies. The card has a group of its own, which Setup
        // turns clickable; a second one cannot be added. Not made uninteractable through it: the
        // card's button would tint its frame half see-through, as disabled. The button is off instead.
        if (!flyer.TryGetComponent(out CanvasGroup group))
            group = flyer.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        if (flyer.TryGetComponent(out Selectable button))
            button.enabled = false;

        // Up out of the workbench icon, growing past its size and settling, the icon gone meanwhile.
        RectTransform icon = resultIcon != null && resultIcon.isActiveAndEnabled ? resultIcon.rectTransform : null;
        Vector3 from = icon != null ? icon.TransformPoint(icon.rect.center) : layer.TransformPoint(layer.rect.center);
        float fromScale = icon != null ? Mathf.Max(0.1f, icon.rect.height / FlyingCardSize.y) : 0.4f;
        Vector3 middle = from + Vector3.up * (CardRise * unit);
        if (icon != null)
            resultIcon.enabled = false;

        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / CardRiseSeconds)
        {
            float eased = EaseOutBack(t);
            rect.position = Vector3.LerpUnclamped(from, middle, eased);
            rect.localScale = Vector3.one * Mathf.LerpUnclamped(fromScale, CardBigScale, eased);
            yield return null;
        }

        rect.position = middle;
        rect.localScale = Vector3.one * CardBigScale;
        yield return new WaitForSecondsRealtime(CardHoldSeconds);
        if (resultIcon != null)
            resultIcon.enabled = true;

        // Into the backpack along an arc, faster and smaller as it goes, gone as it gets there.
        Vector3 to = inventoryTab != null ? inventoryTab.TransformPoint(inventoryTab.rect.center) : middle;
        Vector3 bend = (middle + to) * 0.5f + Vector3.up * (CardArc * unit);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / CardFlySeconds)
        {
            float eased = t * t;
            float rest = 1f - eased;
            rect.position = rest * rest * middle + 2f * rest * eased * bend + eased * eased * to;
            rect.localScale = Vector3.one * Mathf.Lerp(CardBigScale, CardSmallScale, eased);
            group.alpha = 1f - Mathf.Clamp01((eased - 0.75f) / 0.25f);
            yield return null;
        }

        showingCrafted.Remove(flyer);
        Destroy(flyer);
        StartCoroutine(BumpTab());
        yield return ShowCraftedToast(card, made, layer);
    }

    // The backpack swells for a moment, as if it took the card in.
    private IEnumerator BumpTab()
    {
        if (inventoryTab == null)
            yield break;

        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / TabBumpSeconds)
        {
            inventoryTab.localScale = tabScale * (1f + TabBump * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }

        inventoryTab.localScale = tabScale;
    }

    // In the top right corner, where "Game saved" shows too, and like it: a plate with the card's
    // icon and what was made slides in from the right, stays a moment and slides back out.
    private IEnumerator ShowCraftedToast(CardData card, int made, RectTransform layer)
    {
        if (craftedText == null)
            yield break;

        // A newer one takes the place of one still showing.
        if (craftedToast != null)
            Destroy(craftedToast.gameObject);

        RectTransform toast = CreateRect("CraftedToast", layer);
        craftedToast = toast;
        LayOver(toast.gameObject);
        toast.anchorMin = toast.anchorMax = toast.pivot = Vector2.one;
        Image plate = toast.gameObject.AddComponent<Image>();
        plate.color = ToastColor;
        plate.raycastTarget = false;

        float left = ToastPadding;
        if (card.Icon != null)
        {
            RectTransform iconRect = CreateRect("Icon", toast);
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = Vector2.one * ToastIconSize;
            iconRect.anchoredPosition = new Vector2(left, 0f);
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = card.Icon;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            left += ToastIconSize + ToastIconGap;
        }

        TMP_Text line = Instantiate(craftedText, toast, false);
        line.gameObject.SetActive(true);
        string cardName = string.IsNullOrWhiteSpace(card.CardName) ? card.name : card.CardName;
        line.text = $"Crafted {cardName} x{made}";
        line.fontSize = ToastFontSize;
        line.color = TextColor;
        line.alignment = TextAlignmentOptions.Left;
        line.textWrappingMode = TextWrappingModes.NoWrap;
        line.raycastTarget = false;
        float textWidth = line.GetPreferredValues(line.text).x;
        RectTransform lineRect = line.rectTransform;
        lineRect.anchorMin = lineRect.anchorMax = lineRect.pivot = new Vector2(0f, 0.5f);
        lineRect.sizeDelta = new Vector2(textWidth, ToastHeight);
        lineRect.anchoredPosition = new Vector2(left, 0f);

        float width = left + textWidth + ToastPadding;
        toast.sizeDelta = new Vector2(width, ToastHeight);
        toast.SetAsLastSibling();

        // Just past the right edge of the screen.
        Vector2 hidden = new(width + ToastMargin.x, ToastMargin.y);
        yield return SlideToast(toast, hidden, ToastMargin);
        yield return new WaitForSecondsRealtime(ToastHoldSeconds);
        yield return SlideToast(toast, ToastMargin, hidden);

        if (toast != null)
            Destroy(toast.gameObject);
    }

    // Quick at first, easing into place.
    private static IEnumerator SlideToast(RectTransform toast, Vector2 from, Vector2 to)
    {
        for (float t = 0f; t < 1f && toast != null; t += Time.unscaledDeltaTime / ToastSlideSeconds)
        {
            toast.anchoredPosition = Vector2.LerpUnclamped(from, to, 1f - Mathf.Pow(1f - t, 3f));
            yield return null;
        }

        if (toast != null)
            toast.anchoredPosition = to;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject shown = new(name, typeof(RectTransform));
        shown.layer = parent.gameObject.layer;
        shown.transform.SetParent(parent, false);
        return (RectTransform)shown.transform;
    }

    // On top of the window, not placed by any layout the canvas may have.
    private static void LayOver(GameObject shown)
    {
        if (!shown.TryGetComponent(out LayoutElement layout))
            layout = shown.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
    }

    // Past the end and back: a little overshoot, then settled.
    private static float EaseOutBack(float t)
    {
        float back = Mathf.Clamp01(t) - 1f;
        return 1f + 2.70158f * back * back * back + 1.70158f * back * back;
    }

    // A card to look at, not to drag or click: fitted into the holder, under its own raycast block.
    private CardView PlaceCard(RectTransform holder)
    {
        if (!holder.TryGetComponent(out CanvasGroup group))
            group = holder.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        // A holder just made in a layout gets its size at the next layout pass; until then it
        // stretches over the whole row (or the whole page, the first time it opens). So the size
        // the layout will give it, when it says.
        Vector2 room = holder.rect.size;
        if (holder.TryGetComponent(out LayoutElement layout) && layout.preferredWidth > 0f && layout.preferredHeight > 0f)
            room = new Vector2(layout.preferredWidth, layout.preferredHeight);

        CardView card = Instantiate(cardViewPrefab, holder, false);
        if (card.transform is RectTransform rect)
        {
            Vector2 size = rect.rect.size;
            float scale = size.x > 0f && size.y > 0f && room.x > 0f && room.y > 0f
                ? Mathf.Min(room.x / size.x, room.y / size.y)
                : 1f;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one * scale;
        }

        return card;
    }

    private GameObject Clone(RectTransform template)
    {
        GameObject clone = Instantiate(template.gameObject, materialsRow, false);
        clone.SetActive(true);
        return clone;
    }

    private static void HideTemplate(RectTransform template)
    {
        if (template != null)
            template.gameObject.SetActive(false);
    }

    private static T FindComponent<T>(Transform root, string path) where T : Component
    {
        Transform child = root.Find(path);
        return child != null ? child.GetComponent<T>() : null;
    }
}
