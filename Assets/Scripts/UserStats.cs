using UnityEngine;
using TMPro;
using System;
using System.Collections;

public class UserStats : MonoBehaviour
{
    [Header("Metric Text References")]
    [SerializeField] private TextMeshProUGUI _followerText;
    [SerializeField] private TextMeshProUGUI _cashText;
    [SerializeField] private TextMeshProUGUI _credibilityText;
    [SerializeField] private TextMeshProUGUI _likesText;

    [Header("Juicy Metric Update Animation")]
    [Tooltip("指标数字从旧值滚动到新值所需的时间。")]
    [SerializeField] private float _metricAnimationDuration = 1.25f;

    [Tooltip("控制数字滚动的速度曲线。默认会先加速再减速。")]
    [SerializeField] private AnimationCurve _metricAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("数字更新时的最大弹跳缩放。1.18代表放大到118%。")]
    [SerializeField] private float _metricPunchScale = 1.18f;

    [Tooltip("一次完整更新过程中弹跳多少次。")]
    [SerializeField] private float _metricPunchCount = 5f;

    [Tooltip("数字更新音效重复播放的间隔时间。")]
    [SerializeField] private float _metricSoundRepeatInterval = 0.10f;

    [Tooltip("开启后，即使Time.timeScale为0，指标动画和音效仍然会播放。")]
    [SerializeField] private bool _useUnscaledTime = true;

    [Header("Legacy Single-Stat Animation")]
    [Tooltip("其他旧脚本单独修改一个指标时，每一步数字变化的等待时间。")]
    [SerializeField] private float _singleStatStepDelay = 0.02f;

    private AudioManager _audioManager;

    // 中文备注：保留原有的单指标协程，避免影响其他已经直接修改属性的脚本。
    private Coroutine _followerRoutine;
    private Coroutine _cashRoutine;
    private Coroutine _credibilityRoutine;
    private Coroutine _likesRoutine;

    // 中文备注：发布后的四项指标会使用同一个总协程一起动画，方便TacticManager等待全部结束。
    private Coroutine _allMetricsRoutine;

    private Vector3 _followerOriginalScale = Vector3.one;
    private Vector3 _cashOriginalScale = Vector3.one;
    private Vector3 _credibilityOriginalScale = Vector3.one;
    private Vector3 _likesOriginalScale = Vector3.one;

    private int _followerCnt;
    private int _cash;
    private int _credibility = 100;
    private int _likes;

    /// <summary>
    /// 中文备注：TacticManager会等待此值变为false，再弹出Home按钮。
    /// </summary>
    public bool IsMetricAnimationRunning { get; private set; }

    /// <summary>
    /// 中文备注：当所有指标动画完成时触发。保留给其他系统使用。
    /// </summary>
    public event Action MetricAnimationCompleted;

    public int FollowerCount
    {
        get => _followerCnt;
        set => StartSingleStatAnimation(
            MetricKind.Followers,
            value,
            _followerText,
            () => _followerCnt,
            newValue => _followerCnt = newValue,
            _followerOriginalScale);
    }

    public int Cash
    {
        get => _cash;
        set => StartSingleStatAnimation(
            MetricKind.Cash,
            value,
            _cashText,
            () => _cash,
            newValue => _cash = newValue,
            _cashOriginalScale);
    }

    public int Credibility
    {
        get => _credibility;
        set => StartSingleStatAnimation(
            MetricKind.Credibility,
            value,
            _credibilityText,
            () => _credibility,
            newValue => _credibility = newValue,
            _credibilityOriginalScale);
    }

    public int Likes
    {
        get => _likes;
        set => StartSingleStatAnimation(
            MetricKind.Likes,
            value,
            _likesText,
            () => _likes,
            newValue => _likes = newValue,
            _likesOriginalScale);
    }

    public int Level { get; set; }

    private void Awake()
    {
        CacheOriginalScales();
    }

    private void Start()
    {
        FindAudioManager();

        // 中文备注：进入场景时直接显示存档值，不播放发布后的滚动动画和重复音效。
        int startingCash = _cash;
        int startingFollowers = _followerCnt;
        int startingCredibility = _credibility;

        if (GlobalStatManager.Instance != null)
        {
            startingCash = GlobalStatManager.Instance.currentCash;
            startingFollowers = GlobalStatManager.Instance.currentFollowers;
            startingCredibility = GlobalStatManager.Instance.currentCredibility;
        }

        SetMetricsImmediate(startingCash, startingFollowers, startingCredibility, _likes);
        Level = 1;
    }

    /// <summary>
    /// 发布动画结束后调用。四项指标会同时滚动、弹跳，并重复播放音效直到动画结束。
    /// </summary>
    public void AnimateMetricsTo(int targetCash, int targetFollowers, int targetCredibility, int targetLikes)
    {
        StopAllMetricAnimations(false);

        if (_cash == targetCash &&
            _followerCnt == targetFollowers &&
            _credibility == targetCredibility &&
            _likes == targetLikes)
        {
            SetValuesWithoutStopping(targetCash, targetFollowers, targetCredibility, targetLikes);
            ResetAllTextScales();
            IsMetricAnimationRunning = false;
            MetricAnimationCompleted?.Invoke();
            return;
        }

        _allMetricsRoutine = StartCoroutine(AnimateAllMetricsRoutine(
            targetCash,
            targetFollowers,
            targetCredibility,
            targetLikes));
    }

    /// <summary>
    /// 立即设置全部指标，不播放动画。用于初始化或明确需要瞬间更新的地方。
    /// </summary>
    public void SetMetricsImmediate(int cash, int followers, int credibility, int likes)
    {
        StopAllMetricAnimations(false);

        _cash = cash;
        _followerCnt = followers;
        _credibility = credibility;
        _likes = likes;

        RefreshAllTexts();
        ResetAllTextScales();
        IsMetricAnimationRunning = false;
    }

    /// <summary>
    /// 停止所有指标动画。通常不需要从Inspector调用。
    /// </summary>
    public void StopMetricAnimations()
    {
        StopAllMetricAnimations(false);
    }

    private IEnumerator AnimateAllMetricsRoutine(
        int targetCash,
        int targetFollowers,
        int targetCredibility,
        int targetLikes)
    {
        IsMetricAnimationRunning = true;

        int startCash = _cash;
        int startFollowers = _followerCnt;
        int startCredibility = _credibility;
        int startLikes = _likes;

        bool cashChanges = startCash != targetCash;
        bool followerChanges = startFollowers != targetFollowers;
        bool credibilityChanges = startCredibility != targetCredibility;
        bool likesChanges = startLikes != targetLikes;
        bool anythingChanges = cashChanges || followerChanges || credibilityChanges || likesChanges;

        if (!anythingChanges)
        {
            SetValuesWithoutStopping(targetCash, targetFollowers, targetCredibility, targetLikes);
            FinishAllMetricAnimation();
            yield break;
        }

        float duration = Mathf.Max(0.01f, _metricAnimationDuration);
        float elapsed = 0f;
        float soundTimer = _metricSoundRepeatInterval;

        while (elapsed < duration)
        {
            float deltaTime = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += deltaTime;

            float normalized = Mathf.Clamp01(elapsed / duration);
            float curved = _metricAnimationCurve != null
                ? Mathf.Clamp01(_metricAnimationCurve.Evaluate(normalized))
                : normalized;

            _cash = Mathf.RoundToInt(Mathf.Lerp(startCash, targetCash, curved));
            _followerCnt = Mathf.RoundToInt(Mathf.Lerp(startFollowers, targetFollowers, curved));
            _credibility = Mathf.RoundToInt(Mathf.Lerp(startCredibility, targetCredibility, curved));
            _likes = Mathf.RoundToInt(Mathf.Lerp(startLikes, targetLikes, curved));

            RefreshAllTexts();
            ApplyJuicyTextPunch(normalized, cashChanges, followerChanges, credibilityChanges, likesChanges);

            soundTimer += deltaTime;
            if (soundTimer >= Mathf.Max(0.02f, _metricSoundRepeatInterval))
            {
                soundTimer = 0f;
                PlayMetricUpdateSound();
            }

            yield return null;
        }

        SetValuesWithoutStopping(targetCash, targetFollowers, targetCredibility, targetLikes);
        FinishAllMetricAnimation();
    }

    private void FinishAllMetricAnimation()
    {
        _allMetricsRoutine = null;
        ResetAllTextScales();
        IsMetricAnimationRunning = false;
        MetricAnimationCompleted?.Invoke();
    }

    private void ApplyJuicyTextPunch(
        float normalized,
        bool cashChanges,
        bool followerChanges,
        bool credibilityChanges,
        bool likesChanges)
    {
        // 中文备注：使用逐渐衰减的正弦波制造“数字跳动”效果，不需要DOTween插件。
        float wave = Mathf.Abs(Mathf.Sin(normalized * Mathf.PI * Mathf.Max(1f, _metricPunchCount)));
        float decay = 1f - normalized;
        float extraScale = wave * decay * Mathf.Max(0f, _metricPunchScale - 1f);
        float scaleMultiplier = 1f + extraScale;

        SetTextScale(_cashText, _cashOriginalScale, cashChanges ? scaleMultiplier : 1f);
        SetTextScale(_followerText, _followerOriginalScale, followerChanges ? scaleMultiplier : 1f);
        SetTextScale(_credibilityText, _credibilityOriginalScale, credibilityChanges ? scaleMultiplier : 1f);
        SetTextScale(_likesText, _likesOriginalScale, likesChanges ? scaleMultiplier : 1f);
    }

    private enum MetricKind
    {
        Followers,
        Cash,
        Credibility,
        Likes
    }

    private void StartSingleStatAnimation(
        MetricKind kind,
        int targetValue,
        TextMeshProUGUI textElement,
        Func<int> getter,
        Action<int> setter,
        Vector3 originalScale)
    {
        // 中文备注：若发布后的四指标总动画正在运行，新的单项修改会先安全停止总动画。
        if (_allMetricsRoutine != null)
        {
            StopAllMetricAnimations(false);
        }

        StopSingleRoutine(kind);

        if (getter() == targetValue)
        {
            setter(targetValue);
            SetTextValue(textElement, targetValue);
            RefreshRunningState();
            return;
        }

        Coroutine routine = StartCoroutine(UpdateSingleStatCoroutine(
            kind,
            targetValue,
            textElement,
            getter,
            setter,
            originalScale));

        SetSingleRoutine(kind, routine);
        RefreshRunningState();
    }

    private IEnumerator UpdateSingleStatCoroutine(
        MetricKind kind,
        int targetValue,
        TextMeshProUGUI textElement,
        Func<int> getter,
        Action<int> setter,
        Vector3 originalScale)
    {
        int currentValue = getter();

        int difference = Mathf.Abs(targetValue - currentValue);
        int stepAmount = Mathf.Max(1, difference / 40);
        int step = targetValue > currentValue ? stepAmount : -stepAmount;

        while (currentValue != targetValue)
        {
            if (Mathf.Abs(targetValue - currentValue) <= Mathf.Abs(step))
            {
                currentValue = targetValue;
            }
            else
            {
                currentValue += step;
            }

            setter(currentValue);
            SetTextValue(textElement, currentValue);

            if (textElement != null)
            {
                textElement.transform.localScale = originalScale * _metricPunchScale;
            }

            PlayMetricUpdateSound();

            float waitTime = Mathf.Max(0f, _singleStatStepDelay);
            if (waitTime > 0f)
            {
                if (_useUnscaledTime)
                {
                    yield return new WaitForSecondsRealtime(waitTime);
                }
                else
                {
                    yield return new WaitForSeconds(waitTime);
                }
            }
            else
            {
                yield return null;
            }

            if (textElement != null)
            {
                textElement.transform.localScale = originalScale;
            }
        }

        SetSingleRoutine(kind, null);
        RefreshRunningState();
    }

    private void StopSingleRoutine(MetricKind kind)
    {
        Coroutine routine = GetSingleRoutine(kind);
        if (routine != null)
        {
            StopCoroutine(routine);
            SetSingleRoutine(kind, null);
        }
    }

    private Coroutine GetSingleRoutine(MetricKind kind)
    {
        switch (kind)
        {
            case MetricKind.Followers: return _followerRoutine;
            case MetricKind.Cash: return _cashRoutine;
            case MetricKind.Credibility: return _credibilityRoutine;
            case MetricKind.Likes: return _likesRoutine;
            default: return null;
        }
    }

    private void SetSingleRoutine(MetricKind kind, Coroutine routine)
    {
        switch (kind)
        {
            case MetricKind.Followers:
                _followerRoutine = routine;
                break;
            case MetricKind.Cash:
                _cashRoutine = routine;
                break;
            case MetricKind.Credibility:
                _credibilityRoutine = routine;
                break;
            case MetricKind.Likes:
                _likesRoutine = routine;
                break;
        }
    }

    private void RefreshRunningState()
    {
        bool running = _allMetricsRoutine != null ||
                       _followerRoutine != null ||
                       _cashRoutine != null ||
                       _credibilityRoutine != null ||
                       _likesRoutine != null;

        bool wasRunning = IsMetricAnimationRunning;
        IsMetricAnimationRunning = running;

        if (wasRunning && !running)
        {
            ResetAllTextScales();
            MetricAnimationCompleted?.Invoke();
        }
    }

    private void StopAllMetricAnimations(bool invokeCompletion)
    {
        bool wasRunning = IsMetricAnimationRunning;

        if (_allMetricsRoutine != null)
        {
            StopCoroutine(_allMetricsRoutine);
            _allMetricsRoutine = null;
        }

        StopRoutine(ref _followerRoutine);
        StopRoutine(ref _cashRoutine);
        StopRoutine(ref _credibilityRoutine);
        StopRoutine(ref _likesRoutine);

        ResetAllTextScales();
        IsMetricAnimationRunning = false;

        if (invokeCompletion && wasRunning)
        {
            MetricAnimationCompleted?.Invoke();
        }
    }

    private void StopRoutine(ref Coroutine routine)
    {
        if (routine == null)
        {
            return;
        }

        StopCoroutine(routine);
        routine = null;
    }

    private void SetValuesWithoutStopping(int cash, int followers, int credibility, int likes)
    {
        _cash = cash;
        _followerCnt = followers;
        _credibility = credibility;
        _likes = likes;
        RefreshAllTexts();
    }

    private void RefreshAllTexts()
    {
        SetTextValue(_cashText, _cash);
        SetTextValue(_followerText, _followerCnt);
        SetTextValue(_credibilityText, _credibility);
        SetTextValue(_likesText, _likes);
    }

    private void SetTextValue(TextMeshProUGUI textElement, int value)
    {
        if (textElement != null)
        {
            textElement.text = value.ToString();
        }
    }

    private void CacheOriginalScales()
    {
        if (_followerText != null) _followerOriginalScale = _followerText.transform.localScale;
        if (_cashText != null) _cashOriginalScale = _cashText.transform.localScale;
        if (_credibilityText != null) _credibilityOriginalScale = _credibilityText.transform.localScale;
        if (_likesText != null) _likesOriginalScale = _likesText.transform.localScale;
    }

    private void ResetAllTextScales()
    {
        SetTextScale(_followerText, _followerOriginalScale, 1f);
        SetTextScale(_cashText, _cashOriginalScale, 1f);
        SetTextScale(_credibilityText, _credibilityOriginalScale, 1f);
        SetTextScale(_likesText, _likesOriginalScale, 1f);
    }

    private void SetTextScale(TextMeshProUGUI textElement, Vector3 originalScale, float multiplier)
    {
        if (textElement != null)
        {
            textElement.transform.localScale = originalScale * multiplier;
        }
    }

    private void FindAudioManager()
    {
        if (_audioManager != null)
        {
            return;
        }

        GameObject audioObj = GameObject.Find("AudioManager");
        if (audioObj != null)
        {
            _audioManager = audioObj.GetComponent<AudioManager>();
        }

        if (_audioManager == null)
        {
            _audioManager = FindFirstObjectByType<AudioManager>();
        }
    }

    private void PlayMetricUpdateSound()
    {
        FindAudioManager();

        if (_audioManager != null)
        {
            _audioManager.PlayEngagamentNotification();
        }
    }
}
