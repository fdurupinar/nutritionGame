using UnityEngine;
using UnityEngine.UI;

public class DailyPostButtonController : MonoBehaviour
{
    private Button postButton;

    // CHANGED from Start() to OnEnable()
    // OnEnable runs every single time this button becomes active on the screen
    void OnEnable()
    {
        postButton = GetComponent<Button>();

        if (postButton == null)
        {
            Debug.LogWarning("DailyPostButtonController: No Button component found on this object!");
            return;
        }

        // Check our saved flag. 1 means they posted, 0 means they haven't.
        int hasPosted = PlayerPrefs.GetInt("HasPostedToday", 0);

        // Print the result to the console so we can verify it's working
        Debug.Log("Home Button Check -> HasPostedToday value is: " + hasPosted);

        if (hasPosted == 1)
        {
            postButton.interactable = false; // Disable the button!
        }
        else
        {
            postButton.interactable = true;  // Keep it enabled!
        }
    }
}