using UnityEngine;

// Sits on a character the screen is tugging at (Tug Towards Screen step): it trembles around
// Anchor. Remembers where the character stood and its colour before the first tug, so every tug
// is measured from there. Pull Into Screen takes over from it; it is removed when the event ends.
public sealed class ScreenTug : MonoBehaviour
{
    public Vector3 Origin { get; private set; }
    public Color BaseColor { get; private set; }
    public Vector3 Anchor { get; set; }
    public float Shake { get; set; }

    public static ScreenTug Get(StoryEventContext context, Transform actor)
    {
        if (actor.TryGetComponent(out ScreenTug tug))
            return tug;

        SpriteRenderer sprite = StoryEventContext.GetSprite(actor);
        tug = actor.gameObject.AddComponent<ScreenTug>();
        tug.Origin = actor.position;
        tug.Anchor = actor.position;
        tug.BaseColor = sprite != null ? sprite.color : Color.white;
        context.OnRelease(() => Stop(tug));
        return tug;
    }

    // Puts the character back on its anchor, without the trembling, and removes the tug.
    public static void Stop(ScreenTug tug)
    {
        if (tug == null)
            return;

        tug.transform.position = tug.Anchor;
        tug.enabled = false;
        Destroy(tug);
    }

    private void Update()
    {
        transform.position = Anchor + new Vector3(Random.Range(-Shake, Shake), 0f, 0f);
    }
}
