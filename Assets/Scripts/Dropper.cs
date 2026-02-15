using System.Collections.Generic;
using UnityEngine;

public class Dropper : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Prefab of the ball to drop.")]
    [SerializeField] private GameObject ballPrefab;

    [Tooltip("Time in seconds between drops (if auto-drop is enabled).")]
    [SerializeField] private float spawnInterval = 0.5f;

    [Tooltip("Minimum time in seconds between manual drops (button click).")]
    [SerializeField] private float manualDropCooldown = 0.2f;

    [Tooltip("Should balls drop automatically?")]
    [SerializeField] private bool autoDrop;

    [Header("Spawn Settings")]
    [Tooltip("Offset from the dropper's position (Y axis) to spawn the ball.")]
    [SerializeField] private float spawnOffsetY = -0.5f;

    [Tooltip("Width of the area from which balls will be dropped (X axis).")]
    [SerializeField] private float spawnWidth = 1.0f;

    [Header("Pool Settings")]
    [Tooltip("Initial size of the object pool.")]
    [SerializeField] private int initialPoolSize = 20;

    [Tooltip("Y position below which balls are returned to the pool.")]
    [SerializeField] private float destroyYThreshold = -20f;

    // Queue serving as the object pool
    private readonly Queue<GameObject> _ballPool = new Queue<GameObject>();
    private readonly List<GameObject> _activeBalls = new List<GameObject>(); // Track active balls to check their position
    private float _timer;
    private float _lastManualDropTime = -999f; // Allow immediate first drop

    private void Awake()
    {
        InitializePool();
    }

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

    /// <summary>
    /// Checks if any active balls have fallen below the threshold and recycles them.
    /// </summary>
    private void CheckActiveBalls()
    {
        for (int i = _activeBalls.Count - 1; i >= 0; i--)
        {
            GameObject ball = _activeBalls[i];
            
            // If ball was destroyed externally (shouldn't happen often), just remove from list
            if (ball == null)
            {
                _activeBalls.RemoveAt(i);
                continue;
            }

            if (ball.transform.position.y < destroyYThreshold)
            {
                // ReturnToPool will handle removing from the list now
                ReturnToPool(ball);
            }
        }
    }

    /// <summary>
    /// Initializes the object pool to avoid performance drops.
    /// </summary>
    private void InitializePool()
    {
        if (ballPrefab == null)
        {
            Debug.LogError("Ball Prefab is not assigned in Dropper!", this);
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewBallForPool();
        }
    }

    /// <summary>
    /// Creates a new ball and adds it to the pool.
    /// </summary>
    private GameObject CreateNewBallForPool()
    {
        // Instantiate without a parent initially to avoid inheriting the Dropper's scale
        GameObject ball = Instantiate(ballPrefab);
        
        // Ensure scale is correct from prefab
        ball.transform.localScale = ballPrefab.transform.localScale;

        ball.SetActive(false);
        _ballPool.Enqueue(ball);
        return ball;
    }

    /// <summary>
    /// Tries to drop a ball manually. Connect this to your UI Button.
    /// </summary>
    public void DropBallManual()
    {
        if (Time.time >= _lastManualDropTime + manualDropCooldown)
        {
            DropBall();
            _lastManualDropTime = Time.time;
        }
    }

    /// <summary>
    /// Public method to drop a ball.
    /// </summary>
    public void DropBall()
    {
        if (ballPrefab == null) return;

        GameObject ball = GetBallFromPool();

        // Calculate random X position
        float randomX = Random.Range(-spawnWidth / 2f, spawnWidth / 2f);

        // Spawn from the center of this object with Y offset and random X
        Vector3 spawnPosition = transform.position + new Vector3(randomX, spawnOffsetY, 0);

        ball.transform.position = spawnPosition;
        ball.transform.rotation = Quaternion.identity;
        ball.transform.localScale = ballPrefab.transform.localScale; // Force reset scale again
        ball.SetActive(true);

        _activeBalls.Add(ball); // Track this active ball

        ResetPhysics(ball);
    }

    /// <summary>
    /// Resets object velocity (important when reusing from pool).
    /// </summary>
    private void ResetPhysics(GameObject ball)
    {
        // 3D physics handling
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        // 2D physics handling
        Rigidbody2D rb2d = ball.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;
            rb2d.angularVelocity = 0f;
        }
    }

    /// <summary>
    /// Retrieves a ball from the pool.
    /// </summary>
    private GameObject GetBallFromPool()
    {
        if (_ballPool.Count == 0)
        {
            return CreateNewBallForPool();
        }

        GameObject ball = _ballPool.Dequeue();

        // If the retrieved ball is still active (e.g. pool is too small),
        // create a new one instead of taking the one visible to the player.
        if (ball.activeInHierarchy)
        {
            // Add the active one back to the queue for later
            _ballPool.Enqueue(ball);
            return CreateNewBallForPool();
        }

        return ball;
    }

    /// <summary>
    /// Returns a ball back to the pool.
    /// </summary>
    public void ReturnToPool(GameObject ball)
    {
        if (ball == null) return;

        ball.SetActive(false);
        _ballPool.Enqueue(ball);

        // Remove from active list if presents, to avoid duplicates or tracking recycled balls
        if (_activeBalls.Contains(ball))
        {
            _activeBalls.Remove(ball);
        }
    }
}
