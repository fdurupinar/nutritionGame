using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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
    [Tooltip("These objects will be ACTIVE only once when the scene starts on Day 1")]
    public List<GameObject> dayOneStartOnceObjects = new List<GameObject>();

    [Header("Testing / Reset Options")]
    [Tooltip("If true, pressing Reset Day will allow the Day 1 once objects to show again.")]
    public bool resetDayOneStartOnceWhenResetDay = true;

    [Tooltip("Turn this on only for testing if the object already showed once and you want to test it again.")]
    public bool clearDayOneStartOnceFlagOnStart = false;

    private const string DAY_ONE_START_ONCE_KEY_PREFIX = "DayOneStartOnceShown_";

    private string DayOneStartOnceKey
    {
        get
        {
            return DAY_ONE_START_ONCE_KEY_PREFIX + SceneManager.GetActiveScene().name;
        }
    }

    private void Start()
    {
        if (DayManager.Instance == null)
        {
            Debug.LogWarning("DaySceneLink: No DayManager found in the scene.");
            return;
        }

        if (clearDayOneStartOnceFlagOnStart)
        {
            ResetDayOneStartOnceFlag();
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

        bool isDayOne = DayManager.Instance.currentDay == 1;
        bool alreadyShown = PlayerPrefs.GetInt(DayOneStartOnceKey, 0) == 1;

        bool shouldShow = isDayOne && !alreadyShown;

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
            PlayerPrefs.SetInt(DayOneStartOnceKey, 1);
            PlayerPrefs.Save();

            Debug.Log("DaySceneLink: Day 1 start once objects shown.");
        }
        else
        {
            Debug.Log("DaySceneLink: Day 1 start once object hidden. Day = "
                      + DayManager.Instance.currentDay
                      + ", alreadyShown = "
                      + alreadyShown);
        }
    }

    public void ResetDayOneStartOnceFlag()
    {
        PlayerPrefs.DeleteKey(DayOneStartOnceKey);
        PlayerPrefs.Save();

        Debug.Log("DaySceneLink: Day 1 start once flag reset.");
    }
}