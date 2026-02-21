using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DropButtonSetup))]
public class DropButtonSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        DropButtonSetup setup = (DropButtonSetup)target;

        SerializedProperty useCooldownProp = serializedObject.FindProperty("useCooldown");
        bool useCooldown = useCooldownProp != null && useCooldownProp.boolValue;

        if (useCooldown)
        {
            EditorGUILayout.HelpBox(
                "Tryb Z COOLDOWNEM: Przycisk będzie używać BallDropperController.OnDropButtonPressed() - użytkownik nie może spamować przycisku.",
                MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Tryb BEZ COOLDOWNU: Przycisk będzie bezpośrednio wywoływać Dropper.DropBall() - brak ograniczeń.",
                MessageType.Warning
            );
        }

        if (GUILayout.Button("Setup Drop Button", GUILayout.Height(40)))
        {
            setup.SetupDropButton();
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Clear Drop Button", GUILayout.Height(25)))
        {
            setup.ClearDropButton();
        }
    }
}

