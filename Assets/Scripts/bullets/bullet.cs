using UnityEngine;

public class bullet : MonoBehaviour
{
    [Tooltip("Compatibility script: attack/ignore layers are controlled only by OneDropAttack2D.")]
    [SerializeField] private bool showEyes = true;
    [SerializeField, Min(0.01f)] private float eyeSize = 0.06f;
    [SerializeField] private float eyeSpacing = 0.08f;
    [SerializeField] private float eyeHeight = 0.04f;

    private bool hasEyes;

    private void Start()
    {
        if (showEyes && !hasEyes)
        {
            CreateGooglyEyes();
            hasEyes = true;
        }
    }

    private void CreateGooglyEyes()
    {
        CreateEye("LeftEye", new Vector3(-eyeSpacing, eyeHeight, -0.01f));
        CreateEye("RightEye", new Vector3(eyeSpacing, eyeHeight, -0.01f));
    }

    private void CreateEye(string eyeName, Vector3 localPos)
    {
        GameObject eyeWhite = new GameObject(eyeName + "_White");
        eyeWhite.transform.SetParent(transform);
        eyeWhite.transform.localPosition = localPos;
        eyeWhite.transform.localScale = Vector3.one * eyeSize;

        SpriteRenderer whiteRenderer = eyeWhite.AddComponent<SpriteRenderer>();
        whiteRenderer.sprite = CreateCircleSprite(16);
        whiteRenderer.color = Color.white;
        whiteRenderer.sortingOrder = 20;

        GameObject pupil = new GameObject(eyeName + "_Pupil");
        pupil.transform.SetParent(eyeWhite.transform);
        pupil.transform.localPosition = new Vector3(0.15f, 0f, -0.001f);
        pupil.transform.localScale = Vector3.one * 0.5f;

        SpriteRenderer pupilRenderer = pupil.AddComponent<SpriteRenderer>();
        pupilRenderer.sprite = CreateCircleSprite(12);
        pupilRenderer.color = Color.black;
        pupilRenderer.sortingOrder = 21;
    }

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
