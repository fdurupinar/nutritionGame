// Assets/Scripts/Content/CommentLineSO.cs
using UnityEngine;

public enum CommentCategory { positive, skeptic, neutral, spam }
public enum CommentReaction { Any, ClearCaption, ConfusingWords, PoorTacticFit }

[CreateAssetMenu(menuName = "Content/CommentLine")]
public class CommentLineSO : ScriptableObject
{
    public CommentCategory category;

    public string id;
    public string type;
    [Header("Post matching (leave IDs blank to match any)")]
    public string topicId;
    public string subTopicId;
    public string captionTemplateId;
    public CommentReaction reaction;
    public string commenterName;
    //public Sprite icon;

    [Tooltip("Optional placeholders: {topic}, {subtopic}, {caption}, {tactic}.")]
    [TextArea(3, 10)] // This makes the text field in the Inspector bigger.
    public string text;

}
