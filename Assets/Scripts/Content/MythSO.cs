// Assets/Scripts/Content/MythSO.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Content/Myth")]
public class MythSO : ScriptableObject {
    public string id;
    public string title;
    public int difficulty;
    public float baseReach;
    public string[] susceptibleAudiences;     // "teens", "fitness", ...
    [TextArea] public string[] counterFacts;  // 2–3 nuggets
    [TextArea] public string shortDebunk;
    public string[] tags;                     // "weightloss", "fear"
    public bool enabledFlag = true;
}
