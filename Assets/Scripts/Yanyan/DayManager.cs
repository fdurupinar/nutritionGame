using UnityEngine;
using TMPro;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    [Header("Calendar Settings")]
    public int currentDay = 1;
    public int maxDay = 31;

    private TextMeshProUGUI currentText;
    private const string SAVED_DAY_KEY = "SavedCurrentDay"; // The key for saving

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadDay(); // Load the saved day as soon as the manager wakes up
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdateSceneReferences(TextMeshProUGUI newText)
    {
        currentText = newText;
        RefreshUI();
    }

    public void AdvanceDay()
    {
        if (currentDay < maxDay)
        {
            currentDay++;
            SaveDay(); // Save whenever the day changes
            RefreshUI();
        }
    }

    public void ResetDays()
    {
        currentDay = 1;
        SaveDay(); // Save the reset
        RefreshUI();
    }

    // --- Save and Load Logic ---

    private void SaveDay()
    {
        PlayerPrefs.SetInt(SAVED_DAY_KEY, currentDay);
        PlayerPrefs.Save(); // Force write to disk
    }

    private void LoadDay()
    {
        // If the key exists, use it. Otherwise, default to 1.
        currentDay = PlayerPrefs.GetInt(SAVED_DAY_KEY, 1);
    }

    private void RefreshUI()
    {
        if (currentText != null)
        {
            currentText.text = "Day " + currentDay;
        }
    }
}