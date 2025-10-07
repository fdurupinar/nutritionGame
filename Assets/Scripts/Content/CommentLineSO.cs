// Assets/Scripts/Content/CommentLineSO.cs
using UnityEngine;

public enum CommentCategory { positive, skeptic, neutral, spam }

[CreateAssetMenu(menuName = "Content/CommentLine")]
public class CommentLineSO : ScriptableObject {
    public CommentCategory category;

    public string id;
    public string commenterName;
    //public Sprite icon;

    [TextArea(3, 10)] // This makes the text field in the Inspector bigger.
    public string text;

}

