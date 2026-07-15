using System;
using System.Collections.Generic;
using UnityEngine;

public enum PostChoiceQuality
{
    Correct,
    HalfCorrect,
    Neutral,
    Wrong,
    Nonsense
}

public enum MetricPenaltyReason
{
    None,
    SameSubTopic,
    SameTactic,
    SameCaptionTemplate,
    WrongTactic,
    WrongWordChoice
}

[CreateAssetMenu(fileName = "MisinformationMetricFormulaProfile", menuName = "Misinformation Game/Metric Formula Profile")]
public class MisinformationMetricFormulaProfile : ScriptableObject
{
    [Header("Metric Range")]
    [Tooltip("If enabled, money is clamped between 0 and Max Money.")]
    public bool clampMoney = true;

    [Tooltip("If enabled, followers are clamped between 0 and Max Followers.")]
    public bool clampFollowers = true;

    [Tooltip("If enabled, credibility is clamped between 0 and Max Credibility.")]
    public bool clampCredibility = true;

    [Tooltip("Maximum money value. Use 100 if endings use 0-100 ranges.")]
    public int maxMoney = 100;

    [Tooltip("Maximum follower value. Use 100 if endings use 0-100 ranges.")]
    public int maxFollowers = 100;

    [Tooltip("Maximum credibility value. Usually 100.")]
    public int maxCredibility = 100;

    [Header("Base Post Gains")]
    [Tooltip("Base follower gain before tactic, quality, repetition, and credibility multipliers.")]
    public float baseFollowersPerPost = 8f;

    [Tooltip("Base money gain for each post.")]
    public float baseMoneyPerPost = 2f;

    [Tooltip("Base like gain for each post. Likes are stored in UserStats only.")]
    public float baseLikesPerPost = 6f;

    [Tooltip("Extra money gained for each follower gained.")]
    public float moneyPerFollowerGained = 0.12f;

    [Tooltip("Extra likes gained for each follower gained.")]
    public float likesPerFollowerGained = 0.8f;

    [Header("Tactic Card Impact")]
    [Tooltip("How strongly TacticSO.engagementBonus affects follower growth. Example: 1 = +15% if engagementBonus is 0.15.")]
    public float tacticEngagementBonusToGrowthMultiplier = 1f;

    [Tooltip("How strongly TacticSO.engagementBonus affects money gain.")]
    public float tacticEngagementBonusToMoneyMultiplier = 0.5f;

    [Tooltip("How strongly TacticSO.credibilityCost reduces credibility. Example: 100 means 0.08 becomes -8 credibility.")]
    public float tacticCredibilityCostToCredibilityLoss = 100f;

    [Header("JSON-Based Scoring")]
    [Tooltip("If enabled, selected words are scored from DailyPostData.json fields instead of setting many Inspector rules.")]
    public bool useJsonScoring = true;

    [Tooltip("If a sentence has no explicit correctWords list, use blankWords as the correct answers.")]
    public bool useBlankWordsAsCorrectAnswers = true;

    [Tooltip("If enabled, any blank word can count as correct. If disabled, the selected word must match the same blank position. Recommended OFF for ordered fill-in-the-blank captions.")]
    public bool allowAnyBlankWordAsCorrect = false;

    [Tooltip("Quality used for words not listed in correctWords, halfCorrectWords, neutralWords, wrongWords, or nonsenseWords.")]
    public PostChoiceQuality defaultUnlistedWordQuality = PostChoiceQuality.Wrong;

    [Tooltip("Quality used for caption templates when the JSON sentence does not set captionQuality.")]
    public PostChoiceQuality defaultCaptionQuality = PostChoiceQuality.Correct;

    [Tooltip("If enabled, caption-level idealTacticTypes, neutralTacticTypes, and badTacticTypes decide whether the selected tactic fits this caption.")]
    public bool useJsonTacticFit = true;

    [Tooltip("If the tactic is not listed as ideal, neutral, or bad, use this quality. Recommended Neutral so unlisted tactics are accepted but not rewarded.")]
    public PostChoiceQuality defaultUnlistedTacticFitQuality = PostChoiceQuality.Neutral;

    [Header("Optional Global Word Lists")]
    [Tooltip("Optional. These words count as correct everywhere if the sentence does not list them.")]
    public List<string> globalCorrectWords = new List<string>();

    [Tooltip("Optional. These words count as half correct everywhere if the sentence does not list them.")]
    public List<string> globalHalfCorrectWords = new List<string>();

    [Tooltip("Optional. These words count as neutral everywhere if the sentence does not list them.")]
    public List<string> globalNeutralWords = new List<string>();

    [Tooltip("Optional. These words count as wrong everywhere if the sentence does not list them.")]
    public List<string> globalWrongWords = new List<string>();

    [Tooltip("Optional. These words count as nonsense everywhere if the sentence does not list them.")]
    public List<string> globalNonsenseWords = new List<string>();

    [Header("Quality Weights")]
    [Tooltip("Weight of the caption template quality.")]
    public float captionQualityWeight = 0.25f;

    [Tooltip("Weight of the selected blank words quality.")]
    public float wordChoiceQualityWeight = 0.55f;

    [Tooltip("Weight of whether the selected tactic fits the current caption.")]
    public float tacticFitWeight = 0.20f;

    [Header("Quality Scores")]
    [Tooltip("Score for a correct choice.")]
    public float correctScore = 1f;

    [Tooltip("Score for a half-correct choice.")]
    public float halfCorrectScore = 0.5f;

    [Tooltip("Score for a neutral choice.")]
    public float neutralScore = 0f;

    [Tooltip("Score for a wrong choice.")]
    public float wrongScore = -0.65f;

    [Tooltip("Score for a nonsense choice.")]
    public float nonsenseScore = -1f;

    [Tooltip("Score when the selected tactic is listed in the caption idealTacticTypes list.")]
    public float correctTacticScore = 1f;

    [Tooltip("Score when the selected tactic is listed in the caption neutralTacticTypes list, or is unlisted while Default Unlisted Tactic Fit Quality is Neutral.")]
    public float neutralTacticScore = 0f;

    [Tooltip("Score when the selected tactic is listed in the caption badTacticTypes list, or is unlisted while Default Unlisted Tactic Fit Quality is Wrong.")]
    public float wrongTacticScore = -0.75f;

    [Header("Quality To Growth Curve")]
    [Tooltip("X = final quality score from -1 to 1. Y = growth multiplier.")]
    public AnimationCurve qualityToGrowthMultiplier = new AnimationCurve(
        new Keyframe(-1f, 0.05f),
        new Keyframe(-0.5f, 0.25f),
        new Keyframe(0f, 0.65f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 1.55f)
    );

    [Tooltip("Minimum output from the quality curve. Can be negative if you want bad posts to lose followers.")]
    public float minQualityGrowthMultiplier = -0.5f;

    [Tooltip("Maximum output from the quality curve.")]
    public float maxQualityGrowthMultiplier = 2f;

    [Header("Credibility Formula")]
    [Tooltip("Effective misinformation also costs credibility because it spreads farther.")]
    public float successfulMisinformationCredibilityLoss = 2f;

    [Tooltip("Bad or confusing posts cost credibility based on how bad they are.")]
    public float wrongPostCredibilityLoss = 5f;

    [Tooltip("Quality above this value is treated as effective misinformation; lower is treated as a bad post.")]
    public float qualityScoreThresholdForEffectivePost = 0.25f;

    [Header("Low Credibility Reach Loss")]
    [Tooltip("If enabled, low credibility reduces future post growth.")]
    public bool lowCredibilityReducesGrowth = true;

    [Tooltip("Growth multiplier when credibility is 0.")]
    public float zeroCredibilityGrowthMultiplier = 0.35f;

    [Header("Fact Check Events")]
    [Tooltip("If enabled, low credibility can trigger a Fact Check penalty.")]
    public bool enableFactCheckEvents = true;

    [Tooltip("Fact Check triggers when projected credibility is less than or equal to this value.")]
    public int factCheckCredibilityThreshold = 35;

    [Tooltip("Minimum number of days between Fact Check events.")]
    public int factCheckCooldownDays = 3;

    [Tooltip("Extra credibility lost when a Fact Check event triggers.")]
    public int factCheckCredibilityLoss = 6;

    [Tooltip("Percent of current followers lost when a Fact Check event triggers. 0.08 = 8%.")]
    [Range(0f, 1f)]
    public float factCheckFollowerLossPercent = 0.08f;

    [Tooltip("Percent of current money lost when a Fact Check event triggers. 0.03 = 3%.")]
    [Range(0f, 1f)]
    public float factCheckMoneyLossPercent = 0.03f;

    [Header("Repetition / Wrong Choice Penalties")]
    [Tooltip("If multiple penalties happen at once, only apply the strongest one. Recommended.")]
    public bool useOnlyHighestPriorityPenalty = true;

    [Tooltip("Penalty order: Wrong Word Choice > Wrong Tactic = Same Caption Template > Same Tactic > Same Subtopic.")]
    public List<MetricPenaltySettings> repetitionPenaltySettings = new List<MetricPenaltySettings>();

    public float GetQualityScore(PostChoiceQuality quality)
    {
        switch (quality)
        {
            case PostChoiceQuality.Correct:
                return correctScore;
            case PostChoiceQuality.HalfCorrect:
                return halfCorrectScore;
            case PostChoiceQuality.Neutral:
                return neutralScore;
            case PostChoiceQuality.Wrong:
                return wrongScore;
            case PostChoiceQuality.Nonsense:
                return nonsenseScore;
            default:
                return neutralScore;
        }
    }


    public float GetTacticFitScore(PostChoiceQuality quality)
    {
        switch (quality)
        {
            case PostChoiceQuality.Correct:
                return correctTacticScore;
            case PostChoiceQuality.HalfCorrect:
                return halfCorrectScore;
            case PostChoiceQuality.Neutral:
                return neutralTacticScore;
            case PostChoiceQuality.Wrong:
                return wrongTacticScore;
            case PostChoiceQuality.Nonsense:
                return nonsenseScore;
            default:
                return neutralTacticScore;
        }
    }

    public float EvaluateGrowthMultiplierFromQuality(float qualityScore)
    {
        if (qualityToGrowthMultiplier == null)
        {
            return 1f;
        }

        float value = qualityToGrowthMultiplier.Evaluate(Mathf.Clamp(qualityScore, -1f, 1f));
        return Mathf.Clamp(value, minQualityGrowthMultiplier, maxQualityGrowthMultiplier);
    }

    public MetricPenaltySettings GetPenaltySettings(MetricPenaltyReason reason)
    {
        if (repetitionPenaltySettings == null)
        {
            return null;
        }

        for (int i = 0; i < repetitionPenaltySettings.Count; i++)
        {
            if (repetitionPenaltySettings[i] != null && repetitionPenaltySettings[i].reason == reason)
            {
                return repetitionPenaltySettings[i];
            }
        }

        return null;
    }

    public int ClampMoneyValue(int value)
    {
        if (!clampMoney)
        {
            return Mathf.Max(0, value);
        }

        return Mathf.Clamp(value, 0, maxMoney);
    }

    public int ClampFollowerValue(int value)
    {
        if (!clampFollowers)
        {
            return Mathf.Max(0, value);
        }

        return Mathf.Clamp(value, 0, maxFollowers);
    }

    public int ClampCredibilityValue(int value)
    {
        if (!clampCredibility)
        {
            return Mathf.Max(0, value);
        }

        return Mathf.Clamp(value, 0, maxCredibility);
    }

    public static PostChoiceQuality ParseQuality(string value, PostChoiceQuality fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        string normalized = value.Trim().Replace(" ", "").Replace("-", "").Replace("_", "").ToLowerInvariant();

        if (normalized == "correct") return PostChoiceQuality.Correct;
        if (normalized == "halfcorrect" || normalized == "half") return PostChoiceQuality.HalfCorrect;
        if (normalized == "neutral" || normalized == "noeffect") return PostChoiceQuality.Neutral;
        if (normalized == "wrong") return PostChoiceQuality.Wrong;
        if (normalized == "nonsense" || normalized == "weird" || normalized == "wrongwrong") return PostChoiceQuality.Nonsense;

        return fallback;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        BuildDefaultPenaltySettings();
    }

    private void OnValidate()
    {
        if (maxMoney < 1) maxMoney = 1;
        if (maxFollowers < 1) maxFollowers = 1;
        if (maxCredibility < 1) maxCredibility = 1;
        if (repetitionPenaltySettings == null) repetitionPenaltySettings = new List<MetricPenaltySettings>();
        if (repetitionPenaltySettings.Count == 0) BuildDefaultPenaltySettings();
    }
#endif

    private void BuildDefaultPenaltySettings()
    {
        repetitionPenaltySettings = new List<MetricPenaltySettings>();

        repetitionPenaltySettings.Add(new MetricPenaltySettings
        {
            reason = MetricPenaltyReason.WrongWordChoice,
            priority = 50,
            freeStreakDays = 1,
            decayPerExtraDay = 0.55f,
            minGrowthMultiplier = -0.65f,
            exponentialPenaltyGrowth = 0.45f,
            followerLossBase = 5,
            moneyLossBase = 1,
            credibilityLossBase = 4
        });

        repetitionPenaltySettings.Add(new MetricPenaltySettings
        {
            reason = MetricPenaltyReason.WrongTactic,
            priority = 40,
            freeStreakDays = 1,
            decayPerExtraDay = 0.45f,
            minGrowthMultiplier = -0.35f,
            exponentialPenaltyGrowth = 0.38f,
            followerLossBase = 3,
            moneyLossBase = 0,
            credibilityLossBase = 3
        });

        repetitionPenaltySettings.Add(new MetricPenaltySettings
        {
            reason = MetricPenaltyReason.SameCaptionTemplate,
            priority = 40,
            freeStreakDays = 2,
            decayPerExtraDay = 0.4f,
            minGrowthMultiplier = -0.25f,
            exponentialPenaltyGrowth = 0.34f,
            followerLossBase = 3,
            moneyLossBase = 0,
            credibilityLossBase = 2
        });

        repetitionPenaltySettings.Add(new MetricPenaltySettings
        {
            reason = MetricPenaltyReason.SameTactic,
            priority = 30,
            freeStreakDays = 3,
            decayPerExtraDay = 0.28f,
            minGrowthMultiplier = 0.1f,
            exponentialPenaltyGrowth = 0.25f,
            followerLossBase = 2,
            moneyLossBase = 0,
            credibilityLossBase = 1
        });

        repetitionPenaltySettings.Add(new MetricPenaltySettings
        {
            reason = MetricPenaltyReason.SameSubTopic,
            priority = 20,
            freeStreakDays = 3,
            decayPerExtraDay = 0.18f,
            minGrowthMultiplier = 0.2f,
            exponentialPenaltyGrowth = 0.18f,
            followerLossBase = 1,
            moneyLossBase = 0,
            credibilityLossBase = 1
        });
    }
}

[Serializable]
public class MetricPenaltySettings
{
    [Header("Penalty Type")]
    [Tooltip("Why this penalty happens.")]
    public MetricPenaltyReason reason = MetricPenaltyReason.None;

    [Tooltip("Higher number means more severe. Wrong word should be highest.")]
    public int priority = 0;

    [Header("Streak Days")]
    [Tooltip("How many continuous uses are allowed before punishment starts.")]
    public int freeStreakDays = 2;

    [Header("Growth Decay")]
    [Tooltip("After the free streak, growth multiplier uses e^(-decay * extra streak).")]
    public float decayPerExtraDay = 0.25f;

    [Tooltip("Lowest growth multiplier for this penalty. Can be negative if repeated mistakes should lose followers/money.")]
    public float minGrowthMultiplier = 0.1f;

    [Header("Exponential Metric Loss")]
    [Tooltip("How fast the direct metric loss grows after the free streak.")]
    public float exponentialPenaltyGrowth = 0.3f;

    [Tooltip("Base follower loss. Final loss = base * (e^(growth * extra streak) - 1).")]
    public float followerLossBase = 2f;

    [Tooltip("Base money loss. Final loss = base * (e^(growth * extra streak) - 1).")]
    public float moneyLossBase = 0f;

    [Tooltip("Base credibility loss. Final loss = base * (e^(growth * extra streak) - 1).")]
    public float credibilityLossBase = 1f;
}
