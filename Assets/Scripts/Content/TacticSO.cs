using UnityEngine;

public enum TacticType { Emotion, Pseudoscience, Conspiracy, LogicalFallacy, FinancialGain, TwistedEvidence}

[CreateAssetMenu(menuName = "Content/Tactics")]
public class TacticSO : ScriptableObject {
    public string id;
    public string displayName;
    public TacticType type;
    public string text;
    public float engagementBonus;   // 0.15f = +15%
    public float credibilityCost;   // 0.08f = -8    
    public bool enabledFlag = true;
    public Sprite tacticImage; // Add this line for the card's image

}