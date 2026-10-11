using Unity.Cinemachine;
using UnityEngine;

// Moves the player within the scene and switches the camera bounds (doorways, story transitions),
// silencing the outdoor ambience in indoor areas.
// Works on every Cinemachine camera in the scene (the main one and the minimap's), so callers only
// say which area the player goes to. Inactive ones count too: the minimap is hidden in houses.
public static class PlayerTransfer
{
    public static void MovePlayer(Transform player, Vector3 position, Collider2D cameraBounds)
    {
        Vector3 oldPosition = player.position;
        Vector3 newPosition = position;
        newPosition.z = oldPosition.z;

        if (player.TryGetComponent(out Rigidbody2D body))
        {
            body.linearVelocity = Vector2.zero;
            body.position = newPosition;
        }

        player.position = newPosition;
        Physics2D.SyncTransforms();
        SetCameraBounds(cameraBounds);

        // Cameras following the player jump with it instead of panning across the map.
        foreach (CinemachineCamera camera in Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Transform target = camera.Follow;
            if (target == null || !target.IsChildOf(player))
                continue;

            camera.OnTargetObjectWarped(target, newPosition - oldPosition);
            camera.PreviousStateIsValid = false;
        }

        ExplorationPlayerSession.SavePosition(newPosition);
    }

    public static void SetCameraBounds(Collider2D bounds)
    {
        if (bounds == null)
            return;

        // No birds or wind indoors.
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetAmbienceMuted(bounds.TryGetComponent(out IndoorArea _));

        foreach (CinemachineConfiner2D confiner in Object.FindObjectsByType<CinemachineConfiner2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            SetConfinerBounds(confiner, bounds);
    }

    private static void SetConfinerBounds(CinemachineConfiner2D confiner, Collider2D bounds)
    {
        if (confiner.BoundingShape2D == bounds)
            return;

        confiner.BoundingShape2D = bounds;
        confiner.InvalidateBoundingShapeCache();
        CinemachineCamera camera = confiner.GetComponent<CinemachineCamera>();
        if (camera != null)
            camera.PreviousStateIsValid = false;
    }
}
