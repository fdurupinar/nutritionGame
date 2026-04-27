using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StatSceneLink : MonoBehaviour
{
    public TextMeshProUGUI cashText;
    public TextMeshProUGUI followerText;
    public TextMeshProUGUI credibilityText;
    public Button resetButton;

    void Start()
    {
        RefreshUI();

        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(() => {
                if (GlobalStatManager.Instance != null)
                {
                    GlobalStatManager.Instance.ResetStats();
                    RefreshUI();
                    Debug.Log("Reset triggered from StatSceneLink");
                }

                if (DayManager.Instance != null)
                {
                    DayManager.Instance.ResetDays();
                }
            });
        }
    }

    public void RefreshUI()
    {
        if (GlobalStatManager.Instance == null) return;

        // Null-safe checks for text components
        if (cashText != null)
            cashText.text = GlobalStatManager.Instance.currentCash.ToString();

        if (followerText != null)
            followerText.text = GlobalStatManager.Instance.currentFollowers.ToString();

        if (credibilityText != null)
            credibilityText.text = GlobalStatManager.Instance.currentCredibility.ToString();
    }
}