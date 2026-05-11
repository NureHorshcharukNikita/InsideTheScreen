using System.Collections;
using UnityEngine;

public sealed partial class CardBattleDragHandler
{
    private IEnumerator AnimateReturnToHandCoroutine()
    {
        try
        {
            if (rectTransform == null || handParent == null)
                yield break;

            Vector3 from = rectTransform.position;
            Vector3 to = handWorldPosition;
            float duration = Mathf.Max(1e-4f, returnAnimationDuration);

            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                float eased = EaseOutCubic(Mathf.Clamp01(t));
                rectTransform.position = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }

            rectTransform.position = to;
        }
        finally
        {
            RestoreCachedHandSlot();
            returnCoroutine = null;
        }
    }

    private static float EaseOutCubic(float t)
    {
        float u = 1f - t;
        return 1f - u * u * u;
    }
}
