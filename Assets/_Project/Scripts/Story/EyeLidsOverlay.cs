using UnityEngine;
using UnityEngine.UI;

// The view through opening eyes (Open Eyes step): black around an eye-shaped opening with soft
// edges, and the scene blurred until the eyes are open. Above the black of a fade, so it can take
// over from it while closed. Built from code, like the screen noise. The blur is a second
// camera that draws the scene into a small texture, shown stretched over the screen.
public sealed class EyeLidsOverlay : MonoBehaviour
{
    private const int MaskWidth = 256;
    private const int MaskHeight = 128;
    // The opening is wider than the screen, so its corners lie outside, like the corners of an eye.
    private const float OpeningWidth = 1.4f;
    // Height of the opening in screen heights when the eyes are open: big enough that its dark
    // edge has left the screen.
    private const float OpenHeight = 4f;
    // Part of the opening, from its edge inwards, that fades from black to clear.
    private const float SoftEdge = 0.45f;
    // 0.5 = an ellipse, 1 = pointed corners. In between: an eye.
    private const float CornerSharpness = 0.75f;
    // The blurred scene is drawn this many times smaller than the screen.
    private const int BlurDownscale = 6;

    private RectTransform opening;
    private RectTransform top;
    private RectTransform bottom;
    private Texture2D mask;
    private RawImage blur;
    private Camera blurCamera;
    private RenderTexture blurTexture;
    private Camera source;

    public static EyeLidsOverlay Create()
    {
        EyeLidsOverlay eyes = new GameObject("EyeLids", typeof(Canvas)).AddComponent<EyeLidsOverlay>();
        Canvas canvas = eyes.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1001;

        // First, so the black is drawn over it.
        eyes.CreateBlur();
        eyes.mask = CreateMaskTexture();
        eyes.opening = CreateImage(eyes.transform, "Opening", eyes.mask, Color.white);
        eyes.top = CreateImage(eyes.transform, "TopLid", null, Color.black);
        eyes.bottom = CreateImage(eyes.transform, "BottomLid", null, Color.black);
        eyes.SetOpenness(0f, 1f);
        return eyes;
    }

    // openness: 0 = closed, 1 = open. blurAmount: 0 = sharp, 1 = blurred.
    public void SetOpenness(float openness, float blurAmount)
    {
        float open = Mathf.Clamp01(openness);
        float halfHeight = OpenHeight * open * open * 0.5f;
        float halfWidth = OpeningWidth * 0.5f;
        opening.anchorMin = new Vector2(0.5f - halfWidth, 0.5f - halfHeight);
        opening.anchorMax = new Vector2(0.5f + halfWidth, 0.5f + halfHeight);

        // Black above and below the opening, reaching a little into its black border so no seam shows.
        top.anchorMin = new Vector2(0f, Mathf.Min(1f, 0.5f + halfHeight * 0.99f));
        bottom.anchorMax = new Vector2(1f, Mathf.Max(0f, 0.5f - halfHeight * 0.99f));

        if (blur != null)
        {
            blur.color = new Color(1f, 1f, 1f, Mathf.Clamp01(blurAmount));
            blurCamera.enabled = blurAmount > 0f;
        }
    }

    // The camera may move (Cinemachine): the blurred copy follows it.
    private void LateUpdate()
    {
        if (blurCamera == null || source == null)
            return;

        blurCamera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        blurCamera.orthographicSize = source.orthographicSize;
    }

    private void OnDestroy()
    {
        if (mask != null)
            Destroy(mask);

        if (blurCamera != null)
        {
            blurCamera.targetTexture = null;
            Destroy(blurCamera.gameObject);
        }

        if (blurTexture != null)
        {
            blurTexture.Release();
            Destroy(blurTexture);
        }
    }

    private void CreateBlur()
    {
        source = Camera.main;
        if (source == null)
            return;

        blurTexture = new RenderTexture(
            Mathf.Max(1, Screen.width / BlurDownscale),
            Mathf.Max(1, Screen.height / BlurDownscale),
            24)
        {
            filterMode = FilterMode.Bilinear
        };

        blurCamera = new GameObject("EyesBlurCamera").AddComponent<Camera>();
        blurCamera.CopyFrom(source);
        blurCamera.targetTexture = blurTexture;

        blur = CreateImage(transform, "Blur", blurTexture, Color.white).GetComponent<RawImage>();
    }

    private static RectTransform CreateImage(Transform parent, string name, Texture texture, Color color)
    {
        GameObject imageObject = new(name, typeof(RectTransform), typeof(RawImage));
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)imageObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        RawImage image = imageObject.GetComponent<RawImage>();
        image.raycastTarget = false;
        image.texture = texture;
        image.color = color;
        return rect;
    }

    // Black with a clear eye-shaped hole in the middle, softening towards the hole's edge.
    private static Texture2D CreateMaskTexture()
    {
        Texture2D texture = new(MaskWidth, MaskHeight, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[MaskWidth * MaskHeight];
        for (int y = 0; y < MaskHeight; y++)
        {
            float ny = (y + 0.5f) / MaskHeight * 2f - 1f;
            for (int x = 0; x < MaskWidth; x++)
            {
                float nx = (x + 0.5f) / MaskWidth * 2f - 1f;
                // Half the height of the hole at this x: 1 in the middle, 0 at the corners.
                float edge = Mathf.Pow(Mathf.Max(0f, 1f - nx * nx), CornerSharpness);
                float distance = edge > 0f ? Mathf.Abs(ny) / edge : 2f;
                float alpha = Mathf.SmoothStep(0f, 1f, (distance - (1f - SoftEdge)) / SoftEdge);
                pixels[y * MaskWidth + x] = new Color(0f, 0f, 0f, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }
}
