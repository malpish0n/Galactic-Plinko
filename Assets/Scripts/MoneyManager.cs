using TMPro;
using UnityEngine;

/// <summary>
/// Manages the player's currency and updates the UI.
/// Singleton pattern for easy access from other scripts.
/// </summary>
public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Starting money amount")]
    [SerializeField] private int currentMoney;

    [Header("UI Reference")]
    [Tooltip("Reference to the TextMeshPro UI element that displays the money.")]
    [SerializeField] private TextMeshProUGUI moneyText;

    private void Awake()
    {
        // Simple Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: keep between scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateUI();
    }

    /// <summary>
    /// Adds money to the player's balance.
    /// </summary>
    /// <param name="amount">Amount to add.</param>
    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateUI();
    }

    /// <summary>
    /// Checks if player has enough money to spend.
    /// </summary>
    public bool HasEnoughMoney(int amount)
    {
        return currentMoney >= amount;
    }

    /// <summary>
    /// Spends money if available. Returns true if successful.
    /// </summary>
    public bool SpendMoney(int amount)
    {
        if (HasEnoughMoney(amount))
        {
            currentMoney -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Updates the UI text.
    /// </summary>
    private void UpdateUI()
    {
        if (moneyText != null)
        {
            moneyText.text = $"Money: ${currentMoney}";
        }
    }
}

