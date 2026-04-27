using UnityEngine;

public class GlobalStatManager : MonoBehaviour
{
    public static GlobalStatManager Instance { get; private set; }

    [Header("Persistent Data Bank")]
    public int currentCash;
    public int currentFollowers;
    public int currentCredibility = 100;

    private const string SAVE_CASH = "User_Cash";
    private const string SAVE_FOLLOWERS = "User_Followers";
    private const string SAVE_CRED = "User_Credibility";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadFromDisk();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetStats()
    {
        // FORCE the variables in the script to reset immediately
        currentCash = 0;
        currentFollowers = 0;
        currentCredibility = 100;

        // Overwrite the save file
        PlayerPrefs.SetInt(SAVE_CASH, 0);
        PlayerPrefs.SetInt(SAVE_FOLLOWERS, 0);
        PlayerPrefs.SetInt(SAVE_CRED, 100);
        PlayerPrefs.Save();

        Debug.Log("GlobalStatManager: Memory and Disk reset to 0, 0, 100");
    }

    public void SaveToDisk(int cash, int followers, int cred)
    {
        currentCash = cash;
        currentFollowers = followers;
        currentCredibility = cred;

        PlayerPrefs.SetInt(SAVE_CASH, currentCash);
        PlayerPrefs.SetInt(SAVE_FOLLOWERS, currentFollowers);
        PlayerPrefs.SetInt(SAVE_CRED, currentCredibility);
        PlayerPrefs.Save();
    }

    private void LoadFromDisk()
    {
        currentCash = PlayerPrefs.GetInt(SAVE_CASH, 0);
        currentFollowers = PlayerPrefs.GetInt(SAVE_FOLLOWERS, 0);
        currentCredibility = PlayerPrefs.GetInt(SAVE_CRED, 100);
    }
}