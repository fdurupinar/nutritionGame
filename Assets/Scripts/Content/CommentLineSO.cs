// Assets/Scripts/Content/CommentLineSO.cs
using UnityEngine;

public enum CommentCategory { positive, skeptic, neutral, spam }

[CreateAssetMenu(menuName = "Content/CommentLine")]
public class CommentLineSO : ScriptableObject {
    public string id;
    public CommentCategory category;
    [TextArea] public string text;
    public string[] tags; // e.g., "fitness|teens"
}
