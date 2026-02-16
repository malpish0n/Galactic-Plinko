using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

[CustomEditor(typeof(PlinkoBoardGenerator))]
public class PlinkoBoardGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Rysuje domyślny inspektor (wszystkie pola public/SerializeField)
        DrawDefaultInspector();

        PlinkoBoardGenerator generator = (PlinkoBoardGenerator)target;

        GUILayout.Space(10); // Odstęp

        // Przycisk Generuj
        if (GUILayout.Button("Generate Board", GUILayout.Height(30)))
        {
            generator.GenerateBoard();
            // Oznaczamy scenę jako "brudną" (zmienioną), żeby Unity wiedziało, że trzeba zapisać zmiany
            EditorUtility.SetDirty(generator);
        }

        GUILayout.Space(5);

        // Przycisk Czyść
        if (GUILayout.Button("Clear Board"))
        {
            generator.ClearBoard();
            EditorUtility.SetDirty(generator);
        }

        GUILayout.Space(15);
        EditorGUILayout.LabelField("Auto-Setup Drop Button", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Automatycznie znajdzie przycisk 'DropButton' na scenie i połączy go z Dropperem.",
            MessageType.Info
        );

        if (GUILayout.Button("Setup Drop Button (Auto)", GUILayout.Height(30)))
        {
            AutoSetupDropButton();
        }
    }

    private void AutoSetupDropButton()
    {
        // Znajdź przycisk o nazwie "DropButton" (możesz zmienić nazwę)
        Button[] allButtons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        Button dropButton = null;

        foreach (Button btn in allButtons)
        {
            if (btn.name.Contains("Drop") || btn.name.Contains("drop"))
            {
                dropButton = btn;
                break;
            }
        }

        if (dropButton == null)
        {
            EditorUtility.DisplayDialog(
                "Nie znaleziono przycisku",
                "Nie znaleziono przycisku o nazwie zawierającej 'Drop' lub 'drop'. Upewnij się że przycisk istnieje na scenie.",
                "OK"
            );
            return;
        }

        // Znajdź lub stwórz BallDropperController
        BallDropperController controller = Object.FindFirstObjectByType<BallDropperController>();
        
        if (controller == null)
        {
            // Znajdź Dropper i dodaj do niego controller
            Dropper dropper = Object.FindFirstObjectByType<Dropper>();
            if (dropper == null)
            {
                EditorUtility.DisplayDialog(
                    "Nie znaleziono Droppera",
                    "Nie ma na scenie obiektu z komponentem Dropper!",
                    "OK"
                );
                return;
            }

            controller = dropper.gameObject.AddComponent<BallDropperController>();
            EditorUtility.SetDirty(dropper.gameObject);
            Debug.Log($"[PlinkoBoardGenerator] Created BallDropperController on {dropper.name}");
        }

        // Usuń stare listenery
        dropButton.onClick.RemoveAllListeners();

        // Dodaj nowy persistent listener
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            dropButton.onClick,
            new UnityEngine.Events.UnityAction(controller.OnDropButtonPressed)
        );

        EditorUtility.SetDirty(dropButton);
        EditorUtility.SetDirty(dropButton.gameObject);

        EditorUtility.DisplayDialog(
            "Sukces!",
            $"Przycisk '{dropButton.name}' został połączony z {controller.gameObject.name}.OnDropButtonPressed()\n\nCooldown: 0.5s (możesz zmienić w inspektorze)",
            "OK"
        );

        Debug.Log($"[PlinkoBoardGenerator] Button '{dropButton.name}' configured successfully!");
    }
}

