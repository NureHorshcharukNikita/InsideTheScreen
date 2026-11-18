using System.Collections.Generic;
using UnityEngine;

// The way around a well, a lamp post, a house or a person for a character a story event walks (Walk
// To with Avoid Obstacles): nothing when the straight line is clear, otherwise A* on a half-unit
// grid, smoothed so it does not zigzag. Characters the event walks do not get in the way (their
// physics is off while it plays), nor do doors that open on their own. When there is no way, or the
// goal itself is inside something (walking into the barn door), it walks straight, as before.
public static class StoryPath
{
    private const float CellSize = 0.5f;
    private const float SearchMargin = 6f;
    private const int MaxNodes = 6000;
    // About a character's feet, a little above where it stands.
    private static readonly Vector2 FeetSize = new(0.9f, 0.45f);
    private static readonly Vector2 FeetOffset = new(0f, 0.2f);

    private static readonly Vector2Int[] Neighbours =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
    };

    private static readonly Collider2D[] Overlaps = new Collider2D[8];
    private static readonly RaycastHit2D[] Hits = new RaycastHit2D[8];
    private static readonly Dictionary<Collider2D, bool> DoorParts = new();
    private static ContactFilter2D filter = new() { useTriggers = false };

    // The corners to walk through on the way from one point to the other, ending at the goal; null
    // when the straight line is fine (or nothing better is found).
    public static List<Vector2> Find(Vector2 from, Vector2 goal)
    {
        if (CanWalkStraight(from, goal) || !IsFree(goal))
            return null;

        Vector2 min = Vector2.Min(from, goal) - Vector2.one * SearchMargin;
        Vector2 max = Vector2.Max(from, goal) + Vector2.one * SearchMargin;
        Vector2Int minCell = ToCell(min, from);
        Vector2Int maxCell = ToCell(max, from);
        Vector2Int goalCell = ToCell(goal, from);

        var free = new Dictionary<Vector2Int, bool>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var cost = new Dictionary<Vector2Int, float> { [Vector2Int.zero] = 0f };
        var open = new SortedSet<(float score, int order, Vector2Int cell)>();
        int order = 0;
        open.Add((Vector2.Distance(from, goal), order++, Vector2Int.zero));
        var closed = new HashSet<Vector2Int>();

        while (open.Count > 0 && closed.Count < MaxNodes)
        {
            var best = open.Min;
            open.Remove(best);
            Vector2Int cell = best.cell;
            if (!closed.Add(cell))
                continue;

            if (cell == goalCell || CanWalkStraight(ToWorld(cell, from), goal) && cell != Vector2Int.zero
                && Vector2.Distance(ToWorld(cell, from), goal) < CellSize * 3f)
                return Smooth(from, Build(cell, cameFrom, from, goal));

            foreach (Vector2Int step in Neighbours)
            {
                Vector2Int next = cell + step;
                if (closed.Contains(next) || next.x < minCell.x || next.x > maxCell.x || next.y < minCell.y || next.y > maxCell.y)
                    continue;

                if (!IsFreeCell(next, from, free))
                    continue;

                // No cutting a corner past a wall.
                bool diagonal = step.x != 0 && step.y != 0;
                if (diagonal && (!IsFreeCell(new Vector2Int(next.x, cell.y), from, free)
                    || !IsFreeCell(new Vector2Int(cell.x, next.y), from, free)))
                    continue;

                float nextCost = cost[cell] + (diagonal ? 1.4142f : 1f) * CellSize;
                if (cost.TryGetValue(next, out float known) && known <= nextCost)
                    continue;

                cost[next] = nextCost;
                cameFrom[next] = cell;
                open.Add((nextCost + Vector2.Distance(ToWorld(next, from), goal), order++, next));
            }
        }

        return null;
    }

    private static List<Vector2> Build(Vector2Int cell, Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2 origin, Vector2 goal)
    {
        var path = new List<Vector2> { goal };
        while (cell != Vector2Int.zero)
        {
            path.Insert(0, ToWorld(cell, origin));
            cell = cameFrom[cell];
        }

        return path;
    }

    // Skip the corners that can be reached in a straight line.
    private static List<Vector2> Smooth(Vector2 from, List<Vector2> path)
    {
        var smoothed = new List<Vector2>();
        Vector2 current = from;
        int index = 0;
        while (index < path.Count)
        {
            int furthest = index;
            for (int i = path.Count - 1; i > index; i--)
            {
                if (CanWalkStraight(current, path[i]))
                {
                    furthest = i;
                    break;
                }
            }

            current = path[furthest];
            smoothed.Add(current);
            index = furthest + 1;
        }

        return smoothed;
    }

    private static Vector2Int ToCell(Vector2 position, Vector2 origin)
    {
        Vector2 local = (position - origin) / CellSize;
        return new Vector2Int(Mathf.RoundToInt(local.x), Mathf.RoundToInt(local.y));
    }

    private static Vector2 ToWorld(Vector2Int cell, Vector2 origin)
    {
        return origin + (Vector2)cell * CellSize;
    }

    private static bool IsFreeCell(Vector2Int cell, Vector2 origin, Dictionary<Vector2Int, bool> cache)
    {
        if (!cache.TryGetValue(cell, out bool free))
        {
            free = IsFree(ToWorld(cell, origin));
            cache[cell] = free;
        }

        return free;
    }

    // How far a character's feet get straight from here before something is in the way, up to
    // Distance, e.g. Dad hopping back from Haru (Step Back) without landing in a fence.
    public static float ClearDistance(Vector2 from, Vector2 direction, float distance)
    {
        if (distance <= 0f || direction.sqrMagnitude < 0.0001f)
            return 0f;

        float clear = distance;
        int count = Physics2D.BoxCast(from + FeetOffset, FeetSize, 0f, direction.normalized, filter, Hits, distance);
        for (int i = 0; i < count; i++)
        {
            // Something its feet already touch where it stands does not hold it there.
            if (Hits[i].distance > 0f && !IsIgnored(Hits[i].collider))
                clear = Mathf.Min(clear, Hits[i].distance);
        }

        return Mathf.Max(0f, clear - 0.05f);
    }

    // The probe is at least a cell big: walls are thin outlines that could slip between two cells.
    private static bool IsFree(Vector2 position)
    {
        Vector2 size = Vector2.Max(FeetSize, Vector2.one * CellSize * 1.05f);
        int count = Physics2D.OverlapBox(position + FeetOffset, size, 0f, filter, Overlaps);
        for (int i = 0; i < count; i++)
        {
            if (!IsIgnored(Overlaps[i]))
                return false;
        }

        return true;
    }

    private static bool CanWalkStraight(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 0.0001f)
            return true;

        int count = Physics2D.BoxCast(from + FeetOffset, FeetSize, 0f, delta / distance, filter, Hits, distance);
        for (int i = 0; i < count; i++)
        {
            if (!IsIgnored(Hits[i].collider))
                return false;
        }

        return true;
    }

    private static bool IsIgnored(Collider2D other)
    {
        if (other == null)
            return true;

        if (!DoorParts.TryGetValue(other, out bool automaticDoor))
        {
            Door door = other.GetComponentInParent<Door>();
            automaticDoor = door != null && door.OpensAutomatically;
            DoorParts[other] = automaticDoor;
        }

        return automaticDoor;
    }
}
