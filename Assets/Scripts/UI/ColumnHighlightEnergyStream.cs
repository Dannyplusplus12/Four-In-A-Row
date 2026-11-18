using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public sealed class ColumnHighlightEnergyStream : MonoBehaviour
{
    [Header("Particle Appearance")]
    [SerializeField] private Sprite particleSprite;
    [SerializeField] private Color primaryColor = new Color(0.55f, 0.92f, 1f, 0.85f);
    [SerializeField] private Color secondaryColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private Vector2 sizeRange = new Vector2(10f, 22f);
    [SerializeField] private Vector2 verticalStretchRange = new Vector2(1.5f, 2.6f);

    [Header("Stream Motion")]
    [SerializeField] private int particleCount = 26;
    [SerializeField] private Vector2 speedRange = new Vector2(300f, 520f);
    [SerializeField] private float horizontalPadding = 10f;
    [SerializeField] private float swayAmplitude = 5f;
    [SerializeField] private float swayFrequency = 2.5f;
    [SerializeField, Range(0.05f, 0.45f)] private float edgeFadeRatio = 0.18f;

    private struct EnergyParticle
    {
        public RectTransform Rect;
        public Image Image;
        public float BaseX;
        public float Y;
        public float Speed;
        public float Size;
        public float Stretch;
        public float Phase;
        public Color BaseColor;
    }

    private RectTransform containerRect;
    private EnergyParticle[] particles;
    private Sprite runtimeGlowSprite;
    private bool initialized;

    private void Awake()
    {
        containerRect = GetComponent<RectTransform>();
        EnsureRectMask();
        EnsurePool();
    }

    private void OnEnable()
    {
        if (containerRect == null) containerRect = GetComponent<RectTransform>();
        EnsureRectMask();
        EnsurePool();
        Prewarm();
    }

    private void Update()
    {
        if (!initialized || particles == null) return;

        Rect rect = containerRect.rect;
        float halfHeight = rect.height * 0.5f;
        float halfWidth = Mathf.Max(4f, rect.width * 0.5f - horizontalPadding);
        if (halfHeight <= 1f) return;

        float dt = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;

        for (int index = 0; index < particles.Length; index++)
        {
            ref EnergyParticle p = ref particles[index];
            p.Y += p.Speed * dt;

            float particleHalfHeight = p.Size * p.Stretch * 0.5f;
            if (p.Y - particleHalfHeight > halfHeight)
            {
                RespawnParticle(ref p, halfWidth, -halfHeight - particleHalfHeight);
            }

            float sway = Mathf.Sin(time * swayFrequency + p.Phase) * swayAmplitude;
            float clampedX = Mathf.Clamp(p.BaseX + sway, -halfWidth, halfWidth);
            p.Rect.anchoredPosition = new Vector2(clampedX, p.Y);

            float normalizedY = Mathf.InverseLerp(-halfHeight, halfHeight, p.Y);
            float alphaMultiplier = ComputeEdgeFade(normalizedY);
            Color c = p.BaseColor;
            c.a *= alphaMultiplier;
            p.Image.color = c;
        }
    }

    private void OnDestroy()
    {
        if (runtimeGlowSprite != null)
        {
            Destroy(runtimeGlowSprite.texture);
            Destroy(runtimeGlowSprite);
        }
    }

    private void EnsureRectMask()
    {
        if (GetComponent<RectMask2D>() == null)
            gameObject.AddComponent<RectMask2D>();
    }

    private void EnsurePool()
    {
        if (initialized) return;

        Sprite sprite = particleSprite != null ? particleSprite : GetOrCreateGlowSprite();
        int count = Mathf.Max(1, particleCount);
        particles = new EnergyParticle[count];

        for (int index = 0; index < count; index++)
        {
            GameObject go = new GameObject(
                $"EnergyParticle_{index}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            go.transform.SetParent(transform, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            img.preserveAspect = false;

            particles[index] = new EnergyParticle
            {
                Rect = rt,
                Image = img
            };
        }

        initialized = true;
    }

    private void Prewarm()
    {
        if (!initialized || particles == null) return;

        Rect rect = containerRect.rect;
        float halfHeight = rect.height * 0.5f;
        float halfWidth = Mathf.Max(4f, rect.width * 0.5f - horizontalPadding);

        for (int index = 0; index < particles.Length; index++)
        {
            float t = (index + Random.value) / particles.Length;
            float startY = Mathf.Lerp(-halfHeight, halfHeight, t);
            RespawnParticle(ref particles[index], halfWidth, startY);
        }
    }

    private void RespawnParticle(ref EnergyParticle p, float halfWidth, float startY)
    {
        p.BaseX = Random.Range(-halfWidth, halfWidth);
        p.Y = startY;
        p.Speed = Random.Range(speedRange.x, speedRange.y);
        p.Size = Random.Range(sizeRange.x, sizeRange.y);
        p.Stretch = Random.Range(verticalStretchRange.x, verticalStretchRange.y);
        p.Phase = Random.Range(0f, Mathf.PI * 2f);
        p.BaseColor = Color.Lerp(primaryColor, secondaryColor, Random.value);

        p.Rect.sizeDelta = new Vector2(p.Size, p.Size * p.Stretch);
        p.Rect.anchoredPosition = new Vector2(p.BaseX, p.Y);
    }

    private float ComputeEdgeFade(float normalizedY)
    {
        if (normalizedY < edgeFadeRatio)
            return Mathf.Clamp01(normalizedY / edgeFadeRatio);
        if (normalizedY > 1f - edgeFadeRatio)
            return Mathf.Clamp01((1f - normalizedY) / edgeFadeRatio);
        return 1f;
    }

    private Sprite GetOrCreateGlowSprite()
    {
        if (runtimeGlowSprite != null) return runtimeGlowSprite;

        const int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = alpha * alpha;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        runtimeGlowSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        return runtimeGlowSprite;
    }
}
