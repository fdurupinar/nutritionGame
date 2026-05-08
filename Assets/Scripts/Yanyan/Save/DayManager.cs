using UnityEngine;
using TMPro;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    [Header("Calendar Settings")]
    public int currentDay = 1;
    public int maxDay = 31;

    private TextMeshProUGUI currentText;
    private const string SAVED_DAY_KEY = "SavedCurrentDay";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadDay();
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
            PlayerPrefs.SetInt("HasPostedToday", 0); // <-- Resets the post flag
            SaveDay();
            RefreshUI();
        }
    }

    public void ResetDays()
    {
        currentDay = 1;
        PlayerPrefs.SetInt("HasPostedToday", 0); // <-- Resets the post flag
        SaveDay();
        RefreshUI();
    }

    private void SaveDay()
    {
        PlayerPrefs.SetInt(SAVED_DAY_KEY, currentDay);
        PlayerPrefs.Save();
    }

    private void LoadDay()
    {
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