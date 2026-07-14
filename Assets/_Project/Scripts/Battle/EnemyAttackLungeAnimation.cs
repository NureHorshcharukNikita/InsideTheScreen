using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyAttackLungeAnimation : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float lungeDuration = 0.28f;
    [SerializeField] private float returnDuration = 0.22f;
    [SerializeField] private float impactDistance = 1.15f;

    [Header("Impact")]
    [SerializeField] private float impactPause = 0.08f;
    [SerializeField] private float impactPunchScale = 0.12f;
    [SerializeField] private float targetPunchDistance = 0.12f;
    [SerializeField] private int sortingOrderBoost = 10;

    private Tween activeTween;

    private void OnDisable()
    {
        KillActiveTween();
    }

    public IEnumerator Play(EnemyCharacter attacker, Character target, Action onImpact, Func<bool> shouldSkip = null)
    {
        if (attacker == null)
        {
            onImpact?.Invoke();
            yield break;
        }

        Transform attackerTransform = attacker.transform;
        Vector3 startPosition = attackerTransform.position;
        SpriteRenderer attackerRenderer = attacker.GetComponent<SpriteRenderer>();
        SpriteRenderer targetRenderer = target != null ? target.GetComponent<SpriteRenderer>() : null;
        int originalSortingOrder = attackerRenderer != null ? attackerRenderer.sortingOrder : 0;

        if (ShouldSkip(shouldSkip) || target == null || target == attacker)
        {
            onImpact?.Invoke();
            yield return PlayCastPulse(attackerTransform, shouldSkip);
            attackerTransform.position = startPosition;
            yield break;
        }

        RaiseAboveTarget(attackerRenderer, targetRenderer, originalSortingOrder);

        Vector3 impactPosition = GetImpactPosition(startPosition, target.transform.position);
        yield return MoveStraight(attackerTransform, startPosition, impactPosition, lungeDuration, Ease.OutCubic, shouldSkip);

        if (ShouldSkip(shouldSkip))
        {
            attackerTransform.position = startPosition;
            RestoreSortingOrder(attackerRenderer, originalSortingOrder);
            onImpact?.Invoke();
            yield break;
        }

        onImpact?.Invoke();
        yield return PlayImpactPulse(attackerTransform, target.transform, startPosition, shouldSkip);

        if (!ShouldSkip(shouldSkip))
            yield return MoveStraight(attackerTransform, attackerTransform.position, startPosition, returnDuration, Ease.InCubic, shouldSkip);

        attackerTransform.position = startPosition;
        RestoreSortingOrder(attackerRenderer, originalSortingOrder);
    }

    private void RaiseAboveTarget(SpriteRenderer attackerRenderer, SpriteRenderer targetRenderer, int originalSortingOrder)
    {
        if (attackerRenderer == null)
            return;

        int targetSortingOrder = targetRenderer != null ? targetRenderer.sortingOrder : originalSortingOrder;
        attackerRenderer.sortingOrder = Mathf.Max(originalSortingOrder, targetSortingOrder + sortingOrderBoost);
    }

    private static void RestoreSortingOrder(SpriteRenderer attackerRenderer, int originalSortingOrder)
    {
        if (attackerRenderer != null)
            attackerRenderer.sortingOrder = originalSortingOrder;
    }

    private Vector3 GetImpactPosition(Vector3 attackerPosition, Vector3 targetPosition)
    {
        float xDistance = targetPosition.x - attackerPosition.x;

        if (Mathf.Abs(xDistance) <= 1e-4f)
            return attackerPosition;

        float direction = Mathf.Sign(xDistance);
        float travelDistance = Mathf.Max(0f, Mathf.Abs(xDistance) - impactDistance);
        return new Vector3(
            attackerPosition.x + direction * travelDistance,
            attackerPosition.y,
            attackerPosition.z);
    }

    private IEnumerator MoveStraight(Transform target, Vector3 from, Vector3 to, float duration, Ease ease, Func<bool> shouldSkip)
    {
        KillActiveTween();

        float safeDuration = Mathf.Max(1e-4f, duration);
        activeTween = DOVirtual.Float(0f, 1f, safeDuration, progress =>
        {
            if (target == null)
                return;

            float eased = DOVirtual.EasedValue(0f, 1f, progress, ease);
            target.position = Vector3.LerpUnclamped(from, to, eased);
        }).SetEase(Ease.Linear);

        yield return WaitTweenOrSkip(shouldSkip);

        KillActiveTween();
    }

    private IEnumerator PlayImpactPulse(Transform attackerTransform, Transform targetTransform, Vector3 attackerStartPosition, Func<bool> shouldSkip)
    {
        KillActiveTween();

        Vector3 attackerScale = attackerTransform != null ? attackerTransform.localScale : Vector3.one;
        Vector3 targetPosition = targetTransform != null ? targetTransform.position : Vector3.zero;
        Sequence sequence = DOTween.Sequence();
        activeTween = sequence;

        if (attackerTransform != null && impactPunchScale > 0f)
            sequence.Join(attackerTransform.DOPunchScale(Vector3.one * impactPunchScale, impactPause, 6, 0.65f));

        if (targetTransform != null && targetPunchDistance > 0f)
        {
            float xDelta = targetTransform.position.x - attackerStartPosition.x;
            Vector3 knockDirection = Mathf.Abs(xDelta) <= 1e-4f
                ? Vector3.left
                : Vector3.right * Mathf.Sign(xDelta);
            sequence.Join(targetTransform.DOPunchPosition(knockDirection * targetPunchDistance, impactPause, 6, 0.7f));
        }

        yield return WaitTweenOrSkip(shouldSkip);

        KillActiveTween();

        if (attackerTransform != null)
            attackerTransform.localScale = attackerScale;
        if (targetTransform != null)
            targetTransform.position = targetPosition;
    }

    private IEnumerator PlayCastPulse(Transform attackerTransform, Func<bool> shouldSkip)
    {
        if (attackerTransform == null || impactPunchScale <= 0f || ShouldSkip(shouldSkip))
            yield break;

        KillActiveTween();
        Vector3 startScale = attackerTransform.localScale;
        activeTween = attackerTransform.DOPunchScale(Vector3.one * impactPunchScale, impactPause, 6, 0.65f);

        yield return WaitTweenOrSkip(shouldSkip);

        KillActiveTween();
        attackerTransform.localScale = startScale;
    }

    private IEnumerator WaitTweenOrSkip(Func<bool> shouldSkip)
    {
        while (activeTween != null && activeTween.IsActive() && activeTween.IsPlaying() && !ShouldSkip(shouldSkip))
            yield return null;
    }

    private static bool ShouldSkip(Func<bool> shouldSkip)
    {
        return shouldSkip != null && shouldSkip();
    }

    private void KillActiveTween()
    {
        if (activeTween == null)
            return;

        if (activeTween.IsActive())
            activeTween.Kill();

        activeTween = null;
    }
}
