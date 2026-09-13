using UnityEngine;

public class BattleBackgroundFitter : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer targetBackground;
    [SerializeField] private float viewportPadding = 1.02f;

    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        FitToCamera();
    }

    private void LateUpdate()
    {
        if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
            return;

        FitToCamera();
    }

    private void FitToCamera()
    {
        ResolveReferences();
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        if (targetCamera == null
            || targetBackground == null
            || targetBackground.sprite == null
            || !targetCamera.orthographic)
            return;

        Vector2 spriteSize = targetBackground.sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            return;

        Transform backgroundTransform = targetBackground.transform;
        Vector3 parentScale = backgroundTransform.parent != null
            ? backgroundTransform.parent.lossyScale
            : Vector3.one;

        float cameraHeight = targetCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * targetCamera.aspect;
        float scaleX = cameraWidth / Mathf.Abs(spriteSize.x * parentScale.x);
        float scaleY = cameraHeight / Mathf.Abs(spriteSize.y * parentScale.y);
        float scale = Mathf.Max(scaleX, scaleY) * Mathf.Max(1f, viewportPadding);

        backgroundTransform.localScale = new Vector3(
            Mathf.Sign(backgroundTransform.localScale.x) * scale,
            Mathf.Sign(backgroundTransform.localScale.y) * scale,
            backgroundTransform.localScale.z);

        Vector3 cameraPosition = targetCamera.transform.position;
        backgroundTransform.position = new Vector3(
            cameraPosition.x,
            cameraPosition.y,
            backgroundTransform.position.z);
    }

    private void ResolveReferences()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetBackground == null)
            targetBackground = GetComponentInChildren<SpriteRenderer>();
    }
}
