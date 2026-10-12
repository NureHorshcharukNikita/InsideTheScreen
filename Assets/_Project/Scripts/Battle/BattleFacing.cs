using UnityEngine;

// In battle everyone looks at their opponent: whoever stands on the right faces left, whoever
// stands on the left faces right. The profile says which way the art looks; the sprite is mirrored
// when that is the wrong way.
public static class BattleFacing
{
    public static void FaceTowards(Character character, CharacterProfile profile, Transform opponent)
    {
        if (character == null || profile == null || opponent == null)
            return;

        if (!character.TryGetComponent(out SpriteRenderer spriteRenderer))
            return;

        bool shouldFaceRight = opponent.position.x > character.transform.position.x;
        bool artFacesRight = profile.PortraitFacing == SpriteFacing.Right;
        spriteRenderer.flipX = shouldFaceRight != artFacesRight;
    }
}
