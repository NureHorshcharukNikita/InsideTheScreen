using Unity.Cinemachine;
using UnityEngine;

// The point the main camera follows instead of the player while a story event shows something
// else (Camera Look At step): the camera keeps its own framing and smoothing, it only follows
// this point. The point glides to what it shows and then stays on it, also when that walks.
// Destroyed when the event ends, and the camera follows what it followed before.
public sealed class StoryCameraFocus : MonoBehaviour
{
    private CinemachineCamera follower;
    private Transform followedBefore;
    private Transform target;
    private Vector3 offset;

    // Takes over the camera that is showing the game now. Null if there is none (no brain on the
    // main camera, or a camera without anything to follow).
    public static StoryCameraFocus TakeMainCamera()
    {
        Camera main = Camera.main;
        CinemachineBrain brain = main != null ? main.GetComponent<CinemachineBrain>() : null;
        if (brain == null || brain.ActiveVirtualCamera is not CinemachineCamera camera || camera.Follow == null)
            return null;

        StoryCameraFocus focus = new GameObject(nameof(StoryCameraFocus)).AddComponent<StoryCameraFocus>();
        focus.follower = camera;
        focus.followedBefore = camera.Follow;
        focus.transform.position = camera.Follow.position;
        camera.Follow = focus.transform;
        return focus;
    }

    public Vector3 Position => transform.position;

    // Glides are moved by the step; in between it stays on its target.
    public void Stay(Transform onTarget, Vector3 withOffset)
    {
        target = onTarget;
        offset = withOffset;
    }

    public void MoveTo(Vector3 position)
    {
        target = null;
        transform.position = position;
    }

    private void LateUpdate()
    {
        if (target != null)
            transform.position = target.position + offset;
    }

    private void OnDestroy()
    {
        if (follower != null && follower.Follow == transform)
            follower.Follow = followedBefore;
    }
}
