using UnityEngine;

/// <summary>
/// Collects balls and awards points/money to the player.
/// </summary>
public class MoneyBucket : MonoBehaviour
{
    [Header("Bucket Settings")]
    [Tooltip("Amount of money awarded per collected ball.")]
    [SerializeField] private int moneyPerBall = 10;

    [Tooltip("Reference to the Dropper to recycle balls. If empty, it tries to find one automatically.")]
    [SerializeField] private Dropper dropper;

    private void Start()
    {
        if (dropper == null)
        {
            // Find Dropper if not manually assigned
#if UNITY_2023_1_OR_NEWER
            dropper = FindFirstObjectByType<Dropper>();
#else
            dropper = FindObjectOfType<Dropper>();
#endif
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleCollision(other.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other.gameObject);
    }

    private void HandleCollision(GameObject ball)
    {
        // Add logic here to check if the object is indeed a ball, if you have other physics objects.
        // For simplicity, we assume anything falling into the bucket is a ball.

        AddMoney(moneyPerBall);

        if (dropper != null)
        {
            dropper.ReturnToPool(ball);
        }
        else
        {
            // Fallback if no dropper logic exists
            Debug.LogWarning("Dropper reference missing in MoneyBucket! Destroying ball.");
            Destroy(ball);
        }
    }

    private void AddMoney(int amount)
    {
        // Use the MoneyManager singleton if it exists
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddMoney(amount);
        }
        else
        {
            Debug.Log($"Collected a ball! Added ${amount} (No MoneyManager found).");
        }
    }
}
