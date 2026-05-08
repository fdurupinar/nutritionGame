using UnityEngine;
using UnityEngine.UI; 

public class PublishFlagSaver : MonoBehaviour
{

    public void SaveThatWePostedToday()
    {
        PlayerPrefs.SetInt("HasPostedToday", 1);
        PlayerPrefs.Save();

        Debug.Log("<color=green>SUCCESS: POST FLAG SAVED AS 1!</color>");
    }
}