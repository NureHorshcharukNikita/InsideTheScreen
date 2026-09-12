using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class NpcInteraction : MonoBehaviour
{
    [SerializeField] private NpcCharacter npc;
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private string interactHintTemplate = "{0} - {1}";

    [Header("Overhead Speech")]
    [SerializeField] private float overheadLineDuration = 2f;
    [SerializeField] private float promptReturnDelay = 0.45f;

    private bool playerNearby;
    private bool overheadPlaying;
    private bool promptReturnPending;
    private Coroutine overheadRoutine;

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

        ResolveViewReferences();
        HideSpeechRoot();
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

        if (!GameStateManager.IsGameplay)
            return;

        if (!overheadPlaying && !promptReturnPending && promptRoot != null && !promptRoot.activeSelf)
            RefreshPrompt();

        if (GameInputSettings.GetKeyDown(GameInputAction.Interact))
            Interact();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerNearby = true;
        StopOverheadRoutine();
        RefreshPrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerNearby = false;
        StopOverheadRoutine();
        HideSpeechRoot();
    }

    private void Interact()
    {
        if (overheadPlaying || promptReturnPending)
            return;

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
            RequestPlaceholderInteraction(NpcInteractionType.Trade);
            return;
        }
    }

    private void RefreshPrompt()
    {
        if (promptText != null)
        {
            promptText.text = string.Format(
                interactHintTemplate,
                GameInputSettings.GetDisplayName(GameInputAction.Interact),
                GetPromptActionName());
        }

        SetPromptVisible(HasAnyInteraction());
    }

    private bool HasAnyInteraction()
    {
        return npc != null && (npc.CanTalk || npc.CanShowOverheadLines || npc.CanTrade);
    }

    private string GetPromptActionName()
    {
        if (npc == null)
            return "Interact";

        if (npc.CanTalk || npc.CanShowOverheadLines)
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

            if (promptText != null)
                promptText.text = lines[i];

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

    private void SetPromptVisible(bool visible)
    {
        if (promptRoot != null)
            promptRoot.SetActive(visible);

        if (promptCanvasGroup != null)
            promptCanvasGroup.alpha = visible ? 1f : 0f;
    }

    private void HideSpeechRoot()
    {
        if (promptText != null)
            promptText.text = string.Empty;

        SetPromptVisible(false);
    }

    private TMP_Text FindTextChild(string childName)
    {
        Transform child = promptRoot.transform.Find(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private void RequestPlaceholderInteraction(NpcInteractionType interaction)
    {
        if (!npc.TryInteract(interaction))
            return;

        Debug.Log($"{interaction} interaction requested for {npc.DisplayName}. UI is not implemented yet.", npc);
        npc.EndInteraction();
    }
}
