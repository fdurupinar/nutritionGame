using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EndingStoryDatabase", menuName = "Ending Story/Ending Story Database")]
public class EndingStoryDatabase : ScriptableObject
{
    [Header("保存ID")]
    [Tooltip("用于保存结局解锁状态。发布后不要随便改，否则旧存档可能找不到之前解锁的结局。")]
    public string saveId = "MainEndingStoryDatabase";

    [Header("结局故事列表")]
    [Tooltip("固定为12个结局。顺序要和 EndingStoryCollectionPanel 的槽位顺序一致。")]
    public EndingStoryData[] endings = new EndingStoryData[12];

    public int Count
    {
        get
        {
            return endings == null ? 0 : endings.Length;
        }
    }

    public EndingStoryData GetEndingByIndex(int index)
    {
        if (endings == null)
        {
            return null;
        }

        if (index < 0 || index >= endings.Length)
        {
            return null;
        }

        return endings[index];
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(saveId))
        {
            saveId = name;
        }

        if (endings == null || endings.Length != 12)
        {
            Array.Resize(ref endings, 12);
        }

        for (int i = 0; i < endings.Length; i++)
        {
            if (endings[i] == null)
            {
                endings[i] = new EndingStoryData();
            }

            if (string.IsNullOrWhiteSpace(endings[i].endingId))
            {
                endings[i].endingId = "ending_" + (i + 1).ToString("00");
            }
        }
    }
#endif
}

[Serializable]
public class EndingStoryData
{
    [Header("基础信息")]
    [Tooltip("唯一ID，不要重复。例：collapse")]
    public string endingId;

    [Tooltip("结局短标题。例：Collapse")]
    public string title;

    [TextArea(4, 10)]
    [Tooltip("结局故事正文。")]
    public string story;
}