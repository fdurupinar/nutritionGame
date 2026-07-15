using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 中文备注：
/// 负责检测游戏是否到达最后一天，并在游戏场景中先显示结局提示面板。
///
/// 支持两种显示提示面板的方式：
/// 1. 当前天数达到 DayManager.maxDay，并且 HasPostedToday == 1 时自动显示。
/// 2. 当前天数达到 DayManager.maxDay，并点击指定按钮时显示。
///
/// 玩家点击提示面板中的确认按钮后，才会：
/// 1. 保存最终指标。
/// 2. 创建待显示结局标记。
/// 3. 加载结局场景。
/// </summary>
public class EndingTransitionManager : MonoBehaviour
{
    [Header("结局场景设置")]

    [Tooltip(
        "结局场景在 Build Settings 中的 Build Index。" +
        "请填写你当前已经可以正常加载结局的场景序号。"
    )]
    [Min(0)]
    public int endingSceneBuildIndex = 1;

    [Header("游戏场景结局提示面板")]

    [Tooltip(
        "拖入最后一天结束后，需要先显示的提示面板。" +
        "这个面板应该位于游戏场景的 Canvas 中。"
    )]
    public GameObject preEndingPanel;

    [Tooltip(
        "进入游戏场景时自动隐藏结局提示面板。" +
        "建议保持开启。"
    )]
    public bool hidePreEndingPanelOnStart = true;

    [Header("最后一天发布后自动显示面板")]

    [Tooltip(
        "开启后，当 currentDay 达到 maxDay，" +
        "并且 HasPostedToday == 1 时，自动显示结局提示面板。"
    )]
    public bool automaticallyCheckFinalPostedState = true;

    [Tooltip(
        "检测到最后一天已经发布后，等待多少秒再显示提示面板。" +
        "可以用来等待最终发布动画和指标动画播放完成。"
    )]
    [Min(0f)]
    public float automaticLoadDelay = 0f;

    [Tooltip(
        "开启后，等待时间不受 Time.timeScale 影响。"
    )]
    public bool useUnscaledTimeForDelay = true;

    [Header("运行时状态")]

    [Tooltip(
        "运行时只读。提示面板当前打开时为 true。"
    )]
    [SerializeField]
    private bool isPreEndingPanelOpen;

    [Tooltip(
        "运行时只读。当前最后一天条件中是否已经显示过提示面板。" +
        "防止面板每帧重复打开。"
    )]
    [SerializeField]
    private bool hasShownPreEndingPanel;

    [Tooltip(
        "运行时只读。正在切换结局场景时为 true，防止重复加载。"
    )]
    [SerializeField]
    private bool isLoadingEndingScene;

    // 中文备注：
    // PublishFlagSaver 当前使用的每日发布保存键。
    private const string HasPostedTodayKey = "HasPostedToday";

    // 中文备注：
    // 用于通知 EndingSceneAutoResolver：
    // 当前是从游戏最后一天进入结局场景。
    public const string PendingEndingKey =
        "PendingEndingStory";

    // 中文备注：
    // 用于把最后一天的最终指标传递到结局场景。
    public const string FinalMoneyKey =
        "PendingEnding_Money";

    public const string FinalFollowersKey =
        "PendingEnding_Followers";

    public const string FinalCredibilityKey =
        "PendingEnding_Credibility";

    // 中文备注：
    // GlobalStatManager 原本使用的 PlayerPrefs 保存键。
    private const string SavedCashKey =
        "User_Cash";

    private const string SavedFollowersKey =
        "User_Followers";

    private const string SavedCredibilityKey =
        "User_Credibility";

    private Coroutine automaticPanelCoroutine;

    private void Awake()
    {
        // 中文备注：
        // 游戏场景开始时先隐藏提示面板。
        if (hidePreEndingPanelOnStart &&
            preEndingPanel != null)
        {
            preEndingPanel.SetActive(false);
        }

        isPreEndingPanelOpen = false;
        hasShownPreEndingPanel = false;
        isLoadingEndingScene = false;
    }

    private void OnEnable()
    {
        CheckAutomaticEndingPanel();
    }

    private void Update()
    {
        if (!automaticallyCheckFinalPostedState ||
            isLoadingEndingScene)
        {
            return;
        }

        CheckAutomaticEndingPanel();
    }

    private void OnDisable()
    {
        StopAutomaticPanelCoroutine();
    }

    /// <summary>
    /// 中文备注：
    /// 检查：
    /// currentDay >= maxDay
    /// 并且
    /// HasPostedToday == 1
    ///
    /// 满足条件时只显示提示面板，不会直接加载场景。
    /// </summary>
    private void CheckAutomaticEndingPanel()
    {
        bool finalPostCondition =
            IsFinalDay() &&
            HasPostedToday();

        if (!finalPostCondition)
        {
            StopAutomaticPanelCoroutine();

            // 中文备注：
            // 条件重置后，允许下一次达到最后一天时重新显示面板。
            hasShownPreEndingPanel = false;

            return;
        }

        if (hasShownPreEndingPanel ||
            isPreEndingPanelOpen ||
            automaticPanelCoroutine != null)
        {
            return;
        }

        automaticPanelCoroutine =
            StartCoroutine(
                AutomaticShowPanelRoutine()
            );
    }

    /// <summary>
    /// 中文备注：
    /// 等待最终发布动画或指标动画结束后，
    /// 再显示游戏场景中的结局提示面板。
    /// </summary>
    private IEnumerator AutomaticShowPanelRoutine()
    {
        if (automaticLoadDelay > 0f)
        {
            if (useUnscaledTimeForDelay)
            {
                yield return new WaitForSecondsRealtime(
                    automaticLoadDelay
                );
            }
            else
            {
                yield return new WaitForSeconds(
                    automaticLoadDelay
                );
            }
        }
        else
        {
            // 中文备注：
            // 即使延迟为 0，也等待一帧，
            // 确保发布状态和指标已经保存完成。
            yield return null;
        }

        automaticPanelCoroutine = null;

        // 中文备注：
        // 等待结束后再次检查条件。
        if (IsFinalDay() &&
            HasPostedToday())
        {
            ShowPreEndingPanel(
                "Final day was posted."
            );
        }
    }

    /// <summary>
    /// 中文备注：
    /// 手动检查“最后一天 + 已经发布”，
    /// 满足条件后显示提示面板。
    ///
    /// 如果之前有按钮绑定到旧版本的这个方法，
    /// 原来的 UnityEvent 引用仍然可以继续使用。
    /// </summary>
    public void TryLoadEndingAfterFinalPost()
    {
        TryShowEndingPanelAfterFinalPost();
    }

    /// <summary>
    /// 中文备注：
    /// 手动检查最后一天是否已经发布。
    /// 满足条件时显示提示面板，不直接加载场景。
    /// </summary>
    public void TryShowEndingPanelAfterFinalPost()
    {
        if (!IsFinalDay())
        {
            Debug.Log(
                "EndingTransitionManager: " +
                "Current day has not reached DayManager.maxDay."
            );

            return;
        }

        if (!HasPostedToday())
        {
            Debug.Log(
                "EndingTransitionManager: " +
                "HasPostedToday is not 1, " +
                "so the pre-ending panel was not shown."
            );

            return;
        }

        ShowPreEndingPanel(
            "TryShowEndingPanelAfterFinalPost was called."
        );
    }

    /// <summary>
    /// 中文备注：
    /// 可以把游戏场景中的最后一天按钮绑定到这个方法。
    ///
    /// 只检查：
    /// currentDay >= maxDay
    ///
    /// 不要求 HasPostedToday == 1。
    /// </summary>
    public void ShowEndingPanelFromFinalDayButton()
    {
        if (!IsFinalDay())
        {
            Debug.Log(
                "EndingTransitionManager: " +
                "The button was clicked before the final day, " +
                "so the pre-ending panel was not shown."
            );

            return;
        }

        ShowPreEndingPanel(
            "Final-day button was clicked."
        );
    }

    /// <summary>
    /// 中文备注：
    /// 保留旧版本的方法名，防止 Inspector 中已经绑定的按钮失效。
    ///
    /// 旧版本会直接加载结局场景。
    /// 新版本改为先显示提示面板。
    /// </summary>
    public void LoadEndingFromFinalDayButton()
    {
        ShowEndingPanelFromFinalDayButton();
    }

    /// <summary>
    /// 中文备注：
    /// 把提示面板中的“查看结局”或“继续”按钮
    /// OnClick 绑定到这个方法。
    ///
    /// 点击后才会真正加载结局场景。
    /// </summary>
    public void ConfirmLoadEndingScene()
    {
        if (isLoadingEndingScene)
        {
            return;
        }

        if (!IsFinalDay())
        {
            Debug.LogWarning(
                "EndingTransitionManager: " +
                "Cannot load the ending scene because " +
                "the current day has not reached maxDay."
            );

            return;
        }

        BeginEndingTransition(
            "Player confirmed the pre-ending panel."
        );
    }

    /// <summary>
    /// 中文备注：
    /// 可以把提示面板中的关闭或取消按钮绑定到这个方法。
    ///
    /// 自动检测不会立即重新打开面板。
    /// 玩家仍然可以通过最后一天按钮再次打开。
    /// </summary>
    public void ClosePreEndingPanel()
    {
        if (preEndingPanel != null)
        {
            preEndingPanel.SetActive(false);
        }

        isPreEndingPanelOpen = false;

        Debug.Log(
            "EndingTransitionManager: " +
            "Pre-ending panel closed."
        );
    }

    /// <summary>
    /// 中文备注：
    /// 显示游戏场景中的结局提示面板。
    /// </summary>
    private void ShowPreEndingPanel(string reason)
    {
        if (isLoadingEndingScene)
        {
            return;
        }

        StopAutomaticPanelCoroutine();

        // 中文备注：
        // 无论面板引用是否存在，都记录已经尝试显示，
        // 防止缺少引用时每帧重复输出错误。
        hasShownPreEndingPanel = true;

        if (preEndingPanel == null)
        {
            Debug.LogError(
                "EndingTransitionManager: " +
                "Pre Ending Panel is not assigned. " +
                "Create the panel in the gameplay scene " +
                "and assign it in the Inspector."
            );

            return;
        }

        preEndingPanel.SetActive(true);
        isPreEndingPanelOpen = true;

        Debug.Log(
            "EndingTransitionManager: " +
            "Pre-ending panel shown. Reason: " +
            reason
        );
    }

    /// <summary>
    /// 中文备注：
    /// 判断当前是否已经到达 DayManager 设置的最大天数。
    ///
    /// 使用 >= 而不是 ==，
    /// 避免测试时 currentDay 超过 maxDay 后无法触发。
    /// </summary>
    public bool IsFinalDay()
    {
        if (DayManager.Instance == null)
        {
            return false;
        }

        return DayManager.Instance.currentDay >=
               DayManager.Instance.maxDay;
    }

    /// <summary>
    /// 中文备注：
    /// 读取项目现有的每日发布标记。
    /// </summary>
    public bool HasPostedToday()
    {
        return PlayerPrefs.GetInt(
            HasPostedTodayKey,
            0
        ) == 1;
    }

    /// <summary>
    /// 中文备注：
    /// 保存最终指标和待显示标记，然后加载结局场景。
    /// </summary>
    private void BeginEndingTransition(string reason)
    {
        if (isLoadingEndingScene)
        {
            return;
        }

        // 中文备注：
        // 防止脚本被错误放进结局场景后重复加载自己。
        if (SceneManager.GetActiveScene().buildIndex ==
            endingSceneBuildIndex)
        {
            Debug.LogWarning(
                "EndingTransitionManager: " +
                "The active scene is already the ending scene. " +
                "Scene loading was cancelled."
            );

            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(
                endingSceneBuildIndex))
        {
            Debug.LogError(
                "EndingTransitionManager: Scene Build Index " +
                endingSceneBuildIndex +
                " cannot be loaded. " +
                "Check File > Build Settings > Scenes In Build."
            );

            return;
        }

        isLoadingEndingScene = true;

        if (preEndingPanel != null)
        {
            preEndingPanel.SetActive(false);
        }

        isPreEndingPanelOpen = false;

        // 中文备注：
        // 加载结局场景之前先保存最终指标。
        SaveFinalMetricSnapshot();

        PlayerPrefs.SetInt(
            PendingEndingKey,
            1
        );

        PlayerPrefs.Save();

        Debug.Log(
            "EndingTransitionManager: Loading ending scene " +
            endingSceneBuildIndex +
            ". Reason: " +
            reason
        );

        SceneManager.LoadScene(
            endingSceneBuildIndex
        );
    }

    /// <summary>
    /// 中文备注：
    /// 优先读取 GlobalStatManager 当前值。
    ///
    /// 如果当前场景中找不到 GlobalStatManager，
    /// 则读取它保存在 PlayerPrefs 中的数据。
    /// </summary>
    private void SaveFinalMetricSnapshot()
    {
        int money;
        int followers;
        int credibility;

        if (GlobalStatManager.Instance != null)
        {
            money =
                GlobalStatManager.Instance.currentCash;

            followers =
                GlobalStatManager.Instance.currentFollowers;

            credibility =
                GlobalStatManager.Instance.currentCredibility;
        }
        else
        {
            money = PlayerPrefs.GetInt(
                SavedCashKey,
                1000
            );

            followers = PlayerPrefs.GetInt(
                SavedFollowersKey,
                0
            );

            credibility = PlayerPrefs.GetInt(
                SavedCredibilityKey,
                100
            );
        }

        PlayerPrefs.SetInt(
            FinalMoneyKey,
            money
        );

        PlayerPrefs.SetInt(
            FinalFollowersKey,
            followers
        );

        PlayerPrefs.SetInt(
            FinalCredibilityKey,
            credibility
        );

        Debug.Log(
            "EndingTransitionManager: " +
            "Final metric snapshot saved. " +
            "Money = " +
            money +
            ", Followers = " +
            followers +
            ", Credibility = " +
            credibility
        );
    }

    /// <summary>
    /// 中文备注：
    /// 停止等待显示提示面板的协程。
    /// </summary>
    private void StopAutomaticPanelCoroutine()
    {
        if (automaticPanelCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            automaticPanelCoroutine
        );

        automaticPanelCoroutine = null;
    }
}