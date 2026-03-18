using UnityEngine;
using WaterBlob;

/// <summary>
/// Damages the player when an enemy bullet hits.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EnemyBulletDamage : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null)
        {
            TryDamage(collision.gameObject);
        }
    }

    private void TryDamage(GameObject hit)
    {
        if (hit == null || !hit.CompareTag(playerTag))
        {
            return;
        }

        OneDropWaterResource2D resource = hit.GetComponentInParent<OneDropWaterResource2D>();
        if (resource != null)
        {
            resource.ConsumeDamage();
        }

        Destroy(gameObject);
    }
}
