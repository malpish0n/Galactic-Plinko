using UnityEngine;
using TMPro; // Assuming TextMeshPro is used for UI

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int currentMoney = 0;

    [Header("UI References")]
    [Tooltip("TextMeshProUGUI component to display the money count.")]
    [SerializeField] private TextMeshProUGUI moneyText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (moneyText != null)
        {
            moneyText.text = $"Money: {currentMoney}";
        }
    }
}

