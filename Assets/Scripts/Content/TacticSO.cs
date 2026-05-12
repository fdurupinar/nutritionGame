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

public enum TacticIdOption
{
    Emotion,
    Trolling,
    Attack,
    Conspiracy,
    Strawman,
    Scapegoat,
    Twisting,
    Authority,
    Bandwagon
}

[CreateAssetMenu(menuName = "Content/Tactics")]
public class TacticSO : ScriptableObject
{
    [Header("Tactic ID Dropdown")]
    public TacticIdOption tacticIdDropdown;

    [HideInInspector]
    public string tacticId;

    public string id;
    public string displayName;
    public string type;
    public string text;
    public string debunkingText;

    [TextArea(2, 5)]
    public string hintText;

    public int level;
    public float engagementBonus;   // 0.15f = +15%
    public float credibilityCost;   // 0.08f = -8    
    public bool enabledFlag = true;
    public Sprite tacticImage;

    private void OnValidate()
    {
        tacticId = ConvertTacticIdToString(tacticIdDropdown);
    }

    public string GetTacticId()
    {
        return tacticId;
    }

    private string ConvertTacticIdToString(TacticIdOption option)
    {
        switch (option)
        {
            case TacticIdOption.Emotion:
                return "emotion";

            case TacticIdOption.Trolling:
                return "trolling";

            case TacticIdOption.Attack:
                return "attack";

            case TacticIdOption.Conspiracy:
                return "conspiracy";

            case TacticIdOption.Strawman:
                return "strawman";

            case TacticIdOption.Scapegoat:
                return "scapegoat";

            case TacticIdOption.Twisting:
                return "twisting";

            case TacticIdOption.Authority:
                return "authority";

            case TacticIdOption.Bandwagon:
                return "bandwagon";

            default:
                return "emotion";
        }
    }
}