using System.Collections;
using UnityEngine;

public class PublishFlagSaver : MonoBehaviour
{
    private const string KEY = "HasPostedToday";

    private void Start()
    {
        int value = PlayerPrefs.GetInt(KEY, 0);
        Debug.Log("<color=cyan>PublishFlagSaver Start Check -> HasPostedToday value is: " + value + "</color>");
    }

    public void SaveThatWePostedToday()
    {
        PlayerPrefs.SetInt(KEY, 1);
        PlayerPrefs.Save();

        int checkValue = PlayerPrefs.GetInt(KEY, 0);

        Debug.Log("<color=green>SUCCESS: POST FLAG SAVED AS "
            + checkValue
            + " using key "
            + KEY
            + "</color>");

        StartCoroutine(CheckAgainNextFrame());
    }

    private IEnumerator CheckAgainNextFrame()
    {
        yield return null;

        int value = PlayerPrefs.GetInt(KEY, 0);

        Debug.Log("<color=magenta>ONE FRAME AFTER SAVE -> HasPostedToday value is: "
            + value
            + "</color>");

        if (value == 0)
        {
            Debug.LogError("HasPostedToday became 0 right after saving. Another script is resetting it.");
        }
    }

    public void ResetPostedToday()
    {
        PlayerPrefs.SetInt(KEY, 0);
        PlayerPrefs.Save();

        Debug.Log("<color=yellow>POST FLAG RESET TO 0</color>");
    }
}