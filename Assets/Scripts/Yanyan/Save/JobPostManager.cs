using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JobPostManager : MonoBehaviour
{
    [Serializable]
    public class JobPostData
    {
        [Header("Job Info")]
        [TextArea(2, 5)]
        public string jobText;

        [Tooltip("Which game day this job appears.")]
        public int availableDay = 1;

        [Tooltip("How many day changes before this job can be claimed.")]
        public int workDays = 1;

        [Tooltip("Money added when this job is claimed.")]
        public int rewardMoney = 100;
    }

    [Serializable]
    public class JobOfferSlot
    {
        public GameObject rootObject;
        public TextMeshProUGUI jobText;
        public TextMeshProUGUI rewardText;
        public TextMeshProUGUI dayText;
        public Button acceptButton;
    }

    [Header("Job Data")]
    public List<JobPostData> jobs = new List<JobPostData>();

    [Header("Job Panel Animation")]
    public GameObject jobPanel;
    public RectTransform jobPanelRect;
    public CanvasGroup jobPanelCanvasGroup;
    public float openCloseDuration = 0.25f;
    public float closedScale = 0.85f;

    [Header("Job Panel Lock Message")]
    public TextMeshProUGUI jobPanelLockedMessageText;
    public string jobPanelLockedMessage = "You already accepted a job. Claim the reward first.";

    [Header("Job Panel Button Sounds")]
    public AudioSource uiAudioSource;
    public AudioClip openPanelSound;
    public AudioClip lockedPanelSound;

    [Header("Job Offer UI")]
    public int maxJobsPerDay = 2;
    public List<JobOfferSlot> jobOfferSlots = new List<JobOfferSlot>();

    [Header("Accepted Job Display")]
    public GameObject acceptedJobObject;
    public TextMeshProUGUI acceptedJobText;
    public TextMeshProUGUI acceptedJobMoneyDisplayText;
    public TextMeshProUGUI acceptedJobDayLeftDisplayText;
    public Button claimRewardButton;

    [Header("Optional Stat UI Refresh")]
    public UserStats userStats;
    public StatSceneLink statSceneLink;

    private const string KEY_HAS_ACTIVE_JOB = "JobSystem_HasActiveJob";
    private const string KEY_ACTIVE_JOB_INDEX = "JobSystem_ActiveJobIndex";
    private const string KEY_ACTIVE_OFFER_SLOT_INDEX = "JobSystem_ActiveOfferSlotIndex";
    private const string KEY_REMAINING_DAYS = "JobSystem_RemainingDays";
    private const string KEY_LAST_CHECKED_DAY = "JobSystem_LastCheckedDay";
    private const string KEY_READY_TO_CLAIM = "JobSystem_ReadyToClaim";
    private const string KEY_COMPLETED_PREFIX = "JobSystem_Completed_";

    private int lastKnownDay = -1;
    private Coroutine panelAnimationCoroutine;
    private Vector3 originalPanelScale = Vector3.one;

    private void Start()
    {
        if (userStats == null)
            userStats = FindObjectOfType<UserStats>();

        if (statSceneLink == null)
            statSceneLink = FindObjectOfType<StatSceneLink>();

        SetupPanelReferences();
        SetupAudioSource();

        if (jobPanelRect != null)
            originalPanelScale = jobPanelRect.localScale;

        if (claimRewardButton != null)
        {
            claimRewardButton.onClick.RemoveAllListeners();
            claimRewardButton.onClick.AddListener(ClaimAcceptedJobReward);
        }

        lastKnownDay = GetCurrentDay();

        if (!PlayerPrefs.HasKey(KEY_LAST_CHECKED_DAY))
        {
            PlayerPrefs.SetInt(KEY_LAST_CHECKED_DAY, lastKnownDay);
            PlayerPrefs.Save();
        }

        CheckDayChange();
        RefreshAllUI();

        if (jobPanel != null)
            jobPanel.SetActive(false);
    }

    private void Update()
    {
        int currentDay = GetCurrentDay();

        if (currentDay != lastKnownDay)
        {
            lastKnownDay = currentDay;
            CheckDayChange();
            RefreshAllUI();
        }
    }

    private void SetupPanelReferences()
    {
        if (jobPanel == null)
            return;

        if (jobPanelRect == null)
            jobPanelRect = jobPanel.GetComponent<RectTransform>();

        if (jobPanelCanvasGroup == null)
        {
            jobPanelCanvasGroup = jobPanel.GetComponent<CanvasGroup>();

            if (jobPanelCanvasGroup == null)
                jobPanelCanvasGroup = jobPanel.AddComponent<CanvasGroup>();
        }
    }

    private void SetupAudioSource()
    {
        if (uiAudioSource == null)
            uiAudioSource = GetComponent<AudioSource>();

        if (uiAudioSource == null)
            uiAudioSource = gameObject.AddComponent<AudioSource>();

        uiAudioSource.playOnAwake = false;
    }

    private void PlayUISound(AudioClip clip)
    {
        if (uiAudioSource == null || clip == null)
            return;

        uiAudioSource.PlayOneShot(clip);
    }

    public void TryOpenJobPanel()
    {
        CheckDayChange();
        RefreshAllUI();

        if (IsJobPanelLocked())
        {
            PlayUISound(lockedPanelSound);

            if (jobPanelLockedMessageText != null)
                jobPanelLockedMessageText.text = jobPanelLockedMessage;

            Debug.Log("JobPostManager: Job panel locked. Claim the active job reward first.");
            return;
        }

        PlayUISound(openPanelSound);

        if (jobPanelLockedMessageText != null)
            jobPanelLockedMessageText.text = "";

        OpenJobPanelInternalWithAnimation();
    }

    public void OpenJobPanelWithAnimation()
    {
        TryOpenJobPanel();
    }

    private void OpenJobPanelInternalWithAnimation()
    {
        SetupPanelReferences();

        if (jobPanel == null)
            return;

        if (panelAnimationCoroutine != null)
            StopCoroutine(panelAnimationCoroutine);

        panelAnimationCoroutine = StartCoroutine(OpenPanelRoutine());
    }

    public void CloseJobPanelWithAnimation()
    {
        SetupPanelReferences();

        if (jobPanel == null)
            return;

        if (panelAnimationCoroutine != null)
            StopCoroutine(panelAnimationCoroutine);

        panelAnimationCoroutine = StartCoroutine(ClosePanelRoutine());
    }

    private IEnumerator OpenPanelRoutine()
    {
        jobPanel.SetActive(true);

        if (jobPanelCanvasGroup != null)
        {
            jobPanelCanvasGroup.alpha = 0f;
            jobPanelCanvasGroup.blocksRaycasts = false;
            jobPanelCanvasGroup.interactable = false;
        }

        if (jobPanelRect != null)
            jobPanelRect.localScale = originalPanelScale * closedScale;

        float timer = 0f;

        while (timer < openCloseDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / openCloseDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (jobPanelCanvasGroup != null)
                jobPanelCanvasGroup.alpha = smoothT;

            if (jobPanelRect != null)
                jobPanelRect.localScale = Vector3.Lerp(originalPanelScale * closedScale, originalPanelScale, smoothT);

            yield return null;
        }

        if (jobPanelCanvasGroup != null)
        {
            jobPanelCanvasGroup.alpha = 1f;
            jobPanelCanvasGroup.blocksRaycasts = true;
            jobPanelCanvasGroup.interactable = true;
        }

        if (jobPanelRect != null)
            jobPanelRect.localScale = originalPanelScale;
    }

    private IEnumerator ClosePanelRoutine()
    {
        if (jobPanelCanvasGroup != null)
        {
            jobPanelCanvasGroup.blocksRaycasts = false;
            jobPanelCanvasGroup.interactable = false;
        }

        float startAlpha = jobPanelCanvasGroup != null ? jobPanelCanvasGroup.alpha : 1f;
        Vector3 startScale = jobPanelRect != null ? jobPanelRect.localScale : originalPanelScale;
        Vector3 endScale = originalPanelScale * closedScale;

        float timer = 0f;

        while (timer < openCloseDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / openCloseDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (jobPanelCanvasGroup != null)
                jobPanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, smoothT);

            if (jobPanelRect != null)
                jobPanelRect.localScale = Vector3.Lerp(startScale, endScale, smoothT);

            yield return null;
        }

        if (jobPanelCanvasGroup != null)
            jobPanelCanvasGroup.alpha = 0f;

        if (jobPanelRect != null)
            jobPanelRect.localScale = originalPanelScale;

        jobPanel.SetActive(false);
    }

    private bool IsJobPanelLocked()
    {
        return HasActiveJob();
    }

    public void RefreshAllUI()
    {
        RefreshAcceptedJobDisplay();
        RefreshDailyJobOffers();
    }

    private void RefreshDailyJobOffers()
    {
        if (HasActiveJob())
        {
            ShowActiveJobInOfferSlot();
            return;
        }

        HideAllJobOfferSlots();

        int currentDay = GetCurrentDay();
        int slotsToUse = Mathf.Min(maxJobsPerDay, jobOfferSlots.Count);
        int usedSlots = 0;

        for (int jobIndex = 0; jobIndex < jobs.Count; jobIndex++)
        {
            if (usedSlots >= slotsToUse)
                break;

            JobPostData job = jobs[jobIndex];

            if (job == null)
                continue;

            if (job.availableDay != currentDay)
                continue;

            if (IsJobCompleted(jobIndex))
                continue;

            SetupNormalJobOfferSlot(jobOfferSlots[usedSlots], job, jobIndex, usedSlots);
            usedSlots++;
        }
    }

    private void HideAllJobOfferSlots()
    {
        for (int i = 0; i < jobOfferSlots.Count; i++)
        {
            if (jobOfferSlots[i] != null && jobOfferSlots[i].rootObject != null)
                jobOfferSlots[i].rootObject.SetActive(false);

            ClearOfferSlotTexts(jobOfferSlots[i]);
        }
    }

    private void ClearOfferSlotTexts(JobOfferSlot slot)
    {
        if (slot == null)
            return;

        if (slot.jobText != null)
            slot.jobText.text = "";

        if (slot.rewardText != null)
            slot.rewardText.text = "";

        if (slot.dayText != null)
            slot.dayText.text = "";

        if (slot.acceptButton != null)
        {
            slot.acceptButton.onClick.RemoveAllListeners();
            slot.acceptButton.interactable = false;
        }
    }

    private void ShowActiveJobInOfferSlot()
    {
        int jobIndex = PlayerPrefs.GetInt(KEY_ACTIVE_JOB_INDEX, -1);

        if (jobIndex < 0 || jobIndex >= jobs.Count)
            return;

        int offerSlotIndex = PlayerPrefs.GetInt(KEY_ACTIVE_OFFER_SLOT_INDEX, 0);

        if (offerSlotIndex < 0 || offerSlotIndex >= jobOfferSlots.Count)
            offerSlotIndex = 0;

        JobPostData job = jobs[jobIndex];
        JobOfferSlot slot = jobOfferSlots[offerSlotIndex];

        if (slot == null)
            return;

        int remainingDays = GetRemainingDays();

        if (slot.rootObject != null)
            slot.rootObject.SetActive(true);

        if (slot.jobText != null)
            slot.jobText.text = job.jobText;

        if (slot.rewardText != null)
            slot.rewardText.text = "Reward: $" + job.rewardMoney;

        if (slot.dayText != null)
            slot.dayText.text = "Days Left: " + remainingDays;

        if (slot.acceptButton != null)
        {
            slot.acceptButton.gameObject.SetActive(true);
            slot.acceptButton.interactable = false;
            slot.acceptButton.onClick.RemoveAllListeners();
        }
    }

    private void SetupNormalJobOfferSlot(JobOfferSlot slot, JobPostData job, int jobIndex, int offerSlotIndex)
    {
        if (slot == null)
            return;

        if (slot.rootObject != null)
            slot.rootObject.SetActive(true);

        if (slot.jobText != null)
            slot.jobText.text = job.jobText;

        if (slot.rewardText != null)
            slot.rewardText.text = "Reward: $" + job.rewardMoney;

        if (slot.dayText != null)
            slot.dayText.text = "Work Days: " + Mathf.Max(0, job.workDays);

        if (slot.acceptButton != null)
        {
            slot.acceptButton.gameObject.SetActive(true);
            slot.acceptButton.interactable = true;
            slot.acceptButton.onClick.RemoveAllListeners();

            int capturedJobIndex = jobIndex;
            int capturedOfferSlotIndex = offerSlotIndex;

            slot.acceptButton.onClick.AddListener(() =>
            {
                AcceptJob(capturedJobIndex, capturedOfferSlotIndex);
            });
        }
    }

    public void AcceptJob(int jobIndex, int offerSlotIndex)
    {
        if (jobIndex < 0 || jobIndex >= jobs.Count)
            return;

        if (HasActiveJob())
        {
            Debug.Log("JobPostManager: You can only accept one job at a time.");
            return;
        }

        JobPostData job = jobs[jobIndex];
        int safeWorkDays = Mathf.Max(0, job.workDays);

        PlayerPrefs.SetInt(KEY_HAS_ACTIVE_JOB, 1);
        PlayerPrefs.SetInt(KEY_ACTIVE_JOB_INDEX, jobIndex);
        PlayerPrefs.SetInt(KEY_ACTIVE_OFFER_SLOT_INDEX, offerSlotIndex);
        PlayerPrefs.SetInt(KEY_REMAINING_DAYS, safeWorkDays);
        PlayerPrefs.SetInt(KEY_READY_TO_CLAIM, safeWorkDays <= 0 ? 1 : 0);
        PlayerPrefs.SetInt(KEY_LAST_CHECKED_DAY, GetCurrentDay());
        PlayerPrefs.Save();

        RefreshAllUI();

        CloseJobPanelWithAnimation();

        Debug.Log("JobPostManager: Accepted job: " + job.jobText);
    }

    private void CheckDayChange()
    {
        if (!HasActiveJob())
        {
            PlayerPrefs.SetInt(KEY_LAST_CHECKED_DAY, GetCurrentDay());
            PlayerPrefs.Save();
            return;
        }

        int currentDay = GetCurrentDay();
        int lastCheckedDay = PlayerPrefs.GetInt(KEY_LAST_CHECKED_DAY, currentDay);
        int dayDifference = currentDay - lastCheckedDay;

        if (dayDifference <= 0)
            return;

        int remainingDays = GetRemainingDays();
        remainingDays -= dayDifference;
        remainingDays = Mathf.Max(0, remainingDays);

        PlayerPrefs.SetInt(KEY_REMAINING_DAYS, remainingDays);
        PlayerPrefs.SetInt(KEY_LAST_CHECKED_DAY, currentDay);

        if (remainingDays <= 0)
            PlayerPrefs.SetInt(KEY_READY_TO_CLAIM, 1);

        PlayerPrefs.Save();
    }

    private void RefreshAcceptedJobDisplay()
    {
        bool hasActiveJob = HasActiveJob();

        if (acceptedJobObject != null)
            acceptedJobObject.SetActive(hasActiveJob);

        if (!hasActiveJob)
        {
            ClearAcceptedJobDisplayTexts();
            return;
        }

        int jobIndex = PlayerPrefs.GetInt(KEY_ACTIVE_JOB_INDEX, -1);

        if (jobIndex < 0 || jobIndex >= jobs.Count)
        {
            ClearActiveJobSave();
            ClearAcceptedJobDisplayTexts();
            return;
        }

        JobPostData job = jobs[jobIndex];
        int remainingDays = GetRemainingDays();
        bool readyToClaim = IsActiveJobReadyToClaim();

        if (acceptedJobText != null)
            acceptedJobText.text = job.jobText;

        if (acceptedJobMoneyDisplayText != null)
            acceptedJobMoneyDisplayText.text = "Reward: $" + job.rewardMoney;

        if (acceptedJobDayLeftDisplayText != null)
            acceptedJobDayLeftDisplayText.text = "Days Left: " + remainingDays;

        if (claimRewardButton != null)
        {
            claimRewardButton.gameObject.SetActive(readyToClaim);
            claimRewardButton.interactable = readyToClaim;
        }
    }

    private void ClearAcceptedJobDisplayTexts()
    {
        if (acceptedJobText != null)
            acceptedJobText.text = "";

        if (acceptedJobMoneyDisplayText != null)
            acceptedJobMoneyDisplayText.text = "";

        if (acceptedJobDayLeftDisplayText != null)
            acceptedJobDayLeftDisplayText.text = "";

        if (claimRewardButton != null)
        {
            claimRewardButton.gameObject.SetActive(false);
            claimRewardButton.interactable = false;
        }
    }

    public void ClaimAcceptedJobReward()
    {
        if (!HasActiveJob())
            return;

        if (!IsActiveJobReadyToClaim())
            return;

        int jobIndex = PlayerPrefs.GetInt(KEY_ACTIVE_JOB_INDEX, -1);

        if (jobIndex < 0 || jobIndex >= jobs.Count)
            return;

        JobPostData job = jobs[jobIndex];

        AddMoney(job.rewardMoney);

        PlayerPrefs.SetInt(KEY_COMPLETED_PREFIX + jobIndex, 1);

        ClearActiveJobSave();
        ClearAcceptedJobDisplayTexts();
        RefreshAllUI();

        if (jobPanelLockedMessageText != null)
            jobPanelLockedMessageText.text = "";

        Debug.Log("JobPostManager: Claimed job reward: $" + job.rewardMoney);
    }

    private void AddMoney(int amount)
    {
        if (GlobalStatManager.Instance == null)
        {
            Debug.LogWarning("JobPostManager: No GlobalStatManager found. Money was not saved.");
            return;
        }

        int newCash = GlobalStatManager.Instance.currentCash + amount;

        GlobalStatManager.Instance.SaveToDisk(
            newCash,
            GlobalStatManager.Instance.currentFollowers,
            GlobalStatManager.Instance.currentCredibility
        );

        if (userStats != null)
            userStats.Cash = newCash;

        if (statSceneLink != null)
            statSceneLink.RefreshUI();
    }

    private void ClearActiveJobSave()
    {
        PlayerPrefs.SetInt(KEY_HAS_ACTIVE_JOB, 0);
        PlayerPrefs.SetInt(KEY_ACTIVE_JOB_INDEX, -1);
        PlayerPrefs.SetInt(KEY_ACTIVE_OFFER_SLOT_INDEX, -1);
        PlayerPrefs.SetInt(KEY_REMAINING_DAYS, 0);
        PlayerPrefs.SetInt(KEY_READY_TO_CLAIM, 0);
        PlayerPrefs.SetInt(KEY_LAST_CHECKED_DAY, GetCurrentDay());
        PlayerPrefs.Save();
    }

    private bool HasActiveJob()
    {
        return PlayerPrefs.GetInt(KEY_HAS_ACTIVE_JOB, 0) == 1;
    }

    private bool IsActiveJobReadyToClaim()
    {
        return PlayerPrefs.GetInt(KEY_READY_TO_CLAIM, 0) == 1 && GetRemainingDays() <= 0;
    }

    private int GetRemainingDays()
    {
        return PlayerPrefs.GetInt(KEY_REMAINING_DAYS, 0);
    }

    private bool IsJobCompleted(int jobIndex)
    {
        return PlayerPrefs.GetInt(KEY_COMPLETED_PREFIX + jobIndex, 0) == 1;
    }

    private int GetCurrentDay()
    {
        if (DayManager.Instance != null)
            return DayManager.Instance.currentDay;

        return 1;
    }

    [ContextMenu("Reset Job System Save")]
    public void ResetJobSystemSave()
    {
        ClearActiveJobSave();
        ClearAcceptedJobDisplayTexts();

        for (int i = 0; i < jobs.Count; i++)
        {
            PlayerPrefs.DeleteKey(KEY_COMPLETED_PREFIX + i);
        }

        PlayerPrefs.SetInt(KEY_LAST_CHECKED_DAY, GetCurrentDay());
        PlayerPrefs.Save();

        RefreshAllUI();

        Debug.Log("JobPostManager: Job system save reset.");
    }
}