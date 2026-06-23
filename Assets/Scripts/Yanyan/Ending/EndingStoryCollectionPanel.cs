using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndingStoryCollectionPanel : MonoBehaviour
{
    [Header("结局数据库")]
    [Tooltip("拖入 EndingStoryDatabase。里面保存12个结局的ID、标题和故事。")]
    public EndingStoryDatabase endingDatabase;

    [Header("12个结局UI槽位")]
    [Tooltip("每个槽位对应一个结局。每个结局只需要一个 Locked 物体和一个 Unlocked 物体。")]
    public EndingStorySlotUI[] storySlots = new EndingStorySlotUI[12];

    [Header("故事Panel")]
    [Tooltip("共用的故事Panel。点击已解锁结局或测试解锁按钮后，会自动打开这个Panel。")]
    public GameObject storyPanel;

    [Tooltip("打开结局收藏界面时，是否自动关闭故事Panel。")]
    public bool hideStoryPanelOnOpen = true;

    [Tooltip("点击已解锁结局时，是否自动打开故事Panel。测试解锁按钮也会使用这个逻辑。")]
    public bool showStoryPanelWhenUnlockedEndingClicked = true;

    [Tooltip("点击未解锁结局时，是否关闭故事Panel。通常保持关闭即可。")]
    public bool hideStoryPanelWhenLockedEndingClicked = false;

    [Header("共用故事显示文字")]
    [Tooltip("共用的结局标题文字。点击已解锁结局后，这里会显示对应标题。")]
    public TMP_Text sharedTitleTMP;

    [Tooltip("共用的结局故事正文文字。点击已解锁结局后，这里会显示对应故事。")]
    public TMP_Text sharedStoryTMP;

    [Header("普通Text备用")]
    [Tooltip("如果你没有用TMP，可以把普通Text标题拖到这里。可以不填。")]
    public Text sharedTitleText;

    [Tooltip("如果你没有用TMP，可以把普通Text故事正文拖到这里。可以不填。")]
    public Text sharedStoryText;

    [Header("锁定提示Panel")]
    [Tooltip("点击未解锁结局时，需要打开的Locked Panel。")]
    public GameObject lockedPanel;

    [Tooltip("打开结局收藏界面时，是否自动关闭Locked Panel。")]
    public bool hideLockedPanelOnOpen = true;

    [Tooltip("点击已解锁结局时，是否自动关闭Locked Panel。")]
    public bool hideLockedPanelWhenUnlockedEndingClicked = true;

    [Header("锁定时共用文字")]
    [Tooltip("点击未解锁结局时，共用标题显示的文字。如果不想改共用文字，可以留空。")]
    public string lockedTitle = "Locked";

    [TextArea(2, 5)]
    [Tooltip("点击未解锁结局时，共用故事正文显示的文字。如果不想改共用文字，可以留空。")]
    public string lockedStory = "This ending has not been unlocked yet.";

    [Tooltip("点击未解锁结局时，是否也更新共用故事文字。")]
    public bool updateSharedTextWhenLockedClicked = false;

    [Header("刷新设置")]
    [Tooltip("当这个Panel启用时，自动刷新所有结局的锁定/解锁显示。")]
    public bool refreshWhenEnabled = true;

    [Tooltip("Start时自动刷新一次。")]
    public bool refreshOnStart = true;

    [Tooltip("打开界面时，是否自动显示第一个已解锁结局。若你想故事Panel只在点击后出现，保持false。")]
    public bool showFirstUnlockedOnStart = false;

    private const string SaveKeyPrefix = "ENDING_UNLOCKED_";

    private void Awake()
    {
        EnsureSlotArray();
    }

    private void Start()
    {
        if (hideLockedPanelOnOpen)
        {
            CloseLockedPanel();
        }

        if (hideStoryPanelOnOpen)
        {
            CloseStoryPanel();
        }

        if (refreshOnStart)
        {
            RefreshAllSlots();
        }

        if (showFirstUnlockedOnStart)
        {
            ShowFirstUnlockedEnding();
        }
    }

    private void OnEnable()
    {
        if (hideLockedPanelOnOpen)
        {
            CloseLockedPanel();
        }

        if (hideStoryPanelOnOpen)
        {
            CloseStoryPanel();
        }

        if (refreshWhenEnabled)
        {
            RefreshAllSlots();
        }
    }

    /// <summary>
    /// 刷新全部12个结局UI。
    /// 已解锁：隐藏Locked，显示Unlocked。
    /// 未解锁：显示Locked，隐藏Unlocked。
    /// </summary>
    public void RefreshAllSlots()
    {
        EnsureSlotArray();

        for (int i = 0; i < storySlots.Length; i++)
        {
            RefreshSlot(i);
        }
    }

    /// <summary>
    /// 刷新单个结局UI，只负责锁定/解锁物体显示。
    /// </summary>
    public void RefreshSlot(int index)
    {
        if (!IsValidSlotIndex(index))
        {
            return;
        }

        EndingStorySlotUI slot = storySlots[index];

        if (slot == null)
        {
            return;
        }

        string endingId = GetEndingIdForSlot(index);
        bool unlocked = IsEndingUnlocked(endingId);

        if (slot.lockedGameObject != null)
        {
            slot.lockedGameObject.SetActive(!unlocked);
        }

        if (slot.unlockedGameObject != null)
        {
            slot.unlockedGameObject.SetActive(unlocked);
        }
    }

    /// <summary>
    /// 显示指定结局到共用故事文字。
    /// 如果未解锁，则打开Locked Panel。
    /// 如果已解锁，则打开Story Panel。
    /// </summary>
    public void ShowEndingByIndex(int index)
    {
        if (!IsValidSlotIndex(index))
        {
            Debug.LogWarning("[EndingStoryCollectionPanel] Invalid ending index: " + index);
            return;
        }

        string endingId = GetEndingIdForSlot(index);
        bool unlocked = IsEndingUnlocked(endingId);

        if (!unlocked)
        {
            OpenLockedPanel();

            if (hideStoryPanelWhenLockedEndingClicked)
            {
                CloseStoryPanel();
            }

            if (updateSharedTextWhenLockedClicked)
            {
                SetSharedStoryText(lockedTitle, lockedStory);
            }

            return;
        }

        if (hideLockedPanelWhenUnlockedEndingClicked)
        {
            CloseLockedPanel();
        }

        if (showStoryPanelWhenUnlockedEndingClicked)
        {
            OpenStoryPanel();
        }

        EndingStoryData data = GetEndingDataForSlot(index);

        if (data == null)
        {
            SetSharedStoryText("Missing Ending", "This ending data is missing from the database.");
            return;
        }

        SetSharedStoryText(data.title, data.story);
    }

    /// <summary>
    /// Button传数字用：1 = 第1个结局，12 = 第12个结局。
    /// </summary>
    public void ShowEndingByNumber(int endingNumber)
    {
        ShowEndingByIndex(endingNumber - 1);
    }

    /// <summary>
    /// 显示第一个已解锁结局。
    /// </summary>
    public void ShowFirstUnlockedEnding()
    {
        EnsureSlotArray();

        for (int i = 0; i < storySlots.Length; i++)
        {
            string endingId = GetEndingIdForSlot(i);

            if (IsEndingUnlocked(endingId))
            {
                ShowEndingByIndex(i);
                return;
            }
        }

        if (updateSharedTextWhenLockedClicked)
        {
            SetSharedStoryText(lockedTitle, lockedStory);
        }
    }

    /// <summary>
    /// 打开Story Panel。
    /// </summary>
    public void OpenStoryPanel()
    {
        if (storyPanel != null)
        {
            storyPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 关闭Story Panel。可以给Story Panel里的Close按钮调用。
    /// </summary>
    public void CloseStoryPanel()
    {
        if (storyPanel != null)
        {
            storyPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 打开Locked Panel。
    /// </summary>
    public void OpenLockedPanel()
    {
        if (lockedPanel != null)
        {
            lockedPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 关闭Locked Panel。可以给Locked Panel里的Close按钮调用。
    /// </summary>
    public void CloseLockedPanel()
    {
        if (lockedPanel != null)
        {
            lockedPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 解锁结局，并显示到共用故事文字。
    /// 这个方法会永久保存解锁状态。
    /// 0 = 第1个结局，11 = 第12个结局。
    /// </summary>
    public void UnlockEndingByIndex(int index)
    {
        if (!IsValidSlotIndex(index))
        {
            Debug.LogWarning("[EndingStoryCollectionPanel] Invalid ending index: " + index);
            return;
        }

        string endingId = GetEndingIdForSlot(index);

        if (string.IsNullOrWhiteSpace(endingId))
        {
            Debug.LogWarning("[EndingStoryCollectionPanel] Ending ID is empty at index: " + index);
            return;
        }

        // 永久保存解锁状态
        PlayerPrefs.SetInt(BuildSaveKey(endingId), 1);
        PlayerPrefs.Save();

        // 立即刷新UI：隐藏Locked，显示Unlocked
        RefreshAllSlots();

        // 测试按钮点击后，立即打开故事Panel并显示故事
        ShowEndingByIndex(index);

        EndingStoryData data = GetEndingDataForSlot(index);
        string title = data != null ? data.title : endingId;

        Debug.Log("[EndingStoryCollectionPanel] Ending unlocked permanently: " + title);
    }

    /// <summary>
    /// Button传数字用：1 = 第1个结局，12 = 第12个结局。
    /// </summary>
    public void UnlockEndingByNumber(int endingNumber)
    {
        UnlockEndingByIndex(endingNumber - 1);
    }

    /// <summary>
    /// 判断某个结局是否已解锁。
    /// </summary>
    public bool IsEndingUnlocked(string endingId)
    {
        if (string.IsNullOrWhiteSpace(endingId))
        {
            return false;
        }

        return PlayerPrefs.GetInt(BuildSaveKey(endingId), 0) == 1;
    }

    /// <summary>
    /// 锁回指定结局，主要用于测试。
    /// </summary>
    public void LockEndingByIndex(int index)
    {
        if (!IsValidSlotIndex(index))
        {
            Debug.LogWarning("[EndingStoryCollectionPanel] Invalid ending index: " + index);
            return;
        }

        string endingId = GetEndingIdForSlot(index);

        if (string.IsNullOrWhiteSpace(endingId))
        {
            return;
        }

        PlayerPrefs.DeleteKey(BuildSaveKey(endingId));
        PlayerPrefs.Save();

        RefreshAllSlots();

        Debug.Log("[EndingStoryCollectionPanel] Ending locked again: " + endingId);
    }

    /// <summary>
    /// 清空所有结局解锁记录，测试用。
    /// </summary>
    public void ClearAllEndingUnlocks()
    {
        EnsureSlotArray();

        for (int i = 0; i < storySlots.Length; i++)
        {
            string endingId = GetEndingIdForSlot(i);

            if (!string.IsNullOrWhiteSpace(endingId))
            {
                PlayerPrefs.DeleteKey(BuildSaveKey(endingId));
            }
        }

        PlayerPrefs.Save();
        RefreshAllSlots();

        if (updateSharedTextWhenLockedClicked)
        {
            SetSharedStoryText(lockedTitle, lockedStory);
        }

        CloseLockedPanel();
        CloseStoryPanel();

        Debug.Log("[EndingStoryCollectionPanel] All ending unlocks cleared.");
    }

    /// <summary>
    /// 根据三个数值判断结局，并自动解锁。
    /// 正式游戏结束时可以调用这个方法。
    /// </summary>
    public EndingStoryData ResolveAndUnlockEndingFromStats(int money, int credibility, int followers)
    {
        int index = GetEndingIndexFromStats(money, credibility, followers);
        UnlockEndingByIndex(index);
        return GetEndingDataForSlot(index);
    }

    /// <summary>
    /// 根据 Money / Credibility / Followers 返回对应结局Index。
    /// </summary>
    public int GetEndingIndexFromStats(int money, int credibility, int followers)
    {
        money = Mathf.Clamp(money, 0, 100);
        credibility = Mathf.Clamp(credibility, 0, 100);
        followers = Mathf.Clamp(followers, 0, 100);

        if (credibility <= 0)
        {
            return 0; // Collapse
        }
        else if (money < 40 && credibility < 40 && followers < 40)
        {
            return 1; // Forgotten
        }
        else if (money >= 70 && followers >= 70 && credibility < 40)
        {
            return 2; // Noise Empire
        }
        else if (money >= 70 && credibility < 40)
        {
            return 3; // Paid Lies
        }
        else if (followers >= 70 && credibility < 40)
        {
            return 4; // Viral Beast
        }
        else if (money >= 70 && followers < 40 && credibility >= 40)
        {
            return 5; // Quiet Cash
        }
        else if (followers >= 70 && money < 40 && credibility >= 40)
        {
            return 6; // Broke Star
        }
        else if (money >= 70 && credibility >= 70 && followers >= 70)
        {
            return 7; // Powerhouse
        }
        else if (credibility >= 70 && followers >= 70)
        {
            return 8; // Trusted
        }
        else if (credibility >= 70 && money < 40)
        {
            return 9; // Honest Path
        }
        else if (money >= 50 && credibility >= 60 && followers >= 50)
        {
            return 10; // Balanced
        }
        else
        {
            return 11; // Mixed Legacy
        }
    }

    // =========================
    // 12个测试按钮：永久解锁 + 显示Unlocked + 隐藏Locked + 打开Story Panel
    // =========================

    public void TestUnlockEnding01()
    {
        UnlockEndingByIndex(0);
    }

    public void TestUnlockEnding02()
    {
        UnlockEndingByIndex(1);
    }

    public void TestUnlockEnding03()
    {
        UnlockEndingByIndex(2);
    }

    public void TestUnlockEnding04()
    {
        UnlockEndingByIndex(3);
    }

    public void TestUnlockEnding05()
    {
        UnlockEndingByIndex(4);
    }

    public void TestUnlockEnding06()
    {
        UnlockEndingByIndex(5);
    }

    public void TestUnlockEnding07()
    {
        UnlockEndingByIndex(6);
    }

    public void TestUnlockEnding08()
    {
        UnlockEndingByIndex(7);
    }

    public void TestUnlockEnding09()
    {
        UnlockEndingByIndex(8);
    }

    public void TestUnlockEnding10()
    {
        UnlockEndingByIndex(9);
    }

    public void TestUnlockEnding11()
    {
        UnlockEndingByIndex(10);
    }

    public void TestUnlockEnding12()
    {
        UnlockEndingByIndex(11);
    }

    // =========================
    // 12个显示按钮：只显示，不解锁
    // 未解锁时会打开Locked Panel
    // 已解锁时会打开Story Panel
    // =========================

    public void ShowEnding01()
    {
        ShowEndingByIndex(0);
    }

    public void ShowEnding02()
    {
        ShowEndingByIndex(1);
    }

    public void ShowEnding03()
    {
        ShowEndingByIndex(2);
    }

    public void ShowEnding04()
    {
        ShowEndingByIndex(3);
    }

    public void ShowEnding05()
    {
        ShowEndingByIndex(4);
    }

    public void ShowEnding06()
    {
        ShowEndingByIndex(5);
    }

    public void ShowEnding07()
    {
        ShowEndingByIndex(6);
    }

    public void ShowEnding08()
    {
        ShowEndingByIndex(7);
    }

    public void ShowEnding09()
    {
        ShowEndingByIndex(8);
    }

    public void ShowEnding10()
    {
        ShowEndingByIndex(9);
    }

    public void ShowEnding11()
    {
        ShowEndingByIndex(10);
    }

    public void ShowEnding12()
    {
        ShowEndingByIndex(11);
    }

    private void SetSharedStoryText(string title, string story)
    {
        if (sharedTitleTMP != null)
        {
            sharedTitleTMP.text = title;
        }

        if (sharedStoryTMP != null)
        {
            sharedStoryTMP.text = story;
        }

        if (sharedTitleText != null)
        {
            sharedTitleText.text = title;
        }

        if (sharedStoryText != null)
        {
            sharedStoryText.text = story;
        }
    }

    private string GetEndingIdForSlot(int index)
    {
        EndingStoryData data = GetEndingDataForSlot(index);

        if (data != null && !string.IsNullOrWhiteSpace(data.endingId))
        {
            return data.endingId.Trim();
        }

        return "ending_" + (index + 1).ToString("00");
    }

    private EndingStoryData GetEndingDataForSlot(int index)
    {
        if (endingDatabase == null)
        {
            return null;
        }

        return endingDatabase.GetEndingByIndex(index);
    }

    private string BuildSaveKey(string endingId)
    {
        string databaseSaveId = "DefaultEndingDatabase";

        if (endingDatabase != null && !string.IsNullOrWhiteSpace(endingDatabase.saveId))
        {
            databaseSaveId = endingDatabase.saveId.Trim();
        }

        return SaveKeyPrefix + databaseSaveId + "_" + endingId.Trim();
    }

    private bool IsValidSlotIndex(int index)
    {
        EnsureSlotArray();
        return index >= 0 && index < storySlots.Length;
    }

    private void EnsureSlotArray()
    {
        if (storySlots == null || storySlots.Length != 12)
        {
            Array.Resize(ref storySlots, 12);
        }

        for (int i = 0; i < storySlots.Length; i++)
        {
            if (storySlots[i] == null)
            {
                storySlots[i] = new EndingStorySlotUI();
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureSlotArray();
    }
#endif
}

[Serializable]
public class EndingStorySlotUI
{
    [Header("锁定/解锁物体")]
    [Tooltip("未解锁时显示的GameObject。")]
    public GameObject lockedGameObject;

    [Tooltip("已解锁时显示的GameObject。")]
    public GameObject unlockedGameObject;
}