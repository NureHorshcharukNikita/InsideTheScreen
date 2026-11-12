using System;
using System.Collections.Generic;
using UnityEngine;

// A run of frames played once on a character's sprite, outside its Animator (Play Frames step),
// e.g. Haru putting on his belt and backpack. Sounds come in on the frames they belong to.
[CreateAssetMenu(menuName = "Game/Characters/Sprite Flipbook")]
public sealed class SpriteFlipbook : ScriptableObject
{
    [Serializable]
    public sealed class FrameSound
    {
        [Tooltip("The frame it comes in on, counted from 0.")]
        [Min(0)] public int frame;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [SerializeField] private List<Sprite> frames = new();
    [SerializeField, Min(1f)] private float framesPerSecond = 10f;
    [SerializeField] private List<FrameSound> sounds = new();

    public IReadOnlyList<Sprite> Frames => frames;
    public float FrameSeconds => 1f / Mathf.Max(1f, framesPerSecond);
    public IReadOnlyList<FrameSound> Sounds => sounds;
}
