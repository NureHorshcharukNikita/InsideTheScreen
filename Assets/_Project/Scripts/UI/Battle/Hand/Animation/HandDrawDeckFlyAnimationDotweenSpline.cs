using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public static partial class HandDrawDeckFlyAnimationDotween
{
    private static bool TryGetSceneSplineAnchors(HandDrawDeckFlyContext context, out Vector3 sceneStart, out Vector3 sceneEnd)
    {
        sceneStart = default;
        sceneEnd = default;
        if (!TryEvaluateSceneSplineWorld(context, 0f, out sceneStart))
            return false;
        if (!TryEvaluateSceneSplineWorld(context, 1f, out sceneEnd))
            return false;
        return true;
    }

    private static bool TryEvaluateSceneSplineWorld(HandDrawDeckFlyContext context, float t, out Vector3 worldPoint)
    {
        worldPoint = default;
        if (context.FlightSpline == null || context.FlightSpline.Spline == null)
            return false;

        float3 point = context.FlightSpline.EvaluatePosition(Mathf.Clamp01(t));
        worldPoint = new Vector3(point.x, point.y, point.z);
        return true;
    }

    private static Vector3 ResolveStackStartWorld(HandDrawDeckFlyContext context)
    {
        if (TryEvaluateSceneSplineWorld(context, 0f, out Vector3 splineStart))
            return splineStart;

        return context.HandPanel.position;
    }

    private static Vector3 EvaluateFlightPosition(
        HandDrawDeckFlyContext context,
        bool hasSceneSpline,
        Vector3 start,
        Vector3 end,
        Vector3 sceneSplineStart,
        Vector3 sceneSplineEnd,
        float progress)
    {
        if (hasSceneSpline && TryEvaluateSceneSplineWorld(context, progress, out Vector3 splineWorldPoint))
        {
            Vector3 offsetFromStart = start - sceneSplineStart;
            Vector3 anchoredPoint = splineWorldPoint + offsetFromStart;
            Vector3 endCorrection = end - (sceneSplineEnd + offsetFromStart);
            return anchoredPoint + endCorrection * progress;
        }

        return Vector3.Lerp(start, end, progress);
    }
}
