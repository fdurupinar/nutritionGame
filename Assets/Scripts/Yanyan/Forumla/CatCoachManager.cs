using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CatCoachManager : MonoBehaviour
{
    [Header("Mode")]
    [Tooltip("In Easy Mode, the cat coach warns the player before publishing clearly weak or risky posts.")]
    public bool easyMode = true;

    [Tooltip("In Normal Mode, the cat coach does not block publishing.")]
    public bool normalMode = false;

    [Header("Coach Panel")]
    [Tooltip("Cat coach warning panel.")]
    public GameObject coachPanel;

    [Tooltip("Optional title text.")]
    public TextMeshProUGUI coachTitleText;

    [Tooltip("Coach message body text.")]
    public TextMeshProUGUI coachBodyText;

    [Tooltip("Continue button. This ignores the warning and publishes this post.")]
    public Button continueAnywayButton;

    [Tooltip("Revise button. This closes the coach panel and does not publish.")]
    public Button reviseButton;

    [Header("Warning Conditions")]
    [Tooltip("Warn when quality is lower than this value. 0 = worst, 1 = best.")]
    [Range(0f, 1f)]
    public float warnWhenQualityBelow = 0.45f;

    [Tooltip("Warn when one or more selected words are wrong or nonsense.")]
    public bool warnOnWrongWordChoice = true;

    [Tooltip("Warn when the tactic does not fit the topic/subtopic.")]
    public bool warnOnWrongTactic = true;

    [Tooltip("Warn when a repetition penalty is active.")]
    public bool warnOnRepetitionPenalty = true;

    [Tooltip("Warn when a Fact Check event is expected to trigger.")]
    public bool warnOnFactCheck = true;

    [Header("Coach Text")]
    public string defaultTitle = "Cat Coach";

    [TextArea(3, 8)]
    public string defaultWarningPrefix = "This post may perform badly:";

    [TextArea(2, 5)]
    public string continueHint = "You can still publish it, but it may reduce your growth or credibility.";

    private TacticManager pendingTacticManager;

    private void Start()
    {
        if (coachPanel != null)
        {
            coachPanel.SetActive(false);
        }

        if (continueAnywayButton != null)
        {
            continueAnywayButton.onClick.RemoveAllListeners();
            continueAnywayButton.onClick.AddListener(ContinueAnyway);
        }

        if (reviseButton != null)
        {
            reviseButton.onClick.RemoveAllListeners();
            reviseButton.onClick.AddListener(CloseCoachPanel);
        }
    }

    public bool TryShowEasyModeWarning(PostMetricPreview preview, TacticManager tacticManager)
    {
        if (!easyMode || normalMode)
        {
            return false;
        }

        if (preview == null)
        {
            return false;
        }

        List<string> reasons = BuildWarningReasons(preview);

        if (reasons.Count == 0)
        {
            return false;
        }

        pendingTacticManager = tacticManager;
        ShowCoachPanel(reasons);
        return true;
    }

    private List<string> BuildWarningReasons(PostMetricPreview preview)
    {
        List<string> reasons = new List<string>();

        if (preview.combinedQuality01 < warnWhenQualityBelow)
        {
            reasons.Add("The caption and word choices look weak or confusing.");
        }

        if (warnOnWrongWordChoice && preview.wrongWordChoice)
        {
            reasons.Add("Some selected words do not fit the caption.");
        }

        if (warnOnWrongTactic && preview.wrongTactic)
        {
            reasons.Add("The selected tactic does not match this topic well.");
        }

        if (warnOnRepetitionPenalty && preview.mainPenaltyReason != MetricPenaltyReason.None)
        {
            reasons.Add("You are repeating the same pattern, so the post will have less effect.");
        }

        if (warnOnFactCheck && preview.factCheckTriggered)
        {
            reasons.Add("A Fact Check event may trigger because credibility is too low.");
        }

        return reasons;
    }

    private void ShowCoachPanel(List<string> reasons)
    {
        if (coachTitleText != null)
        {
            coachTitleText.text = defaultTitle;
        }

        if (coachBodyText != null)
        {
            string message = defaultWarningPrefix + "\n\n";

            for (int i = 0; i < reasons.Count; i++)
            {
                message += "• " + reasons[i] + "\n";
            }

            message += "\n" + continueHint;
            coachBodyText.text = message;
        }

        if (coachPanel != null)
        {
            coachPanel.SetActive(true);
        }
    }

    public void ContinueAnyway()
    {
        TacticManager managerToContinue = pendingTacticManager;
        pendingTacticManager = null;

        if (coachPanel != null)
        {
            coachPanel.SetActive(false);
        }

        if (managerToContinue != null)
        {
            managerToContinue.ContinuePublishAfterCatCoachWarning();
        }
    }

    public void CloseCoachPanel()
    {
        pendingTacticManager = null;

        if (coachPanel != null)
        {
            coachPanel.SetActive(false);
        }
    }
}
