using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class VerifyMainMenuNavigation
{
    [MenuItem("Tools/Food for Thought/Verify Main Menu Navigation")]
    public static void Verify()
    {
        string[] keys = { MainMenuNavigation.CompletedKey, EndingTransitionManager.PendingEndingKey,
            EndingTransitionManager.FinalCredibilityKey, "SavedCurrentDay", "User_Credibility" };
        var existed = keys.Select(PlayerPrefs.HasKey).ToArray();
        var values = keys.Select(k => PlayerPrefs.GetInt(k)).ToArray();
        try
        {
            foreach (var key in keys) PlayerPrefs.DeleteKey(key);
            Check(!MainMenuNavigation.IsRunCompleted, "Fresh game");
            PlayerPrefs.SetInt(MainMenuNavigation.CompletedKey, 1);
            Check(MainMenuNavigation.IsRunCompleted, "Completed run");
            PlayerPrefs.DeleteKey(MainMenuNavigation.CompletedKey);
            PlayerPrefs.SetInt(EndingTransitionManager.PendingEndingKey, 1);
            Check(MainMenuNavigation.IsRunCompleted, "Pending ending");
            PlayerPrefs.DeleteKey(EndingTransitionManager.PendingEndingKey);
            PlayerPrefs.SetInt(EndingTransitionManager.FinalCredibilityKey, 50);
            PlayerPrefs.SetInt("SavedCurrentDay", 31);
            Check(MainMenuNavigation.IsRunCompleted, "Legacy final-day save");
            PlayerPrefs.SetInt("SavedCurrentDay", 4);
            PlayerPrefs.SetInt("User_Credibility", 0);
            Check(MainMenuNavigation.IsRunCompleted, "Legacy credibility ending");
            PlayerPrefs.DeleteKey(EndingTransitionManager.FinalCredibilityKey);
            PlayerPrefs.SetInt("User_Credibility", 100);
            Check(!MainMenuNavigation.IsRunCompleted, "Active run");
        }
        finally
        {
            for (int i = 0; i < keys.Length; i++)
                if (existed[i]) PlayerPrefs.SetInt(keys[i], values[i]); else PlayerPrefs.DeleteKey(keys[i]);
            PlayerPrefs.Save();
        }
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/1 Main Page.unity");
        try
        {
            var roots = scene.GetRootGameObjects();
            var navigation = roots.SelectMany(r => r.GetComponentsInChildren<MainMenuNavigation>(true)).Single();
            Check(navigation.continueButton != null, "Continue reference");
            foreach (var link in roots.SelectMany(r => r.GetComponentsInChildren<DaySceneLink>(true)))
                Check(link.advanceButtons.Count == 0 && link.resetButtons.Count == 0 && link.objectsToActivate.Count == 0,
                    "Day listeners no longer override menu navigation");
            foreach (var link in roots.SelectMany(r => r.GetComponentsInChildren<StatSceneLink>(true)))
                Check(link.resetButton == null, "No competing reset listener");
            foreach (var method in new[] { "NewGame", "ContinueGame" })
                Check(roots.SelectMany(r => r.GetComponentsInChildren<Button>(true)).Any(b =>
                    Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Any(i =>
                        b.onClick.GetPersistentTarget(i) == navigation && b.onClick.GetPersistentMethodName(i) == method)),
                    method + " callback");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        Debug.Log("MAIN_MENU_NAVIGATION_VERIFICATION_PASSED");
    }
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
