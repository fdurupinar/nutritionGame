using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class VerifyJobAvailability
{
    [MenuItem("Tools/Food for Thought/Verify Job Availability")]
    public static void Run()
    {
        if (EditorApplication.isPlaying || DayManager.Instance != null)
            throw new Exception("Stop Play mode before verifying jobs.");
        string[] keys = { "JobSystem_HasActiveJob", "JobSystem_Completed_0", "JobSystem_Completed_1", "JobSystem_Completed_2", "JobSystem_ActiveJobIndex", "JobSystem_ActiveOfferSlotIndex", "JobSystem_Run", "JobSystem_RemainingDays", "JobSystem_LastCheckedDay", "JobSystem_ReadyToClaim", "SavedCurrentDay", "HasPostedToday" };
        var existed = Array.ConvertAll(keys, PlayerPrefs.HasKey);
        var values = Array.ConvertAll(keys, k => PlayerPrefs.GetInt(k));
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Job verification");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        var singleton = typeof(DayManager).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        try
        {
            foreach (var key in keys) PlayerPrefs.DeleteKey(key);
            var day = root.AddComponent<DayManager>();
            singleton.SetValue(null, day);
            var manager = root.AddComponent<JobPostManager>();
            for (int i = 0; i < 3; i++)
                manager.jobs.Add(new JobPostManager.JobPostData { availableDay = i + 1, jobText = "Job " + i });
            for (int i = 0; i < 2; i++)
            {
                var slot = new GameObject("Offer " + i, typeof(RectTransform), typeof(Button));
                slot.transform.SetParent(root.transform);
                manager.jobOfferSlots.Add(new JobPostManager.JobOfferSlot { rootObject = slot, acceptButton = slot.GetComponent<Button>() });
            }
            var refresh = typeof(JobPostManager).GetMethod("RefreshDailyJobOffers", BindingFlags.Instance | BindingFlags.NonPublic);
            day.currentDay = 1; refresh.Invoke(manager, null);
            Check(manager.jobOfferSlots[0].rootObject.activeSelf && !manager.jobOfferSlots[1].rootObject.activeSelf, "Future jobs hidden");
            day.currentDay = 13; refresh.Invoke(manager, null);
            Check(manager.jobOfferSlots[0].rootObject.activeSelf && manager.jobOfferSlots[1].rootObject.activeSelf, "Older offers remain available on day 13");
            PlayerPrefs.SetInt(keys[1], 1); PlayerPrefs.SetInt(keys[2], 1); refresh.Invoke(manager, null);
            Check(manager.jobOfferSlots[0].rootObject.activeSelf && !manager.jobOfferSlots[1].rootObject.activeSelf, "Completed jobs excluded; queued job appears");
            PlayerPrefs.SetInt(keys[0], 1); PlayerPrefs.SetInt(keys[4], 2); PlayerPrefs.SetInt(keys[5], 1); refresh.Invoke(manager, null);
            Check(!manager.jobOfferSlots[0].rootObject.activeSelf && manager.jobOfferSlots[1].rootObject.activeSelf && !manager.jobOfferSlots[1].acceptButton.interactable, "Only active job shown and cannot be accepted again");
            PlayerPrefs.SetInt("JobSystem_RemainingDays", 0);
            PlayerPrefs.SetInt("JobSystem_ReadyToClaim", 1);
            day.ResetDays(); // Exercise the actual New game reset path.
            refresh.Invoke(manager, null);
            Check(!PlayerPrefs.HasKey("JobSystem_HasActiveJob") && !PlayerPrefs.HasKey("JobSystem_ReadyToClaim"), "New game clears old active job and reward");
            Check(manager.jobOfferSlots[0].rootObject.activeSelf && manager.jobOfferSlots[1].rootObject.activeSelf == false && manager.jobOfferSlots[0].acceptButton.interactable,
                "New game restores completed Day 1 job as an available offer");
            Check(PlayerPrefs.GetInt("JobSystem_Completed_0") == 1, "Old completion data remains isolated from new run");
            VerifySceneLayering();
            Debug.Log("JOB_AVAILABILITY_VERIFICATION_PASSED");
        }
        finally
        {
            singleton.SetValue(null, null);
            UnityEngine.Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(scene);
            for (int i = 0; i < keys.Length; i++)
                if (existed[i]) PlayerPrefs.SetInt(keys[i], values[i]); else PlayerPrefs.DeleteKey(keys[i]);
            PlayerPrefs.Save();
        }
    }
    static void VerifySceneLayering()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/2 Home Page.unity");
        try
        {
            JobPostManager manager = null;
            foreach (var root in scene.GetRootGameObjects())
                if (manager == null) manager = root.GetComponentInChildren<JobPostManager>(true);
            Check(manager != null, "Home scene has job manager");
            var method = typeof(JobPostManager).GetMethod("OpenPanelRoutine", BindingFlags.Instance | BindingFlags.NonPublic);
            var routine = (System.Collections.IEnumerator)method.Invoke(manager, null);
            routine.MoveNext();
            Check(manager.jobPanel.activeSelf, "Job panel opens");
            Check(manager.jobPanel.transform.GetSiblingIndex() == manager.jobPanel.transform.parent.childCount - 1,
                "Job panel renders above the main menu");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Job availability: " + message);
    }
}
