using UnityEngine;

public enum TacticType { Fear, FakeExpert, CherryPick, Anecdote, Clickbait, EmojiSpam }

[CreateAssetMenu(menuName = "Content/Tactic")]
public class TacticSO : ScriptableObject {
    public string id;
    public string displayName;
    public TacticType type;
    public float engagementBonus;   // 0.15f = +15%
    public float credibilityCost;   // 0.08f = -8
    public string[] synergies;      // pipe-delimited
    public string[] tags;           // pipe-delimited
    public bool enabledFlag = true;
}