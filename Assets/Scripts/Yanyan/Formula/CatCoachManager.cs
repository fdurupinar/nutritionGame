using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CatCoachManager : MonoBehaviour
{
    [Header("Mode")]
    [Tooltip("In Easy Mode, the cat coach checks the completed caption before tactic selection.")]
    public bool easyMode = true;

    [Tooltip("In Normal Mode, the cat coach does not interrupt the player.")]
    public bool normalMode = false;

    [Header("Coach Panel")]
    [Tooltip("Cat coach warning panel. Keep this panel outside the Fill Blank Panel hierarchy if that panel is hidden during warnings.")]
    public GameObject coachPanel;

    [Tooltip("Optional coach title text.")]
    public TextMeshProUGUI coachTitleText;

    [Tooltip("Coach warning body text.")]
    public TextMeshProUGUI coachBodyText;

    [Tooltip("Keeps the current caption choices and continues to tactic selection.")]
    public Button continueAnywayButton;

    [Tooltip("Returns to the same caption and clears its selected blank answers.")]
    public Button reviseButton;

    [Header("Caption Warning Conditions")]
    [Tooltip("Warn when the caption-and-word quality is lower than this value. 0 = worst, 1 = best.")]
    [Range(0f, 1f)]
    public float warnWhenQualityBelow = 0.45f;

    [Tooltip("Warn when one or more selected words are scored Nonsense.")]
    public bool warnOnNonsenseWordChoice = true;

    [Tooltip("Optional: warn even when a word is only Half Correct.")]
    public bool warnOnHalfCorrectWordChoice = false;

    [Header("Optional Final-Publish Warnings")]
    [Tooltip("Used only if TacticManager enables its optional final publish coach check.")]
    public bool warnOnWrongTactic = true;

    [Tooltip("Used only if TacticManager enables its optional final publish coach check.")]
    public bool warnOnRepetitionPenalty = true;

    [Tooltip("Used only if TacticManager enables its optional final publish coach check.")]
    public bool warnOnFactCheck = true;

    [Header("Coach Text")]
    public string defaultTitle = "Cat Coach";

    [TextArea(3, 8)]
    public string defaultWarningPrefix = "Let’s check how the words fit:";

    [TextArea(2, 5)]
    public string continueHint = "You can revise it, or continue anyway and choose a tactic card.";

    private DailyPostFillBlankManager pendingFillBlankManager;
    private TacticManager pendingTacticManager;

    private bool EasyModeIsActive
    {
        get { return easyMode && !normalMode; }
    }

    private void Start()
    {
        if (coachPanel != null)
        {
            coachPanel.SetActive(false);
        }

        if (continueAnywayButton != null)
        {
            continueAnywayButton.onClick.RemoveListener(ContinueAnyway);
            continueAnywayButton.onClick.AddListener(ContinueAnyway);
        }

        if (reviseButton != null)
        {
            reviseButton.onClick.RemoveListener(ReviseCaption);
            reviseButton.onClick.AddListener(ReviseCaption);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 中文备注：避免 Easy 和 Normal 同时生效。Normal 开启时，Easy 的提示会被跳过。
        if (normalMode)
        {
            easyMode = false;
        }
    }
#endif

    /// <summary>
    /// Easy Mode check used immediately after the player completes the fill-in-the-blank caption.
    /// No tactic has been selected yet, so this checks only caption/word quality.
    /// </summary>
    public bool TryShowCaptionChoiceWarning(PostMetricPreview preview, DailyPostFillBlankManager fillBlankManager)
    {
        if (!EasyModeIsActive || preview == null || fillBlankManager == null)
        {
            return false;
        }

        List<string> reasons = BuildCaptionWarningReasons(preview);

        if (reasons.Count == 0)
        {
            return false;
        }

        pendingFillBlankManager = fillBlankManager;
        pendingTacticManager = null;

        // 中文备注：这里只隐藏填词 Panel，不会清除当前句子或玩家已经选择的词。
        // 如果 Coach Panel 错误地放在 Fill Blank Panel 下面，则不隐藏父物体，避免 Coach 自己也消失。
        bool coachIsChildOfFillPanel =
            coachPanel != null &&
            fillBlankManager.fillBlankPanel != null &&
            coachPanel.transform.IsChildOf(fillBlankManager.fillBlankPanel.transform);

        if (coachIsChildOfFillPanel)
        {
            Debug.LogWarning("CatCoachManager: Coach Panel is a child of Fill Blank Panel. Move it outside that hierarchy for the cleanest setup.");
        }
        else
        {
            fillBlankManager.HideFillBlankPanelForCoach();
        }

        ShowCoachPanel(reasons);
        return true;
    }

    /// <summary>
    /// Legacy/optional final-publish warning. The requested flow keeps this disabled in TacticManager by default.
    /// </summary>
    public bool TryShowEasyModeWarning(PostMetricPreview preview, TacticManager tacticManager)
    {
        if (!EasyModeIsActive || preview == null || tacticManager == null)
        {
            return false;
        }

        List<string> reasons = BuildFinalPublishWarningReasons(preview);

        if (reasons.Count == 0)
        {
            return false;
        }

        pendingFillBlankManager = null;
        pendingTacticManager = tacticManager;
        ShowCoachPanel(reasons);
        return true;
    }

    private List<string> BuildCaptionWarningReasons(PostMetricPreview preview)
    {
        List<string> reasons = new List<string>();

        if (preview.wordQualities == null) return reasons;
        for (int i = 0; i < preview.wordQualities.Count; i++)
        {
            var quality = preview.wordQualities[i];
            bool warn = (quality == PostChoiceQuality.Nonsense && warnOnNonsenseWordChoice) ||
                (quality == PostChoiceQuality.HalfCorrect && warnOnHalfCorrectWordChoice);
            if (!warn) continue;
            reasons.Add(preview.wordFitFeedback != null && i < preview.wordFitFeedback.Count
                ? preview.wordFitFeedback[i]
                : "Blank " + (i + 1) + ": Does not fit this caption. Check the surrounding words.");
        }

        return reasons;
    }

    private List<string> BuildFinalPublishWarningReasons(PostMetricPreview preview)
    {
        List<string> reasons = BuildCaptionWarningReasons(preview);

        if (warnOnWrongTactic && preview.wrongTactic)
        {
            reasons.Add("The selected tactic is a bad match for this caption.");
        }

        if (warnOnRepetitionPenalty && preview.mainPenaltyReason != MetricPenaltyReason.None)
        {
            reasons.Add("You are repeating the same pattern, so the post will have less effect.");
        }

        if (warnOnFactCheck && preview.factCheckTriggered)
        {
            reasons.Add("This post is expected to trigger a fact check because it reduces credibility.");
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
            // Keep the interruption to the two most useful points, above a fixed button row.
            string message = "";
            for (int i = 0; i < Mathf.Min(2, reasons.Count); i++)
            {
                if (i > 0) message += "\n\n";
                message += TacticExplorer.Compact(reasons[i], 150);
            }
            message += "\n\n<size=85%>A good fit does not prove the health claim.</size>";
            coachBodyText.text = message;
            coachBodyText.enableAutoSizing = true;
            coachBodyText.fontSizeMin = 24;
            coachBodyText.fontSizeMax = 30;
            coachBodyText.textWrappingMode = TextWrappingModes.Normal;
            coachBodyText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (coachPanel != null)
        {
            coachPanel.SetActive(true);
        }
    }

    public void ContinueAnyway()
    {
        DailyPostFillBlankManager fillBlankToContinue = pendingFillBlankManager;
        TacticManager tacticManagerToContinue = pendingTacticManager;

        CloseCoachPanelInternal();

        if (fillBlankToContinue != null)
        {
            fillBlankToContinue.ContinueToTacticSelectionAfterCoach();
            return;
        }

        if (tacticManagerToContinue != null)
        {
            tacticManagerToContinue.ContinuePublishAfterCatCoachWarning();
        }
    }

    public void ReviseCaption()
    {
        DailyPostFillBlankManager fillBlankToRevise = pendingFillBlankManager;

        CloseCoachPanelInternal();

        if (fillBlankToRevise != null)
        {
            fillBlankToRevise.ReviseCurrentCaptionFromCoach();
        }
    }

    public void CloseCoachPanel()
    {
        // 中文备注：如果这是填词阶段的警告，直接关闭也按“Revise”处理，避免填词 Panel 被隐藏后无法返回。
        if (pendingFillBlankManager != null)
        {
            ReviseCaption();
            return;
        }

        CloseCoachPanelInternal();
    }

    private void CloseCoachPanelInternal()
    {
        pendingFillBlankManager = null;
        pendingTacticManager = null;

        if (coachPanel != null)
        {
            coachPanel.SetActive(false);
        }
    }
}
