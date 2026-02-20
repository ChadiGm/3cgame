using UnityEngine;

public class bullet : MonoBehaviour
{
    [Tooltip("Seconds before the bullet auto-destroys")]
    public float lifeTime = 4f;

    [Header("Target Filtering")]
    [Tooltip("Layers the bullet CAN hit and deal damage to")]
    [SerializeField] private LayerMask attackableLayers = ~0; // everything by default

    [Tooltip("Layers the bullet completely passes through (ignored)")]
    [SerializeField] private LayerMask ignoredLayers = 0; // nothing by default

    [Header("Impact Effect")]
    [Tooltip("Prefab to spawn on impact (water splash). If null, a default one is created at runtime.")]
    [SerializeField] private GameObject impactEffectPrefab;

    [Header("Googly Eyes (like the character)")]
    [Tooltip("If true, adds tiny googly eyes to the bullet blob")]
    [SerializeField] private bool showEyes = true;
    [SerializeField, Min(0.01f)] private float eyeSize = 0.06f;
    [SerializeField] private float eyeSpacing = 0.08f;
    [SerializeField] private float eyeHeight = 0.04f;

    private bool hasEyes;

    /// <summary>
    /// Called by OneDropAttack2D at runtime to pass layer masks and impact prefab.
    /// </summary>
    public void RuntimeSetup(LayerMask attackable, LayerMask ignored, GameObject impactPrefab)
    {
        attackableLayers = attackable;
        ignoredLayers = ignored;
        impactEffectPrefab = impactPrefab;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);

        if (showEyes && !hasEyes)
        {
            CreateGooglyEyes();
            hasEyes = true;
        }
    }

    /// <summary>
    /// Checks if the given layer is in the ignored set.
    /// </summary>
    private bool IsLayerIgnored(int layer)
    {
        return (ignoredLayers.value & (1 << layer)) != 0;
    }

    /// <summary>
    /// Checks if the given layer is in the attackable set.
    /// </summary>
    private bool IsLayerAttackable(int layer)
    {
        return (attackableLayers.value & (1 << layer)) != 0;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        int layer = collision.gameObject.layer;

        // Completely ignore this object
        if (IsLayerIgnored(layer))
        {
            return;
        }

        // Spawn impact effect at collision point
        Vector2 contactPoint = collision.contactCount > 0 ? collision.GetContact(0).point : (Vector2)transform.position;
        Vector2 contactNormal = collision.contactCount > 0 ? collision.GetContact(0).normal : Vector2.up;
        SpawnImpactEffect(contactPoint, contactNormal);

        // Destroy bullet
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        int layer = collision.gameObject.layer;

        if (IsLayerIgnored(layer))
        {
            return;
        }

        Vector3 contactPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
        Vector3 contactNormal = collision.contactCount > 0 ? collision.GetContact(0).normal : Vector3.up;
        SpawnImpactEffect(contactPoint, contactNormal);

        Destroy(gameObject);
    }

    private void SpawnImpactEffect(Vector3 position, Vector3 normal)
    {
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, position, Quaternion.LookRotation(Vector3.forward, normal));
        }
        else
        {
            // Create a default water splash effect at runtime
            GameObject fx = new GameObject("WaterSplash_Runtime");
            fx.transform.position = position;
            fx.transform.rotation = Quaternion.LookRotation(Vector3.forward, normal);
            fx.AddComponent<WaterSplashEffect>();
        }
    }

    /// <summary>
    /// Creates tiny googly eye sprites on the bullet so it looks like a mini character.
    /// </summary>
    private void CreateGooglyEyes()
    {
        CreateEye("LeftEye", new Vector3(-eyeSpacing, eyeHeight, -0.01f));
        CreateEye("RightEye", new Vector3(eyeSpacing, eyeHeight, -0.01f));
    }

    private void CreateEye(string eyeName, Vector3 localPos)
    {
        // White part
        GameObject eyeWhite = new GameObject(eyeName + "_White");
        eyeWhite.transform.SetParent(transform);
        eyeWhite.transform.localPosition = localPos;
        eyeWhite.transform.localScale = Vector3.one * eyeSize;

        SpriteRenderer whiteRenderer = eyeWhite.AddComponent<SpriteRenderer>();
        whiteRenderer.sprite = CreateCircleSprite(16);
        whiteRenderer.color = Color.white;
        whiteRenderer.sortingOrder = 20;

        // Pupil
        GameObject pupil = new GameObject(eyeName + "_Pupil");
        pupil.transform.SetParent(eyeWhite.transform);
        pupil.transform.localPosition = new Vector3(0.15f, 0f, -0.001f);
        pupil.transform.localScale = Vector3.one * 0.5f;

        SpriteRenderer pupilRenderer = pupil.AddComponent<SpriteRenderer>();
        pupilRenderer.sprite = CreateCircleSprite(12);
        pupilRenderer.color = Color.black;
        pupilRenderer.sortingOrder = 21;
    }

    /// <summary>
    /// Creates a simple circle sprite procedurally (no texture asset needed).
    /// </summary>
    private static Sprite CreateCircleSprite(int resolution)
    {
        int size = resolution;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = size * 0.5f;
        float radiusSq = center * center;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float distSq = dx * dx + dy * dy;
                tex.SetPixel(x, y, distSq <= radiusSq ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
