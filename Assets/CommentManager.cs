using UnityEngine;
using TMPro; // Required for TextMeshPro
using System.Collections.Generic;
using System.Text;


public class CommentDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI commentBox;

    [Header("Comment Data")]
    [SerializeField] private List<CommentSO> commentsToDisplay;

    void Start()
    {
        UpdateCommentBox();
    }

    private void UpdateCommentBox()
    {
        if (commentBox == null) return;

        // Use a StringBuilder for better performance when joining many strings.
        StringBuilder builder = new StringBuilder();

        foreach (CommentSO comment in commentsToDisplay)
        {
            // Format: "<b>Name:</b> The comment text."
            builder.AppendLine($"<b>{comment.commenterName}:</b> {comment.commentText}\n");
        }

        commentBox.text = builder.ToString();
    }
}