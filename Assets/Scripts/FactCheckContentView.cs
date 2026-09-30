using TMPro;
using UnityEngine;

/// <summary>Shows the latest published-post fact check on the home screen and alert panels.</summary>
public class FactCheckContentView : MonoBehaviour
{
    public TMP_Text homeSummary, homeStatus, boardSummary, detailTitle, detailBody;
    void OnEnable() => Refresh();
    public void Refresh()
    {
        var item = FactCheckEventStore.Current;
        bool resolved = FactCheckResponseFeedback.HasResolvedCurrentEvent;
        if (homeSummary != null) homeSummary.text = item == null ? "No fact checks yet" : item.title;
        if (homeStatus != null) homeStatus.text = item == null ? "Your alerts appear here" : resolved ? "Response complete" : "New fact check · Day " + item.day;
        if (boardSummary != null) boardSummary.text = item == null ? "No fact checks yet." : item.title + "\n\nYOUR POST\n“" + FactCheckEventStore.CaptionExcerpt(item) + "”";
        if (detailTitle != null) detailTitle.text = item?.title ?? "Fact check";
        if (detailBody != null)
        {
            detailBody.richText = false; // Captions are content, not TMP formatting instructions.
            detailBody.text = item == null ? "Publish a post to begin." : "YOUR POST\n“" + FactCheckEventStore.CaptionExcerpt(item) + "”\n\nTHE CHECK\n" + item.explanation;
        }
    }
}
