// Assets/Scripts/Content/AvatarSO.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Content/Avatar")]
public class AvatarSO : ScriptableObject {
    public string id;
    public string name;
    // "FakeExpert:0.10|CherryPick:0.03"
    public List<TacticTypeBonus> perk_tacticTypeBonus = new();
    // "fitness:0.05|general:0.02"
    public List<AudienceAffinity> audienceAffinity = new();
    public string cosmeticSet;
    public bool enabledFlag = true;
}

[System.Serializable] public class TacticTypeBonus { public TacticType type; public float bonus; }
[System.Serializable] public class AudienceAffinity { public string audienceId; public float bonus; }
