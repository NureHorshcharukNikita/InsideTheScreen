using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

// One character hops at another and strikes, e.g. Haru hitting Dad with the stick he found, to see
// whether they have health at all: first a tap that does nothing, then harder, and a health bar
// shows over Dad with the damage (Overhead Health Bar). What the striker holds (Held Item, e.g. the
// stick his card turned into) is swung down as they land; holding nothing, Held is drawn at their side
// for the strike. The target flinches at the hit. Both end where they stood. Turn the striker
// towards the target first (Face step).
[Serializable, SubclassMenu("Characters/Strike")]
public sealed class StrikeStep : StoryStep
{
    private const float FlinchSeconds = 0.2f;
    private const float AfterHitSeconds = 0.15f;

    [SerializeField] private StoryTarget striker = new(StoryTargetType.Player);
    [SerializeField] private StoryTarget target = new(StoryTargetType.Npc);
    [Tooltip("What the striker swings when holding nothing (Held Item), e.g. the stick. Empty = bare hands.")]
    [SerializeField] private Sprite held;
    [Tooltip("Where it is held, as a share of the striker's sprite: across from its middle towards the target, and up from its bottom.")]
    [SerializeField] private Vector2 heldAt = new(0.3f, 0.42f);
    [Tooltip("How far it is raised before the strike and swung through it, in degrees.")]
    [SerializeField] private float raisedAngle = 60f;
    [SerializeField] private float struckAngle = -45f;
    [Tooltip("How far towards the target the striker hops, in world units.")]
    [SerializeField, Min(0f)] private float lunge = 0.45f;
    [SerializeField, Min(0f)] private float hopHeight = 0.3f;
    [SerializeField, Min(0.05f)] private float hopSeconds = 0.22f;
    [SerializeField] private AudioClip hitSound; // [SFX] sfx_stick_hit
    [SerializeField, Range(0f, 1f)] private float hitVolume = 0.8f;
    [Tooltip("How far the target flinches at the hit, in world units.")]
    [SerializeField, Min(0f)] private float flinch = 0.08f;
    [Tooltip("Taken off the target's health, shown over their head. 0 = no bar, just the hit.")]
    [SerializeField, Min(0)] private int damage;
    [Tooltip("Deals this card's damage instead, as the card reads now, e.g. the stick's card. Empty = Damage.")]
    [SerializeField] private CardData card;
    [Tooltip("The target's health from the data they fight with: full before the hit, out of that, e.g. Hideshi Battle Data. Empty = Health and Max Health.")]
    [SerializeField] private EnemyData targetFighter;
    [Tooltip("The target's health before the hit, and at most.")]
    [SerializeField, Min(1)] private int health = 30;
    [SerializeField, Min(1)] private int maxHealth = 30;
    [SerializeField] private OverheadHealthBar healthBar;

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform attacker = striker.Resolve(context);
        Transform struck = target.Resolve(context);
        if (attacker == null || struck == null)
            yield break;

        context.TakeControl(attacker);
        context.TakeControl(struck);

        Vector3 start = attacker.position;
        Vector3 toTarget = struck.position - start;
        toTarget.z = 0f;
        Vector3 step = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized * Mathf.Min(lunge, toTarget.magnitude * 0.5f) : Vector3.zero;
        float side = toTarget.x >= 0f ? 1f : -1f;

        SpriteRenderer body = attacker.GetComponentInChildren<SpriteRenderer>();
        HeldItem holding = HeldItem.Of(attacker);
        GameObject swung = holding != null ? null : CreateHeld(body);
        context.OnRelease(() => Remove(swung));
        if (holding != null)
            holding.Side = side;

        // The hop at the target, the stick coming down as the striker lands.
        for (float time = 0f; time < hopSeconds; time += GameClock.DeltaTime)
        {
            float progress = time / hopSeconds;
            attacker.position = start + step * progress + Vector3.up * (hopHeight * 4f * progress * (1f - progress));
            float angle = Mathf.Lerp(raisedAngle, struckAngle, progress * progress);
            if (holding != null)
                holding.Angle = angle;
            else
                Hold(swung, body, side, angle);
            yield return null;
        }

        attacker.position = start + step;
        if (holding != null)
            holding.Angle = struckAngle;
        else
            Hold(swung, body, side, struckAngle);

        if (hitSound != null)
            AudioManager.TryPlaySfx(hitSound, hitVolume);

        int dealt = Damage;
        int most = targetFighter != null ? Mathf.Max(1, targetFighter.maxHealth) : maxHealth;
        if (dealt > 0)
            OverheadHealthBar.Show(healthBar, struck, targetFighter != null ? most : health, most, dealt);

        yield return Flinch(struck, side);
        yield return new WaitForGameSeconds(AfterHitSeconds);
        Remove(swung);
        if (holding != null)
            holding.Angle = HeldItem.RestAngle;

        // And back to where it stood, a smaller hop.
        Vector3 from = attacker.position;
        for (float time = 0f; time < hopSeconds; time += GameClock.DeltaTime)
        {
            float progress = time / hopSeconds;
            attacker.position = Vector3.Lerp(from, start, progress) + Vector3.up * (hopHeight * 2f * progress * (1f - progress));
            yield return null;
        }

        attacker.position = start;
    }

    private GameObject CreateHeld(SpriteRenderer body)
    {
        if (held == null)
            return null;

        GameObject swung = new("Held");
        SpriteRenderer drawn = swung.AddComponent<SpriteRenderer>();
        drawn.sprite = held;
        if (body != null)
        {
            drawn.sortingLayerID = body.sortingLayerID;
            drawn.sortingOrder = body.sortingOrder + 1;
            drawn.sharedMaterial = body.sharedMaterial;
        }

        return swung;
    }

    // At the striker's hand on the target's side, tilted the way it swings and mirrored with the side.
    private void Hold(GameObject swung, SpriteRenderer body, float side, float angle)
    {
        if (swung == null || body == null)
            return;

        Bounds bounds = body.bounds;
        swung.transform.position = new Vector3(
            bounds.center.x + side * heldAt.x * bounds.size.x,
            bounds.min.y + heldAt.y * bounds.size.y,
            body.transform.position.z);
        swung.transform.localScale = new Vector3(side, 1f, 1f);
        swung.transform.rotation = Quaternion.Euler(0f, 0f, side * angle);
    }

    // A quick shake away from the hit and back.
    private IEnumerator Flinch(Transform struck, float side)
    {
        Vector3 rest = struck.position;
        for (float time = 0f; time < FlinchSeconds; time += GameClock.DeltaTime)
        {
            float progress = time / FlinchSeconds;
            struck.position = rest + Vector3.right * (side * flinch * Mathf.Sin(progress * Mathf.PI * 3f) * (1f - progress));
            yield return null;
        }

        struck.position = rest;
    }

    private static void Remove(GameObject swung)
    {
        if (swung != null)
            Object.Destroy(swung);
    }

    private int Damage => card != null ? Mathf.Max(0, CardDescriptionFormatter.PrimaryValue(card)) : damage;

    public override string ToString()
    {
        int dealt = Damage;
        string with = card != null ? $" with {card.CardName}" : string.Empty;
        return dealt > 0 ? $"{striker} strikes {target}{with} (-{dealt})" : $"{striker} strikes {target}{with}";
    }
}
