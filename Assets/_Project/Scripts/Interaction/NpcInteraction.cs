using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// The talk zone of an NPC. When the player stands in several zones at once, only the focused
// NPC shows its prompt and answers the Interact key:
// 1. a companion that follows the player (Hideshi) comes last, so walking together never
//    blocks talking to others. Something to pick up or a chest in reach (ForagePickup, LootChest)
//    comes before any NPC the player does not look at, e.g. Dad waiting behind Haru by the
//    bushes; an NPC he looks at (in front of him) comes first;
// 2. NPCs in front of the player (where they look) come before the ones beside or behind;
// 3. then the closest one.
// An NPC walking off somewhere by itself (BackgroundWalk) has no prompt until it stands there.
// A zone can also talk for several NPCs standing together (Together), e.g. one prompt between Sorrel
// and Fenna until they have been met together: their own zones say nothing while it answers, and
// the talk is its NPC's (Sorrel's), which the others speak in, or a story event it plays instead
// (Plays Event), e.g. their first meeting with Fenna coming out from behind her mother.
[RequireComponent(typeof(Collider2D))]
public sealed class NpcInteraction : MonoBehaviour
{
    // cos 60°: an NPC up to 60° away from where the player looks counts as in front.
    private const float FrontCone = 0.5f;

    private static readonly List<NpcInteraction> Nearby = new();
    // Zones that talk for others too (Together), wherever the player is.
    private static readonly List<NpcInteraction> Shared = new();
    private static Transform player;
    private static PlayerMovement playerMovement;

    [Tooltip("Whose talk it starts. Empty = the NPC this zone is on.")]
    [SerializeField] private NpcCharacter npc;
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private string interactHintTemplate = "{0} - {1}";

    [Header("Talking Together")]
    [Tooltip("Others this zone talks for too, e.g. Fenna beside Sorrel: one prompt between them, and their own zones (and its NPC's) say nothing while this one answers. Empty = only its NPC, in its own zone.")]
    [SerializeField] private List<NpcCharacter> together = new();
    [Tooltip("It answers only while all of these are met, e.g. Sorrel and Fenna's zone until they have been met together (a1_met_fox_family not set). Empty = always.")]
    [SerializeField] private List<StoryCondition> answersWhile = new();
    [Tooltip("The Interact key plays this story event (Start: When Called) instead of the NPC's talk while it can start, e.g. Sorrel and Fenna's first meeting with Fenna coming out from behind her mother, or Dad's talk after the gathering, which walks Haru round to his left first. Otherwise the NPC's own talk. Empty = always the NPC's talk.")]
    [SerializeField] private StoryEvent playsEvent;

    [Header("Overhead Speech")]
    [SerializeField] private float overheadLineDuration = 2f;
    [SerializeField] private float promptReturnDelay = 0.45f;

    private InteractionPrompt prompt;
    private bool playerNearby;
    private bool overheadPlaying;
    private bool promptReturnPending;
    private Coroutine overheadRoutine;
    private CompanionFollower companion;

    private void Reset()
    {
        if (TryGetComponent(out Collider2D trigger))
            trigger.isTrigger = true;

        npc = GetComponent<NpcCharacter>();
    }

    private void Awake()
    {
        if (npc == null)
            npc = GetComponent<NpcCharacter>();

        if (npc != null)
            companion = npc.GetComponent<CompanionFollower>();

        ResolveViewReferences();
        HideSpeechRoot();
    }

    private void OnEnable()
    {
        if (together != null && together.Count > 0 && !Shared.Contains(this))
            Shared.Add(this);
    }

    // Hidden or destroyed while the player stands in the zone: no exit message comes then.
    private void OnDisable()
    {
        playerNearby = false;
        Nearby.Remove(this);
        Shared.Remove(this);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlayMode()
    {
        Nearby.Clear();
        Shared.Clear();
    }

    private void ResolveViewReferences()
    {
        if (promptRoot == null)
            promptRoot = transform.Find("PromptCanvas")?.gameObject;

        if (promptRoot == null)
            return;

        if (promptCanvasGroup == null)
            promptCanvasGroup = promptRoot.GetComponent<CanvasGroup>();

        if (promptText == null)
            promptText = FindTextChild("PromptText");
    }

    private void Update()
    {
        if (!playerNearby || npc == null)
            return;

        // Nothing to press while a dialogue, a story event or a menu has the game: the prompt and
        // any overhead line go, and the prompt comes back after.
        if (!GameStateManager.IsGameplay)
        {
            if (promptRoot != null && (PromptShown || overheadPlaying || promptReturnPending))
            {
                StopOverheadRoutine();
                HideSpeechRoot();
            }

            return;
        }

        bool idle = !overheadPlaying && !promptReturnPending && promptRoot != null;
        if (GetFocused() != this || YieldsToThingInReach)
        {
            if (idle && PromptShown)
                HideSpeechRoot();
            return;
        }

        if (idle && !PromptShown)
            RefreshPrompt();

        if (GameInputSettings.GetKeyDown(GameInputAction.Interact))
            Interact();
    }

    // The prompt itself is shown from Update, once it is clear which NPC has the focus.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        player = other.transform;
        playerMovement = other.GetComponentInParent<PlayerMovement>();
        playerNearby = true;
        if (!Nearby.Contains(this))
            Nearby.Add(this);
        StopOverheadRoutine();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerNearby = false;
        Nearby.Remove(this);
        StopOverheadRoutine();
        HideSpeechRoot();
    }

    // An NPC Haru looks at, other than Hideshi following along, answers the Interact key right now:
    // things on the ground and chests let it go first (ForagePickup, LootChest).
    public static bool AnyTakesKeyFromThings =>
        GameStateManager.IsGameplay && GetFocused() is { TakesKeyFromThings: true };

    private bool TakesKeyFromThings => !IsFollowingCompanion && IsInFrontOfPlayer();

    // Hideshi walking along, or an NPC beside or behind Haru, leaves the key to something in reach.
    private bool YieldsToThingInReach => !TakesKeyFromThings && (ForagePickup.AnyInReach || LootChest.AnyInReach);

    private static NpcInteraction GetFocused()
    {
        NpcInteraction focused = null;
        foreach (NpcInteraction candidate in Nearby)
        {
            if (candidate != null && candidate.HasAnyInteraction() && (focused == null || candidate.ComesBefore(focused)))
                focused = candidate;
        }

        return focused;
    }

    private bool ComesBefore(NpcInteraction other)
    {
        bool following = IsFollowingCompanion;
        if (following != other.IsFollowingCompanion)
            return !following;

        bool inFront = IsInFrontOfPlayer();
        if (inFront != other.IsInFrontOfPlayer())
            return inFront;

        return DistanceToPlayer() < other.DistanceToPlayer();
    }

    private bool IsFollowingCompanion => companion != null && companion.IsFollowing;

    private bool IsInFrontOfPlayer()
    {
        if (player == null || playerMovement == null)
            return false;

        Vector2 toNpc = transform.position - player.position;
        return toNpc.sqrMagnitude > 0.0001f && Vector2.Dot(playerMovement.FacingDirection, toNpc.normalized) >= FrontCone;
    }

    // From the zone itself: on an NPC where it stands, between them for a zone talking for several.
    private float DistanceToPlayer()
    {
        return player != null ? Vector2.Distance(player.position, transform.position) : 0f;
    }

    private void Interact()
    {
        if (overheadPlaying || promptReturnPending)
            return;

        if (PlaysEventNow)
        {
            if (playsEvent.TryPlay())
                HideSpeechRoot();
            return;
        }

        if (npc.CanTalk && DialogueManager.Instance != null && DialogueManager.Instance.StartDialogue(npc))
        {
            HideSpeechRoot();
            return;
        }

        if (npc.CanShowOverheadLines)
        {
            PlayOverheadSpeech();
            return;
        }

        if (npc.CanTrade)
        {
            OpenTrade();
            return;
        }
    }

    private void RefreshPrompt()
    {
        string keyName = GameInputSettings.GetDisplayName(GameInputAction.Interact);
        string action = GetPromptActionName();
        if (Prompt != null)
            Prompt.ShowAction(keyName, action);
        else if (promptText != null)
            promptText.text = string.Format(interactHintTemplate, keyName, action);

        SetPromptVisible(HasAnyInteraction());
    }

    // An overhead line: on the prompt's panel, without the key.
    private void ShowLine(string line)
    {
        if (Prompt != null)
            Prompt.ShowMessage(line);
        else if (promptText != null)
            promptText.text = line;
    }

    private InteractionPrompt Prompt => prompt != null ? prompt : prompt = InteractionPrompt.For(promptText);

    // Nothing while it walks off somewhere by itself (BackgroundWalk), e.g. Garr on his way to the
    // forge, nor while a zone talking for it together with others answers instead (Together).
    private bool HasAnyInteraction()
    {
        if (npc == null || !npc.gameObject.activeInHierarchy || !Answers || SpokenForElsewhere || BackgroundWalk.IsWalking(npc))
            return false;

        return PlaysEventNow || npc.CanTalk || npc.CanShowOverheadLines || npc.CanTrade;
    }

    // Its story event can start now: the key plays it instead of the talk.
    private bool PlaysEventNow => playsEvent != null && playsEvent.IsReady;

    private bool Answers => StoryCondition.AreMet(answersWhile);

    // A zone talking for this NPC together with others answers now: this one says nothing.
    private bool SpokenForElsewhere
    {
        get
        {
            foreach (NpcInteraction shared in Shared)
            {
                if (shared != null && shared != this && shared.Answers && shared.TalksFor(npc))
                    return true;
            }

            return false;
        }
    }

    private bool TalksFor(NpcCharacter character)
    {
        return character != null && (character == npc || together.Contains(character));
    }

    private string GetPromptActionName()
    {
        if (npc == null)
            return "Interact";

        if (PlaysEventNow || npc.CanTalk || npc.CanShowOverheadLines)
            return "Talk";

        if (npc.CanTrade)
            return "Trade";

        return "Interact";
    }

    private void PlayOverheadSpeech()
    {
        StopOverheadRoutine();
        overheadRoutine = StartCoroutine(PlayOverheadSpeechRoutine());
    }

    private IEnumerator PlayOverheadSpeechRoutine()
    {
        IReadOnlyList<string> lines = npc != null ? npc.OverheadLines : null;
        if (lines == null || lines.Count == 0)
            yield break;

        overheadPlaying = true;
        promptReturnPending = false;
        SetPromptVisible(true);

        for (int i = 0; i < lines.Count; i++)
        {
            if (!playerNearby)
                break;

            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            ShowLine(lines[i]);

            yield return new WaitForSeconds(overheadLineDuration);
        }

        overheadPlaying = false;
        promptReturnPending = true;
        HideSpeechRoot();

        yield return new WaitForSeconds(promptReturnDelay);

        promptReturnPending = false;
        if (playerNearby && GameStateManager.IsGameplay)
            RefreshPrompt();

        overheadRoutine = null;
    }

    private void StopOverheadRoutine()
    {
        if (overheadRoutine == null)
            return;

        StopCoroutine(overheadRoutine);
        overheadRoutine = null;
        overheadPlaying = false;
        promptReturnPending = false;
    }

    // The prompt plays its way in and out itself (InteractionPrompt).
    private void SetPromptVisible(bool visible)
    {
        if (Prompt != null)
        {
            Prompt.SetShown(visible);
            return;
        }

        if (promptRoot != null)
            promptRoot.SetActive(visible);

        if (promptCanvasGroup != null)
            promptCanvasGroup.alpha = visible ? 1f : 0f;
    }

    // Shown, or on its way in.
    private bool PromptShown => Prompt != null ? Prompt.IsShown : promptRoot.activeSelf;

    private void HideSpeechRoot()
    {
        // Its text stays while it slides out.
        if (Prompt == null && promptText != null)
            promptText.text = string.Empty;

        SetPromptVisible(false);
    }

    private TMP_Text FindTextChild(string childName)
    {
        Transform child = promptRoot.transform.Find(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private void OpenTrade()
    {
        if (npc == null || !npc.CanTrade)
            return;

        PlayerCharacter player = FindFirstObjectByType<PlayerCharacter>(FindObjectsInactive.Include);
        if (!ShopWindow.Open(npc.Shop, npc, player))
        {
            npc.EndInteraction();
            return;
        }

        HideSpeechRoot();
    }
}
