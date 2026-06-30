using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MisinformationMetricEngine : MonoBehaviour
{
    [Header("Formula Profile")]
    [Tooltip("Drag in MisinformationMetricFormulaProfile. All math, curves, penalties, and defaults are edited there.")]
    public MisinformationMetricFormulaProfile formulaProfile;

    [Header("Save Settings")]
    [Tooltip("If enabled, repeated subtopic/tactic/caption history is saved with PlayerPrefs.")]
    public bool saveHistoryToPlayerPrefs = true;

    [Tooltip("PlayerPrefs prefix for metric history. Do not change after release unless you want to reset history.")]
    public string savePrefix = "MisinformationMetrics_";

    [Header("Optional Fact Check UI")]
    [Tooltip("Optional panel opened when a Fact Check event triggers.")]
    public GameObject factCheckPanel;

    [Tooltip("Optional text used when a Fact Check event triggers.")]
    public TextMeshProUGUI factCheckText;

    [TextArea(2, 5)]
    [Tooltip("Message shown when a Fact Check event triggers.")]
    public string factCheckMessage = "Fact Check Alert: Your credibility is too low. The platform is reducing your reach.";

    [Header("Debug")]
    [Tooltip("Last preview result. Use this in Play Mode to debug why a post gained or lost metrics.")]
    public PostMetricPreview lastPreview;

    public PostMetricPreview PreviewPost(TacticSO selectedTactic, DailyPostFillBlankManager fillBlankManager, UserStats userStats)
    {
        if (formulaProfile == null)
        {
            Debug.LogWarning("MisinformationMetricEngine: No formula profile assigned.");
            return null;
        }

        PostMetricContext context = BuildContext(selectedTactic, fillBlankManager);

        int currentMoney = GetCurrentMoney(userStats);
        int currentFollowers = GetCurrentFollowers(userStats);
        int currentCredibility = GetCurrentCredibility(userStats);
        int currentDay = GetCurrentDay();

        PostMetricPreview preview = new PostMetricPreview();
        preview.topicId = context.topicId;
        preview.subTopicId = context.subTopicId;
        preview.tacticType = context.tacticType;
        preview.captionTemplateKey = context.captionTemplateKey;
        preview.selectedSentence = context.sentence;
        preview.selectedWords = new List<string>(context.selectedWords);
        preview.currentMoney = currentMoney;
        preview.currentFollowers = currentFollowers;
        preview.currentCredibility = currentCredibility;
        preview.matchedRuleName = formulaProfile.useJsonScoring ? "DailyPostData.json direct scoring" : "Formula defaults";

        EvaluateQuality(context, preview);
        EvaluateProjectedStreaks(preview);
        EvaluatePenalty(preview);
        CalculateMetricDeltas(selectedTactic, preview, currentMoney, currentFollowers, currentCredibility, currentDay);

        lastPreview = preview;
        return preview;
    }

    public PostMetricPreview ApplyPostResult(TacticSO selectedTactic, DailyPostFillBlankManager fillBlankManager, UserStats userStats)
    {
        if (formulaProfile == null)
        {
            Debug.LogWarning("MisinformationMetricEngine: No formula profile assigned. No metric result applied.");
            return null;
        }

        PostMetricPreview preview = PreviewPost(selectedTactic, fillBlankManager, userStats);

        if (preview == null)
        {
            return null;
        }

        int newMoney = formulaProfile.ClampMoneyValue(preview.currentMoney + preview.moneyDelta);
        int newFollowers = formulaProfile.ClampFollowerValue(preview.currentFollowers + preview.followersDelta);
        int newCredibility = formulaProfile.ClampCredibilityValue(preview.currentCredibility + preview.credibilityDelta);
        int newLikes = Mathf.Max(0, GetCurrentLikes(userStats) + preview.likesDelta);

        if (userStats != null)
        {
            userStats.Cash = newMoney;
            userStats.FollowerCount = newFollowers;
            userStats.Credibility = newCredibility;
            userStats.Likes = newLikes;
        }

        if (GlobalStatManager.Instance != null)
        {
            GlobalStatManager.Instance.SaveToDisk(newMoney, newFollowers, newCredibility);
        }

        SaveHistoryFromPreview(preview);

        if (preview.factCheckTriggered)
        {
            PlayerPrefs.SetInt(BuildKey("LastFactCheckDay"), GetCurrentDay());
            PlayerPrefs.Save();
            ShowFactCheckUI();
        }

        Debug.Log("MisinformationMetricEngine: Applied post result. " +
                  "Money " + preview.moneyDelta + ", Followers " + preview.followersDelta +
                  ", Credibility " + preview.credibilityDelta + ", Penalty " + preview.mainPenaltyReason +
                  ", WordScore " + preview.wordChoiceQualityScore + ", TacticFit " + preview.tacticFitScore);

        return preview;
    }

    public static void ResetSavedMetricHistory(string prefix = "MisinformationMetrics_")
    {
        PlayerPrefs.DeleteKey(prefix + "LastSubTopic");
        PlayerPrefs.DeleteKey(prefix + "SubTopicStreak");
        PlayerPrefs.DeleteKey(prefix + "LastTactic");
        PlayerPrefs.DeleteKey(prefix + "TacticStreak");
        PlayerPrefs.DeleteKey(prefix + "LastCaptionTemplate");
        PlayerPrefs.DeleteKey(prefix + "CaptionTemplateStreak");
        PlayerPrefs.DeleteKey(prefix + "WrongTacticStreak");
        PlayerPrefs.DeleteKey(prefix + "WrongWordChoiceStreak");
        PlayerPrefs.DeleteKey(prefix + "LastFactCheckDay");
        PlayerPrefs.Save();
    }

    [ContextMenu("Reset Metric History")]
    public void ResetMetricHistoryFromInspector()
    {
        ResetSavedMetricHistory(savePrefix);
    }

    private PostMetricContext BuildContext(TacticSO selectedTactic, DailyPostFillBlankManager fillBlankManager)
    {
        PostMetricContext context = new PostMetricContext();

        if (selectedTactic != null)
        {
            context.tacticType = NormalizeKey(!string.IsNullOrWhiteSpace(selectedTactic.GetTacticId()) ? selectedTactic.GetTacticId() : selectedTactic.type);
            context.tacticDisplayName = selectedTactic.displayName;
        }

        if (fillBlankManager != null)
        {
            context.topicId = NormalizeKey(fillBlankManager.currentTopicId);

            if (fillBlankManager.currentTopicData != null)
            {
                context.topicIdealTacticTypes = SafeCopy(fillBlankManager.currentTopicData.idealTacticTypes);
                context.topicBadTacticTypes = SafeCopy(fillBlankManager.currentTopicData.badTacticTypes);
            }

            if (fillBlankManager.currentSubTopicData != null)
            {
                context.subTopicId = NormalizeKey(fillBlankManager.currentSubTopicData.subTopicId);
                context.subTopicName = fillBlankManager.currentSubTopicData.subTopicName;
                context.subTopicIdealTacticTypes = SafeCopy(fillBlankManager.currentSubTopicData.idealTacticTypes);
                context.subTopicBadTacticTypes = SafeCopy(fillBlankManager.currentSubTopicData.badTacticTypes);
            }

            if (fillBlankManager.currentSentenceData != null)
            {
                DailyPostSentenceJson sentenceData = fillBlankManager.currentSentenceData;
                context.sentence = sentenceData.sentence;
                context.sentenceId = sentenceData.sentenceId;
                context.captionTemplateId = sentenceData.captionTemplateId;
                context.captionQualityRaw = sentenceData.captionQuality;
                context.blankWords = SafeCopy(sentenceData.blankWords);
                context.correctWords = SafeCopy(sentenceData.correctWords);
                context.halfCorrectWords = SafeCopy(sentenceData.halfCorrectWords);
                context.neutralWords = SafeCopy(sentenceData.neutralWords);
                context.wrongWords = SafeCopy(sentenceData.wrongWords);
                context.nonsenseWords = SafeCopy(sentenceData.nonsenseWords);
                context.sentenceIdealTacticTypes = SafeCopy(sentenceData.idealTacticTypes);
                context.sentenceBadTacticTypes = SafeCopy(sentenceData.badTacticTypes);
            }

            if (fillBlankManager.currentBlankValues != null)
            {
                for (int i = 0; i < fillBlankManager.currentBlankValues.Count; i++)
                {
                    context.selectedWords.Add(fillBlankManager.currentBlankValues[i]);
                }
            }
        }

        context.captionTemplateKey = BuildCaptionTemplateKey(context);
        return context;
    }

    private void EvaluateQuality(PostMetricContext context, PostMetricPreview preview)
    {
        float captionScore = formulaProfile.GetQualityScore(GetCaptionQuality(context));
        bool wrongTactic = IsWrongTactic(context);
        preview.wrongTactic = wrongTactic;
        float tacticFitScore = wrongTactic ? formulaProfile.wrongTacticScore : formulaProfile.correctTacticScore;

        float wordScore = formulaProfile.neutralScore;
        preview.wordQualities.Clear();

        if (context.selectedWords.Count > 0)
        {
            float totalWordScore = 0f;
            int wrongOrNonsenseCount = 0;

            for (int i = 0; i < context.selectedWords.Count; i++)
            {
                PostChoiceQuality quality = GetWordQuality(context, context.selectedWords[i], i);
                preview.wordQualities.Add(quality);

                float score = formulaProfile.GetQualityScore(quality);
                totalWordScore += score;

                if (quality == PostChoiceQuality.Wrong || quality == PostChoiceQuality.Nonsense)
                {
                    wrongOrNonsenseCount++;
                }
            }

            wordScore = totalWordScore / context.selectedWords.Count;
            preview.wrongWordChoice = wrongOrNonsenseCount > 0;
        }
        else
        {
            preview.wrongWordChoice = false;
        }

        float totalWeight = Mathf.Max(0.0001f,
            formulaProfile.captionQualityWeight +
            formulaProfile.wordChoiceQualityWeight +
            formulaProfile.tacticFitWeight);

        float combined =
            (captionScore * formulaProfile.captionQualityWeight +
             wordScore * formulaProfile.wordChoiceQualityWeight +
             tacticFitScore * formulaProfile.tacticFitWeight) / totalWeight;

        preview.captionQualityScore = captionScore;
        preview.wordChoiceQualityScore = wordScore;
        preview.tacticFitScore = tacticFitScore;
        preview.combinedQualityScore = Mathf.Clamp(combined, -1f, 1f);
        preview.combinedQuality01 = Mathf.InverseLerp(-1f, 1f, preview.combinedQualityScore);
        preview.qualityGrowthMultiplier = formulaProfile.EvaluateGrowthMultiplierFromQuality(preview.combinedQualityScore);
    }

    private PostChoiceQuality GetCaptionQuality(PostMetricContext context)
    {
        if (!formulaProfile.useJsonScoring)
        {
            return formulaProfile.defaultCaptionQuality;
        }

        return MisinformationMetricFormulaProfile.ParseQuality(context.captionQualityRaw, formulaProfile.defaultCaptionQuality);
    }

    private PostChoiceQuality GetWordQuality(PostMetricContext context, string selectedWord, int blankIndex)
    {
        if (!formulaProfile.useJsonScoring)
        {
            return formulaProfile.defaultUnlistedWordQuality;
        }

        if (ContainsWord(context.nonsenseWords, selectedWord) || ContainsWord(formulaProfile.globalNonsenseWords, selectedWord))
            return PostChoiceQuality.Nonsense;

        if (ContainsWord(context.wrongWords, selectedWord) || ContainsWord(formulaProfile.globalWrongWords, selectedWord))
            return PostChoiceQuality.Wrong;

        if (ContainsWord(context.neutralWords, selectedWord) || ContainsWord(formulaProfile.globalNeutralWords, selectedWord))
            return PostChoiceQuality.Neutral;

        if (ContainsWord(context.halfCorrectWords, selectedWord) || ContainsWord(formulaProfile.globalHalfCorrectWords, selectedWord))
            return PostChoiceQuality.HalfCorrect;

        if (ContainsWord(context.correctWords, selectedWord) || ContainsWord(formulaProfile.globalCorrectWords, selectedWord))
            return PostChoiceQuality.Correct;

        if (formulaProfile.useBlankWordsAsCorrectAnswers && context.blankWords != null)
        {
            if (formulaProfile.allowAnyBlankWordAsCorrect)
            {
                if (ContainsWord(context.blankWords, selectedWord))
                {
                    return PostChoiceQuality.Correct;
                }
            }
            else
            {
                if (blankIndex >= 0 && blankIndex < context.blankWords.Count && SameKey(context.blankWords[blankIndex], selectedWord))
                {
                    return PostChoiceQuality.Correct;
                }
            }
        }

        return formulaProfile.defaultUnlistedWordQuality;
    }

    private bool IsWrongTactic(PostMetricContext context)
    {
        if (!formulaProfile.useJsonTacticFit)
        {
            return false;
        }

        string tactic = context.tacticType;

        if (ContainsWord(context.sentenceBadTacticTypes, tactic) ||
            ContainsWord(context.subTopicBadTacticTypes, tactic) ||
            ContainsWord(context.topicBadTacticTypes, tactic))
        {
            return true;
        }

        List<string> idealList = GetMostSpecificNonEmptyList(
            context.sentenceIdealTacticTypes,
            context.subTopicIdealTacticTypes,
            context.topicIdealTacticTypes);

        if (idealList == null || idealList.Count == 0)
        {
            return false;
        }

        return !ContainsWord(idealList, tactic);
    }

    private void EvaluateProjectedStreaks(PostMetricPreview preview)
    {
        preview.sameSubTopicStreak = GetProjectedSameKeyStreak("LastSubTopic", "SubTopicStreak", preview.subTopicId);
        preview.sameTacticStreak = GetProjectedSameKeyStreak("LastTactic", "TacticStreak", preview.tacticType);
        preview.sameCaptionTemplateStreak = GetProjectedSameKeyStreak("LastCaptionTemplate", "CaptionTemplateStreak", preview.captionTemplateKey);
        preview.wrongTacticStreak = GetProjectedBoolStreak("WrongTacticStreak", preview.wrongTactic);
        preview.wrongWordChoiceStreak = GetProjectedBoolStreak("WrongWordChoiceStreak", preview.wrongWordChoice);
    }

    private void EvaluatePenalty(PostMetricPreview preview)
    {
        preview.mainPenaltyReason = MetricPenaltyReason.None;
        preview.repetitionGrowthMultiplier = 1f;
        preview.repetitionFollowerLoss = 0;
        preview.repetitionMoneyLoss = 0;
        preview.repetitionCredibilityLoss = 0;

        List<MetricPenaltyRuntime> activePenalties = new List<MetricPenaltyRuntime>();

        AddPenaltyIfActive(activePenalties, MetricPenaltyReason.SameSubTopic, preview.sameSubTopicStreak);
        AddPenaltyIfActive(activePenalties, MetricPenaltyReason.SameTactic, preview.sameTacticStreak);
        AddPenaltyIfActive(activePenalties, MetricPenaltyReason.SameCaptionTemplate, preview.sameCaptionTemplateStreak);
        AddPenaltyIfActive(activePenalties, MetricPenaltyReason.WrongTactic, preview.wrongTacticStreak);
        AddPenaltyIfActive(activePenalties, MetricPenaltyReason.WrongWordChoice, preview.wrongWordChoiceStreak);

        if (activePenalties.Count == 0)
        {
            return;
        }

        if (formulaProfile.useOnlyHighestPriorityPenalty)
        {
            MetricPenaltyRuntime strongest = activePenalties[0];

            for (int i = 1; i < activePenalties.Count; i++)
            {
                if (activePenalties[i].priority > strongest.priority)
                {
                    strongest = activePenalties[i];
                }
            }

            ApplyPenaltyRuntimeToPreview(strongest, preview);
            return;
        }

        MetricPenaltyRuntime combined = new MetricPenaltyRuntime();
        combined.reason = MetricPenaltyReason.None;
        combined.priority = -1;
        combined.growthMultiplier = 1f;

        for (int i = 0; i < activePenalties.Count; i++)
        {
            MetricPenaltyRuntime penalty = activePenalties[i];
            combined.growthMultiplier *= penalty.growthMultiplier;
            combined.followerLoss += penalty.followerLoss;
            combined.moneyLoss += penalty.moneyLoss;
            combined.credibilityLoss += penalty.credibilityLoss;

            if (penalty.priority > combined.priority)
            {
                combined.priority = penalty.priority;
                combined.reason = penalty.reason;
            }
        }

        ApplyPenaltyRuntimeToPreview(combined, preview);
    }

    private void AddPenaltyIfActive(List<MetricPenaltyRuntime> activePenalties, MetricPenaltyReason reason, int streak)
    {
        MetricPenaltySettings settings = formulaProfile.GetPenaltySettings(reason);

        if (settings == null)
        {
            return;
        }

        int extraStreak = streak - Mathf.Max(0, settings.freeStreakDays);

        if (extraStreak <= 0)
        {
            return;
        }

        MetricPenaltyRuntime runtime = new MetricPenaltyRuntime();
        runtime.reason = reason;
        runtime.priority = settings.priority;
        runtime.growthMultiplier = Mathf.Max(settings.minGrowthMultiplier, Mathf.Exp(-settings.decayPerExtraDay * extraStreak));

        float exponentialValue = Mathf.Exp(settings.exponentialPenaltyGrowth * extraStreak) - 1f;
        runtime.followerLoss = Mathf.RoundToInt(settings.followerLossBase * exponentialValue);
        runtime.moneyLoss = Mathf.RoundToInt(settings.moneyLossBase * exponentialValue);
        runtime.credibilityLoss = Mathf.RoundToInt(settings.credibilityLossBase * exponentialValue);

        activePenalties.Add(runtime);
    }

    private void ApplyPenaltyRuntimeToPreview(MetricPenaltyRuntime runtime, PostMetricPreview preview)
    {
        preview.mainPenaltyReason = runtime.reason;
        preview.repetitionGrowthMultiplier = runtime.growthMultiplier;
        preview.repetitionFollowerLoss = runtime.followerLoss;
        preview.repetitionMoneyLoss = runtime.moneyLoss;
        preview.repetitionCredibilityLoss = runtime.credibilityLoss;
    }

    private void CalculateMetricDeltas(TacticSO selectedTactic, PostMetricPreview preview, int currentMoney, int currentFollowers, int currentCredibility, int currentDay)
    {
        float tacticEngagementBonus = selectedTactic != null ? selectedTactic.engagementBonus : 0f;
        float tacticCredibilityCost = selectedTactic != null ? selectedTactic.credibilityCost : 0f;

        float tacticGrowthMultiplier = Mathf.Max(0f, 1f + tacticEngagementBonus * formulaProfile.tacticEngagementBonusToGrowthMultiplier);
        float tacticMoneyMultiplier = Mathf.Max(0f, 1f + tacticEngagementBonus * formulaProfile.tacticEngagementBonusToMoneyMultiplier);
        float credibilityVisibilityMultiplier = GetCredibilityVisibilityMultiplier(currentCredibility);

        preview.tacticGrowthMultiplier = tacticGrowthMultiplier;
        preview.credibilityVisibilityMultiplier = credibilityVisibilityMultiplier;

        float followerFloat = formulaProfile.baseFollowersPerPost * tacticGrowthMultiplier * preview.qualityGrowthMultiplier * preview.repetitionGrowthMultiplier * credibilityVisibilityMultiplier;
        int followerDelta = Mathf.RoundToInt(followerFloat) - preview.repetitionFollowerLoss;

        float moneyFloat = (formulaProfile.baseMoneyPerPost * tacticMoneyMultiplier) + (followerDelta * formulaProfile.moneyPerFollowerGained);
        int moneyDelta = Mathf.RoundToInt(moneyFloat) - preview.repetitionMoneyLoss;

        float likesFloat = formulaProfile.baseLikesPerPost + (Mathf.Max(0, followerDelta) * formulaProfile.likesPerFollowerGained);
        int likesDelta = Mathf.RoundToInt(likesFloat);

        int credibilityDelta = -Mathf.RoundToInt(tacticCredibilityCost * formulaProfile.tacticCredibilityCostToCredibilityLoss);

        if (preview.combinedQualityScore >= formulaProfile.qualityScoreThresholdForEffectivePost)
        {
            credibilityDelta -= Mathf.RoundToInt(formulaProfile.successfulMisinformationCredibilityLoss * Mathf.Max(0f, preview.qualityGrowthMultiplier));
        }
        else
        {
            credibilityDelta -= Mathf.RoundToInt(formulaProfile.wrongPostCredibilityLoss * Mathf.Abs(preview.combinedQualityScore));
        }

        credibilityDelta -= preview.repetitionCredibilityLoss;

        preview.followersDelta = followerDelta;
        preview.moneyDelta = moneyDelta;
        preview.likesDelta = likesDelta;
        preview.credibilityDelta = credibilityDelta;

        ApplyFactCheckPreview(preview, currentMoney, currentFollowers, currentCredibility, currentDay);
    }

    private void ApplyFactCheckPreview(PostMetricPreview preview, int currentMoney, int currentFollowers, int currentCredibility, int currentDay)
    {
        if (!formulaProfile.enableFactCheckEvents)
        {
            return;
        }

        int projectedCredibility = currentCredibility + preview.credibilityDelta;
        if (projectedCredibility > formulaProfile.factCheckCredibilityThreshold)
        {
            return;
        }

        int lastFactCheckDay = PlayerPrefs.GetInt(BuildKey("LastFactCheckDay"), -9999);
        int daysSinceLastFactCheck = currentDay - lastFactCheckDay;

        if (daysSinceLastFactCheck < formulaProfile.factCheckCooldownDays)
        {
            return;
        }

        int projectedMoney = Mathf.Max(0, currentMoney + preview.moneyDelta);
        int projectedFollowers = Mathf.Max(0, currentFollowers + preview.followersDelta);

        int factCheckMoneyLoss = Mathf.RoundToInt(projectedMoney * formulaProfile.factCheckMoneyLossPercent);
        int factCheckFollowerLoss = Mathf.RoundToInt(projectedFollowers * formulaProfile.factCheckFollowerLossPercent);

        preview.factCheckTriggered = true;
        preview.factCheckMoneyLoss = factCheckMoneyLoss;
        preview.factCheckFollowerLoss = factCheckFollowerLoss;
        preview.factCheckCredibilityLoss = formulaProfile.factCheckCredibilityLoss;

        preview.moneyDelta -= factCheckMoneyLoss;
        preview.followersDelta -= factCheckFollowerLoss;
        preview.credibilityDelta -= formulaProfile.factCheckCredibilityLoss;
    }

    private float GetCredibilityVisibilityMultiplier(int currentCredibility)
    {
        if (!formulaProfile.lowCredibilityReducesGrowth)
        {
            return 1f;
        }

        float credibility01 = Mathf.Clamp01((float)currentCredibility / Mathf.Max(1, formulaProfile.maxCredibility));
        return Mathf.Lerp(formulaProfile.zeroCredibilityGrowthMultiplier, 1f, credibility01);
    }

    private int GetProjectedSameKeyStreak(string lastKeyName, string streakKeyName, string currentKeyValue)
    {
        if (string.IsNullOrWhiteSpace(currentKeyValue))
        {
            return 0;
        }

        string lastValue = PlayerPrefs.GetString(BuildKey(lastKeyName), "");
        int currentStreak = PlayerPrefs.GetInt(BuildKey(streakKeyName), 0);

        if (lastValue == currentKeyValue)
        {
            return currentStreak + 1;
        }

        return 1;
    }

    private int GetProjectedBoolStreak(string streakKeyName, bool activeThisPost)
    {
        if (!activeThisPost)
        {
            return 0;
        }

        int currentStreak = PlayerPrefs.GetInt(BuildKey(streakKeyName), 0);
        return currentStreak + 1;
    }

    private void SaveHistoryFromPreview(PostMetricPreview preview)
    {
        if (!saveHistoryToPlayerPrefs || preview == null)
        {
            return;
        }

        PlayerPrefs.SetString(BuildKey("LastSubTopic"), preview.subTopicId);
        PlayerPrefs.SetInt(BuildKey("SubTopicStreak"), preview.sameSubTopicStreak);

        PlayerPrefs.SetString(BuildKey("LastTactic"), preview.tacticType);
        PlayerPrefs.SetInt(BuildKey("TacticStreak"), preview.sameTacticStreak);

        PlayerPrefs.SetString(BuildKey("LastCaptionTemplate"), preview.captionTemplateKey);
        PlayerPrefs.SetInt(BuildKey("CaptionTemplateStreak"), preview.sameCaptionTemplateStreak);

        PlayerPrefs.SetInt(BuildKey("WrongTacticStreak"), preview.wrongTactic ? preview.wrongTacticStreak : 0);
        PlayerPrefs.SetInt(BuildKey("WrongWordChoiceStreak"), preview.wrongWordChoice ? preview.wrongWordChoiceStreak : 0);

        PlayerPrefs.Save();
    }

    private string BuildCaptionTemplateKey(PostMetricContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.captionTemplateId))
        {
            return NormalizeKey(context.tacticType + "|" + context.captionTemplateId);
        }

        if (!string.IsNullOrWhiteSpace(context.sentenceId))
        {
            return NormalizeKey(context.tacticType + "|" + context.sentenceId);
        }

        string sentenceKey = NormalizeKey(context.sentence);
        if (string.IsNullOrWhiteSpace(sentenceKey))
        {
            sentenceKey = "no_sentence";
        }

        return NormalizeKey(context.tacticType + "|" + sentenceKey);
    }

    private void ShowFactCheckUI()
    {
        if (factCheckText != null)
        {
            factCheckText.text = factCheckMessage;
        }

        if (factCheckPanel != null)
        {
            factCheckPanel.SetActive(true);
        }
    }

    private int GetCurrentMoney(UserStats userStats)
    {
        if (GlobalStatManager.Instance != null)
        {
            return GlobalStatManager.Instance.currentCash;
        }

        if (userStats != null)
        {
            return userStats.Cash;
        }

        return 0;
    }

    private int GetCurrentFollowers(UserStats userStats)
    {
        if (GlobalStatManager.Instance != null)
        {
            return GlobalStatManager.Instance.currentFollowers;
        }

        if (userStats != null)
        {
            return userStats.FollowerCount;
        }

        return 0;
    }

    private int GetCurrentCredibility(UserStats userStats)
    {
        if (GlobalStatManager.Instance != null)
        {
            return GlobalStatManager.Instance.currentCredibility;
        }

        if (userStats != null)
        {
            return userStats.Credibility;
        }

        return 100;
    }

    private int GetCurrentLikes(UserStats userStats)
    {
        if (userStats != null)
        {
            return userStats.Likes;
        }

        return 0;
    }

    private int GetCurrentDay()
    {
        if (DayManager.Instance != null)
        {
            return DayManager.Instance.currentDay;
        }

        return 1;
    }

    private string BuildKey(string key)
    {
        return savePrefix + key;
    }

    private string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        return value.Trim().ToLowerInvariant();
    }

    private bool SameKey(string a, string b)
    {
        return NormalizeKey(a) == NormalizeKey(b);
    }

    private bool ContainsWord(List<string> list, string word)
    {
        if (list == null || string.IsNullOrWhiteSpace(word))
        {
            return false;
        }

        string normalizedWord = NormalizeKey(word);

        for (int i = 0; i < list.Count; i++)
        {
            if (NormalizeKey(list[i]) == normalizedWord)
            {
                return true;
            }
        }

        return false;
    }

    private List<string> SafeCopy(List<string> source)
    {
        if (source == null)
        {
            return new List<string>();
        }

        return new List<string>(source);
    }

    private List<string> GetMostSpecificNonEmptyList(List<string> sentenceList, List<string> subTopicList, List<string> topicList)
    {
        if (sentenceList != null && sentenceList.Count > 0) return sentenceList;
        if (subTopicList != null && subTopicList.Count > 0) return subTopicList;
        if (topicList != null && topicList.Count > 0) return topicList;
        return null;
    }
}

[Serializable]
public class PostMetricPreview
{
    [Header("Current Selection")]
    public string topicId;
    public string subTopicId;
    public string tacticType;
    public string captionTemplateKey;

    [TextArea(2, 5)]
    public string selectedSentence;

    public List<string> selectedWords = new List<string>();
    public List<PostChoiceQuality> wordQualities = new List<PostChoiceQuality>();
    public string matchedRuleName;

    [Header("Current Metrics")]
    public int currentMoney;
    public int currentFollowers;
    public int currentCredibility;

    [Header("Quality Score")]
    public float captionQualityScore;
    public float wordChoiceQualityScore;
    public float tacticFitScore;
    public float combinedQualityScore;
    public float combinedQuality01;

    [Header("Multipliers")]
    public float qualityGrowthMultiplier = 1f;
    public float tacticGrowthMultiplier = 1f;
    public float repetitionGrowthMultiplier = 1f;
    public float credibilityVisibilityMultiplier = 1f;

    [Header("Mistake State")]
    public bool wrongTactic;
    public bool wrongWordChoice;

    [Header("Streaks")]
    public int sameSubTopicStreak;
    public int sameTacticStreak;
    public int sameCaptionTemplateStreak;
    public int wrongTacticStreak;
    public int wrongWordChoiceStreak;

    [Header("Main Penalty")]
    public MetricPenaltyReason mainPenaltyReason = MetricPenaltyReason.None;
    public int repetitionFollowerLoss;
    public int repetitionMoneyLoss;
    public int repetitionCredibilityLoss;

    [Header("Fact Check")]
    public bool factCheckTriggered;
    public int factCheckFollowerLoss;
    public int factCheckMoneyLoss;
    public int factCheckCredibilityLoss;

    [Header("Final Changes")]
    public int moneyDelta;
    public int followersDelta;
    public int credibilityDelta;
    public int likesDelta;
}

public class PostMetricContext
{
    public string topicId;
    public string subTopicId;
    public string subTopicName;
    public string tacticType;
    public string tacticDisplayName;

    public string sentenceId;
    public string captionTemplateId;
    public string sentence;
    public string captionTemplateKey;
    public string captionQualityRaw;

    public List<string> selectedWords = new List<string>();
    public List<string> blankWords = new List<string>();

    public List<string> correctWords = new List<string>();
    public List<string> halfCorrectWords = new List<string>();
    public List<string> neutralWords = new List<string>();
    public List<string> wrongWords = new List<string>();
    public List<string> nonsenseWords = new List<string>();

    public List<string> topicIdealTacticTypes = new List<string>();
    public List<string> topicBadTacticTypes = new List<string>();
    public List<string> subTopicIdealTacticTypes = new List<string>();
    public List<string> subTopicBadTacticTypes = new List<string>();
    public List<string> sentenceIdealTacticTypes = new List<string>();
    public List<string> sentenceBadTacticTypes = new List<string>();
}

public class MetricPenaltyRuntime
{
    public MetricPenaltyReason reason;
    public int priority;
    public float growthMultiplier = 1f;
    public int followerLoss;
    public int moneyLoss;
    public int credibilityLoss;
}
