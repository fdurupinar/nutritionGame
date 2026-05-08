using UnityEngine;

public enum TacticType
{
    Emotion,
    Trolling,
    Attack,
    Conspiracy,
    Strawman,
    Scapegoat,
    TwistingFacts,
    Authority,
    Bandwagon
}

[CreateAssetMenu(menuName = "Content/Tactics")]
public class TacticSO : ScriptableObject
{
    public string id;
    public string displayName;
    public string type;
    public string text;
    public string debunkingText;
    public int level;
    public float engagementBonus;   // 0.15f = +15%
    public float credibilityCost;   // 0.08f = -8    
    public bool enabledFlag = true;
    public Sprite tacticImage; // Add this line for the card's image

}
