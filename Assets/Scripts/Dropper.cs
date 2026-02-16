using System.Collections.Generic;
using UnityEngine;

public class Dropper : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Prefab of the ball to drop.")]
    [SerializeField] private GameObject ballPrefab;

    [Tooltip("Time in seconds between drops (if auto-drop is enabled).")]
    [SerializeField] private float spawnInterval = 0.5f;

    [Tooltip("Should balls drop automatically?")]
    [SerializeField] private bool autoDrop;

    [Header("Spawn Settings")]
    [Tooltip("Offset from the dropper's position (Y axis) to spawn the ball.")]
    [SerializeField] private float spawnOffsetY = -0.5f;

    [Tooltip("Width of the area from which balls will be dropped (X axis).")]
    [SerializeField] private float spawnWidth = 1.0f;

    [Tooltip("Y position below which balls are destroyed (fallback cleanup).")]
    [SerializeField] private float destroyYThreshold = -20f;

    private List<GameObject> _activeBalls = new List<GameObject>();
    private float _timer;

    private void Update()
    {
        CheckActiveBalls();

        if (!autoDrop) return;

        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            DropBall();
            _timer = 0f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 center = transform.position + new Vector3(0, spawnOffsetY, 0);
        Gizmos.DrawWireCube(center, new Vector3(spawnWidth, 0.5f, 0.1f));
        Gizmos.DrawLine(transform.position, center);
    }

    private void CheckActiveBalls()
    {
        for (int i = _activeBalls.Count - 1; i >= 0; i--)
        {
            GameObject ball = _activeBalls[i];
            
            if (ball == null)
            {
                _activeBalls.RemoveAt(i);
                continue;
            }

            // Jeśli kulka spadła za nisko, zniszcz ją (fallback cleanup)
            if (ball.transform.position.y < destroyYThreshold)
            {
                _activeBalls.RemoveAt(i);
                Destroy(ball);
                Debug.Log($"[Dropper] Ball destroyed (fell too low). Active: {_activeBalls.Count}");
            }
        }
    }

    public void DropBall()
    {
        if (ballPrefab == null)
        {
            Debug.LogError("[Dropper] Ball Prefab is null!");
            return;
        }

        // Losowa pozycja w całym zakresie spawnWidth (bez dead zone)
        float randomX = Random.Range(-spawnWidth / 2f, spawnWidth / 2f);
        
        Vector3 spawnPosition = transform.position + new Vector3(randomX, spawnOffsetY, 0);

        // Stwórz nową kulkę
        GameObject ball = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
        ball.transform.localScale = ballPrefab.transform.localScale;

        // Upewnij się, że kulka ma skrypt Ball (anti-stuck)
        if (ball.GetComponent<Ball>() == null)
        {
            ball.AddComponent<Ball>();
        }

        _activeBalls.Add(ball);

        ResetPhysics(ball);
        
        Debug.Log($"[Dropper] Ball dropped at {spawnPosition}. Active: {_activeBalls.Count}");
    }

    private void ResetPhysics(GameObject ball)
    {
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        Rigidbody2D rb2d = ball.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;
            rb2d.angularVelocity = 0f;
        }
    }

    // Publiczna metoda do niszczenia kulki (wywoływana przez MoneyBucket)
    public void DestroyBall(GameObject ball)
    {
        if (ball == null) return;

        if (_activeBalls.Contains(ball))
        {
            _activeBalls.Remove(ball);
        }

        Destroy(ball);
        Debug.Log($"[Dropper] Ball destroyed. Active: {_activeBalls.Count}");
    }
}

