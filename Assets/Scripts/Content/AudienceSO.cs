// Assets/Scripts/Content/AudienceSO.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Content/Audience")]
public class AudienceSO : ScriptableObject {
    public string id;
    public string displayName;
    [Range(0f, 1f)] public float emotionality;
    [Range(0f, 1f)] public float skepticism;
    public string[] interests;
    public bool enabledFlag = true;
}
