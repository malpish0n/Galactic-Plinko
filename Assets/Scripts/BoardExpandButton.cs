using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script for "Expand Board" button - expands the board while preserving Dropper and MoneyBuckets.
/// </summary>
public class BoardExpandButton : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to PlinkoBoardGenerator")]
    [SerializeField] private PlinkoBoardGenerator boardGenerator;
    
    [Header("Settings")]
    [Tooltip("How many rows to expand the board by per click")]
    [SerializeField] private int rowsPerExpand = 1;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        
        if (_button != null)
        {
            _button.onClick.AddListener(OnExpandButtonClicked);
        }
        
        // If not assigned in inspector, find automatically
        if (boardGenerator == null)
        {
            boardGenerator = Object.FindFirstObjectByType<PlinkoBoardGenerator>();
            
            if (boardGenerator == null)
            {
                Debug.LogError("[BoardExpandButton] PlinkoBoardGenerator not found in scene!", this);
            }
        }
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(OnExpandButtonClicked);
        }
    }

    private void OnExpandButtonClicked()
    {
        if (boardGenerator != null)
        {
            boardGenerator.ExpandBoard(rowsPerExpand);
            Debug.Log($"[BoardExpandButton] Board expanded by {rowsPerExpand} rows!");
        }
        else
        {
            Debug.LogError("[BoardExpandButton] Board Generator reference is null!");
        }
    }
}

