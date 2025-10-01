// Assets/Scripts/Content/FormatSO.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Content/Format")]
public class FormatSO : ScriptableObject {
    public string id;
    public string name;
    public bool enabledFlag = true;
    // audienceMultipliers: "teens:0.10|parents:-0.05"
    public List<AudienceMultiplier> audienceMultipliers = new();
}

[System.Serializable]
public class AudienceMultiplier {
    public string audienceId;
    public float multiplier;
}
