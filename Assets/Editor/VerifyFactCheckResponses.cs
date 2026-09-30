using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Small regression check runnable with -executeMethod VerifyFactCheckResponses.Run.</summary>
public static class VerifyFactCheckResponses
{
    [MenuItem("Tools/Food for Thought/Verify Fact Check Responses")]
    public static void Run()
    {
        string[] keys = { FactCheckResponseFeedback.ResponseSaveKey, "User_Cash", "User_Followers", "User_Credibility", "MisinformationMetrics_LastFactCheckDay", FactCheckEventStore.SaveKey };
        bool[] existed = Array.ConvertAll(keys, PlayerPrefs.HasKey);
        string savedEvent = PlayerPrefs.GetString(FactCheckEventStore.SaveKey, "");
        string savedResponse = PlayerPrefs.GetString(keys[0], "");
        int[] values = Array.ConvertAll(keys, k => PlayerPrefs.GetInt(k, 0));
        GameObject root = null;
        var testScene = EditorSceneManager.NewPreviewScene();
        try
        {
            if (GlobalStatManager.Instance != null) throw new Exception("Run in a fresh editor batch session.");
            root = new GameObject("Response verification");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, testScene);
            var bank = root.AddComponent<GlobalStatManager>();
            typeof(GlobalStatManager).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, bank);
            var response = root.AddComponent<FactCheckResponseFeedback>();
            response.feedbackPanel = Child(root, "Feedback");
            response.backButton = Child(root, "Back");
            response.heading = Child(root, "Heading").AddComponent<TextMeshProUGUI>();
            response.explanation = Child(root, "Explanation").AddComponent<TextMeshProUGUI>();
            response.continueLabel = Child(root, "Continue").AddComponent<TextMeshProUGUI>();
            response.tutorMessage = Child(root, "Tutor").AddComponent<TextMeshProUGUI>();
            var entry = Child(root, "Fact check entry").AddComponent<FactCheckEntryState>();
            var entryButton = entry.GetComponent<Button>();
            PlayerPrefs.DeleteKey(FactCheckEventStore.SaveKey);
            entry.Refresh();
            Check(!entryButton.interactable, "No event means no clickable fact check");
            FactCheckEventStore.Record(new PostMetricPreview { subTopicId = "raw_milk", captionTemplateKey = "milk1", selectedSentence = "My milk caption" }, 1);
            int returns = 0;
            response.returnToMenu.AddListener(() => returns++);
            Action[] choose = { response.Apologize, response.Hire, response.Defend, response.Ignore };
            int[] expectedTrust = { 58, 54, 42, 46 };
            int[] expectedFollowers = { 990, 995, 970, 980 };
            for (int i = 0; i < choose.Length; i++)
            {
                PlayerPrefs.DeleteKey(keys[0]);
                bank.SaveToDisk(1000, 1000, 50);
                entry.Refresh();
                Check(entryButton.interactable, "Unresolved event is clickable");
                choose[i]();
                Check(bank.currentCredibility == 50, "Preview must not change stats");
                response.Continue();
                Check(bank.currentCredibility == expectedTrust[i], "Credibility outcome " + i);
                Check(bank.currentFollowers == expectedFollowers[i], "Follower outcome " + i);
                Check(bank.currentCash == (i == 1 ? 900 : 1000), "Hiring cost only");
                Check(response.heading.text == (i == 3 ? "Response recorded" : "Response published"), "Publish result visible");
                Check(!response.backButton.activeSelf, "Cannot switch strategy after commit");
                response.Continue();
                Check(!entryButton.interactable, "Committed event is disabled immediately");
                response.feedbackPanel.SetActive(false);
                response.Apologize(); // Stale calls cannot reopen the old outcome.
                Check(!response.feedbackPanel.activeSelf, "Resolved result stays closed");
                var reloadedEntry = Child(root, "Reloaded entry").AddComponent<FactCheckEntryState>();
                reloadedEntry.Refresh();
                Check(!reloadedEntry.GetComponent<Button>().interactable, "Saved response disables a new entry");
                UnityEngine.Object.DestroyImmediate(reloadedEntry.gameObject);
                Check(bank.currentCredibility == expectedTrust[i], "No duplicate reward");
                Check(PlayerPrefs.GetInt("User_Credibility") == expectedTrust[i], "Saved stats");
            }
            Check(returns == 8, "Result returns home");
            PlayerPrefs.DeleteKey(keys[0]);
            bank.SaveToDisk(99, 1000, 50);
            response.Hire(); response.Continue();
            Check(bank.currentCash == 99 && !PlayerPrefs.HasKey(keys[0]), "Unaffordable hire blocked");
            response.Back();
            Check(!response.feedbackPanel.activeSelf, "Cancel closes preview");
            Check(response.CalculateResult(0, 0, 0, 99).credibilityAfter == 100, "Trust ceiling");
            Check(response.CalculateResult(2, 0, 1, 2).credibilityAfter == 0, "Trust floor");
            Check(response.CalculateResult(2, 0, 1, 2).followersAfter == 0, "Follower floor");
            FactCheckEventStore.Record(new PostMetricPreview { subTopicId = "raw_milk" }, 12);
            response.Apologize(); response.Continue();
            FactCheckEventStore.Record(new PostMetricPreview { subTopicId = "fasting" }, 16);
            entry.Refresh();
            Check(entryButton.interactable, "New fact check unlocks entry");
            response.Defend();
            Check(response.continueLabel.text == "Publish defense", "New event allows new response");
            bank.ResetStats();
            Check(!PlayerPrefs.HasKey(keys[0]), "New game clears response");
            entry.Refresh(); // Preview scenes are intentionally excluded from Unity global object searches.
            Check(!entryButton.interactable, "Reset hides old fact-check access");
            VerifyTriggerRules(root);
            VerifyScene();
            Debug.Log("FACT_CHECK_RESPONSE_VERIFICATION_PASSED");
        }
        finally
        {
            if (root != null)
            {
                typeof(GlobalStatManager).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
                UnityEngine.Object.DestroyImmediate(root);
            }
            EditorSceneManager.ClosePreviewScene(testScene);
            for (int i = 0; i < keys.Length; i++)
            {
                if (!existed[i]) PlayerPrefs.DeleteKey(keys[i]);
                else if (keys[i] == FactCheckEventStore.SaveKey) PlayerPrefs.SetString(keys[i], savedEvent);
                else if (i == 0) PlayerPrefs.SetString(keys[i], savedResponse);
                else PlayerPrefs.SetInt(keys[i], values[i]);
            }
            PlayerPrefs.Save();
            FactCheckEntryState.RefreshAll();
        }
    }

    static void VerifyTriggerRules(GameObject root)
    {
        var engine = root.AddComponent<MisinformationMetricEngine>();
        var profile = ScriptableObject.CreateInstance<MisinformationMetricFormulaProfile>();
        try
        {
            engine.formulaProfile = profile;
            profile.enableFactCheckEvents = true;
            profile.factCheckCooldownDays = 4;
            var apply = typeof(MisinformationMetricEngine).GetMethod("ApplyFactCheckPreview", BindingFlags.Instance | BindingFlags.NonPublic);
            PlayerPrefs.SetInt("MisinformationMetrics_LastFactCheckDay", 1);
            var early = new PostMetricPreview { credibilityDelta = -2 };
            apply.Invoke(engine, new object[] { early, 100, 100, 90, 4 });
            Check(!early.factCheckTriggered, "Three days is too early");
            var next = new PostMetricPreview { credibilityDelta = -2 };
            apply.Invoke(engine, new object[] { next, 100, 100, 90, 5 });
            Check(next.factCheckTriggered, "Negative post triggers after four days even at high credibility");
            foreach (int delta in new[] { 0, 2 })
            {
                var stable = new PostMetricPreview { credibilityDelta = delta };
                apply.Invoke(engine, new object[] { stable, 100, 100, 10, 5 });
                Check(!stable.factCheckTriggered, "Unchanged or rising credibility never triggers");
            }
            profile.factCheckCooldownDays = 1;
            var minimum = new PostMetricPreview { credibilityDelta = -2 };
            apply.Invoke(engine, new object[] { minimum, 100, 100, 90, 3 });
            Check(!minimum.factCheckTriggered, "Four-day minimum cannot be bypassed by profile");
            Check(!FactCheckEventStore.ShouldTrigger(0, -2, 10, 1, 4), "Credibility already at zero cannot decrease");
            FactCheckEventStore.Record(new PostMetricPreview { subTopicId = "magic_gummies", captionTemplateKey = "gummy", selectedSentence = "My gummy caption" }, 5);
            var saved = FactCheckEventStore.Current;
            Check(saved.subTopicId == "magic_gummies" && saved.caption == "My gummy caption" && saved.title == "Vitamin gummy promises", "Caption-specific event persists");
            Check(FactCheckEventStore.Match("fasting").title != saved.title, "Topics receive different checks");
            Check(FactCheckEventStore.Match("unknown").title == "Unsupported food claim", "Unknown topics have a safe fallback");
        }
        finally { UnityEngine.Object.DestroyImmediate(profile); }
    }

    static void VerifyScene()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/2 Home Page.unity");
        try
        {
            FactCheckResponseFeedback response = null;
            foreach (var root in scene.GetRootGameObjects())
                if (response == null) response = root.GetComponentInChildren<FactCheckResponseFeedback>(true);
            Check(response != null && response.backButton != null, "Scene references");
            Check(response.supportCost == 100 && response.apologyCredibility == 8, "Default balance deserializes");
            string[] methods = { "Ignore", "Apologize", "Hire", "Defend" };
            for (int i = 0; i < methods.Length; i++)
            {
                var button = response.transform.Find((i + 1).ToString()).GetComponent<Button>();
                Check(button.onClick.GetPersistentEventCount() == 1 && button.onClick.GetPersistentMethodName(0) == methods[i], "Response button wiring");
            }
            Check(response.returnToMenu.GetPersistentEventCount() == 4, "Home navigation wiring");
            int entries = 0;
            foreach (var root in scene.GetRootGameObjects())
                entries += root.GetComponentsInChildren<FactCheckEntryState>(true).Length;
            Check(entries == 4, "All four fact-check entry points guarded");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
    static GameObject Child(GameObject root, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(root.transform);
        return go;
    }
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Fact check regression: " + message);
    }
}
