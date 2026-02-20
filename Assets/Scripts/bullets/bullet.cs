using UnityEngine;

public class bullet : MonoBehaviour
{
    [Tooltip("Seconds before the bullet auto-destroys")]
    public float lifeTime = 4f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Destroy bullet on any 2D collision (not with the player who shot it)
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Destroy bullet on any 3D collision
        Destroy(gameObject);
    }
}
