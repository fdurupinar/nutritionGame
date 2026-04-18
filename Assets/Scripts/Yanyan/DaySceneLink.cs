using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DaySceneLink : MonoBehaviour
{
    [Header("UI Display")]
    public TextMeshProUGUI sceneDayText;

    [Header("Advance & Reset")]
    public List<Button> advanceButtons = new List<Button>();
    public List<Button> resetButtons = new List<Button>();

    [Header("Day 1 Filtering")]
    [Tooltip("These objects will be ACTIVE only if Day is NOT 1")]
    public List<GameObject> objectsToActivate = new List<GameObject>();

    void Start()
    {
        // 1. Get current day from Manager
        int currentDay = 1;
        if (DayManager.Instance != null)
        {
            currentDay = DayManager.Instance.currentDay;
            DayManager.Instance.UpdateSceneReferences(sceneDayText);
        }

        // 2. Handle objects that should be hidden on Day 1
        bool isNotDayOne = (currentDay != 1);
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(isNotDayOne);
            }
        }

        // 3. Link Advance Buttons
        foreach (Button btn in advanceButtons)
        {
            if (btn != null)
                btn.onClick.AddListener(() => DayManager.Instance.AdvanceDay());
        }

        // 4. Link Reset Buttons
        foreach (Button btn in resetButtons)
        {
            if (btn != null)
                btn.onClick.AddListener(() => DayManager.Instance.ResetDays());
        }
    }
}