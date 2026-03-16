using System.Collections;
using UnityEngine;

public class FireEnemyAnimator : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Target to measure distance from (defaults to Player tag if empty).")]
    [SerializeField] private Transform target;
    [SerializeField] private float attackDistance = 4f;
    [SerializeField] private float attackCooldown = 0.5f;

    [Header("Clips")]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip preShootClip;
    [SerializeField] private AnimationClip shootClip;
    [SerializeField] private AnimationClip postShootClip;

    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField, Min(0f)] private float bulletSpeed = 10f;
    [SerializeField, Min(0f)] private float bulletLifetime = 4f;
    [SerializeField] private Vector2 fireOffset = new Vector2(0.6f, 0.05f);

    private Animator anim;
    private bool isAttacking;
    private float nextAttackTime;
    private bool hasLoggedMissingBullet;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }

        PlayIdleIfNeeded();
    }

    private void Update()
    {
        if (anim == null || isAttacking)
        {
            return;
        }

        if (target == null)
        {
            PlayIdleIfNeeded();
            return;
        }

        float sqrDist = (target.position - transform.position).sqrMagnitude;
        float attackSqr = attackDistance * attackDistance;

        if (sqrDist <= attackSqr && Time.time >= nextAttackTime)
        {
            StartCoroutine(AttackSequence());
            return;
        }

        PlayIdleIfNeeded();
    }

    private IEnumerator AttackSequence()
    {
        isAttacking = true;

        yield return PlayFor(preShootClip, "PreShoot", 0.42f);
        SpawnBullet();
        yield return PlayFor(shootClip, "Shoot", 0.08f);
        yield return PlayFor(postShootClip, "PostShoot", 0.25f);

        nextAttackTime = Time.time + attackCooldown;
        isAttacking = false;
    }

    private IEnumerator PlayFor(AnimationClip clip, string stateName, float fallbackLength)
    {
        if (anim != null)
        {
            anim.Play(stateName, 0, 0f);
        }

        float length = clip != null ? clip.length : fallbackLength;
        if (length <= 0f)
        {
            length = 0.1f;
        }

        yield return new WaitForSeconds(length);
    }

    private void SpawnBullet()
    {
        if (bulletPrefab == null)
        {
            if (!hasLoggedMissingBullet)
            {
                Debug.LogWarning("[FireEnemyAnimator] Missing bulletPrefab reference.", this);
                hasLoggedMissingBullet = true;
            }
            return;
        }

        float facingSign = transform.localScale.x >= 0f ? 1f : -1f;
        Vector3 spawnPos = transform.position + new Vector3(fireOffset.x * facingSign, fireOffset.y, 0f);
        GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        Rigidbody2D bulletRb2D = bulletObj.GetComponent<Rigidbody2D>();
        if (bulletRb2D != null)
        {
            bulletRb2D.linearVelocity = new Vector2(bulletSpeed * facingSign, 0f);
            bulletRb2D.gravityScale = 0f;
        }
        else
        {
            Rigidbody bulletRb3D = bulletObj.GetComponent<Rigidbody>();
            if (bulletRb3D != null)
            {
                bulletRb3D.useGravity = false;
                bulletRb3D.linearVelocity = new Vector3(bulletSpeed * facingSign, 0f, 0f);
                bulletRb3D.constraints = RigidbodyConstraints.FreezePositionY
                                       | RigidbodyConstraints.FreezePositionZ
                                       | RigidbodyConstraints.FreezeRotation;
            }
        }

        if (bulletLifetime > 0f)
        {
            Destroy(bulletObj, bulletLifetime);
        }
    }

    private void PlayIdleIfNeeded()
    {
        if (anim == null)
        {
            return;
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("Idle"))
        {
            return;
        }

        anim.Play("Idle", 0, 0f);
    }
}
