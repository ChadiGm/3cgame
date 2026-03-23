using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class OneDropWaterResource2D : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField, Min(1)] private int maxHealthPoints = 4;
        [SerializeField, Min(0f)] private float gameRestartDelay = 0.6f;

        [Header("Water Resource")]
        [SerializeField, Min(1f)] private float maxWater = 100f;
        [SerializeField, Min(0f)] private float moveDrainPerSecond = 3.5f;
        [SerializeField, Min(0f)] private float moveSpeedThreshold = 0.2f;
        [SerializeField, Min(0f)] private float jumpDrain = 10f;
        [SerializeField, Min(0f)] private float slideDrain = 12f;
        [SerializeField, Min(0f)] private float shootDrain = 6f;
        [SerializeField, Min(0f)] private float damageDrain = 20f;

        [Header("Death")]
        [SerializeField] private GameObject deathVaporEffectPrefab;
        [SerializeField, Min(0f)] private float disableDelay = 0.15f;
        [SerializeField, Min(0f)] private float respawnDelay = 1.2f;
        [SerializeField] private bool useCheckpointManager = true;
        [SerializeField] private CheckpointManager2D checkpointManager;
        [SerializeField] private bool debugRespawnLogs = false;

        private float currentWater;
        private int currentHealthPoints;
        private bool dead;
        private Rigidbody2D rb;
        private Vector3 initialSpawnPosition;
        private Quaternion initialSpawnRotation;

        public float WaterRatio => Mathf.Clamp01(currentWater / Mathf.Max(0.0001f, maxWater));
        public int CurrentHealthPoints => currentHealthPoints;
        public int MaxHealthPoints => maxHealthPoints;
        public event Action<int, int> HealthChanged;
        public event Action FinalHealthDepleted;

        private void Reset()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EnsureEditorUiAndFlowComponents();
        }

        private void OnValidate()
        {
            maxHealthPoints = Mathf.Max(1, maxHealthPoints);
            gameRestartDelay = Mathf.Max(0f, gameRestartDelay);
            maxWater = Mathf.Max(1f, maxWater);
            moveDrainPerSecond = Mathf.Max(0f, moveDrainPerSecond);
            jumpDrain = Mathf.Max(0f, jumpDrain);
            slideDrain = Mathf.Max(0f, slideDrain);
            shootDrain = Mathf.Max(0f, shootDrain);
            damageDrain = Mathf.Max(0f, damageDrain);

            if (Application.isPlaying)
            {
                return;
            }

            EnsureEditorUiAndFlowComponents();
        }


        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            maxHealthPoints = Mathf.Max(1, maxHealthPoints);
            gameRestartDelay = Mathf.Max(0f, gameRestartDelay);
            maxWater = Mathf.Max(1f, maxWater);
            moveDrainPerSecond = Mathf.Max(0f, moveDrainPerSecond);
            jumpDrain = Mathf.Max(0f, jumpDrain);
            slideDrain = Mathf.Max(0f, slideDrain);
            shootDrain = Mathf.Max(0f, shootDrain);
            damageDrain = Mathf.Max(0f, damageDrain);
            currentHealthPoints = maxHealthPoints;
            currentWater = maxWater;
            initialSpawnPosition = transform.position;
            initialSpawnRotation = transform.rotation;
            ResolveCheckpointManagerAndRegisterPlayer();

            EnsureHealthUI();
            EnsureGameFlowSystem();
            NotifyHealthChanged();
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
            if (Consume(damageDrain))
            {
                TriggerDamageFeedback();
            }
        }

        public void DepleteAllWater()
        {
            if (dead)
            {
                return;
            }

            currentWater = 0f;
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

            if (currentWater <= 0f)
            {
                StartCoroutine(HandleDeath());
                return false;
            }

            return true;
        }

        private void TriggerDamageFeedback()
        {
            OneDropDamageFeedback feedback = GetComponent<OneDropDamageFeedback>();
            if (feedback == null)
            {
                feedback = gameObject.AddComponent<OneDropDamageFeedback>();
            }
            feedback.Play();
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

            ConsumeHealthOnDeath();

            if (currentHealthPoints > 0)
            {
                yield return new WaitForSeconds(respawnDelay);
                RespawnPlayer(controller, attack);
                yield break;
            }

            bool hasFinalDeathListeners = FinalHealthDepleted != null;
            FinalHealthDepleted?.Invoke();
            if (!hasFinalDeathListeners)
            {
                yield return new WaitForSeconds(gameRestartDelay);
                RestartGameFromBeginning();
            }
        }

        private void RespawnPlayer(OneDropController2D controller, OneDropAttack2D attack)
        {
            Vector3 respawnPosition = initialSpawnPosition;
            Quaternion respawnRotation = initialSpawnRotation;

            ResolveCheckpointManagerAndRegisterPlayer();
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
        }

        private void ResolveCheckpointManagerAndRegisterPlayer()
        {
            if (!useCheckpointManager)
            {
                return;
            }

            if (checkpointManager == null)
            {
                checkpointManager = CheckpointManager2D.Instance;
            }

            if (checkpointManager == null)
            {
                checkpointManager = FindFirstObjectByType<CheckpointManager2D>(FindObjectsInactive.Include);
            }

            if (checkpointManager == null)
            {
                checkpointManager = CheckpointManager2D.EnsureInstance();
            }

            if (checkpointManager != null)
            {
                checkpointManager.RegisterPlayer(transform);
            }
        }

        private void ConsumeHealthOnDeath()
        {
            currentHealthPoints = Mathf.Max(0, currentHealthPoints - 1);
            NotifyHealthChanged();
        }

        private void NotifyHealthChanged()
        {
            HealthChanged?.Invoke(currentHealthPoints, maxHealthPoints);
        }

        private void EnsureHealthUI()
        {
            if (GetComponent<OneDropHealthBlobUI2D>() == null)
            {
                gameObject.AddComponent<OneDropHealthBlobUI2D>();
            }
        }

        private void EnsureGameFlowSystem()
        {
            if (FindObjectsByType<OneDropGameManager2D>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0)
            {
                gameObject.AddComponent<OneDropGameManager2D>();
            }

            if (FindObjectsByType<OneDropGameUIManager2D>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0)
            {
                gameObject.AddComponent<OneDropGameUIManager2D>();
            }
        }

        private void EnsureEditorUiAndFlowComponents()
        {
            if (GetComponent<OneDropHealthBlobUI2D>() == null)
            {
                gameObject.AddComponent<OneDropHealthBlobUI2D>();
            }

            if (GetComponent<OneDropGameManager2D>() == null)
            {
                gameObject.AddComponent<OneDropGameManager2D>();
            }

            if (GetComponent<OneDropGameUIManager2D>() == null)
            {
                gameObject.AddComponent<OneDropGameUIManager2D>();
            }
        }

        private static void RestartGameFromBeginning()
        {
            Time.timeScale = 1f;

            if (SceneManager.sceneCountInBuildSettings > 0)
            {
                SceneManager.LoadScene(0);
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
            }
            else
            {
                SceneManager.LoadScene(activeScene.name);
            }
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
    }
}
