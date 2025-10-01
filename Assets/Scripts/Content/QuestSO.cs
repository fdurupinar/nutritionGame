// Assets/Scripts/Content/QuestSO.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Content/Quest")]
public class QuestSO : ScriptableObject {
    public string id;
    public string title;
    [TextArea] public string description;
    public int reward_coins;
    public int reward_truthTokens;
    public bool enabledFlag = true;
}
