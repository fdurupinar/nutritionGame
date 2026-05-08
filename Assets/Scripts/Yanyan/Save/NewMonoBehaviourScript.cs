using UnityEngine;
using UnityEngine.UI;

public class DailyPostLimiter : MonoBehaviour
{
    [Tooltip("Drag the Button you want to disable into this slot.")]
    public Button buttonToDisable;

    void Start()
    {
        // If you forgot to drag the button in, it tries to find it automatically
        if (buttonToDisable == null)
        {
            buttonToDisable = GetComponent<Button>();
        }

        if (buttonToDisable == null)
        {
            Debug.LogError("DailyPostLimiter: Cannot find the Button! Please drag it into the Inspector.");
            return;
        }

        // Read the saved data
        int hasPosted = PlayerPrefs.GetInt("HasPostedToday", 0);

        // Print a bright message to the Console
        Debug.Log("<color=cyan>HOME SCENE LOADED -> HasPostedToday value is: " + hasPosted + "</color>");

        if (hasPosted == 1)
        {
            buttonToDisable.interactable = false; // Disable it!
        }
        else
        {
            buttonToDisable.interactable = true;  // Keep it clickable
        }
    }
}