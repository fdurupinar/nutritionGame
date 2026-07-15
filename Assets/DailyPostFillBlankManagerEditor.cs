#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DailyPostFillBlankManager))]
public class DailyPostFillBlankManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Daily Post JSON Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Open the JSON editor. The new JSON structure is Topic > Subtopic > Caption. Captions contain ordered blank scoring and caption-level tactic fit.", MessageType.Info);

        DailyPostFillBlankManager manager = (DailyPostFillBlankManager)target;

        using (new EditorGUI.DisabledScope(manager.dailyPostJsonFile == null))
        {
            if (GUILayout.Button("Open Daily Post JSON Editor"))
            {
                DailyPostJsonEditorWindow.OpenWithFile(manager.dailyPostJsonFile);
            }
        }

        if (manager.dailyPostJsonFile == null)
        {
            EditorGUILayout.HelpBox("Assign Daily Post Json File first, then the editor button will become available.", MessageType.Warning);
        }
    }
}
#endif
