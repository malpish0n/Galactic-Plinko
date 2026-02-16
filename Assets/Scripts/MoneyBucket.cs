using UnityEngine;
using System.Collections.Generic;

public class MoneyBucket : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the Dropper that spawns balls.")]
    [SerializeField] private Dropper dropper;

    [Header("Settings")]
    [Tooltip("Base amount of money to give when a ball enters this bucket.")]
    [SerializeField] private int moneyValue = 10;

    [Tooltip("Multiplier for money value. Final reward = moneyValue * moneyMultiplier")]
    [SerializeField] private float moneyMultiplier = 1f;

    [Tooltip("Tag of the object to detect (usually the ball). Leave empty to catch all objects.")]
    [SerializeField] private string ballTag = "";

    // Track processed balls to avoid counting them multiple times
    private HashSet<GameObject> _processedBalls = new HashSet<GameObject>();

    private void Start()
    {
        // Auto-find dropper if not assigned
        if (dropper == null)
        {
            dropper = FindAnyObjectByType<Dropper>();
            if (dropper == null)
            {
                Debug.LogError("[MoneyBucket] No Dropper found in scene!", this);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleCollision(other.gameObject);
    }

    private void HandleCollision(GameObject obj)
    {
        // Check if this ball was already processed
        if (_processedBalls.Contains(obj))
        {
            return; // Ignore, already counted
        }

        // Check tag if specified
        if (!string.IsNullOrEmpty(ballTag) && !obj.CompareTag(ballTag))
        {
            return;
        }

        // Mark ball as processed
        _processedBalls.Add(obj);

        // Calculate final money value with multiplier
        int finalMoneyValue = Mathf.RoundToInt(moneyValue * moneyMultiplier);

        // Add money
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddMoney(finalMoneyValue);
        }

        // Destroy ball
        if (dropper != null)
        {
            dropper.DestroyBall(obj);
            Debug.Log($"[MoneyBucket] Ball destroyed. Money added: {finalMoneyValue} (base: {moneyValue} x {moneyMultiplier})");
        }
        else
        {
            Debug.LogWarning("[MoneyBucket] Dropper reference is null! Destroying ball directly.");
            Destroy(obj);
        }
    }

    /// <summary>
    /// Sets the money multiplier for this bucket.
    /// </summary>
    public void SetMoneyMultiplier(float multiplier)
    {
        moneyMultiplier = Mathf.Max(0f, multiplier); // Ensure non-negative
        Debug.Log($"[MoneyBucket] Money multiplier set to {moneyMultiplier}x");
    }

    /// <summary>
    /// Gets the current money multiplier.
    /// </summary>
    public float GetMoneyMultiplier()
    {
        return moneyMultiplier;
    }

    /// <summary>
    /// Gets the final money value (base * multiplier).
    /// </summary>
    public int GetFinalMoneyValue()
    {
        return Mathf.RoundToInt(moneyValue * moneyMultiplier);
    }
}
