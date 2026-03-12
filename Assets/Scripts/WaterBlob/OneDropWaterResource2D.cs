using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class OneDropWaterResource2D : MonoBehaviour
    {
        [Header("Water Resource")]
        [SerializeField, Min(1f)] private float maxWater = 100f;
        [SerializeField, Min(0f)] private float moveDrainPerSecond = 3.5f;
        [SerializeField, Min(0f)] private float moveSpeedThreshold = 0.2f;
        [SerializeField, Min(0f)] private float jumpDrain = 10f;
        [SerializeField, Min(0f)] private float slideDrain = 12f;
        [SerializeField, Min(0f)] private float shootDrain = 6f;
        [SerializeField, Min(0f)] private float damageDrain = 20f;

        [Header("UI")]
        [SerializeField, Min(0f)] private float topOffset = 24f;
        [SerializeField] private Vector2 gaugeSize = new Vector2(90f, 90f);
        [SerializeField] private Color backgroundColor = new Color(1f, 1f, 1f, 0.12f);
        [SerializeField] private Color waterColor = new Color(0.22f, 0.86f, 1f, 0.85f);
        [SerializeField] private Color frameColor = new Color(0.9f, 0.98f, 1f, 0.8f);
        [SerializeField, Range(0.02f, 0.4f)] private float frameThickness = 0.1f;

        [Header("Death")]
        [SerializeField] private GameObject deathVaporEffectPrefab;
        [SerializeField, Min(0f)] private float disableDelay = 0.15f;
        [SerializeField] private bool respawnOnDeath = true;
        [SerializeField, Min(0f)] private float respawnDelay = 1.2f;
        [SerializeField] private bool useCheckpointManager = true;
        [SerializeField] private CheckpointManager2D checkpointManager;
        [SerializeField] private bool debugRespawnLogs = false;

        private float currentWater;
        private bool dead;
        private Rigidbody2D rb;
        private Vector3 initialSpawnPosition;
        private Quaternion initialSpawnRotation;

        private Image waterFillImage;
        private RectTransform gaugeRoot;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            maxWater = Mathf.Max(1f, maxWater);
            moveDrainPerSecond = Mathf.Max(0f, moveDrainPerSecond);
            jumpDrain = Mathf.Max(0f, jumpDrain);
            slideDrain = Mathf.Max(0f, slideDrain);
            shootDrain = Mathf.Max(0f, shootDrain);
            damageDrain = Mathf.Max(0f, damageDrain);
            currentWater = maxWater;
            initialSpawnPosition = transform.position;
            initialSpawnRotation = transform.rotation;
            if (useCheckpointManager)
            {
                if (checkpointManager == null)
                {
                    checkpointManager = CheckpointManager2D.EnsureInstance();
                }

                checkpointManager.RegisterPlayer(transform);
            }
            EnsureUI();
            RefreshUI();
        }

        public bool ConsumeShoot()
        {
            return Consume(shootDrain);
        }

        public void ConsumeJump()
        {
            Consume(jumpDrain);
        }

        public void ConsumeSlide()
        {
            Consume(slideDrain);
        }

        public void ConsumeDamage()
        {
            Consume(damageDrain);
        }

        public void DepleteAllWater()
        {
            if (dead)
            {
                return;
            }

            currentWater = 0f;
            RefreshUI();
            StartCoroutine(HandleDeath());
        }

        public void ConsumeMove(float deltaTime, bool isMoving)
        {
            if (!isMoving || deltaTime <= 0f)
            {
                return;
            }

            if (rb != null && Mathf.Abs(rb.linearVelocity.x) < moveSpeedThreshold && Mathf.Abs(rb.linearVelocity.y) < moveSpeedThreshold)
            {
                return;
            }

            Consume(moveDrainPerSecond * deltaTime);
        }

        private bool Consume(float amount)
        {
            if (dead || amount <= 0f)
            {
                return !dead;
            }

            currentWater = Mathf.Max(0f, currentWater - amount);
            RefreshUI();

            if (currentWater <= 0f)
            {
                StartCoroutine(HandleDeath());
                return false;
            }

            return true;
        }

        private IEnumerator HandleDeath()
        {
            if (dead)
            {
                yield break;
            }

            dead = true;
            SpawnDeathVapor(transform.position);

            OneDropController2D controller = GetComponent<OneDropController2D>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            OneDropAttack2D attack = GetComponent<OneDropAttack2D>();
            if (attack != null)
            {
                attack.enabled = false;
            }

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.simulated = false;
            }

            yield return new WaitForSeconds(disableDelay);

            Collider2D[] colls = GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colls.Length; i++)
            {
                colls[i].enabled = false;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = false;
            }

            if (!respawnOnDeath)
            {
                yield break;
            }

            yield return new WaitForSeconds(respawnDelay);
            RespawnPlayer(controller, attack);
        }

        private void RespawnPlayer(OneDropController2D controller, OneDropAttack2D attack)
        {
            Vector3 respawnPosition = initialSpawnPosition;
            Quaternion respawnRotation = initialSpawnRotation;

            if (useCheckpointManager && checkpointManager != null && checkpointManager.TryGetRespawn(out Vector3 cpPos, out Quaternion cpRot))
            {
                respawnPosition = cpPos;
                respawnRotation = cpRot;
            }
            if (debugRespawnLogs)
            {
                Debug.Log($"[OneDropWaterResource2D] Respawn at {respawnPosition}", this);
            }
            transform.SetPositionAndRotation(respawnPosition, respawnRotation);

            if (rb != null)
            {
                rb.simulated = true;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            Collider2D[] colls = GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colls.Length; i++)
            {
                colls[i].enabled = true;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = true;
            }

            WaterBlobCharacter2D blobCharacter = GetComponent<WaterBlobCharacter2D>();
            if (blobCharacter != null)
            {
                blobCharacter.enabled = true;
                blobCharacter.RebuildBlob();
                if (blobCharacter.PointBodies != null)
                {
                    for (int i = 0; i < blobCharacter.PointBodies.Count; i++)
                    {
                        Rigidbody2D pointBody = blobCharacter.PointBodies[i];
                        if (pointBody == null)
                        {
                            continue;
                        }

                        pointBody.gravityScale = blobCharacter.gravityScale;
                        pointBody.linearVelocity = Vector2.zero;
                        pointBody.angularVelocity = 0f;
                    }
                }
            }

            if (controller != null)
            {
                controller.enabled = true;
            }

            if (attack != null)
            {
                attack.ResetAfterRespawn();
                attack.enabled = true;
            }

            currentWater = maxWater;
            dead = false;
            RefreshUI();
        }

        private void SpawnDeathVapor(Vector3 position)
        {
            if (deathVaporEffectPrefab != null)
            {
                Instantiate(deathVaporEffectPrefab, position, Quaternion.identity);
                return;
            }

            GameObject fx = new GameObject("OneDropDeathVapor_Runtime");
            fx.transform.position = position;
            ParticleSystem ps = fx.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.8f, 0.95f, 1f, 0.95f),
                new Color(0.55f, 0.85f, 1f, 0.65f)
            );
            main.maxParticles = 140;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 64) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.28f;

            ps.Play();
            Destroy(fx, 1.4f);
        }

        private void EnsureUI()
        {
            const string canvasName = "OneDropWaterUI_Canvas";
            const string rootName = "WaterGaugeRoot";

            GameObject canvasGo = GameObject.Find(canvasName);
            Canvas canvas;
            if (canvasGo == null)
            {
                canvasGo = new GameObject(canvasName);
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                canvasGo.AddComponent<GraphicRaycaster>();
            }
            else
            {
                canvas = canvasGo.GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = canvasGo.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }
            }

            Transform root = canvas.transform.Find(rootName);
            if (root == null)
            {
                GameObject rootGo = new GameObject(rootName);
                rootGo.transform.SetParent(canvas.transform, false);
                gaugeRoot = rootGo.AddComponent<RectTransform>();
                gaugeRoot.anchorMin = new Vector2(0.5f, 1f);
                gaugeRoot.anchorMax = new Vector2(0.5f, 1f);
                gaugeRoot.pivot = new Vector2(0.5f, 1f);
                gaugeRoot.anchoredPosition = new Vector2(0f, -topOffset);
                gaugeRoot.sizeDelta = gaugeSize;

                Sprite circle = CreateCircleSprite(128);
                Sprite ring = CreateRingSprite(128, frameThickness);

                GameObject bgGo = CreateImageChild("GaugeBackground", gaugeRoot, circle, backgroundColor);
                bgGo.GetComponent<RectTransform>().sizeDelta = gaugeSize;

                GameObject fillGo = CreateImageChild("GaugeFill", gaugeRoot, circle, waterColor);
                RectTransform fillRt = fillGo.GetComponent<RectTransform>();
                fillRt.sizeDelta = gaugeSize * 0.9f;
                Image fillImage = fillGo.GetComponent<Image>();
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Vertical;
                fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
                fillImage.fillAmount = 1f;
                waterFillImage = fillImage;

                GameObject frameGo = CreateImageChild("GaugeFrame", gaugeRoot, ring, frameColor);
                RectTransform frameRt = frameGo.GetComponent<RectTransform>();
                frameRt.sizeDelta = gaugeSize;
            }
            else
            {
                gaugeRoot = root as RectTransform;
                waterFillImage = root.Find("GaugeFill")?.GetComponent<Image>();
                gaugeRoot.anchorMin = new Vector2(0.5f, 1f);
                gaugeRoot.anchorMax = new Vector2(0.5f, 1f);
                gaugeRoot.pivot = new Vector2(0.5f, 1f);
                gaugeRoot.anchoredPosition = new Vector2(0f, -topOffset);
            }
        }

        private void RefreshUI()
        {
            if (waterFillImage == null)
            {
                return;
            }

            float ratio = Mathf.Clamp01(currentWater / Mathf.Max(0.0001f, maxWater));
            waterFillImage.fillAmount = ratio;
            Color c = waterColor;
            c.a = Mathf.Lerp(0.2f, waterColor.a, ratio);
            waterFillImage.color = c;
        }

        private static GameObject CreateImageChild(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            return go;
        }

        private static Sprite CreateCircleSprite(int resolution)
        {
            int size = Mathf.Max(16, resolution);
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            float center = size * 0.5f;
            float radius = center - 1f;
            float radiusSq = radius * radius;

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

        private static Sprite CreateRingSprite(int resolution, float thickness01)
        {
            int size = Mathf.Max(16, resolution);
            float normalizedThickness = Mathf.Clamp(thickness01, 0.02f, 0.4f);
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            float center = size * 0.5f;
            float outerRadius = center - 1f;
            float innerRadius = outerRadius * (1f - normalizedThickness);
            float outerSq = outerRadius * outerRadius;
            float innerSq = innerRadius * innerRadius;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float distSq = dx * dx + dy * dy;
                    bool isRingPixel = distSq <= outerSq && distSq >= innerSq;
                    tex.SetPixel(x, y, isRingPixel ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
