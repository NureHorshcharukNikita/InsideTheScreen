using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public static class HandSplineSetup
{
    public static SplineContainer EnsureFlightSplineExists(
        Transform handTransform,
        DeckUI deckUi,
        SplineContainer currentSpline,
        bool autoCreateIfMissing)
    {
        if (currentSpline != null || !autoCreateIfMissing)
            return currentSpline;

        return CreateDefaultFlightSpline(handTransform, deckUi);
    }

    public static SplineContainer CreateDefaultFlightSpline(Transform handTransform, DeckUI deckUi)
    {
        RectTransform canvasRoot = handTransform.GetComponentInParent<Canvas>()?.transform as RectTransform;
        Transform parent = canvasRoot != null ? canvasRoot : handTransform;

        var splineObject = new GameObject("HandFlightSpline");
        splineObject.transform.SetParent(parent, worldPositionStays: false);
        splineObject.transform.localPosition = Vector3.zero;
        splineObject.transform.localRotation = Quaternion.identity;
        splineObject.transform.localScale = Vector3.one;

        var container = splineObject.AddComponent<SplineContainer>();
        var spline = new Spline();

        Vector3 startWorld = deckUi != null ? deckUi.transform.position : handTransform.position + Vector3.right * 400f;
        Vector3 endWorld = handTransform.position;
        Vector3 upDirection = parent.lossyScale.y >= 0f ? parent.up : -parent.up;
        Vector3 middleWorld = Vector3.Lerp(startWorld, endWorld, 0.5f) + upDirection * 220f;

        Vector3 localStart = splineObject.transform.InverseTransformPoint(startWorld);
        Vector3 localMiddle = splineObject.transform.InverseTransformPoint(middleWorld);
        Vector3 localEnd = splineObject.transform.InverseTransformPoint(endWorld);

        spline.Add(new BezierKnot((float3)localStart));
        spline.Add(new BezierKnot((float3)localMiddle));
        spline.Add(new BezierKnot((float3)localEnd));
        container.Spline = spline;

        return container;
    }
}
