using System;
using System.Collections.Generic;
using UnityEngine;

// Posts use only three quality categories.
// Correct = fully correct, HalfCorrect = partly correct, and Nonsense = every other incorrect or meaningless choice.
public enum PostChoiceQuality
{
    Correct = 0,
    HalfCorrect = 1,
    Nonsense = 2
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
    [Tooltip("Money has no maximum. It is only prevented from going below 0.")]
    public bool keepMoneyAtOrAboveZero = true;

    [Tooltip("If enabled, credibility is clamped between 0 and Max Credibility.")]
    public bool clampCredibility = true;

    [Tooltip("Maximum credibility value. Usually 100.")]
    public int maxCredibility = 100;

    [Header("Base Post Gains")]
    [Tooltip("Base follower scale for a 31-day game. Half Correct play averages around 20,000 followers; Correct play can exceed 40,000.")]
    public float baseFollowersPerPost = 950f;

    [Tooltip("Base money gain for each post.")]
    public float baseMoneyPerPost = 5f;

    [Tooltip("Base like gain for each post. Likes are stored in UserStats only.")]
    public float baseLikesPerPost = 6f;

    [Tooltip("Small money conversion per follower gained. Kept low so jobs remain important even when followers reach tens of thousands.")]
    public float moneyPerFollowerGained = 0.005f;

    [Tooltip("Extra likes gained for each follower gained.")]
    public float likesPerFollowerGained = 0.8f;

    [Header("Tactic Card Impact")]
    [Tooltip("How strongly TacticSO.engagementBonus affects follower growth.")]
    public float tacticEngagementBonusToGrowthMultiplier = 1.1f;

    [Tooltip("How strongly TacticSO.engagementBonus affects money gain.")]
    public float tacticEngagementBonusToMoneyMultiplier = 0.75f;

    [Tooltip("How strongly TacticSO.credibilityCost reduces credibility.")]
    public float tacticCredibilityCostToCredibilityLoss = 40f;

    [Header("JSON-Based Scoring")]
    [Tooltip("If enabled, selected words are scored from DailyPostData.json.")]
    public bool useJsonScoring = true;

    [Tooltip("If a sentence has no explicit correctWords list, use blankWords as the correct answers.")]
    public bool useBlankWordsAsCorrectAnswers = true;

    [Tooltip("If enabled, any blank word can count as correct. Keep this OFF for ordered blanks.")]
    public bool allowAnyBlankWordAsCorrect = false;

    [Tooltip("Words not listed as Correct or Half Correct are treated as Nonsense.")]
    public PostChoiceQuality defaultUnlistedWordQuality = PostChoiceQuality.Nonsense;

    [Tooltip("If enabled, caption-level tactic lists decide whether the selected tactic fits.")]
    public bool useJsonTacticFit = true;

    [Tooltip("Tactics not listed as Correct or Half Correct are treated as Nonsense.")]
    public PostChoiceQuality defaultUnlistedTacticFitQuality = PostChoiceQuality.Nonsense;

    [Header("Optional Global Word Lists")]
    [Tooltip("Optional words that count as Correct everywhere.")]
    public List<string> globalCorrectWords = new List<string>();

    [Tooltip("Optional words that count as Half Correct everywhere.")]
    public List<string> globalHalfCorrectWords = new List<string>();

    [Tooltip("Optional words that count as Nonsense everywhere.")]
    public List<string> globalNonsenseWords = new List<string>();

    [Header("Quality Weights - Word Choice and Tactic Only")]
    [Range(0f, 1f)]
    [Tooltip("Weight of the average selected word-choice quality. Default is 0.50, meaning 50 percent of the final post quality.")]
    public float wordChoiceQualityWeight = 0.50f;

    [Range(0f, 1f)]
    [Tooltip("Weight of whether the selected tactic fits the current sentence. Default is 0.50, meaning 50 percent of the final post quality.")]
    public float tacticFitWeight = 0.50f;

    [Header("Three Quality Scores")]
    [Tooltip("Score for a Correct choice.")]
    public float correctScore = 1f;

    [Tooltip("Score for a Half Correct choice.")]
    public float halfCorrectScore = 0.45f;

    [Tooltip("Score for every other choice, treated as Nonsense.")]
    public float nonsenseScore = -1f;

    [Header("Quality To Growth Curve")]
    [Tooltip("X = final quality score from -1 to 1. Y = growth multiplier.")]
    // Thirty-one-day follower calibration: Nonsense gives very little growth,
    // Half Correct can finish near 20,000, and Correct can exceed 40,000.
    public AnimationCurve qualityToGrowthMultiplier = new AnimationCurve(
        new Keyframe(-1f, 0.02f),
        new Keyframe(0.45f, 0.75f),
        new Keyframe(1f, 1.55f)
    );

    [Tooltip("Minimum output from the quality curve. Quality alone never creates a negative follower gain.")]
    public float minQualityGrowthMultiplier = 0f;

    [Tooltip("Maximum output from the quality curve.")]
    public float maxQualityGrowthMultiplier = 2f;

    [Header("Credibility Formula")]
    [Tooltip("Small background credibility cost for effective posts.")]
    public float successfulMisinformationCredibilityLoss = 0.2f;

    [Tooltip("Nonsense posts cost credibility based on how bad their combined score is.")]
    public float wrongPostCredibilityLoss = 4f;

    [Tooltip("Quality at or above this value is treated as an effective post.")]
    public float qualityScoreThresholdForEffectivePost = 0.3f;

    [Header("Low Credibility Reach Loss")]
    [Tooltip("If enabled, low credibility reduces future post growth.")]
    public bool lowCredibilityReducesGrowth = true;

    [Tooltip("Growth multiplier when credibility is 0.")]
    public float zeroCredibilityGrowthMultiplier = 0.45f;

    [Header("Fact Check Events")]
    [Tooltip("If enabled, low credibility can trigger a Fact Check penalty.")]
    public bool enableFactCheckEvents = true;

    [Tooltip("Fact Check triggers when projected credibility is less than or equal to this value.")]
    public int factCheckCredibilityThreshold = 30;

    [Tooltip("Minimum number of days between Fact Check events.")]
    public int factCheckCooldownDays = 4;

    [Tooltip("Extra credibility lost when a Fact Check event triggers.")]
    public int factCheckCredibilityLoss = 4;

    [Tooltip("Percent of current followers lost when a Fact Check event triggers.")]
    [Range(0f, 1f)]
    public float factCheckFollowerLossPercent = 0.07f;

    [Tooltip("Percent of current money lost when a Fact Check event triggers.")]
    [Range(0f, 1f)]
    public float factCheckMoneyLossPercent = 0.05f;

    [Header("Repetition / Nonsense Choice Penalties")]
    [Tooltip("When enabled, only the strongest normal penalty is used. Penalties marked Always Stack, such as repeated caption and repeated tactic, are still combined.")]
    public bool useOnlyHighestPriorityPenalty = true;

    public List<MetricPenaltySettings> repetitionPenaltySettings = new List<MetricPenaltySettings>();

    public float GetQualityScore(PostChoiceQuality quality)
    {
        switch (quality)
        {
            case PostChoiceQuality.Correct:
                return correctScore;
            case PostChoiceQuality.HalfCorrect:
                return halfCorrectScore;
            case PostChoiceQuality.Nonsense:
            default:
                return nonsenseScore;
        }
    }

    public float GetTacticFitScore(PostChoiceQuality quality)
    {
        return GetQualityScore(quality);
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
        // Money has no maximum; this only prevents a negative value.
        return keepMoneyAtOrAboveZero ? Mathf.Max(0, value) : value;
    }

    public int ClampFollowerValue(int value)
    {
        // Followers have no maximum; they are only prevented from going below zero.
        return Mathf.Max(0, value);
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

        if (normalized == "correct")
        {
            return PostChoiceQuality.Correct;
        }

        if (normalized == "halfcorrect" || normalized == "half")
        {
            return PostChoiceQuality.HalfCorrect;
        }

        // Legacy JSON values such as Neutral, Wrong, and Nonsense are all treated as Nonsense,
        // so existing DailyPostData.json files continue to work.
        return PostChoiceQuality.Nonsense;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        BuildDefaultPenaltySettings();
    }

    private void OnValidate()
    {
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
            alwaysStack = false,
            freeStreakDays = 2,
            decayPerExtraDay = 0.22f,
            minGrowthMultiplier = 0f,
            exponentialPenaltyGrowth = 0.12f,
            followerLossBase = 2f,
            moneyLossBase = 5f,
            credibilityLossBase = 0.5f
        });

        repetitionPenaltySettings.Add(new MetricPenaltySettings
        {
            reason = MetricPenaltyReason.WrongTactic,
            priority = 45,
            alwaysStack = false,
            freeStreakDays = 2,
            decayPerExtraDay = 0.18f,
            minGrowthMultiplier = 0f,
            exponentialPenaltyGrowth = 0.10f,
            followerLossBase = 1.5f,
            moneyLossBase = 2f,
            credibilityLossBase = 0.5f
        });

        // The second consecutive use of the same caption template starts an
        // exponential audience-fatigue penalty. It always stacks with tactic repetition.
        repetitionPenaltySettings.Add(new MetricPenaltySettings
        {
            reason = MetricPenaltyReason.SameCaptionTemplate,
            priority = 60,
            alwaysStack = true,
            freeStreakDays = 1,
            decayPerExtraDay = 0.28f,
            minGrowthMultiplier = 0.10f,
            exponentialPenaltyGrowth = 0.35f,
            followerLossBase = 25f,
            moneyLossBase = 1f,
            credibilityLossBase = 0f
        });

        // The second consecutive use of the same tactic starts an exponential
        // audience-fatigue penalty. It always stacks with caption repetition.
        repetitionPenaltySettings.Add(new MetricPenaltySettings
        {
            reason = MetricPenaltyReason.SameTactic,
            priority = 55,
            alwaysStack = true,
            freeStreakDays = 1,
            decayPerExtraDay = 0.20f,
            minGrowthMultiplier = 0.20f,
            exponentialPenaltyGrowth = 0.28f,
            followerLossBase = 15f,
            moneyLossBase = 0.5f,
            credibilityLossBase = 0f
        });

        repetitionPenaltySettings.Add(new MetricPenaltySettings
        {
            reason = MetricPenaltyReason.SameSubTopic,
            priority = 20,
            alwaysStack = false,
            freeStreakDays = 4,
            decayPerExtraDay = 0.08f,
            minGrowthMultiplier = 0.40f,
            exponentialPenaltyGrowth = 0.05f,
            followerLossBase = 0.5f,
            moneyLossBase = 0f,
            credibilityLossBase = 0.2f
        });
    }

}

[Serializable]
public class MetricPenaltySettings
{
    [Header("Penalty Type")]
    public MetricPenaltyReason reason = MetricPenaltyReason.None;

    [Tooltip("Higher values identify the stronger normal penalty.")]
    public int priority = 0;

    [Tooltip("When enabled, this penalty is applied even when Use Only Highest Priority Penalty is enabled. Repeated captions and repeated tactics use this so both effects can stack.")]
    public bool alwaysStack = false;

    [Header("Streak Days")]
    [Tooltip("Number of consecutive uses allowed before the penalty starts. A value of 1 means the second consecutive use is penalized.")]
    public int freeStreakDays = 2;

    [Header("Exponential Growth Reduction")]
    [Tooltip("Growth multiplier uses e^(-Decay Per Extra Day x Extra Repeats). Larger values reduce follower growth faster.")]
    public float decayPerExtraDay = 0.25f;

    [Tooltip("Lowest follower-growth multiplier this penalty can reach.")]
    public float minGrowthMultiplier = 0.1f;

    [Header("Exponential Direct Metric Loss")]
    [Tooltip("Direct losses use Base Loss x (e^(Exponential Penalty Growth x Extra Repeats) - 1).")]
    public float exponentialPenaltyGrowth = 0.3f;

    public float followerLossBase = 2f;
    public float moneyLossBase = 0f;
    public float credibilityLossBase = 1f;
}
