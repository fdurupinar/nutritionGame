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
        currentCash = 1000;
        currentFollowers = 0;
        currentCredibility = 100;

        // 中文备注：起始现金统一为 1000。这里原来写入 10000，
        // 会导致脚本变量和 PlayerPrefs 存档中的现金数值不一致。
        PlayerPrefs.SetInt(SAVE_CASH, 1000);
        PlayerPrefs.SetInt(SAVE_FOLLOWERS, 0);
        PlayerPrefs.SetInt(SAVE_CRED, 100);
        PlayerPrefs.Save();

        Debug.Log("GlobalStatManager: reset to 1000, 0, 100");
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
        currentCash = PlayerPrefs.GetInt(SAVE_CASH, 1000);
        currentFollowers = PlayerPrefs.GetInt(SAVE_FOLLOWERS, 0);
        currentCredibility = PlayerPrefs.GetInt(SAVE_CRED, 100);
    }
}