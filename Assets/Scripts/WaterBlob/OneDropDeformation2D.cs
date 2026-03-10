using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(OneDropController2D))]
    public class OneDropDeformation2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private WaterBlobCharacter2D blobCharacter;
        [SerializeField] private OneDropWaterResource2D waterResource;

        [Header("Soft Body Visual Settings")]
        [SerializeField, Min(0f)] private float deformLerpSpeed = 14f;
        [SerializeField, Min(0f)] private float idleGroundSquash = 0.08f;
        [SerializeField, Min(0f)] private float idleGroundSpread = 0.06f;
        [SerializeField, Min(0f)] private float runSquash = 0.12f;
        [SerializeField, Min(0f)] private float accelerationStretch = 0.1f;
        [SerializeField, Min(0f)] private float decelerationSquash = 0.12f;
        [SerializeField, Min(0f)] private float jumpStretch = 0.2f;
        [SerializeField, Min(0f)] private float fallStretch = 0.16f;
        [SerializeField, Min(0f)] private float jumpPreCompress = 0.16f;
        [SerializeField, Min(0f)] private float landingSquash = 0.22f;
        [SerializeField, Min(0f)] private float landingMinImpactSpeed = 6f;
        [SerializeField, Min(0f)] private float damageSquash = 0.28f;
        [SerializeField, Min(0f)] private float peakStretch = 0.08f;
        [SerializeField, Min(0f)] private float slopeAdaptiveSquash = 0.08f;
        [SerializeField, Min(0f)] private float wallAdhesionFlatten = 0.12f;
        [SerializeField, Min(0f)] private float wallAdhesionStretch = 0.1f;
        [SerializeField, Min(0f)] private float wallDetachStretch = 0.12f;
        [SerializeField, Min(0f)] private float visualMomentumOffset = 0.09f;

        [Header("Anti Crush")]
        [SerializeField, Range(0.3f, 1f)] private float minVisualHeightScale = 0.82f;
        [SerializeField, Range(0f, 1f)] private float ceilingCompressionDampen = 0.25f;
        [SerializeField, Range(0f, 1f)] private float ceilingSpreadDampen = 0.55f;

        private Vector3 baseVisualScale;
        private Vector3 baseVisualLocalPosition;
        private bool visualRootIsSelf;
        private bool waterSubscribed;

        private float landingPulse;
        private float jumpPulse;
        private float wallDetachPulse;
        private int wallDetachDirection;
        private float accelerationSignal;
        private float damagePulse;

        private void Awake()
        {
            BindReferences();
            CacheVisualDefaults();
        }

        private void OnEnable()
        {
            BindReferences();
            SubscribeWaterDamage();
        }

        private void OnDisable()
        {
            UnsubscribeWaterDamage();
        }

        private void OnValidate()
        {
            BindReferences();
            CacheVisualDefaults();
        }

        private void BindReferences()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            if (blobCharacter == null)
            {
                blobCharacter = GetComponent<WaterBlobCharacter2D>();
            }

            if (waterResource == null)
            {
                waterResource = GetComponent<OneDropWaterResource2D>();
            }
        }

        private void CacheVisualDefaults()
        {
            if (visualRoot == null)
            {
                return;
            }

            baseVisualScale = visualRoot.localScale;
            baseVisualLocalPosition = visualRoot.localPosition;
            visualRootIsSelf = visualRoot == transform;
        }

        private void SubscribeWaterDamage()
        {
            if (waterSubscribed || waterResource == null)
            {
                return;
            }

            waterResource.OnTakeDamage += HandleDamageTaken;
            waterSubscribed = true;
        }

        private void UnsubscribeWaterDamage()
        {
            if (!waterSubscribed || waterResource == null)
            {
                return;
            }

            waterResource.OnTakeDamage -= HandleDamageTaken;
            waterSubscribed = false;
        }

        private void HandleDamageTaken()
        {
            damagePulse = 1f;
        }

        public void ApplyControllerState(OneDropController2D.DeformationState state, float deltaTime)
        {
            if (visualRoot == null)
            {
                return;
            }

            DecayPulses(deltaTime);
            ApplySoftBodyState(state, deltaTime);
            ApplyVisualState(state, deltaTime);
        }

        public void NotifyLanding(float impactSpeed)
        {
            float impact = Mathf.Clamp01((impactSpeed - landingMinImpactSpeed) / Mathf.Max(0.01f, landingMinImpactSpeed));
            landingPulse = Mathf.Clamp01(0.55f + impact);
        }

        public void NotifyJump(Vector2 pointImpulse)
        {
            jumpPulse = 1f;
            ApplyPointImpulse(pointImpulse);
        }

        public void NotifyWallDetach(int direction, Vector2 pointImpulse)
        {
            wallDetachPulse = 1f;
            wallDetachDirection = direction;
            ApplyPointImpulse(pointImpulse);
        }

        private void DecayPulses(float deltaTime)
        {
            landingPulse = Mathf.MoveTowards(landingPulse, 0f, 3.2f * deltaTime);
            jumpPulse = Mathf.MoveTowards(jumpPulse, 0f, 7f * deltaTime);
            wallDetachPulse = Mathf.MoveTowards(wallDetachPulse, 0f, 6f * deltaTime);
            damagePulse = Mathf.MoveTowards(damagePulse, 0f, 2.8f * deltaTime);
        }

        private void ApplySoftBodyState(OneDropController2D.DeformationState state, float deltaTime)
        {
            if (blobCharacter == null || blobCharacter.PointBodies == null)
            {
                return;
            }

            bool enableSoftBodySimulation = state.SoftBodyMode != OneDropController2D.SoftBodyReactionMode.ClimbSync;
            if (blobCharacter.enabled != enableSoftBodySimulation)
            {
                blobCharacter.enabled = enableSoftBodySimulation;
            }

            float targetGravity = enableSoftBodySimulation ? blobCharacter.gravityScale : 0f;
            for (int i = 0; i < blobCharacter.PointBodies.Count; i++)
            {
                Rigidbody2D pointBody = blobCharacter.PointBodies[i];
                if (pointBody == null)
                {
                    continue;
                }

                pointBody.gravityScale = targetGravity;

                switch (state.SoftBodyMode)
                {
                    case OneDropController2D.SoftBodyReactionMode.SlideSync:
                    {
                        float pointSyncStiffness = (blobCharacter != null) ? blobCharacter.pointSyncStiffness : 45f;
                        float nextVX = Mathf.MoveTowards(pointBody.linearVelocity.x, state.SoftBodySyncVelocity.x, pointSyncStiffness * deltaTime);
                        pointBody.linearVelocity = new Vector2(nextVX, pointBody.linearVelocity.y);
                        break;
                    }
                    case OneDropController2D.SoftBodyReactionMode.ClimbSync:
                    {
                        float pointSyncStiffness = (blobCharacter != null) ? blobCharacter.pointSyncStiffness : 45f;
                        float nextVX = Mathf.MoveTowards(pointBody.linearVelocity.x, state.SoftBodySyncVelocity.x, pointSyncStiffness * deltaTime);
                        float nextVY = Mathf.MoveTowards(pointBody.linearVelocity.y, state.SoftBodySyncVelocity.y, pointSyncStiffness * deltaTime);
                        pointBody.linearVelocity = new Vector2(nextVX, nextVY);
                        break;
                    }
                }
            }
        }

        private void ApplyVisualState(OneDropController2D.DeformationState state, float deltaTime)
        {
            Vector2 velocity = state.Velocity;
            float safeDt = Mathf.Max(0.0001f, deltaTime);
            accelerationSignal = Mathf.MoveTowards(
                accelerationSignal,
                (velocity.x - state.PreviousVelocity.x) / safeDt,
                state.Acceleration * 2.8f * safeDt);

            float speed01 = Mathf.Clamp01(Mathf.Abs(velocity.x) / Mathf.Max(0.01f, state.MoveSpeed));
            float rise01 = Mathf.Clamp01(velocity.y / Mathf.Max(0.01f, state.JumpForce));
            float fall01 = Mathf.Clamp01(-velocity.y / Mathf.Max(0.01f, state.JumpForce));
            float accel01 = Mathf.Clamp01(Mathf.Abs(accelerationSignal) / Mathf.Max(0.01f, state.Acceleration * 2f));

            bool accelerating = Mathf.Abs(state.InputX) > 0.05f && Mathf.Sign(state.InputX) == Mathf.Sign(velocity.x) && Mathf.Abs(velocity.x) > 0.08f;
            bool decelerating = Mathf.Abs(velocity.x) > 0.08f && (Mathf.Abs(state.InputX) < 0.05f || Mathf.Sign(state.InputX) != Mathf.Sign(velocity.x));

            float peak01 = (!state.IsGrounded && !state.IsClimbing) ? 1f - Mathf.Clamp01(Mathf.Abs(velocity.y) / 1.4f) : 0f;
            float slope01 = (state.IsGrounded && !state.IsClimbing)
                ? Mathf.Clamp01(Mathf.Abs(Vector2.SignedAngle(Vector2.up, state.GroundNormal)) / Mathf.Max(1f, state.MaxGroundAngle))
                : 0f;
            float wall01 = (state.IsClimbing || state.TouchingWall) ? 1f : 0f;
            float idle01 = state.IsGrounded && Mathf.Abs(velocity.x) < 0.08f && !state.IsClimbing ? 1f : 0f;

            float horizontalSpread = 0f;
            float verticalCompress = 0f;
            float verticalStretch = 0f;

            horizontalSpread += idleGroundSpread * idle01;
            verticalCompress += idleGroundSquash * idle01;

            horizontalSpread += runSquash * speed01;
            verticalCompress += runSquash * speed01 * 0.58f;

            if (accelerating)
            {
                horizontalSpread += accelerationStretch * accel01;
            }

            if (decelerating)
            {
                verticalCompress += decelerationSquash * accel01;
            }

            verticalStretch += jumpStretch * rise01;
            verticalStretch += fallStretch * fall01;
            verticalStretch += peakStretch * peak01;

            horizontalSpread += jumpPreCompress * jumpPulse;
            verticalCompress += jumpPreCompress * jumpPulse;

            horizontalSpread += landingSquash * landingPulse;
            verticalCompress += landingSquash * landingPulse * 0.95f;

            horizontalSpread += damageSquash * damagePulse;
            verticalCompress += damageSquash * damagePulse * 0.9f;

            horizontalSpread += slopeAdaptiveSquash * slope01;
            verticalCompress += slopeAdaptiveSquash * slope01 * 0.6f;

            horizontalSpread += wallAdhesionFlatten * wall01;
            verticalStretch += wallAdhesionStretch * wall01;

            horizontalSpread += wallDetachStretch * wallDetachPulse;
            verticalStretch += wallDetachStretch * wallDetachPulse * 0.32f;

            if (state.TouchingCeiling)
            {
                float damp = Mathf.Lerp(1f, ceilingCompressionDampen, state.CeilingCompression01);
                verticalCompress *= damp;
                horizontalSpread *= Mathf.Lerp(1f, ceilingSpreadDampen, state.CeilingCompression01);
            }

            float targetXAbs = Mathf.Abs(baseVisualScale.x) * (1f + horizontalSpread - verticalStretch * 0.35f);
            float targetY = baseVisualScale.y * (1f - verticalCompress + verticalStretch);
            targetY = Mathf.Max(baseVisualScale.y * minVisualHeightScale, targetY);

            float targetSign = Mathf.Sign(state.FacingSign);
            if (targetSign == 0f)
            {
                targetSign = 1f;
            }

            Vector3 targetScale = new(Mathf.Max(0.08f, targetXAbs) * targetSign, Mathf.Max(0.08f, targetY), baseVisualScale.z);
            targetScale.x = Mathf.Clamp(targetScale.x, -baseVisualScale.x * 2.5f, baseVisualScale.x * 2.5f);
            targetScale.y = Mathf.Clamp(targetScale.y, 0.08f, baseVisualScale.y * 2.5f);

            Vector3 targetPos = baseVisualLocalPosition;
            if (!visualRootIsSelf)
            {
                float momentumSign = Mathf.Abs(velocity.x) > 0.1f ? Mathf.Sign(velocity.x) : Mathf.Sign(state.FacingSign);
                float momentumOffset = visualMomentumOffset * speed01;
                float accelOffset = visualMomentumOffset * 0.7f * Mathf.Clamp(accelerationSignal / Mathf.Max(0.01f, state.Acceleration * 2f), -1f, 1f);
                int wallDirection = state.TouchingWallDirection == 0 ? state.ClimbWallDirection : state.TouchingWallDirection;
                float wallOffset = wall01 > 0f ? 0.04f * Mathf.Sign(wallDirection) : 0f;
                float detachOffset = 0.06f * wallDetachPulse * wallDetachDirection;
                float groundSink = (idleGroundSquash * idle01 + landingSquash * landingPulse * 0.5f) * 0.2f;

                targetPos.x += momentumOffset * momentumSign + accelOffset + wallOffset - detachOffset;
                targetPos.y -= groundSink;
            }

            visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, targetScale, deformLerpSpeed * deltaTime);
            if (!visualRootIsSelf)
            {
                visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, targetPos, deformLerpSpeed * deltaTime);
            }
        }

        private void ApplyPointImpulse(Vector2 impulse)
        {
            if (blobCharacter == null || blobCharacter.PointBodies == null || impulse == Vector2.zero)
            {
                return;
            }

            for (int i = 0; i < blobCharacter.PointBodies.Count; i++)
            {
                Rigidbody2D pointBody = blobCharacter.PointBodies[i];
                if (pointBody != null)
                {
                    pointBody.AddForce(impulse, ForceMode2D.Impulse);
                }
            }
        }
    }
}
