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
                ? "Tap a card, then explore how it works."
                : "Exploring: <b>" + Escape(tactic.displayName) + "</b>\nTry another card to compare.";
        if (publishButton != null)
            publishButton.interactable = tactic != null && manager.IsTacticUnlocked(tactic);
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
