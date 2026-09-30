using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Preview, commit once, and show the audience outcome of a fact-check response.</summary>
public class FactCheckResponseFeedback : MonoBehaviour
{
    public const string ResponseSaveKey = "FactCheckResponse_LastResult";
    public GameObject feedbackPanel;
    public TMP_Text heading;
    public TMP_Text explanation;
    public TMP_Text continueLabel;
    public GameObject backButton;
    public Image tutorImage;
    public TMP_Text tutorMessage;
    public Sprite apologyCat;
    public Sprite hiringCat;
    public Sprite defenseCat;
    public UnityEvent returnToMenu = new UnityEvent();

    [Header("Response balance (game values, not real-world predictions)")]
    public int apologyCredibility = 8;
    public int supportCredibility = 4;
    public int defenseCredibility = -8;
    public int ignoreCredibility = -4;
    [Min(0)] public int supportCost = 100;
    [Range(0, 1)] public float apologyFollowerLoss = .01f;
    [Range(0, 1)] public float supportFollowerLoss = .005f;
    [Range(0, 1)] public float defenseFollowerLoss = .03f;
    [Range(0, 1)] public float ignoreFollowerLoss = .02f;

    [Serializable]
    public class ResponseResult
    {
        public string eventId;
        public int choice;
        public string post;
        public int cashBefore, followersBefore, credibilityBefore;
        public int cashAfter, followersAfter, credibilityAfter;
    }

    int selection = -1;
    bool showingResult;

    public void Apologize() => Show(0);
    public void Hire() => Show(1);
    public void Defend() => Show(2);
    public void Ignore() => Show(3);

    static string CurrentEventId() => FactCheckEventStore.Current?.id;

    public static bool HasResolvedCurrentEvent => SavedResult() != null;

    static ResponseResult SavedResult()
    {
        string json = PlayerPrefs.GetString(ResponseSaveKey, "");
        if (string.IsNullOrEmpty(json) || CurrentEventId() == null) return null;
        var result = JsonUtility.FromJson<ResponseResult>(json);
        return result != null && result.eventId == CurrentEventId() ? result : null;
    }

    string Draft(int choice)
    {
        var item = FactCheckEventStore.Current;
        string title = item?.title ?? "this food claim";
        string correction = item?.correction ?? "I should have checked the evidence before sharing.";
        switch (choice)
        {
            case 0: return "I'm sorry for my post about " + title.ToLowerInvariant() + ". " + correction;
            case 1: return "I've hired help to review and correct my post about " + title.ToLowerInvariant() + ". " + correction;
            case 2: return "I still stand by my post about " + title.ToLowerInvariant() + ", despite the fact check.";
            default: return "No response will be posted. The disputed claim stays uncorrected.";
        }
    }

    public ResponseResult CalculateResult(int choice, int cash, int followers, int credibility)
    {
        int[] trust = { apologyCredibility, supportCredibility, defenseCredibility, ignoreCredibility };
        float[] loss = { apologyFollowerLoss, supportFollowerLoss, defenseFollowerLoss, ignoreFollowerLoss };
        if (choice < 0 || choice >= trust.Length) throw new ArgumentOutOfRangeException(nameof(choice));
        int lost = followers > 0 && loss[choice] > 0 ? Mathf.Max(1, Mathf.RoundToInt(followers * loss[choice])) : 0;
        return new ResponseResult {
            eventId = CurrentEventId(), choice = choice, post = Draft(choice),
            cashBefore = cash, followersBefore = followers, credibilityBefore = credibility,
            cashAfter = Mathf.Max(0, cash - (choice == 1 ? Mathf.Max(0, supportCost) : 0)),
            followersAfter = Mathf.Max(0, followers - lost),
            credibilityAfter = Mathf.Clamp(credibility + trust[choice], 0, 100)
        };
    }

    static string Change(int before, int after) => (after - before).ToString("+0;-0;0");
    static string Effects(ResponseResult r) =>
        "Credibility " + Change(r.credibilityBefore, r.credibilityAfter) +
        "   |   Followers " + Change(r.followersBefore, r.followersAfter) +
        "   |   Cash " + Change(r.cashBefore, r.cashAfter);

    void Show(int choice)
    {
        if (FactCheckEventStore.Current == null) { returnToMenu.Invoke(); return; }
        var saved = SavedResult();
        if (saved != null) { returnToMenu.Invoke(); return; }
        selection = choice;
        showingResult = false;
        if (backButton != null) backButton.SetActive(true);
        heading.text = new[] { "Preview your apology", "Hire help & correct", "Preview your defense", "Leave it unanswered?" }[choice];
        explanation.text = (choice == 3 ? "YOUR DECISION\n" : "YOUR RESPONSE POST\n") + Draft(choice);
        var bank = GlobalStatManager.Instance;
        if (bank != null)
            explanation.text += "\n\nON CONFIRM: " + Effects(CalculateResult(choice, bank.currentCash, bank.currentFollowers, bank.currentCredibility));
        continueLabel.text = new[] { "Publish apology", "Hire & publish correction", "Publish defense", "Confirm ignore" }[choice];
        SetTutor(choice == 0 ? apologyCat : choice == 1 ? hiringCat : defenseCat,
            new[] { "A correction can rebuild trust, but some followers may still leave.",
                "Pay for help checking and correcting the claim, not for agreement.",
                "This draft dismisses the fact check without evidence. Loyal fans may agree, but others lose trust.",
                "Silence leaves your followers with the misleading claim." }[choice]);
        OpenPanel();
    }

    void OpenPanel()
    {
        feedbackPanel.SetActive(true);
        feedbackPanel.transform.SetAsLastSibling();
    }

    void SetTutor(Sprite expression, string message)
    {
        if (tutorImage != null) { tutorImage.sprite = expression; tutorImage.enabled = expression != null; }
        if (tutorMessage != null) tutorMessage.text = message;
    }

    public void Back()
    {
        if (showingResult) { returnToMenu.Invoke(); return; }
        selection = -1;
        feedbackPanel.SetActive(false);
    }

    public void Continue()
    {
        if (showingResult) { returnToMenu.Invoke(); return; }
        if (selection < 0) return;
        var saved = SavedResult();
        if (saved != null) { returnToMenu.Invoke(); return; }
        var bank = GlobalStatManager.Instance;
        if (bank == null) { SetTutor(defenseCat, "Your progress couldn't be loaded. Please return and try again."); return; }
        if (selection == 1 && bank.currentCash < Mathf.Max(0, supportCost))
        {
            SetTutor(hiringCat, "You need " + supportCost + " cash to hire help. Choose another strategy or come back later.");
            return;
        }
        var result = CalculateResult(selection, bank.currentCash, bank.currentFollowers, bank.currentCredibility);
        // Save both the committed response and its stats before showing the outcome.
        PlayerPrefs.SetString(ResponseSaveKey, JsonUtility.ToJson(result));
        bank.SaveToDisk(result.cashAfter, result.followersAfter, result.credibilityAfter);
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            foreach (var link in root.GetComponentsInChildren<StatSceneLink>(true)) link.RefreshUI();
            foreach (var stats in root.GetComponentsInChildren<UserStats>(true))
                stats.SetMetricsImmediate(result.cashAfter, result.followersAfter, result.credibilityAfter, stats.Likes);
        }
        FactCheckEntryState.RefreshScene(gameObject.scene);
        ShowResult(result);
    }

    void ShowResult(ResponseResult result)
    {
        selection = -1;
        showingResult = true;
        if (backButton != null) backButton.SetActive(false);
        heading.text = result.choice == 3 ? "Response recorded" : "Response published";
        explanation.text = (result.choice == 3 ? "YOUR DECISION\n" : "YOUR POST\n") + result.post +
            "\n\n" + Effects(result);
        string[] reactions = {
            "\"Thanks for correcting this.\"\n\"I wish you'd checked before recommending it.\"",
            "\"Glad you're getting help checking the facts.\"\n\"I'll wait for reliable information.\"",
            "\"I still agree with you.\"\n\"You're dismissing a safety concern. Unfollowing.\"",
            "\"Are you going to address the fact check?\"\n\"I can't trust a recommendation you won't correct.\""
        };
        SetTutor(result.choice == 0 ? apologyCat : result.choice == 1 ? hiringCat : defenseCat,
            "AUDIENCE REACTIONS\n" + reactions[result.choice]);
        continueLabel.text = "Return to main menu";
        OpenPanel();
    }

    void OnDisable()
    {
        selection = -1;
        showingResult = false;
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
    }
}
