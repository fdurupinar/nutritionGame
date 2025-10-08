using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public class CommentManager : MonoBehaviour {
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _commentBox;
    [SerializeField] private ScrollRect _scrollRect;

    [Header("Settings")]
    [SerializeField] private float _commentDelay = 0.2f;

    private List<CommentLineSO> _commentsList;
    private bool _needsScrollToBottom = false;

    void Start() {
        // Find the ScrollRect automatically if not assigned
        if(_scrollRect == null && _commentBox != null) {
            _scrollRect = _commentBox.GetComponentInParent<ScrollRect>();
        }

        // Clear the comment box at the start
        _commentBox.text = string.Empty;


        LoadComments();
        StartCoroutine(DisplayCommentsRoutine(_commentDelay));
    }

    

    private void LoadComments() {
        var folderPath = "Content/Comments/";
        var commentsArray = Resources.LoadAll<CommentLineSO>(folderPath);

        if(commentsArray == null || commentsArray.Length == 0) {
            Debug.LogWarning($"No CommentData assets found in Resources/{folderPath}.");
            _commentsList = new List<CommentLineSO>();
            return;
        }
        _commentsList = commentsArray.ToList();
    }

    public IEnumerator DisplayCommentsRoutine(float delay) {
        StringBuilder builder = new StringBuilder();

        foreach(CommentLineSO comment in _commentsList) {
            // Check if the user is at the bottom BEFORE adding the new text.
            // A small tolerance (e.g., 0.1f) is good practice to account for floating point inaccuracies.
            bool isAtBottom = _scrollRect.verticalNormalizedPosition <= 0.1f;

            // Append the new comment instead of rebuilding the whole string every time.
            if(builder.Length > 0) {
                builder.AppendLine(); // Add a newline before the next comment
            }
            builder.Append($"<b>@{comment.commenterName}:</b> {comment.text}");

            // Update the text box with the new cumulative text
            _commentBox.text = builder.ToString();

            // If the user was at the bottom, request a scroll
            if(isAtBottom) {
                _needsScrollToBottom = true;
            }

            // Wait for the specified delay before adding the next comment
            yield return new WaitForSeconds(delay);
        }
    }

    void LateUpdate() {
        // If a scroll is requested, execute it here and reset the flag.
        // This is the most reliable way to scroll after the UI layout has been updated.
        if(_needsScrollToBottom) {
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
            _needsScrollToBottom = false;
        }
    }
}