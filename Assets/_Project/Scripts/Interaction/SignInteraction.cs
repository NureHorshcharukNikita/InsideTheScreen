using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class SignInteraction : MonoBehaviour
{
    [SerializeField] private GameObject prompt;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [SerializeField] private SpriteRenderer signSprite;
    [SerializeField] private Vector2 promptOffset = new(0f, 0.25f);
    [SerializeField] private string readAction = "Read";
    [SerializeField] private string message = "Sign text";
    [SerializeField] private float fadeDuration = 0.2f;

    private InteractionPrompt promptView;
    private bool playerNearby;
    private bool messageVisible;
    private Coroutine fadeRoutine;

    private void Reset()
    {
        if (TryGetComponent(out Collider2D trigger))
            trigger.isTrigger = true;
    }

    private void Awake()
    {
        SetPromptVisible(false);
    }

    private void Update()
    {
        if (!playerNearby || !GameStateManager.IsGameplay)
            return;

        if (GameInputSettings.GetKeyDown(GameInputAction.Interact))
            ShowMessage();
    }

    private void LateUpdate()
    {
        if (prompt != null && prompt.activeSelf)
            CenterPromptAboveSign();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerNearby = true;
        messageVisible = false;
        string keyName = GameInputSettings.GetDisplayName(GameInputAction.Interact);
        if (PromptView != null)
        {
            PromptView.ShowAction(keyName, readAction);
            ShowPrompt();
        }
        else
        {
            ShowPromptText($"{keyName} - {readAction}");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerNearby = false;
        messageVisible = false;
        SetPromptVisible(false);
    }

    private void ShowMessage()
    {
        if (messageVisible)
            return;

        messageVisible = true;
        if (PromptView != null)
        {
            PromptView.ShowMessage(message);
            ShowPrompt();
        }
        else
        {
            ShowPromptText(message);
        }
    }

    private InteractionPrompt PromptView => promptView != null ? promptView : promptView = InteractionPrompt.For(promptText);

    private void ShowPromptText(string text)
    {
        if (promptText != null)
            promptText.text = text;

        ShowPrompt();
    }

    private void ShowPrompt()
    {
        SetPromptVisible(true);
        if (PromptView == null)
            PlayFade(0f, 1f);
    }

    // The prompt plays its way in and out itself (InteractionPrompt); a plain text prompt fades in.
    private void SetPromptVisible(bool visible)
    {
        if (PromptView != null)
        {
            PromptView.SetShown(visible);
            return;
        }

        if (prompt == null)
            return;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        prompt.SetActive(visible);

        if (promptCanvasGroup != null)
            promptCanvasGroup.alpha = visible ? 1f : 0f;
    }

    private void PlayFade(float from, float to)
    {
        if (promptCanvasGroup == null)
            return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadePrompt(from, to));
    }

    private IEnumerator FadePrompt(float from, float to)
    {
        float elapsed = 0f;
        promptCanvasGroup.alpha = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeDuration);
            promptCanvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        promptCanvasGroup.alpha = to;
        fadeRoutine = null;
    }

    private void CenterPromptAboveSign()
    {
        if (prompt == null || signSprite == null)
            return;

        Bounds bounds = signSprite.bounds;
        Vector3 position = prompt.transform.position;

        prompt.transform.position = position;

        // The key cap and panel stand on top of the sign, centred.
        if (PromptView != null)
        {
            Bounds shown = PromptView.GetWorldBounds();
            prompt.transform.position += new Vector3(
                bounds.center.x + promptOffset.x - shown.center.x,
                bounds.max.y + promptOffset.y - shown.min.y,
                0f);
            return;
        }

        if (promptText == null)
        {
            prompt.transform.position = new(
                bounds.center.x + promptOffset.x,
                bounds.max.y + promptOffset.y,
                prompt.transform.position.z);
            return;
        }

        promptText.ForceMeshUpdate();
        Bounds textBounds = promptText.textBounds;
        Vector3 textMin = promptText.transform.TransformPoint(textBounds.min);
        Vector3 textMax = promptText.transform.TransformPoint(textBounds.max);
        float textCenterX = (textMin.x + textMax.x) * 0.5f;
        float targetBottomY = bounds.max.y + promptOffset.y;

        Vector3 offset = new(
            bounds.center.x + promptOffset.x - textCenterX,
            targetBottomY - textMin.y,
            0f);

        prompt.transform.position += offset;
    }
}
