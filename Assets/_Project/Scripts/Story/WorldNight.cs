using System;
using System.Collections.Generic;
using UnityEngine;

// The time of day, from the story: dusk after the wolf in the forest, night from the trader's
// campfire until the morning in Garr's barn, and the night the crystal is forged. A night is on
// while all of its conditions are met; the first one on gives the tint, its own colour or the night
// colour. The tint eases from one to the next as the story moves on (also when a scene loads in the
// middle of a night, then at once); story events that tint the world themselves (Tint World) hold
// it meanwhile and hand it back when they end.
public sealed class WorldNight : MonoBehaviour
{
    [Serializable]
    private sealed class Night
    {
        [Tooltip("Only a name for the inspector, e.g. \"First night in Leymar\".")]
        public string name;
        [Tooltip("Its own tint, e.g. a lighter dusk. Alpha 0 = the Night Color.")]
        public Color color;
        [Tooltip("All of them must be met.")]
        public List<StoryCondition> conditions = new();
    }

    private static WorldNight instance;

    [Tooltip("Alpha is how dark: 0 = no tint.")]
    [SerializeField] private Color nightColor = new(0.03f, 0.05f, 0.15f, 0.42f);
    [Tooltip("How long dusk and dawn take when the story moves on.")]
    [SerializeField, Min(0f)] private float fadeSeconds = 6f;
    [SerializeField] private List<Night> nights = new();

    private Color shown;

    // After a story event tinted the world itself: back to the time of day, or no tint at all in a
    // scene without a WorldNight.
    public static void Restore(WorldTint tint, float seconds)
    {
        if (instance != null)
            instance.Apply(seconds);
        else if (tint != null)
            tint.SetTint(Color.clear, seconds);
    }

    private void OnEnable()
    {
        instance = this;
        StoryFlagManager.Changed += Refresh;
        QuestManager.QuestChanged += RefreshForQuest;
    }

    // Once the scene has loaded, so the tint is made in it (and goes with it to the battle and back).
    private void Start()
    {
        Apply(0f);
    }

    private void OnDisable()
    {
        StoryFlagManager.Changed -= Refresh;
        QuestManager.QuestChanged -= RefreshForQuest;
        if (instance == this)
            instance = null;
    }

    private void RefreshForQuest(QuestData quest, QuestStatus status)
    {
        Refresh();
    }

    // Dusk or dawn only when the time of day changes, not on every flag.
    private void Refresh()
    {
        if (!WorldTint.IsHeldByStory && CurrentTint() != shown)
            Apply(fadeSeconds);
    }

    private void Apply(float seconds)
    {
        shown = CurrentTint();
        WorldTint.Get().SetTint(shown, seconds);
    }

    private Color CurrentTint()
    {
        foreach (Night night in nights)
        {
            if (night != null && night.conditions != null && night.conditions.Count > 0 && StoryCondition.AreMet(night.conditions))
                return night.color.a > 0f ? night.color : nightColor;
        }

        return new Color(nightColor.r, nightColor.g, nightColor.b, 0f);
    }
}
