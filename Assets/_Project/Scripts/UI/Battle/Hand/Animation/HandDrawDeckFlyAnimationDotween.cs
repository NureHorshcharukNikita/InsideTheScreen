using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public static partial class HandDrawDeckFlyAnimationDotween
{
    public static IEnumerator Run(HandDrawDeckFlyContext context, int drawCount)
    {
        if (!TryPrepare(context, drawCount, out HandDeckFlyPreparedData preparedFlyData))
        {
            HandCardSequence.CopySnapshot(context.DeckManager.Hand.Cards, context.SnapshotDestination);
            yield break;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(context.HandRect);
        if (context.Settings.DealStartDelay > 0f)
            yield return new WaitForSeconds(context.Settings.DealStartDelay);
        else
            yield return null;

        yield return AnimateCardsToHand(context, preparedFlyData);

        RestoreCardsToHand(context, preparedFlyData);
        context.FlyingBuffer.Clear();
        LayoutRebuilder.ForceRebuildLayoutImmediate(context.HandRect);
        HandCardSequence.CopySnapshot(context.DeckManager.Hand.Cards, context.SnapshotDestination);
    }

}
