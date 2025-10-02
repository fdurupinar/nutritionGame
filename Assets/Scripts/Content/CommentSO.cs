using UnityEngine;

[CreateAssetMenu(fileName = "New Comment", menuName = "Comments/Comment Data")]
public class CommentSO : ScriptableObject
{
    public string commenterName;
    public Sprite icon;

    [TextArea(3, 10)] // This makes the text field in the Inspector bigger.
    public string commentText;
}
