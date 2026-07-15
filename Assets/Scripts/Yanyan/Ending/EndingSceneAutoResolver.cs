using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 中文备注：
/// 放在结局场景中的自动结局解析脚本。
///
/// 当 EndingTransitionManager 加载结局场景后，本脚本会：
/// 1. 读取最终 Money、Followers、Credibility。
/// 2. 根据 EndingStoryCollectionPanel 的公式选择结局。
/// 3. 永久解锁对应结局。
/// 4. 激活结局收藏界面。
/// 5. 激活 Story Panel。
/// 6. 显示对应 Title。
/// 7. 使用打字机动画显示对应 Story。
/// </summary>
public class EndingSceneAutoResolver : MonoBehaviour
{
    [Header("结局界面引用")]

    [Tooltip(
        "拖入结局场景中现有的 EndingStoryCollectionPanel。" +
        "如果没有拖入，运行时会自动查找，包括未激活的对象。"
    )]
    public EndingStoryCollectionPanel endingStoryCollectionPanel;

    [Tooltip(
        "结局收藏界面的最外层父物体。" +
        "进入结局场景后会先激活这个物体。" +
        "如果 EndingStoryCollectionPanel 一开始已经激活，可以不填写。"
    )]
    public GameObject endingCollectionRoot;

    [Header("自动读取设置")]

    [Tooltip(
        "开启后，只有 EndingTransitionManager 创建了待显示结局标记时，" +
        "本脚本才会自动显示结局。"
    )]
    public bool requirePendingEndingRequest = true;

    [Tooltip(
        "进入结局场景后额外等待的时间。" +
        "即使设置为 0，脚本仍然会至少等待一帧，" +
        "确保其他脚本的 Start 和 OnEnable 已经执行完成。"
    )]
    [Min(0f)]
    public float additionalStartDelay = 0f;

    [Tooltip(
        "成功读取结局后清除待显示标记，" +
        "防止以后进入这个场景时自动重复打开结局。"
    )]
    public bool clearPendingRequestAfterSuccess = true;

    [Header("故事自动显示")]

    [Tooltip(
        "进入结局场景时自动激活 Story Panel。" +
        "建议保持开启。"
    )]
    public bool automaticallyOpenStoryPanel = true;

    [Tooltip(
        "进入结局场景时始终播放 Story Text 打字机动画。" +
        "即使这个结局以前已经解锁，也会重新播放动画。"
    )]
    public bool alwaysPlayStoryTypewriter = true;

    [Tooltip(
        "打字动画完成后，确保完整故事文字全部显示。"
    )]
    public bool forceCompleteTextAfterTyping = true;

    [Header("运行时状态")]

    [Tooltip(
        "运行时只读。正在处理结局时会变成 true，防止重复运行。"
    )]
    [SerializeField]
    private bool isResolvingEnding;

    [Tooltip(
        "运行时只读。当前自动显示的结局序号。1 表示第一个结局。"
    )]
    [SerializeField]
    private int resolvedEndingNumber;

    // 中文备注：
    // GlobalStatManager 当前使用的 PlayerPrefs 保存键。
    // 当 EndingTransitionManager 的最终快照不存在时，作为备用数据。
    private const string SavedCashKey = "User_Cash";
    private const string SavedFollowersKey = "User_Followers";
    private const string SavedCredibilityKey = "User_Credibility";

    private Coroutine resolveCoroutine;

    private IEnumerator Start()
    {
        // 中文备注：
        // 必须等待一帧，让场景中其他组件先运行 Awake、OnEnable 和 Start。
        // EndingStoryCollectionPanel.Start() 可能会关闭 Story Panel，
        // 所以不能在同一帧过早打开 Story Panel。
        yield return null;

        if (additionalStartDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                additionalStartDelay
            );
        }

        ResolvePendingEnding();
    }

    private void OnDisable()
    {
        if (resolveCoroutine != null)
        {
            StopCoroutine(resolveCoroutine);
            resolveCoroutine = null;
        }

        isResolvingEnding = false;
    }

    /// <summary>
    /// 中文备注：
    /// 开始读取和显示待处理的结局。
    ///
    /// 也可以把测试按钮 OnClick 绑定到这个方法。
    /// </summary>
    public void ResolvePendingEnding()
    {
        if (isResolvingEnding)
        {
            return;
        }

        if (resolveCoroutine != null)
        {
            StopCoroutine(resolveCoroutine);
        }

        resolveCoroutine = StartCoroutine(
            ResolvePendingEndingRoutine()
        );
    }

    /// <summary>
    /// 中文备注：
    /// 完整的结局解析流程。
    /// </summary>
    private IEnumerator ResolvePendingEndingRoutine()
    {
        isResolvingEnding = true;

        if (requirePendingEndingRequest &&
            PlayerPrefs.GetInt(
                EndingTransitionManager.PendingEndingKey,
                0
            ) != 1)
        {
            Debug.Log(
                "EndingSceneAutoResolver: " +
                "No pending ending request was found."
            );

            FinishResolving();
            yield break;
        }

        // 中文备注：
        // 先激活结局收藏界面的最外层父物体。
        if (endingCollectionRoot != null)
        {
            endingCollectionRoot.SetActive(true);
        }

        // 中文备注：
        // Inspector 没有指定时，自动查找组件。
        // true 表示同时查找未激活对象。
        if (endingStoryCollectionPanel == null)
        {
            endingStoryCollectionPanel =
                FindObjectOfType<EndingStoryCollectionPanel>(
                    true
                );
        }

        if (endingStoryCollectionPanel == null)
        {
            Debug.LogError(
                "EndingSceneAutoResolver: " +
                "No EndingStoryCollectionPanel was found. " +
                "Assign it in the Inspector or make sure it exists " +
                "inside the ending scene."
            );

            FinishResolving();
            yield break;
        }

        // 中文备注：
        // 如果 EndingStoryCollectionPanel 自己的 GameObject 没有激活，
        // 这里将它激活。
        if (!endingStoryCollectionPanel.gameObject.activeSelf)
        {
            endingStoryCollectionPanel.gameObject.SetActive(true);
        }

        // 中文备注：
        // 激活对象后再次等待一帧。
        // 这样 EndingStoryCollectionPanel 的 OnEnable 和 Start
        // 会先执行完成，不会在后面把刚打开的 Story Panel 再次关闭。
        yield return null;

        if (!endingStoryCollectionPanel.gameObject.activeInHierarchy)
        {
            Debug.LogError(
                "EndingSceneAutoResolver: " +
                "EndingStoryCollectionPanel is still inactive in the hierarchy. " +
                "Assign its active parent to Ending Collection Root."
            );

            FinishResolving();
            yield break;
        }

        int money = PlayerPrefs.GetInt(
            EndingTransitionManager.FinalMoneyKey,
            PlayerPrefs.GetInt(
                SavedCashKey,
                1000
            )
        );

        int followers = PlayerPrefs.GetInt(
            EndingTransitionManager.FinalFollowersKey,
            PlayerPrefs.GetInt(
                SavedFollowersKey,
                0
            )
        );

        int credibility = PlayerPrefs.GetInt(
            EndingTransitionManager.FinalCredibilityKey,
            PlayerPrefs.GetInt(
                SavedCredibilityKey,
                100
            )
        );

        // 中文备注：
        // 使用 EndingStoryCollectionPanel 原本的结局公式。
        int endingIndex =
            endingStoryCollectionPanel.GetEndingIndexFromStats(
                money,
                credibility,
                followers
            );

        resolvedEndingNumber = endingIndex + 1;

        // 中文备注：
        // 临时关闭 EndingStoryCollectionPanel 自己的首次解锁打字动画。
        //
        // 原因：
        // 本脚本会在解锁完成后统一播放一次打字动画。
        // 这样无论结局是第一次解锁还是已经解锁，
        // 都只会播放一次，不会出现两个动画互相冲突。
        bool originalFirstUnlockTypewriterSetting =
            endingStoryCollectionPanel
                .playTypewriterOnlyOnFirstUnlock;

        endingStoryCollectionPanel
            .playTypewriterOnlyOnFirstUnlock = false;

        EndingStoryData resolvedEnding = null;

        try
        {
            resolvedEnding =
                endingStoryCollectionPanel
                    .ResolveAndUnlockEndingFromStats(
                        money,
                        credibility,
                        followers
                    );
        }
        finally
        {
            // 中文备注：
            // 解锁结束后恢复原本 Inspector 设置，
            // 不影响玩家之后手动点击结局按钮时的行为。
            endingStoryCollectionPanel
                .playTypewriterOnlyOnFirstUnlock =
                originalFirstUnlockTypewriterSetting;
        }

        if (resolvedEnding == null)
        {
            Debug.LogError(
                "EndingSceneAutoResolver: " +
                "Ending index " +
                endingIndex +
                " was selected, but its EndingStoryData is missing. " +
                "Check EndingStoryDatabase and the 12 ending entries."
            );

            FinishResolving();
            yield break;
        }

        // 中文备注：
        // 确保 Locked Panel 不会挡住 Story Panel。
        endingStoryCollectionPanel.CloseLockedPanel();

        if (automaticallyOpenStoryPanel)
        {
            endingStoryCollectionPanel.OpenStoryPanel();

            if (endingStoryCollectionPanel.storyPanel != null)
            {
                endingStoryCollectionPanel.storyPanel.SetActive(true);
            }
        }

        // 中文备注：
        // 再等待一帧，确保 Story Panel 的 UI 布局和 TMP 网格已经创建。
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (alwaysPlayStoryTypewriter)
        {
            yield return PlayResolvedStoryTypewriter(
                resolvedEnding
            );
        }
        else
        {
            ShowResolvedStoryInstantly(
                resolvedEnding
            );
        }

        if (clearPendingRequestAfterSuccess)
        {
            PlayerPrefs.SetInt(
                EndingTransitionManager.PendingEndingKey,
                0
            );

            PlayerPrefs.Save();
        }

        Debug.Log(
            "EndingSceneAutoResolver: Ending " +
            resolvedEndingNumber +
            " was unlocked, Story Panel was opened, " +
            "and the corresponding story was displayed. Title: " +
            resolvedEnding.title +
            ". Final metrics -> Money: " +
            money +
            ", Followers: " +
            followers +
            ", Credibility: " +
            credibility
        );

        FinishResolving();
    }

    /// <summary>
    /// 中文备注：
    /// 使用 EndingStoryCollectionPanel 中现有的速度和延迟设置，
    /// 播放 Story Text 打字机动画。
    ///
    /// Title 会立即显示。
    /// Story 会逐字显示。
    /// </summary>
    private IEnumerator PlayResolvedStoryTypewriter(
        EndingStoryData endingData
    )
    {
        if (endingData == null)
        {
            yield break;
        }

        string title = endingData.title ?? string.Empty;
        string story = endingData.story ?? string.Empty;

        // 中文备注：
        // 确保 Story Panel 在打字期间始终激活。
        if (automaticallyOpenStoryPanel)
        {
            endingStoryCollectionPanel.OpenStoryPanel();

            if (endingStoryCollectionPanel.storyPanel != null)
            {
                endingStoryCollectionPanel.storyPanel.SetActive(true);
            }
        }

        endingStoryCollectionPanel.CloseLockedPanel();

        // 中文备注：
        // Title 不播放打字动画，进入 Story Panel 后立即显示。
        SetTitleText(title);

        TMP_Text storyTMP =
            endingStoryCollectionPanel.sharedStoryTMP;

        Text storyText =
            endingStoryCollectionPanel.sharedStoryText;

        int totalVisibleCharacters;

        if (storyTMP != null)
        {
            // 中文备注：
            // TMP 使用 maxVisibleCharacters，
            // 可以正常处理换行和富文本标签。
            storyTMP.text = story;
            storyTMP.ForceMeshUpdate(true, true);
            storyTMP.maxVisibleCharacters = 0;

            totalVisibleCharacters =
                storyTMP.textInfo.characterCount;
        }
        else
        {
            // 中文备注：
            // 如果没有 TMP，使用普通 Unity Text。
            totalVisibleCharacters = story.Length;
        }

        if (storyText != null)
        {
            storyText.text = string.Empty;
        }

        float startDelay = Mathf.Max(
            0f,
            endingStoryCollectionPanel.typewriterStartDelay
        );

        if (startDelay > 0f)
        {
            yield return WaitForSecondsUsingPanelSettings(
                startDelay
            );
        }

        float charactersPerSecond = Mathf.Max(
            1f,
            endingStoryCollectionPanel
                .typewriterCharactersPerSecond
        );

        float visibleCharacterProgress = 0f;
        int currentVisibleCharacters = 0;

        while (currentVisibleCharacters <
               totalVisibleCharacters)
        {
            // 中文备注：
            // 如果其他物体把 Story Panel 关闭，
            // 这里再次确保它处于激活状态。
            if (automaticallyOpenStoryPanel &&
                endingStoryCollectionPanel.storyPanel != null &&
                !endingStoryCollectionPanel.storyPanel.activeSelf)
            {
                endingStoryCollectionPanel.storyPanel.SetActive(
                    true
                );
            }

            float deltaTime =
                endingStoryCollectionPanel
                    .typewriterUseUnscaledTime
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

            visibleCharacterProgress +=
                charactersPerSecond * deltaTime;

            int newVisibleCharacterCount =
                Mathf.Clamp(
                    Mathf.FloorToInt(
                        visibleCharacterProgress
                    ),
                    0,
                    totalVisibleCharacters
                );

            if (newVisibleCharacterCount !=
                currentVisibleCharacters)
            {
                currentVisibleCharacters =
                    newVisibleCharacterCount;

                if (storyTMP != null)
                {
                    storyTMP.maxVisibleCharacters =
                        currentVisibleCharacters;
                }

                if (storyText != null)
                {
                    int safeLength = Mathf.Clamp(
                        currentVisibleCharacters,
                        0,
                        story.Length
                    );

                    storyText.text = story.Substring(
                        0,
                        safeLength
                    );
                }
            }

            yield return null;
        }

        if (forceCompleteTextAfterTyping)
        {
            SetCompleteStoryText(story);
        }
    }

    /// <summary>
    /// 中文备注：
    /// 不播放动画，立即显示完整 Title 和 Story。
    /// </summary>
    private void ShowResolvedStoryInstantly(
        EndingStoryData endingData
    )
    {
        if (endingData == null)
        {
            return;
        }

        SetTitleText(
            endingData.title ?? string.Empty
        );

        SetCompleteStoryText(
            endingData.story ?? string.Empty
        );
    }

    /// <summary>
    /// 中文备注：
    /// 设置结局标题。
    /// 同时支持 TMP_Text 和普通 Unity Text。
    /// </summary>
    private void SetTitleText(string title)
    {
        if (endingStoryCollectionPanel.sharedTitleTMP != null)
        {
            endingStoryCollectionPanel.sharedTitleTMP.text =
                title;
        }

        if (endingStoryCollectionPanel.sharedTitleText != null)
        {
            endingStoryCollectionPanel.sharedTitleText.text =
                title;
        }
    }

    /// <summary>
    /// 中文备注：
    /// 显示完整结局故事。
    /// </summary>
    private void SetCompleteStoryText(string story)
    {
        if (endingStoryCollectionPanel.sharedStoryTMP != null)
        {
            endingStoryCollectionPanel.sharedStoryTMP.text =
                story;

            endingStoryCollectionPanel.sharedStoryTMP
                .maxVisibleCharacters = int.MaxValue;

            endingStoryCollectionPanel.sharedStoryTMP
                .ForceMeshUpdate(true, true);
        }

        if (endingStoryCollectionPanel.sharedStoryText != null)
        {
            endingStoryCollectionPanel.sharedStoryText.text =
                story;
        }
    }

    /// <summary>
    /// 中文备注：
    /// 根据 EndingStoryCollectionPanel 的设置决定
    /// 使用正常时间还是不受 Time.timeScale 影响的时间。
    /// </summary>
    private IEnumerator WaitForSecondsUsingPanelSettings(
        float seconds
    )
    {
        float timer = 0f;

        while (timer < seconds)
        {
            timer +=
                endingStoryCollectionPanel
                    .typewriterUseUnscaledTime
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

            yield return null;
        }
    }

    /// <summary>
    /// 中文备注：
    /// 结束本次解析并恢复运行状态。
    /// </summary>
    private void FinishResolving()
    {
        isResolvingEnding = false;
        resolveCoroutine = null;
    }
}