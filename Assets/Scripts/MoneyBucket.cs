using UnityEngine;
using System.Collections.Generic;

public class MoneyBucket : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the Dropper that spawns balls.")]
    [SerializeField] private Dropper dropper;

    [Header("Settings")]
    [Tooltip("Amount of money to give when a ball enters this bucket.")]
    [SerializeField] private int moneyValue = 10;

    [Tooltip("Tag of the object to detect (usually the ball). Leave empty to catch all objects.")]
    [SerializeField] private string ballTag = "";

    // Śledzenie przetworzonych kulek, żeby nie liczyć ich wielokrotnie
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
        // Sprawdź czy ta kulka została już przetworzona
        if (_processedBalls.Contains(obj))
        {
            return; // Ignoruj, już była policzona
        }

        // Check tag if specified
        if (!string.IsNullOrEmpty(ballTag) && !obj.CompareTag(ballTag))
        {
            return;
        }

        // Oznacz kulkę jako przetworzoną
        _processedBalls.Add(obj);

        // Add money
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddMoney(moneyValue);
        }

        // Zniszcz kulkę
        if (dropper != null)
        {
            dropper.DestroyBall(obj);
            Debug.Log($"[MoneyBucket] Ball destroyed. Money added: {moneyValue}");
        }
        else
        {
            Debug.LogWarning("[MoneyBucket] Dropper reference is null! Destroying ball directly.");
            Destroy(obj);
        }
    }
}

