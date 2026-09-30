using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Exploration is read-only. Only the existing publish action commits a choice.
public class TacticExplorer : MonoBehaviour
{
    public TacticManager manager;
    public TextMeshProUGUI captionText;
    public TextMeshProUGUI selectionText;
    public Button publishButton;
    private void OnEnable() => Refresh();

    public void Refresh()
    {
        var caption = manager?.dailyPostFillBlankManager?.currentCompletedSentence;
        if (captionText != null)
            captionText.text = "<b>YOUR POST</b>\n" + Escape(string.IsNullOrWhiteSpace(caption)
                ? "Your completed caption will appear here." : caption);
        var tactic = manager != null ? manager.SelectedTactic : null;
        if (selectionText != null)
            selectionText.text = tactic == null
                ? "Select the tactic that best matches your caption."
                : "Selected: <b>" + Escape(tactic.displayName) + "</b>\nRight-click a card for a hint.";
        if (publishButton != null)
            publishButton.interactable = tactic != null && manager.IsTacticUnlocked(tactic);
    }

    public void CheckMyChoice()
    {
        var tactic = manager != null ? manager.SelectedTactic : null;
        if (tactic == null || !manager.IsTacticUnlocked(tactic)) return;
        var fill = manager.dailyPostFillBlankManager;
        var quality = manager.metricEngine != null ? manager.metricEngine.PreviewTacticFit(tactic, fill) : null;
        var hint = manager.tacticHintPanel;
        if (hint == null) return;
        string title = quality == PostChoiceQuality.Correct ? "Strong match!" :
            quality == PostChoiceQuality.HalfCorrect ? "Partial match" :
            quality.HasValue ? "Weak match" : "Keep exploring";
        Sprite face = fill == null ? null : quality == PostChoiceQuality.Correct ? fill.captionCatSmile :
            quality == PostChoiceQuality.HalfCorrect ? fill.captionCatUnsure :
            quality.HasValue ? fill.captionCatSideEye : fill.captionCatNeutral;
        hint.ShowFeedback(title, BuildFeedback(tactic, fill, quality), face);
    }

    public static string BuildFeedback(TacticSO tactic, DailyPostFillBlankManager fill, PostChoiceQuality? quality)
    {
        string name = Escape(tactic.displayName);
        string result = "<b>" + name + "</b>\n";
        if (!quality.HasValue)
            result += "I can't judge this match yet. Compare the explanation below with your caption.";
        else if (quality == PostChoiceQuality.Correct)
            result += "Yes! You identified one of this caption's main persuasion tactics.";
        else if (quality == PostChoiceQuality.HalfCorrect)
            result += "This partly fits. Another tactic captures the main message better.";
        else
            result += "This tactic does not match the caption’s main message. Try another card.";

        var explanation = !string.IsNullOrWhiteSpace(tactic.hintText) ? tactic.hintText : tactic.debunkingText;
        if (!string.IsNullOrWhiteSpace(explanation))
            result += "\n\n" + Escape(Compact(explanation, 160));

        if (quality.HasValue && quality != PostChoiceQuality.Correct && fill != null && fill.currentSentenceData != null)
        {
            var ideals = fill.currentSentenceData.idealTacticTypes;
            var stronger = System.Array.Find(Resources.LoadAll<TacticSO>("Content/Tactics"), candidate =>
                ideals != null && ideals.Exists(type => string.Equals(type?.Trim(),
                    (string.IsNullOrWhiteSpace(candidate.GetTacticId()) ? candidate.type : candidate.GetTacticId())?.Trim(), System.StringComparison.OrdinalIgnoreCase)));
            if (stronger != null)
                result += "\n\nTry comparing it with <b>" + Escape(stronger.displayName) + "</b>.";
        }
        return result + "\n\n<size=85%>A good fit does not prove the health claim.</size>";
    }

    public static string Compact(string value, int maxCharacters)
    {
        value = (value ?? "").Trim();
        if (value.Length <= maxCharacters) return value;
        int end = value.LastIndexOf(' ', maxCharacters - 1);
        if (end < maxCharacters / 2) end = maxCharacters - 1;
        return value.Substring(0, end).TrimEnd(',', ';', ':') + "…";
    }

    public static string Escape(string value) => (value ?? "").Replace("<", "&lt;").Replace(">", "&gt;");

    public static string Describe(TacticSO tactic)
    {
        if (tactic == null)
            return "<b>What makes a post persuasive?</b>\n\nTactics are ways a post tries to influence people. Tap a card to investigate one.\n\nCompare its explanation with your caption. Which words show that tactic? What evidence would you check before trusting the claim?\n\nYou can explore as many cards as you like. Nothing is posted until you tap Publish post.";
        var explanation = !string.IsNullOrWhiteSpace(tactic.hintText) ? tactic.hintText : tactic.debunkingText;
        if (string.IsNullOrWhiteSpace(explanation))
            explanation = "Look at how this tactic tries to influence the reader. Does it appeal to feelings, authority, or a claim about evidence?";
        var result = "<b>" + Escape(tactic.displayName) + "</b>\n\n<b>HOW IT WORKS</b>\n" + Escape(explanation);
        if (!string.IsNullOrWhiteSpace(tactic.debunkingText) && tactic.debunkingText != explanation)
            result += "\n\n<b>LOOK CLOSER</b>\n" + Escape(tactic.debunkingText);
        return result + "\n\n<b>CONNECT IT TO YOUR POST</b>\nWhich words in your caption use this tactic? What might a reader feel or assume?\n\n<b>TRY ANOTHER</b>\nClose this explanation and compare a different card. Your choice does not rewrite the caption; its fit affects the post's result. A persuasive post is not necessarily true.";
    }
}
