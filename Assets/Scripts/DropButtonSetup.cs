using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
#endif

/// <summary>
/// Automatycznie łączy przycisk drop z Dropperem na scenie.
/// Wywołaj SetupDropButton() aby skonfigurować przycisk.
/// </summary>
public class DropButtonSetup : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("References")]
    [Tooltip("Przycisk który ma wywoływać drop kulek")]
    [SerializeField] private Button dropButton;

    [Header("Settings")]
    [Tooltip("Użyj BallDropperController (z cooldownem) zamiast bezpośrednio Dropper")]
    [SerializeField] private bool useCooldown = true;

    [ContextMenu("Setup Drop Button")]
    public void SetupDropButton()
    {
        if (dropButton == null)
        {
            Debug.LogError("[DropButtonSetup] Drop Button is not assigned!");
            return;
        }

        // Wyczyść istniejące listenery
        dropButton.onClick.RemoveAllListeners();

        if (useCooldown)
        {
            SetupWithCooldown();
        }
        else
        {
            SetupDirect();
        }

        // Oznacz jako zmienione w edytorze
        EditorUtility.SetDirty(dropButton);
        EditorUtility.SetDirty(dropButton.gameObject);
    }

    private void SetupWithCooldown()
    {
        // Znajdź BallDropperController na scenie
        BallDropperController controller = FindFirstObjectByType<BallDropperController>();
        
        if (controller == null)
        {
            Debug.LogWarning("[DropButtonSetup] No BallDropperController found. Creating one on Dropper...");
            
            // Znajdź Dropper i dodaj do niego BallDropperController
            Dropper dropper = FindFirstObjectByType<Dropper>();
            if (dropper == null)
            {
                Debug.LogError("[DropButtonSetup] No Dropper found in scene!");
                return;
            }
            
            controller = dropper.gameObject.AddComponent<BallDropperController>();
            EditorUtility.SetDirty(dropper.gameObject);
        }

        // Dodaj listener który wywołuje controller.OnDropButtonPressed()
        UnityAction dropAction = new UnityAction(controller.OnDropButtonPressed);
        UnityEventTools.AddPersistentListener(dropButton.onClick, dropAction);

        Debug.Log($"[DropButtonSetup] Button '{dropButton.name}' configured to call {controller.name}.OnDropButtonPressed() WITH cooldown");
    }

    private void SetupDirect()
    {
        // Znajdź Dropper na scenie
        Dropper dropper = FindFirstObjectByType<Dropper>();
        if (dropper == null)
        {
            Debug.LogError("[DropButtonSetup] No Dropper found in scene!");
            return;
        }

        // Dodaj listener który wywołuje dropper.DropBall()
        UnityAction dropAction = new UnityAction(dropper.DropBall);
        UnityEventTools.AddPersistentListener(dropButton.onClick, dropAction);

        Debug.Log($"[DropButtonSetup] Button '{dropButton.name}' configured to call {dropper.name}.DropBall() WITHOUT cooldown");
    }

    [ContextMenu("Clear Drop Button")]
    public void ClearDropButton()
    {
        if (dropButton == null)
        {
            Debug.LogError("[DropButtonSetup] Drop Button is not assigned!");
            return;
        }

        dropButton.onClick.RemoveAllListeners();
        EditorUtility.SetDirty(dropButton);
        
        Debug.Log($"[DropButtonSetup] Button '{dropButton.name}' listeners cleared.");
    }
#endif
}

