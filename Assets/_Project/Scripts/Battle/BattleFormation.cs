using System;
using System.Collections.Generic;
using UnityEngine;

// Where the fighters stand when a battle is not one on one. Each side stands in file along a
// diagonal, like soldiers lined up, pointing down and outwards like an arrow: the front one
// (file[0]: the player, the enemy from the map) furthest out and lowest, on the one-on-one ground
// line on both sides; each next one a step nearer the middle, a little higher up, a little smaller
// (as if further away) and drawn behind. The sides then share out the room left on screen
// (SpreadSides), so the battle sits about in the middle. The more fighters, the smaller everyone is.
internal static class BattleFormation
{
    // How a side stands in file.
    public readonly struct FileShape
    {
        // Distance between neighbours as a share of their two widths: below 1 they overlap,
        // above 1 there is a gap between them.
        public readonly float StepX;
        // Share of the front one's height each one back stands higher up.
        public readonly float StepY;
        // How much smaller each one back is.
        public readonly float RankScale;
        // How far below the one-on-one ground line the front one stands, in world units.
        public readonly float FrontDrop;
        // How big the side is two against three, against its one-on-one size (see CrowdScale).
        public readonly float SizeTwoOnThree;

        public FileShape(float stepX, float stepY, float rankScale, float frontDrop, float sizeTwoOnThree)
        {
            StepX = stepX;
            StepY = stepY;
            RankScale = rankScale;
            FrontDrop = frontDrop;
            SizeTwoOnThree = sizeTwoOnThree;
        }
    }

    // Both shapes are measured off the two-against-three layout set up by hand in the battle scene
    // (Haru and Hideshi against Slime, Wolf, Slime).

    // The player at the left, the allies beside them with a gap, a little higher up and smaller.
    public static readonly FileShape PlayerFile = new(
        stepX: 1.37f, stepY: 0.16f, rankScale: 0.88f, frontDrop: 0f, sizeTwoOnThree: 0.912f);
    // From the deck pile inwards, a little overlapping, each a step higher up and smaller. The
    // monsters are big, so they get smaller in a crowd than the player's side does.
    public static readonly FileShape EnemyFile = new(
        stepX: 0.83f, stepY: 0.14f, rankScale: 0.93f, frontDrop: 0f, sizeTwoOnThree: 0.666f);

    // Kept free between the two sides at least.
    public const float MiddleGap = 4f;

    // How the room the fighters leave free on screen is shared out (SpreadSides): before the
    // player's side, between the sides, after the enemies' side. Measured off the layouts set up
    // by hand, so the battle sits about in the middle of the screen, the sides further apart the
    // more room there is.
    private const float LeftShare = 0.27f;
    private const float MiddleShare = 0.4f;
    private const float RightShare = 0.33f;

    private const float ScreenMargin = 0.5f;

    // Two against two: how much further out than Haru's mirror the front enemy stands.
    private const float FrontEnemyStepOut = 1.5f;

    private const float MinCrowdScale = 0.4f;
    // Kept free above the highest one for its intent.
    private const float IntentRoom = 2f;

    // outward: -1 for the player's side (left), +1 for the enemies' (right). outerEdge: the screen
    // edge, or UI in the way; innerEdge: how far towards the middle the file may reach.
    // groundFeet: where feet stand in a one-on-one battle. hpBarWidth: the width of a fighter's HP
    // bar in world units; it stays on screen too. The file shrinks if it cannot fit at all.
    // Returns how far inwards the file reaches, HP bars included.
    public static float ArrangeInFile(
        IReadOnlyList<Character> file,
        FileShape shape,
        float outward,
        float outerEdge,
        float innerEdge,
        float groundFeet,
        Camera camera,
        Func<Character, float> hpBarWidth)
    {
        if (file == null || file.Count == 0 || camera == null)
            return outerEdge;

        int count = file.Count;

        // A lone fighter just steps to the edge, front and lowest, keeping its size.
        if (count == 1)
        {
            float reach = Reach(file[0], hpBarWidth);
            float centre = outerEdge - outward * reach;
            StandAt(file[0], centre, groundFeet - shape.FrontDrop);
            return centre - outward * reach;
        }

        float screenTop = camera.transform.position.y + camera.orthographicSize;

        var baseScales = new Vector3[count];
        for (int i = 0; i < count; i++)
            baseScales[i] = file[i].transform.localScale;

        float fit = 1f;
        float inner = outerEdge;
        for (int attempt = 0; attempt < 6; attempt++)
        {
            for (int i = 0; i < count; i++)
                file[i].transform.localScale = Scaled(baseScales[i], fit * Mathf.Pow(shape.RankScale, i));

            // Centres from the edge inwards.
            float[] centres = new float[count];
            centres[0] = outerEdge - outward * Reach(file[0], hpBarWidth);
            for (int i = 1; i < count; i++)
                centres[i] = centres[i - 1] - outward * shape.StepX * (Width(file[i - 1]) + Width(file[i])) / 2f;

            inner = centres[count - 1] - outward * Reach(file[count - 1], hpBarWidth);

            float stepY = shape.StepY * Height(file[0]);
            float frontFeet = groundFeet - shape.FrontDrop;
            float top = frontFeet;
            for (int i = 0; i < count; i++)
            {
                float feet = frontFeet + i * stepY;
                StandAt(file[i], centres[i], feet);
                top = Mathf.Max(top, feet + Height(file[i]) + IntentRoom);
            }

            float room = Mathf.Abs(outerEdge - innerEdge);
            float span = Mathf.Abs(outerEdge - inner);
            bool fitsAcross = span <= room + 0.01f;
            bool fitsUp = top <= screenTop + 0.01f;
            if (fitsAcross && fitsUp)
                break;

            float ratio = 1f;
            if (!fitsAcross)
                ratio = Mathf.Min(ratio, room / Mathf.Max(0.01f, span));
            if (!fitsUp)
                ratio = Mathf.Min(ratio, (screenTop - frontFeet) / Mathf.Max(0.01f, top - frontFeet));

            fit *= ratio * 0.99f;
        }

        DrawFrontOnTop(file);
        return inner;
    }

    // As many on each side (two against two): the enemies stand where the player's side stands
    // (already in file), mirrored about the middle of the screen, centre for centre and feet for
    // feet, each a little smaller than the one in front (rankScale). The monsters are wider, so the
    // player's side moves in as far as it takes for the enemies to stay clear of enemySideEdge (the
    // deck pile); the enemies shrink if even that is not enough.
    public static void ArrangeMirrored(
        IReadOnlyList<Character> playerSide,
        IReadOnlyList<Character> enemySide,
        float rankScale,
        float enemySideEdge,
        Camera camera,
        Func<Character, float> hpBarWidth)
    {
        int count = enemySide.Count;
        if (playerSide.Count != count || count == 0)
            return;

        float middle = camera.transform.position.x;
        // The player's side may move in until it keeps half the gap from the middle.
        float playerSideInner = playerSide[count - 1].transform.position.x + Reach(playerSide[count - 1], hpBarWidth);
        float maxShift = Mathf.Max(0f, middle - MiddleGap / 2f - playerSideInner);

        var baseScales = new Vector3[count];
        for (int i = 0; i < count; i++)
            baseScales[i] = enemySide[i].transform.localScale;

        float shift = 0f;
        float fit = 1f;
        for (int attempt = 0; attempt < 6; attempt++)
        {
            for (int i = 0; i < count; i++)
                enemySide[i].transform.localScale = Scaled(baseScales[i], fit * Mathf.Pow(rankScale, i));

            // Moving the player's side in by shift moves every mirrored enemy in by as much.
            shift = 0f;
            float widest = 0f;
            for (int i = 0; i < count; i++)
            {
                float mirroredX = 2f * middle - playerSide[i].transform.position.x;
                shift = Mathf.Max(shift, mirroredX + Reach(enemySide[i], hpBarWidth) - enemySideEdge);
                widest = Mathf.Max(widest, Reach(enemySide[i], hpBarWidth));
            }

            if (shift <= maxShift + 0.01f)
                break;

            // Shrinks so the one that sticks out furthest fits, to half size at most.
            float over = shift - maxShift;
            fit = Mathf.Max(0.5f, fit * (widest - over) / Mathf.Max(0.01f, widest) * 0.99f);
        }

        shift = Mathf.Min(shift, maxShift);
        for (int i = 0; i < count; i++)
        {
            Transform playerFighter = playerSide[i].transform;
            playerFighter.position += new Vector3(shift, 0f, 0f);
            StandAt(enemySide[i], 2f * middle - playerFighter.position.x, FeetOf(playerSide[i]));
        }

        // The front one a little further out than the mirror, apart from the one behind. It may
        // pass under the deck pile: only a monster's lower part is that wide, below the pile.
        Transform front = enemySide[0].transform;
        float screenEdgeRoom = ScreenEdge(camera, 1f) - (front.position.x + Reach(enemySide[0], hpBarWidth));
        front.position += new Vector3(Mathf.Clamp(FrontEnemyStepOut, 0f, Mathf.Max(0f, screenEdgeRoom)), 0f, 0f);

        DrawFrontOnTop(enemySide);
    }

    // The front one on top of the ones behind; the last one keeps the scene's order, so none goes
    // behind the background. Every other order is left free for the HP bar drawn just above each
    // fighter (HealthBarUI.DrawAmongSprites).
    private static void DrawFrontOnTop(IReadOnlyList<Character> file)
    {
        if (!TryGetSprite(file[0], out SpriteRenderer frontSprite))
            return;

        int backOrder = frontSprite.sortingOrder;
        for (int i = 0; i < file.Count; i++)
        {
            if (TryGetSprite(file[i], out SpriteRenderer sprite))
                sprite.sortingOrder = backOrder + 2 * (file.Count - 1 - i);
        }
    }

    // The more fighters in the battle, the smaller a side is, so the screen is not crowded: its
    // one-on-one size with two fighters, shape.SizeTwoOnThree with five, a little smaller still
    // with six.
    public static float CrowdScale(int fighterCount, FileShape shape)
    {
        float crowd = (fighterCount - 2) / 3f;
        return Mathf.Max(MinCrowdScale, Mathf.LerpUnclamped(1f, shape.SizeTwoOnThree, crowd));
    }

    public static void ScaleKeepingFeet(Character fighter, float factor)
    {
        float feet = FeetOf(fighter);
        fighter.transform.localScale = Scaled(fighter.transform.localScale, factor);
        StandAt(fighter, fighter.transform.position.x, feet);
    }

    // Sprites only need x and y scaled.
    private static Vector3 Scaled(Vector3 scale, float factor)
    {
        return new Vector3(scale.x * factor, scale.y * factor, scale.z);
    }

    // How far out a side may reach at most: the screen edge, less the margin.
    public static float ScreenEdge(Camera camera, float outward, float margin = ScreenMargin)
    {
        return camera.transform.position.x + outward * (camera.orthographicSize * camera.aspect - margin);
    }

    // Moves both sides (each already in file, from outer to inner: its extent, HP bars included) so
    // the room left free on screen is shared out as LeftShare, MiddleShare and RightShare. The
    // enemies' side keeps at least enemySideMinMargin from its edge (the deck pile), the sides at
    // least MiddleGap apart.
    public static void SpreadSides(
        IReadOnlyList<Character> playerSide,
        float playerSideOuter,
        float playerSideInner,
        IReadOnlyList<Character> enemySide,
        float enemySideOuter,
        float enemySideInner,
        float enemySideMinMargin,
        Camera camera)
    {
        float halfWidth = camera.orthographicSize * camera.aspect;
        float free = 2f * halfWidth
            - Mathf.Abs(playerSideInner - playerSideOuter)
            - Mathf.Abs(enemySideOuter - enemySideInner);

        float right = Mathf.Max(free * RightShare, enemySideMinMargin);
        float rest = free - right;
        float middle = Mathf.Max(MiddleGap, rest * MiddleShare / (MiddleShare + LeftShare));
        float left = Mathf.Max(ScreenMargin, rest - middle);

        float cameraX = camera.transform.position.x;
        Shift(playerSide, cameraX - halfWidth + left - playerSideOuter);
        Shift(enemySide, cameraX + halfWidth - right - enemySideOuter);
    }

    private static void Shift(IReadOnlyList<Character> side, float dx)
    {
        foreach (Character fighter in side)
            fighter.transform.position += new Vector3(dx, 0f, 0f);
    }

    // How far in from the screen edge an edge is (the margin ScreenEdge would take for it).
    public static float MarginOf(Camera camera, float edge, float outward)
    {
        return camera.orthographicSize * camera.aspect - outward * (edge - camera.transform.position.x);
    }

    public static float FeetOf(Character fighter)
    {
        return TryGetSprite(fighter, out SpriteRenderer sprite) ? sprite.bounds.min.y : fighter.transform.position.y;
    }

    // From a fighter's centre to the furthest of their body and the HP bar under it.
    private static float Reach(Character fighter, Func<Character, float> hpBarWidth)
    {
        float barWidth = hpBarWidth != null ? hpBarWidth(fighter) : 0f;
        return Mathf.Max(Width(fighter), barWidth) / 2f;
    }

    public static void StandAt(Character fighter, float x, float feet)
    {
        Transform transform = fighter.transform;
        float feetBelowPivot = TryGetSprite(fighter, out SpriteRenderer sprite)
            ? transform.position.y - sprite.bounds.min.y
            : 0f;
        transform.position = new Vector3(x, feet + feetBelowPivot, transform.position.z);
    }

    private static float Width(Character fighter)
    {
        return TryGetSprite(fighter, out SpriteRenderer sprite) ? sprite.bounds.size.x : 0f;
    }

    private static float Height(Character fighter)
    {
        return TryGetSprite(fighter, out SpriteRenderer sprite) ? sprite.bounds.size.y : 0f;
    }

    private static bool TryGetSprite(Character fighter, out SpriteRenderer sprite)
    {
        sprite = null;
        return fighter != null && fighter.TryGetComponent(out sprite) && sprite.sprite != null;
    }
}
