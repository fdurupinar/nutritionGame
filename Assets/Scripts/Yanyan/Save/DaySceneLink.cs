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

        RefreshDayBasedObjects();
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
        RefreshDayBasedObjects();
    }

    public void ResetDay()
    {
        if (DayManager.Instance == null)
            return;

        DayManager.Instance.ResetDays();
        RefreshDayBasedObjects();
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
}