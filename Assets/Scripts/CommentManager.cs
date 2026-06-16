using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;

public class CommentManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _commentBox;
    [SerializeField] private ScrollRect _scrollRect;

    [Header("Settings")]
    [SerializeField] private float _commentDelay = 0.2f;
    [SerializeField] private float _typewriterSpeed = 60f;

    // 【需要你修改】在 Inspector 里设置两种交替的颜色
    [SerializeField] private Color _usernameColor1 = new Color(0.36f, 0.68f, 0.89f, 1f); // 默认淡蓝
    [SerializeField] private Color _usernameColor2 = new Color(0.89f, 0.68f, 0.36f, 1f); // 默认淡橙

    private List<CommentLineSO> _commentsList;
    private bool _needsScrollToBottom = false;
    AudioManager _audioManager;


    void Start()
    {
        // Find the ScrollRect automatically if not assigned
        if (_scrollRect == null && _commentBox != null)
        {
            _scrollRect = _commentBox.GetComponentInParent<ScrollRect>();
        }

        // Clear the comment box at the start
        _commentBox.text = string.Empty;

        _audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
        LoadComments();
    }


    private void LoadComments()
    {
        var folderPath = "Content/Comments/";
        var commentsArray = Resources.LoadAll<CommentLineSO>(folderPath);

        if (commentsArray == null || commentsArray.Length == 0)
        {
            UnityEngine.Debug.LogWarning($"No CommentData assets found in Resources/{folderPath}.");
            _commentsList = new List<CommentLineSO>();
            return;
        }
        _commentsList = commentsArray.ToList();
    }

    public IEnumerator DisplayCommentsRoutine(string type, float delay)
    {
        StringBuilder builder = new StringBuilder();
        List<CommentLineSO> commentsWithType = new List<CommentLineSO>();

        commentsWithType = _commentsList.FindAll(comment => comment.type == type);

        // Wait for the specified delay before adding the next comment
        yield return new WaitForSeconds(delay);

        _commentBox.maxVisibleCharacters = 0;

        // 【需要你修改】用于控制颜色交替的开关
        bool useColor1 = true;

        foreach (CommentLineSO comment in commentsWithType)
        {
            // Append the new comment instead of rebuilding the whole string every time.
            if (builder.Length > 0)
            {
                builder.Append("\n<size=50%>\n</size>");
            }

            // 【需要你修改】决定当前这条评论用哪个颜色，并转换成 Hex 字符串
            Color currentColor = useColor1 ? _usernameColor1 : _usernameColor2;
            string colorHex = "#" + ColorUtility.ToHtmlStringRGB(currentColor);

            // 【需要你修改】反转开关，让下一条评论用另一种颜色
            useColor1 = !useColor1;

            // 动态应用计算出的颜色
            builder.Append($"<color={colorHex}><b>@{comment.commenterName}:</b></color> {comment.text}");

            // Update the text box with the new cumulative text
            _commentBox.text = builder.ToString();

            _commentBox.ForceMeshUpdate();
            int totalCharacters = _commentBox.textInfo.characterCount;
            int currentVisible = _commentBox.maxVisibleCharacters;

            _audioManager.PlayCommentNotification();

            while (currentVisible < totalCharacters)
            {
                currentVisible += Mathf.CeilToInt(_typewriterSpeed * Time.deltaTime);
                _commentBox.maxVisibleCharacters = currentVisible;

                _needsScrollToBottom = true;

                yield return null;
            }

            _commentBox.maxVisibleCharacters = totalCharacters;

            // Wait for the specified delay before adding the next comment
            yield return new WaitForSeconds(_commentDelay);
        }
    }


    public void ClearComments()
    {
        StopAllCoroutines();

        if (_commentBox != null)
        {
            _commentBox.text = string.Empty;
            _commentBox.maxVisibleCharacters = 0;
        }

        _needsScrollToBottom = false;

        if (_scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 1f;
        }
    }


    void LateUpdate()
    {
        // If a scroll is requested, execute it here and reset the flag.
        // This is the most reliable way to scroll after the UI layout has been updated.
        if (_needsScrollToBottom)
        {
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
            _needsScrollToBottom = false;
        }
    }
}