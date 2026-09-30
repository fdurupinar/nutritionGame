using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuNavigation : MonoBehaviour
{
    public const string CompletedKey = "GameRun_Completed";
    public GameObject continueButton;

    public static bool IsRunCompleted
    {
        get
        {
            if (PlayerPrefs.GetInt(CompletedKey, 0) == 1 ||
                PlayerPrefs.GetInt(EndingTransitionManager.PendingEndingKey, 0) == 1)
                return true;

            // Recognize completed saves made before the explicit completion flag existed.
            int lastDay = DayManager.Instance != null ? DayManager.Instance.maxDay : 31;
            return PlayerPrefs.HasKey(EndingTransitionManager.FinalCredibilityKey) &&
                (PlayerPrefs.GetInt("SavedCurrentDay", 1) >= lastDay ||
                 PlayerPrefs.GetInt("User_Credibility", 100) <= 0);
        }
    }

    private void OnEnable()
    {
        if (continueButton != null)
            continueButton.SetActive(PlayerPrefs.HasKey("SavedCurrentDay") && !IsRunCompleted);
    }

    public void ContinueGame()
    {
        if (IsRunCompleted) return;
        SceneManager.LoadScene("2 Home Page");
    }

    public void NewGame()
    {
        ResetRun();
        SceneManager.LoadScene("2 Home Page");
    }

    public static void ResetRun()
    {
        if (GlobalStatManager.Instance == null)
            new GameObject("GlobalStatManager").AddComponent<GlobalStatManager>();
        if (DayManager.Instance == null)
            new GameObject("DayManager").AddComponent<DayManager>();

        GlobalStatManager.Instance.ResetStats();
        DayManager.Instance.ResetDays();
        MisinformationMetricEngine.ResetSavedMetricHistory();
        PlayerPrefs.DeleteKey(CompletedKey);
        PlayerPrefs.DeleteKey(EndingTransitionManager.PendingEndingKey);
        PlayerPrefs.DeleteKey(EndingTransitionManager.FinalMoneyKey);
        PlayerPrefs.DeleteKey(EndingTransitionManager.FinalFollowersKey);
        PlayerPrefs.DeleteKey(EndingTransitionManager.FinalCredibilityKey);
        PlayerPrefs.DeleteKey("DayOneStartOnceShown_2 Home Page");
        PlayerPrefs.DeleteKey("DayOneStartOnceShown_3 Game");
        // Ending unlocks, sound preferences, and the chosen avatar belong to the player.
        PlayerPrefs.Save();
    }
}
