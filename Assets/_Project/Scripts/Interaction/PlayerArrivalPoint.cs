using UnityEngine;

// A place story events move the player to, e.g. from Haru's home into the forest after the
// screen pulls them in. Unlike a HouseDoorway, the player cannot walk back through it.
public sealed class PlayerArrivalPoint : MonoBehaviour
{
    [Tooltip("Camera bounds of the area this point is in.")]
    [SerializeField] private Collider2D cameraBounds;
    [SerializeField] private GameObject minimapRoot;
    [SerializeField] private bool hideMinimap;

    // No fade of its own: the caller covers the jump (the intro uses TV noise).
    public void MovePlayer(Transform player, Vector2 offset = default)
    {
        Vector3 position = transform.position + (Vector3)offset;
        PlayerTransfer.MovePlayer(player, position, cameraBounds);

        if (minimapRoot != null)
            minimapRoot.SetActive(!hideMinimap);
    }
}
