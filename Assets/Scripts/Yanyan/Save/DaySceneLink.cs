using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DaySceneLink : MonoBehaviour
{
    [Header("UI Display")]
    public TextMeshProUGUI sceneDayText;

    [Header("Advance & Reset Buttons")]
    public List<Button> advanceButtons = new List<Button>();
    public List<Button> resetButtons = new List<Button>();

    [Header("Day 1 Filtering")]
    [Tooltip("These objects will be ACTIVE only if Day is NOT 1")]
    public List<GameObject> objectsToActivate = new List<GameObject>();

    [Header("Day 1 Start Once")]
    [Tooltip("These objects will be activated once every time the game enters Day 1.")]
    public List<GameObject> dayOneStartOnceObjects = new List<GameObject>();

    [Header("Testing / Reset Options")]
    [Tooltip("If true, pressing Reset Day will allow the Day 1 once objects to show again.")]
    public bool resetDayOneStartOnceWhenResetDay = true;

    // This is runtime only.
    // It does NOT use PlayerPrefs anymore.
    // That means every new Day 1 cycle can show the objects again.
    private bool _dayOneStartOnceShownThisDayOne = false;

    private int _lastCheckedDay = -1;

    private void Start()
    {
        if (DayManager.Instance == null)
        {
            Debug.LogWarning("DaySceneLink: No DayManager found in the scene.");
            return;
        }

        DayManager.Instance.UpdateSceneReferences(sceneDayText);

        SetupAdvanceButtons();
        SetupResetButtons();

        RefreshAllDayObjects();
    }

    public void SetupAdvanceButtons()
    {
        for (int i = 0; i < advanceButtons.Count; i++)
        {
            Button button = advanceButtons[i];

            if (button == null)
                continue;

            button.onClick.RemoveListener(AdvanceOneDay);
            button.onClick.AddListener(AdvanceOneDay);
        }
    }

    public void SetupResetButtons()
    {
        for (int i = 0; i < resetButtons.Count; i++)
        {
            Button button = resetButtons[i];

            if (button == null)
                continue;

            button.onClick.RemoveListener(ResetDay);
            button.onClick.AddListener(ResetDay);
        }
    }

    public void AdvanceOneDay()
    {
        if (DayManager.Instance == null)
            return;

        DayManager.Instance.AdvanceDay();
        RefreshAllDayObjects();
    }

    public void ResetDay()
    {
        if (DayManager.Instance == null)
            return;

        DayManager.Instance.ResetDays();

        if (resetDayOneStartOnceWhenResetDay)
        {
            ResetDayOneStartOnceFlag();
        }

        RefreshAllDayObjects();
    }

    public void RefreshAllDayObjects()
    {
        RefreshDayBasedObjects();
        CheckDayOneStartOnceObjects();
    }

    public void RefreshDayBasedObjects()
    {
        if (DayManager.Instance == null)
            return;

        bool isNotDayOne = DayManager.Instance.currentDay != 1;

        for (int i = 0; i < objectsToActivate.Count; i++)
        {
            GameObject obj = objectsToActivate[i];

            if (obj != null)
            {
                obj.SetActive(isNotDayOne);
            }
        }
    }

    public void CheckDayOneStartOnceObjects()
    {
        if (DayManager.Instance == null)
            return;

        int currentDay = DayManager.Instance.currentDay;

        // If the day changed, update tracking.
        if (_lastCheckedDay != currentDay)
        {
            // When leaving Day 1, reset the once flag.
            // This allows the objects to show again next time the game returns to Day 1.
            if (currentDay != 1)
            {
                _dayOneStartOnceShownThisDayOne = false;
            }

            _lastCheckedDay = currentDay;
        }

        bool isDayOne = currentDay == 1;
        bool shouldShow = isDayOne && !_dayOneStartOnceShownThisDayOne;

        for (int i = 0; i < dayOneStartOnceObjects.Count; i++)
        {
            GameObject obj = dayOneStartOnceObjects[i];

            if (obj != null)
            {
                obj.SetActive(shouldShow);
            }
        }

        if (shouldShow)
        {
            _dayOneStartOnceShownThisDayOne = true;

            Debug.Log("DaySceneLink: Day 1 start once objects activated.");
        }
        else
        {
            Debug.Log("DaySceneLink: Day 1 start once objects not activated. Day = "
                      + currentDay
                      + ", alreadyShownThisDayOne = "
                      + _dayOneStartOnceShownThisDayOne);
        }
    }

    public void ResetDayOneStartOnceFlag()
    {
        _dayOneStartOnceShownThisDayOne = false;
        _lastCheckedDay = -1;

        Debug.Log("DaySceneLink: Day 1 start once runtime flag reset.");
    }
}