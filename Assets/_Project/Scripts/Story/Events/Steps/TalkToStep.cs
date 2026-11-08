using System;
using System.Collections;
using UnityEngine;

// Walks Haru up to an NPC and starts their talk, as if he had come over and pressed Interact: the
// NPC says what it would say now (its dialogue entries) and the talk counts as one. In a talk Hideshi
// leads (NpcData.companionLeadsTalks) both walk up together: the father to the spot the talk is held
// from, Haru to a step behind him to the side (CompanionStepsForward). Otherwise Haru walks up alone.
// The spot is Distance from the NPC below, above or beside it, on the side Haru comes from. E.g.
// Eldon's first meeting, started from a zone around him (When Player Enters) instead of the key.
[Serializable, SubclassMenu("Characters/Walk Up and Talk")]
public sealed class TalkToStep : StoryStep
{
    [SerializeField] private StoryTarget npc = new(StoryTargetType.Npc);
    [Tooltip("How far from the NPC the one leading the talk stands, in units: about where its Talk prompt shows.")]
    [SerializeField, Min(0.5f)] private float distance = 1.6f;
    [SerializeField, Min(0.1f)] private float speed = 2.4f;
    [Tooltip("Goes around walls, props and people in the way instead of through them. Straight where nothing is in the way.")]
    [SerializeField] private bool avoidObstacles = true;

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform target = npc.Resolve(context);
        Transform player = context.Player;
        DialogueManager dialogueManager = DialogueManager.Instance;
        if (target == null || player == null || dialogueManager == null
            || !target.TryGetComponent(out NpcCharacter character) || !character.CanTalk)
            yield break;

        character.SelectDialogue(out NpcDialogueEntry entry);
        CompanionFollower companion = DialogueManager.FindCompanionLeading(character, entry);
        Transform father = companion != null && CompanionStepsForward.CanPlay(target, player, companion.transform)
            ? companion.transform
            : null;

        Vector2 spot = CompanionStepsForward.FindStandSpot(target, player, father, distance);
        yield return CompanionStepsForward.Play(context, target, father, spot, speed, avoidObstacles);

        if (!dialogueManager.StartDialogue(character))
            yield break;

        yield return new WaitUntil(() => !dialogueManager.IsDialogueActive);

        // The dialogue gives back the state it started in; keep the event in control.
        GameStateManager.SetState(GameState.Dialogue);
    }

    public override string ToString()
    {
        return $"Player walks up and talks to {npc}";
    }
}
