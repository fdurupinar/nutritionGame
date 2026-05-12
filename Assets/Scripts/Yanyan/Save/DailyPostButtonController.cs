using UnityEngine;
using UnityEngine.UI;

public class DailyPostButtonController : MonoBehaviour
{
    private const string KEY = "HasPostedToday";

    public Button dailyPostButton;

    [Header("Auto Refresh")]
    public bool checkEveryFrame = true;

    private int lastPostedValue = -999;
    private int lastDayValue = -999;

    private void OnEnable()
    {
        RefreshButtonState();
    }

    private void Start()
    {
        RefreshButtonState();
    }

    private void Update()
    {
        if (!checkEveryFrame)
            return;

        int currentPostedValue = PlayerPrefs.GetInt(KEY, 0);

        int currentDayValue = -1;
        if (DayManager.Instance != null)
        {
            currentDayValue = DayManager.Instance.currentDay;
        }

        if (currentPostedValue != lastPostedValue || currentDayValue != lastDayValue)
        {
            RefreshButtonState();
        }
    }

    public void RefreshButtonState()
    {
        int hasPostedToday = PlayerPrefs.GetInt(KEY, 0);

        int currentDayValue = -1;
        if (DayManager.Instance != null)
        {
            currentDayValue = DayManager.Instance.currentDay;
        }

        lastPostedValue = hasPostedToday;
        lastDayValue = currentDayValue;

        Debug.Log("Daily Post Button Check -> Day: " + currentDayValue + " HasPostedToday: " + hasPostedToday);

        if (dailyPostButton != null)
        {
            dailyPostButton.interactable = hasPostedToday == 0;
        }
    }
}