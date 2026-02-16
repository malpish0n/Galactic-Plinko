using UnityEngine;

public class Ball : MonoBehaviour
{
    [Header("Anti-Stuck Settings")]
    [Tooltip("Minimalna prędkość - jeśli kulka jest wolniejsza, dostanie pchnięcie")]
    [SerializeField] private float minVelocityThreshold = 0.1f;
    
    [Tooltip("Siła losowego pchnięcia gdy kulka się zatrzyma")]
    [SerializeField] private float unstuckForce = 2f;
    
    [Tooltip("Co ile sekund sprawdzać czy kulka się nie zatrzymała")]
    [SerializeField] private float checkInterval = 0.5f;

    private Rigidbody _rb;
    private Rigidbody2D _rb2d;
    private float _checkTimer;
    private bool _use2D;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb2d = GetComponent<Rigidbody2D>();
        _use2D = (_rb2d != null);
    }

    private void Update()
    {
        _checkTimer += Time.deltaTime;
        
        if (_checkTimer >= checkInterval)
        {
            _checkTimer = 0f;
            CheckIfStuck();
        }
    }

    private void CheckIfStuck()
    {
        float currentSpeed = 0f;
        
        if (_use2D && _rb2d != null)
        {
            currentSpeed = _rb2d.linearVelocity.magnitude;
        }
        else if (_rb != null)
        {
            currentSpeed = _rb.linearVelocity.magnitude;
        }

        // Jeśli kulka prawie się nie rusza - pchnij ją!
        if (currentSpeed < minVelocityThreshold)
        {
            ApplyUnstuckForce();
        }
    }

    private void ApplyUnstuckForce()
    {
        // Losowy kierunek: lewo lub prawo, i trochę w dół
        float randomX = Random.Range(-1f, 1f);
        float downwardY = Random.Range(-0.5f, -1f); // Zawsze trochę w dół
        
        if (_use2D && _rb2d != null)
        {
            Vector2 force = new Vector2(randomX, downwardY).normalized * unstuckForce;
            _rb2d.AddForce(force, ForceMode2D.Impulse);
            Debug.Log($"[Ball] Unstuck impulse applied (2D): {force}");
        }
        else if (_rb != null)
        {
            Vector3 force = new Vector3(randomX, downwardY, 0).normalized * unstuckForce;
            _rb.AddForce(force, ForceMode.Impulse);
            Debug.Log($"[Ball] Unstuck impulse applied (3D): {force}");
        }
    }
}

