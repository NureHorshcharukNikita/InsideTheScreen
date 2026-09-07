using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Dialogue/Dialogue Data")]
public sealed class DialogueData : ScriptableObject
{
    public List<DialogueLine> lines = new();

    public bool HasLines
    {
        get
        {
            if (lines == null)
                return false;

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] != null && !string.IsNullOrWhiteSpace(lines[i].text))
                    return true;
            }

            return false;
        }
    }
}
