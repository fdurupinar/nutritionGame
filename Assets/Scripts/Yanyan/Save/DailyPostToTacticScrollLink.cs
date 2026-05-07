using UnityEngine;

public class DailyPostToTacticScrollLink : MonoBehaviour
{
    public DailyPostFillBlankManager dailyPostFillBlankManager;
    public TacticManager tacticManager;

    public void SendCompletedSentenceToTacticScroll()
    {
        if (dailyPostFillBlankManager == null)
        {
            Debug.LogWarning("DailyPostToTacticScrollLink: Missing DailyPostFillBlankManager.");
            return;
        }

        if (tacticManager == null)
        {
            Debug.LogWarning("DailyPostToTacticScrollLink: Missing TacticManager.");
            return;
        }

        string completedSentence = dailyPostFillBlankManager.currentCompletedSentence;

        tacticManager.ShowDailyPostCompletedSentenceOnly(completedSentence);
    }

}