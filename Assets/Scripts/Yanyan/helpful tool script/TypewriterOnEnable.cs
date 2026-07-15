using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 中文备注：
/// 当这个面板从未激活变成激活时，
/// 自动播放一次文字打字机动画。
///
/// 动画完成后停止，并显示完整文字。
/// 面板下次重新激活时，会重新播放一次。
/// </summary>
public class TypewriterOnEnable : MonoBehaviour
{
    [Header("文字引用")]

    [Tooltip("拖入需要播放打字机动画的 TMP 文字。")]
    public TMP_Text targetText;

    [Header("打字机设置")]

    [Tooltip("每秒显示多少个字符。")]
    [Min(1f)]
    public float charactersPerSecond = 30f;

    [Tooltip("面板激活后，等待多少秒再开始播放文字。")]
    [Min(0f)]
    public float startDelay = 0f;

    [Tooltip("开启后，打字动画不受 Time.timeScale 影响。")]
    public bool useUnscaledTime = true;

    private Coroutine typewriterCoroutine;
    private string fullText = string.Empty;

    private void Awake()
    {
        if (targetText != null)
        {
            // 中文备注：
            // 保存 Inspector 中原本填写的完整文字。
            fullText = targetText.text;
        }
    }

    private void OnEnable()
    {
        if (targetText == null)
        {
            Debug.LogWarning(
                "TypewriterOnEnable: Target Text is not assigned.",
                this
            );

            return;
        }

        // 中文备注：
        // 如果运行过程中修改过文字，
        // 激活时读取当前完整文字。
        if (!string.IsNullOrEmpty(targetText.text))
        {
            fullText = targetText.text;
        }

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        typewriterCoroutine = StartCoroutine(
            PlayTypewriter()
        );
    }

    private void OnDisable()
    {
        // 中文备注：
        // 面板关闭时停止尚未完成的动画。
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
    }

    /// <summary>
    /// 中文备注：
    /// 播放一次打字机动画，完成后保留全部文字。
    /// </summary>
    private IEnumerator PlayTypewriter()
    {
        targetText.text = fullText;
        targetText.maxVisibleCharacters = 0;
        targetText.ForceMeshUpdate();

        if (startDelay > 0f)
        {
            if (useUnscaledTime)
            {
                yield return new WaitForSecondsRealtime(
                    startDelay
                );
            }
            else
            {
                yield return new WaitForSeconds(
                    startDelay
                );
            }
        }

        targetText.ForceMeshUpdate();

        int totalCharacters =
            targetText.textInfo.characterCount;

        float displayedCharacterAmount = 0f;

        while (targetText.maxVisibleCharacters <
               totalCharacters)
        {
            float deltaTime = useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            displayedCharacterAmount +=
                charactersPerSecond * deltaTime;

            targetText.maxVisibleCharacters =
                Mathf.Min(
                    Mathf.FloorToInt(
                        displayedCharacterAmount
                    ),
                    totalCharacters
                );

            yield return null;
        }

        // 中文备注：
        // 动画完成后确保完整文字显示。
        targetText.maxVisibleCharacters =
            int.MaxValue;

        typewriterCoroutine = null;
    }

    /// <summary>
    /// 中文备注：
    /// 可以由其他脚本在面板激活前设置新的文字。
    /// </summary>
    public void SetText(string newText)
    {
        fullText = newText ?? string.Empty;

        if (targetText != null)
        {
            targetText.text = fullText;
        }
    }
}