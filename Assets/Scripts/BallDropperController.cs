using UnityEngine;

public class BallDropperController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Cooldown time in seconds between button clicks.")]
    [SerializeField] private float dropCooldown = 0.5f;
    
    private Dropper _dropper;
    private float _lastDropTime = -999f;
    
    private void Start()
    {
        _dropper = GetComponent<Dropper>();

        if (_dropper == null)
        {
            _dropper = FindFirstObjectByType<Dropper>();
        }
    }
    
    // Publiczna metoda do wywołania z przycisków UI
    public void OnDropButtonPressed()
    {
        if (_dropper == null) return;
        
        // Sprawdź cooldown
        if (Time.time - _lastDropTime < dropCooldown)
        {
            Debug.Log($"[BallDropperController] Cooldown active! Wait {dropCooldown - (Time.time - _lastDropTime):F2}s");
            return;
        }
        
        _dropper.DropBall();
        _lastDropTime = Time.time;
        Debug.Log("[BallDropperController] Drop button pressed!");
    }
}

