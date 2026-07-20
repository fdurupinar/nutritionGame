using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
///
/// This script does not modify the real save or unlock endings.
/// It simulates 31 posting days in memory and counts all 12 endings.
///
/// Setup:
/// 1. Add this component to an empty GameObject.
/// 2. Assign MisinformationMetricFormulaProfile.
/// 3. Assign EndingStoryCollectionPanel to use the game ending thresholds.
/// 4. Use Run Ending Simulation or call RunSimulation() from a Button.
/// </summary>
public class TeenagerEndingSimulation : MonoBehaviour
{
    public enum ProfileSelectionMode
    {
        WeightedPopulation,
        EqualPerProfile
    }

    [Header("Simulation Settings")]
    [Min(1)]
    [Tooltip("Total number of simulated playthroughs. Default is 1,000.")]
    public int simulationCount = 1000;

    [Min(1)]
    [Tooltip("Number of posting days in each simulated playthrough. The game uses 31 days.")]
    public int daysPerSimulation = 31;

    [Tooltip("Weighted Population uses each profile weight. Equal Per Profile distributes runs evenly.")]
    public ProfileSelectionMode profileSelectionMode = ProfileSelectionMode.WeightedPopulation;

    [Tooltip("Use a different random seed each time. Disable to repeat the fixed-seed test.")]
    public bool useRandomSeed = false;

    [Tooltip("Fixed random seed for repeatable results.")]
    public int randomSeed = 20260720;

    [Tooltip("End the run immediately as Trust Collapse when Credibility reaches 0.")]
    public bool stopImmediatelyWhenCredibilityReachesZero = true;

    [Tooltip("Run automatically on Start. Usually leave disabled and run manually.")]
    public bool runOnStart = false;

    [Header("Starting Metrics")]
    public int startingMoney = 1000;
    public int startingFollowers = 0;
    public int startingCredibility = 100;

    [Header("Game References")]
    [Tooltip("Assign the real game formula asset. The simulator reads all metric and penalty values from it.")]
    public MisinformationMetricFormulaProfile formulaProfile;

    [Tooltip("Assign EndingStoryCollectionPanel to use GetEndingIndexFromStats. When empty, fallback conditions are used.")]
    public EndingStoryCollectionPanel endingResolver;

    [Header("Teenager Profiles")]
    [Tooltip("Twelve test behavior profiles for balance coverage; they are not demographic claims.")]
    public List<TeenagerSimulationProfile> teenagerProfiles = new List<TeenagerSimulationProfile>();

    [Header("Output")]
    [Tooltip("Save summary and detailed CSV reports to Application.persistentDataPath.")]
    public bool saveCsvFiles = true;

    [Tooltip("Save one detailed row per run. Disable to save only the summary.")]
    public bool saveDetailedRunCsv = true;

    [TextArea(20, 60)]
    [Tooltip("Latest simulation report; also printed to the Unity Console.")]
    public string lastReport;

    [Tooltip("Path to the latest summary CSV.")]
    public string lastSummaryCsvPath;

    [Tooltip("Path to the latest detailed CSV.")]
    public string lastDetailedCsvPath;

    private static readonly string[] FallbackEndingNames =
    {
        "Trust Collapse",
        "Forgotten",
        "Noise Empire",
        "Viral Beast",
        "Quiet Cash",
        "Broke Star",
        "Powerhouse",
        "Trusted",
        "Honest Path",
        "Responsible Influencer",
        "Paid Lies",
        "Unclear Legacy"
    };

    private void Start()
    {
        if (runOnStart)
        {
            RunSimulation();
        }
    }

    private void Reset()
    {
        CreateDefaultTeenagerProfiles();
    }

    [ContextMenu("Create Default Teenager Profiles")]
    public void CreateDefaultTeenagerProfiles()
    {
        teenagerProfiles = new List<TeenagerSimulationProfile>();

        // These 12 profiles are for balance and ending-coverage tests.
        // Population Weights total 100 for easy interpretation.
        teenagerProfiles.Add(CreateProfile(
            "Reckless Doomscroller", 6f,
            0.15f, 0.25f, 0.60f,
            0.10f, 0.20f, 0.70f,
            2, 4,
            0.15f, 0.45f,
            0.10f, 0.18f,
            0.55f, 0.65f, 0.55f,
            0.35f, 80, 120));

        teenagerProfiles.Add(CreateProfile(
            "Disengaged Poster", 6f,
            0.15f, 0.55f, 0.30f,
            0.15f, 0.55f, 0.30f,
            2, 4,
            -0.88f, -0.72f,
            0.018f, 0.030f,
            0.35f, 0.35f, 0.35f,
            0.00f, 80, 120));

        teenagerProfiles.Add(CreateProfile(
            "Outrage Entrepreneur", 8f,
            0.65f, 0.30f, 0.05f,
            0.55f, 0.35f, 0.10f,
            2, 4,
            0.28f, 0.45f,
            0.040f, 0.058f,
            0.25f, 0.35f, 0.25f,
            0.28f, 90, 120));

        teenagerProfiles.Add(CreateProfile(
            "Viral Attention Seeker", 8f,
            0.65f, 0.30f, 0.05f,
            0.55f, 0.35f, 0.10f,
            2, 4,
            0.30f, 0.48f,
            0.040f, 0.058f,
            0.25f, 0.35f, 0.25f,
            0.01f, 80, 100));

        teenagerProfiles.Add(CreateProfile(
            "Quiet Side Hustler", 7f,
            0.70f, 0.25f, 0.05f,
            0.70f, 0.25f, 0.05f,
            2, 4,
            -0.84f, -0.70f,
            0.000f, 0.010f,
            0.25f, 0.25f, 0.20f,
            0.35f, 90, 120));

        teenagerProfiles.Add(CreateProfile(
            "Popular but Unmonetized", 7f,
            0.85f, 0.13f, 0.02f,
            0.80f, 0.18f, 0.02f,
            2, 4,
            0.15f, 0.30f,
            0.000f, 0.005f,
            0.15f, 0.15f, 0.15f,
            0.00f, 80, 100));

        teenagerProfiles.Add(CreateProfile(
            "Disciplined Creator", 5f,
            0.90f, 0.09f, 0.01f,
            0.88f, 0.11f, 0.01f,
            2, 4,
            0.20f, 0.35f,
            0.000f, 0.003f,
            0.12f, 0.12f, 0.12f,
            0.30f, 90, 120));

        teenagerProfiles.Add(CreateProfile(
            "Careful Community Builder", 10f,
            0.80f, 0.18f, 0.02f,
            0.78f, 0.20f, 0.02f,
            2, 4,
            -0.35f, -0.18f,
            0.000f, 0.004f,
            0.18f, 0.18f, 0.18f,
            0.10f, 90, 110));

        teenagerProfiles.Add(CreateProfile(
            "Honest Low Reach Student", 8f,
            0.90f, 0.09f, 0.01f,
            0.90f, 0.09f, 0.01f,
            2, 4,
            -0.78f, -0.66f,
            0.000f, 0.002f,
            0.15f, 0.15f, 0.15f,
            0.00f, 80, 100));

        teenagerProfiles.Add(CreateProfile(
            "Balanced Everyday Creator", 15f,
            0.65f, 0.30f, 0.05f,
            0.65f, 0.30f, 0.05f,
            2, 4,
            -0.58f, -0.42f,
            0.000f, 0.008f,
            0.22f, 0.22f, 0.22f,
            0.18f, 90, 110));

        teenagerProfiles.Add(CreateProfile(
            "Sponsored Rumor Pusher", 8f,
            0.40f, 0.40f, 0.20f,
            0.30f, 0.40f, 0.30f,
            2, 4,
            0.08f, 0.22f,
            0.035f, 0.052f,
            0.35f, 0.45f, 0.35f,
            0.35f, 100, 130));

        teenagerProfiles.Add(CreateProfile(
            "Inconsistent Experimenter", 12f,
            0.45f, 0.40f, 0.15f,
            0.45f, 0.40f, 0.15f,
            2, 4,
            -0.20f, 0.00f,
            0.005f, 0.020f,
            0.30f, 0.30f, 0.30f,
            0.05f, 80, 100));

        Debug.Log("TeenagerEndingSimulation: Created 12 default teenager profiles.");
    }

    [ContextMenu("Run Ending Simulation")]
    public void RunSimulation()
    {
        if (!ValidateSettings())
        {
            return;
        }

        int seedUsed = useRandomSeed ? Environment.TickCount : randomSeed;
        System.Random random = new System.Random(seedUsed);

        int[] overallEndingCounts = new int[12];
        List<ProfileSimulationSummary> profileSummaries = CreateProfileSummaries();
        List<SimulationRunResult> detailedResults = new List<SimulationRunResult>(simulationCount);

        for (int runIndex = 0; runIndex < simulationCount; runIndex++)
        {
            int profileIndex = SelectProfileIndex(random, runIndex);
            TeenagerSimulationProfile profile = teenagerProfiles[profileIndex];

            SimulationRunResult result = SimulateOneTeenager(random, runIndex + 1, profileIndex, profile);
            detailedResults.Add(result);

            int endingIndex = Mathf.Clamp(result.endingIndex, 0, 11);
            overallEndingCounts[endingIndex]++;
            profileSummaries[profileIndex].Add(result);
        }

        lastReport = BuildTextReport(seedUsed, overallEndingCounts, profileSummaries, detailedResults);
        Debug.Log(lastReport);

        if (saveCsvFiles)
        {
            SaveCsvReports(seedUsed, overallEndingCounts, profileSummaries, detailedResults);
        }
    }

    private bool ValidateSettings()
    {
        if (formulaProfile == null)
        {
            Debug.LogError("TeenagerEndingSimulation: Formula Profile is missing. Drag MisinformationMetricFormulaProfile into the component.");
            return false;
        }

        if (simulationCount <= 0 || daysPerSimulation <= 0)
        {
            Debug.LogError("TeenagerEndingSimulation: Simulation Count and Days Per Simulation must be greater than 0.");
            return false;
        }

        if (teenagerProfiles == null || teenagerProfiles.Count == 0)
        {
            Debug.LogWarning("TeenagerEndingSimulation: No profiles found. Creating the 12 default profiles now.");
            CreateDefaultTeenagerProfiles();
        }

        for (int i = teenagerProfiles.Count - 1; i >= 0; i--)
        {
            if (teenagerProfiles[i] == null)
            {
                Debug.LogError("TeenagerEndingSimulation: Teenager profile at index " + i + " is null.");
                return false;
            }
        }

        if (endingResolver == null)
        {
            Debug.LogWarning("TeenagerEndingSimulation: Ending Resolver is empty. The simulator will use its built-in copy of the current 12 ending conditions.");
        }

        return true;
    }

    private SimulationRunResult SimulateOneTeenager(
        System.Random random,
        int runNumber,
        int profileIndex,
        TeenagerSimulationProfile profile)
    {
        SimulationRunResult result = new SimulationRunResult();
        result.runNumber = runNumber;
        result.profileIndex = profileIndex;
        result.profileName = profile.profileName;
        result.money = Mathf.Max(0, startingMoney);
        result.followers = Mathf.Max(0, startingFollowers);
        result.credibility = formulaProfile.ClampCredibilityValue(startingCredibility);

        SimulationStreakState streaks = new SimulationStreakState();
        int lastFactCheckDay = -9999;

        for (int day = 1; day <= daysPerSimulation; day++)
        {
            PostChoiceQuality tacticQuality = profile.tacticQuality.Roll(random);

            int wordCount = profile.wordChoicesPerPost.RollInclusive(random);
            wordCount = Mathf.Max(1, wordCount);

            float totalWordScore = 0f;
            bool hasNonsenseWord = false;

            for (int wordIndex = 0; wordIndex < wordCount; wordIndex++)
            {
                PostChoiceQuality wordQuality = profile.wordQuality.Roll(random);
                totalWordScore += formulaProfile.GetQualityScore(wordQuality);

                if (wordQuality == PostChoiceQuality.Nonsense)
                {
                    hasNonsenseWord = true;
                }
            }

            float wordScore = totalWordScore / wordCount;
            float tacticScore = formulaProfile.GetTacticFitScore(tacticQuality);

            // Caption quality is not used. Word Choice and Tactic each contribute 50 percent.
            float totalWeight = Mathf.Max(
                0.0001f,
                formulaProfile.wordChoiceQualityWeight + formulaProfile.tacticFitWeight);

            float combinedQualityScore =
                (wordScore * formulaProfile.wordChoiceQualityWeight +
                 tacticScore * formulaProfile.tacticFitWeight) / totalWeight;

            combinedQualityScore = Mathf.Clamp(combinedQualityScore, -1f, 1f);
            float qualityGrowthMultiplier = formulaProfile.EvaluateGrowthMultiplierFromQuality(combinedQualityScore);

            UpdateRepeatStreak(random, ref streaks.sameSubTopicStreak, profile.repeatSameSubTopicChance);
            UpdateRepeatStreak(random, ref streaks.sameTacticStreak, profile.repeatSameTacticChance);
            UpdateRepeatStreak(random, ref streaks.sameCaptionStreak, profile.repeatSameCaptionChance);

            bool wrongTactic = tacticQuality == PostChoiceQuality.Nonsense;
            streaks.wrongTacticStreak = wrongTactic ? streaks.wrongTacticStreak + 1 : 0;
            streaks.wrongWordChoiceStreak = hasNonsenseWord ? streaks.wrongWordChoiceStreak + 1 : 0;

            SimulationPenaltyResult penalty = EvaluateRepetitionPenalty(streaks);

            float engagementBonus = profile.tacticEngagementBonus.Roll(random);
            float credibilityCost = profile.tacticCredibilityCost.Roll(random);

            float tacticGrowthMultiplier = Mathf.Max(
                0f,
                1f + engagementBonus * formulaProfile.tacticEngagementBonusToGrowthMultiplier);

            float tacticMoneyMultiplier = Mathf.Max(
                0f,
                1f + engagementBonus * formulaProfile.tacticEngagementBonusToMoneyMultiplier);

            float credibilityVisibilityMultiplier = GetCredibilityVisibilityMultiplier(result.credibility);

            float followerFloat =
                formulaProfile.baseFollowersPerPost *
                tacticGrowthMultiplier *
                qualityGrowthMultiplier *
                penalty.growthMultiplier *
                credibilityVisibilityMultiplier;

            int followerDelta = Mathf.RoundToInt(followerFloat) - penalty.followerLoss;

            float moneyFloat =
                formulaProfile.baseMoneyPerPost * tacticMoneyMultiplier +
                followerDelta * formulaProfile.moneyPerFollowerGained;

            int moneyDelta = Mathf.RoundToInt(moneyFloat) - penalty.moneyLoss;

            int credibilityDelta = -Mathf.RoundToInt(
                credibilityCost * formulaProfile.tacticCredibilityCostToCredibilityLoss);

            if (combinedQualityScore >= formulaProfile.qualityScoreThresholdForEffectivePost)
            {
                credibilityDelta -= Mathf.RoundToInt(
                    formulaProfile.successfulMisinformationCredibilityLoss *
                    Mathf.Max(0f, qualityGrowthMultiplier));
            }
            else
            {
                credibilityDelta -= Mathf.RoundToInt(
                    formulaProfile.wrongPostCredibilityLoss *
                    Mathf.Abs(combinedQualityScore));
            }

            credibilityDelta -= penalty.credibilityLoss;

            // Fact Check is applied in the same order as the real MisinformationMetricEngine.
            if (formulaProfile.enableFactCheckEvents)
            {
                int projectedCredibility = result.credibility + credibilityDelta;
                int daysSinceLastFactCheck = day - lastFactCheckDay;

                if (projectedCredibility <= formulaProfile.factCheckCredibilityThreshold &&
                    daysSinceLastFactCheck >= formulaProfile.factCheckCooldownDays)
                {
                    int projectedMoney = Mathf.Max(0, result.money + moneyDelta);
                    int projectedFollowers = Mathf.Max(0, result.followers + followerDelta);

                    int factCheckMoneyLoss = Mathf.RoundToInt(
                        projectedMoney * formulaProfile.factCheckMoneyLossPercent);

                    int factCheckFollowerLoss = Mathf.RoundToInt(
                        projectedFollowers * formulaProfile.factCheckFollowerLossPercent);

                    moneyDelta -= factCheckMoneyLoss;
                    followerDelta -= factCheckFollowerLoss;
                    credibilityDelta -= formulaProfile.factCheckCredibilityLoss;

                    result.factCheckCount++;
                    lastFactCheckDay = day;
                }
            }

            result.money = formulaProfile.ClampMoneyValue(result.money + moneyDelta);
            result.followers = formulaProfile.ClampFollowerValue(result.followers + followerDelta);
            result.credibility = formulaProfile.ClampCredibilityValue(result.credibility + credibilityDelta);

            // JobPostManager adds Money independently in the real game.
            // The simulator therefore rolls the daily job reward after the post formula finishes.
            if (RollChance(random, profile.dailyJobRewardChance))
            {
                int jobReward = profile.jobReward.RollInclusive(random);
                result.money = formulaProfile.ClampMoneyValue(result.money + Mathf.Max(0, jobReward));
                result.jobRewardCount++;
                result.totalJobMoney += Mathf.Max(0, jobReward);
            }

            result.daysCompleted = day;

            if (result.credibility <= 0)
            {
                result.trustCollapseDay = day;

                if (stopImmediatelyWhenCredibilityReachesZero)
                {
                    break;
                }
            }
        }

        result.endingIndex = result.credibility <= 0
            ? 0
            : ResolveEndingIndex(result.money, result.credibility, result.followers);

        result.endingName = GetEndingName(result.endingIndex);
        return result;
    }

    private float GetCredibilityVisibilityMultiplier(int currentCredibility)
    {
        if (!formulaProfile.lowCredibilityReducesGrowth)
        {
            return 1f;
        }

        float credibility01 = Mathf.Clamp01(
            (float)currentCredibility / Mathf.Max(1, formulaProfile.maxCredibility));

        return Mathf.Lerp(
            formulaProfile.zeroCredibilityGrowthMultiplier,
            1f,
            credibility01);
    }

    private void UpdateRepeatStreak(System.Random random, ref int streak, float repeatChance)
    {
        if (streak <= 0)
        {
            streak = 1;
            return;
        }

        streak = RollChance(random, repeatChance) ? streak + 1 : 1;
    }

    private SimulationPenaltyResult EvaluateRepetitionPenalty(SimulationStreakState streaks)
    {
        List<SimulationPenaltyResult> activePenalties = new List<SimulationPenaltyResult>();
        AddPenaltyIfActive(activePenalties, MetricPenaltyReason.SameSubTopic, streaks.sameSubTopicStreak);
        AddPenaltyIfActive(activePenalties, MetricPenaltyReason.SameTactic, streaks.sameTacticStreak);
        AddPenaltyIfActive(activePenalties, MetricPenaltyReason.SameCaptionTemplate, streaks.sameCaptionStreak);
        AddPenaltyIfActive(activePenalties, MetricPenaltyReason.WrongTactic, streaks.wrongTacticStreak);
        AddPenaltyIfActive(activePenalties, MetricPenaltyReason.WrongWordChoice, streaks.wrongWordChoiceStreak);
        if (activePenalties.Count == 0) return SimulationPenaltyResult.None();

        SimulationPenaltyResult combined = SimulationPenaltyResult.None();
        combined.priority = -1;
        if (formulaProfile.useOnlyHighestPriorityPenalty)
        {
            SimulationPenaltyResult strongestNormalPenalty = null;
            for (int i = 0; i < activePenalties.Count; i++)
            {
                SimulationPenaltyResult penalty = activePenalties[i];
                if (penalty.alwaysStack)
                {
                    CombineSimulationPenalty(combined, penalty);
                    continue;
                }
                if (strongestNormalPenalty == null || penalty.priority > strongestNormalPenalty.priority)
                    strongestNormalPenalty = penalty;
            }
            if (strongestNormalPenalty != null) CombineSimulationPenalty(combined, strongestNormalPenalty);
        }
        else
        {
            for (int i = 0; i < activePenalties.Count; i++) CombineSimulationPenalty(combined, activePenalties[i]);
        }
        return combined;
    }

    private void CombineSimulationPenalty(SimulationPenaltyResult combined, SimulationPenaltyResult penalty)
    {
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

    private void AddPenaltyIfActive(
        List<SimulationPenaltyResult> activePenalties,
        MetricPenaltyReason reason,
        int streak)
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

        SimulationPenaltyResult result = new SimulationPenaltyResult();
        result.reason = reason;
        result.priority = settings.priority;
        result.alwaysStack = settings.alwaysStack;
        result.growthMultiplier = Mathf.Max(
            settings.minGrowthMultiplier,
            Mathf.Exp(-settings.decayPerExtraDay * extraStreak));

        float exponentialValue =
            Mathf.Exp(settings.exponentialPenaltyGrowth * extraStreak) - 1f;

        result.followerLoss = Mathf.RoundToInt(
            settings.followerLossBase * exponentialValue);

        result.moneyLoss = Mathf.RoundToInt(
            settings.moneyLossBase * exponentialValue);

        result.credibilityLoss = Mathf.RoundToInt(
            settings.credibilityLossBase * exponentialValue);

        activePenalties.Add(result);
    }

    private int ResolveEndingIndex(int money, int credibility, int followers)
    {
        if (endingResolver != null)
        {
            return endingResolver.GetEndingIndexFromStats(money, credibility, followers);
        }

        // Use fallback ending conditions only when Ending Resolver is not assigned.
        money = Mathf.Max(0, money);
        followers = Mathf.Max(0, followers);
        credibility = Mathf.Clamp(credibility, 0, 100);

        if (credibility <= 0)
            return 0; // Trust Collapse

        if (money >= 1800 && followers >= 38000 && credibility >= 75)
            return 6; // Powerhouse

        if (money >= 1700 && followers >= 30000 && credibility < 40)
            return 2; // Noise Empire

        if (followers >= 30000 && credibility < 40)
            return 3; // Viral Beast

        if (money >= 1700 && credibility < 40)
            return 10; // Paid Lies

        if (money < 1250 && followers < 2500 && credibility < 45)
            return 1; // Forgotten

        if (money >= 1700 && followers < 8000 && credibility >= 40)
            return 4; // Quiet Cash

        if (followers >= 30000 && money < 1450 && credibility >= 40)
            return 5; // Broke Star

        if (credibility >= 75 && followers >= 20000 && money >= 1450)
            return 7; // Trusted

        if (credibility >= 80 && followers < 12000 && money < 1450)
            return 8; // Honest Path

        if (money >= 1450 && credibility >= 55 && followers >= 12000)
            return 9; // Responsible Influencer

        return 11; // Unclear Legacy
    }

    private string GetEndingName(int endingIndex)
    {
        endingIndex = Mathf.Clamp(endingIndex, 0, 11);

        if (endingResolver != null && endingResolver.endingDatabase != null)
        {
            EndingStoryData data = endingResolver.endingDatabase.GetEndingByIndex(endingIndex);

            if (data != null && !string.IsNullOrWhiteSpace(data.title))
            {
                return data.title;
            }
        }

        return FallbackEndingNames[endingIndex];
    }

    private int SelectProfileIndex(System.Random random, int runIndex)
    {
        if (profileSelectionMode == ProfileSelectionMode.EqualPerProfile)
        {
            return runIndex % teenagerProfiles.Count;
        }

        float totalWeight = 0f;

        for (int i = 0; i < teenagerProfiles.Count; i++)
        {
            totalWeight += Mathf.Max(0f, teenagerProfiles[i].populationWeight);
        }

        if (totalWeight <= 0f)
        {
            return random.Next(0, teenagerProfiles.Count);
        }

        float roll = (float)random.NextDouble() * totalWeight;
        float accumulated = 0f;

        for (int i = 0; i < teenagerProfiles.Count; i++)
        {
            accumulated += Mathf.Max(0f, teenagerProfiles[i].populationWeight);

            if (roll <= accumulated)
            {
                return i;
            }
        }

        return teenagerProfiles.Count - 1;
    }

    private List<ProfileSimulationSummary> CreateProfileSummaries()
    {
        List<ProfileSimulationSummary> summaries = new List<ProfileSimulationSummary>();

        for (int i = 0; i < teenagerProfiles.Count; i++)
        {
            ProfileSimulationSummary summary = new ProfileSimulationSummary();
            summary.profileIndex = i;
            summary.profileName = teenagerProfiles[i].profileName;
            summaries.Add(summary);
        }

        return summaries;
    }

    private string BuildTextReport(
        int seedUsed,
        int[] overallEndingCounts,
        List<ProfileSimulationSummary> profileSummaries,
        List<SimulationRunResult> detailedResults)
    {
        StringBuilder builder = new StringBuilder(12000);

        builder.AppendLine("============================================================");
        builder.AppendLine("TEENAGER ENDING SIMULATION REPORT");
        builder.AppendLine("============================================================");
        builder.AppendLine("Simulation runs: " + simulationCount);
        builder.AppendLine("Days per run: " + daysPerSimulation);
        builder.AppendLine("Seed used: " + seedUsed);
        builder.AppendLine("Profile selection: " + profileSelectionMode);
        builder.AppendLine("Starting metrics: Money " + startingMoney +
                           ", Followers " + startingFollowers +
                           ", Credibility " + startingCredibility);
        builder.AppendLine("Formula profile: " + formulaProfile.name);
        builder.AppendLine("Ending resolver: " +
                           (endingResolver != null ? "Game EndingStoryCollectionPanel" : "Built-in current ending rules"));
        builder.AppendLine();

        builder.AppendLine("OVERALL ENDING COUNTS");
        builder.AppendLine("------------------------------------------------------------");

        int totalResolved = 0;

        for (int i = 0; i < overallEndingCounts.Length; i++)
        {
            totalResolved += overallEndingCounts[i];
            float percent = simulationCount > 0
                ? overallEndingCounts[i] * 100f / simulationCount
                : 0f;

            builder.AppendLine(
                (i + 1).ToString("00") + ". " +
                GetEndingName(i) + ": " +
                overallEndingCounts[i] + " / " + simulationCount +
                " (" + percent.ToString("0.00", CultureInfo.InvariantCulture) + "%)");
        }

        builder.AppendLine();
        builder.AppendLine("Resolved runs: " + totalResolved + " / " + simulationCount);

        if (totalResolved != simulationCount)
        {
            builder.AppendLine("WARNING: Not every run was assigned an ending.");
        }

        int totalEarlyCollapses = 0;
        long totalMoney = 0;
        long totalFollowers = 0;
        long totalCredibility = 0;
        long totalFactChecks = 0;
        long totalJobs = 0;

        for (int i = 0; i < detailedResults.Count; i++)
        {
            SimulationRunResult result = detailedResults[i];
            totalMoney += result.money;
            totalFollowers += result.followers;
            totalCredibility += result.credibility;
            totalFactChecks += result.factCheckCount;
            totalJobs += result.jobRewardCount;

            if (result.trustCollapseDay > 0)
            {
                totalEarlyCollapses++;
            }
        }

        float runDivisor = Mathf.Max(1, detailedResults.Count);

        builder.AppendLine();
        builder.AppendLine("OVERALL FINAL METRIC AVERAGES");
        builder.AppendLine("------------------------------------------------------------");
        builder.AppendLine("Average Money: " + (totalMoney / runDivisor).ToString("0.0", CultureInfo.InvariantCulture));
        builder.AppendLine("Average Followers: " + (totalFollowers / runDivisor).ToString("0.0", CultureInfo.InvariantCulture));
        builder.AppendLine("Average Credibility: " + (totalCredibility / runDivisor).ToString("0.0", CultureInfo.InvariantCulture));
        builder.AppendLine("Average Fact Checks: " + (totalFactChecks / runDivisor).ToString("0.00", CultureInfo.InvariantCulture));
        builder.AppendLine("Average Job Rewards: " + (totalJobs / runDivisor).ToString("0.00", CultureInfo.InvariantCulture));
        builder.AppendLine("Runs that reached Credibility 0: " + totalEarlyCollapses);

        builder.AppendLine();
        builder.AppendLine("RESULTS BY TEENAGER PROFILE");
        builder.AppendLine("------------------------------------------------------------");

        for (int i = 0; i < profileSummaries.Count; i++)
        {
            ProfileSimulationSummary summary = profileSummaries[i];

            if (summary.runCount <= 0)
            {
                builder.AppendLine(summary.profileName + ": 0 runs");
                continue;
            }

            builder.AppendLine("[" + summary.profileName + "]");
            builder.AppendLine("Runs: " + summary.runCount);
            builder.AppendLine("Average final metrics: Money " + summary.AverageMoney.ToString("0.0", CultureInfo.InvariantCulture) +
                               ", Followers " + summary.AverageFollowers.ToString("0.0", CultureInfo.InvariantCulture) +
                               ", Credibility " + summary.AverageCredibility.ToString("0.0", CultureInfo.InvariantCulture));
            builder.AppendLine("Average Fact Checks: " + summary.AverageFactChecks.ToString("0.00", CultureInfo.InvariantCulture) +
                               ", Average Job Rewards: " + summary.AverageJobs.ToString("0.00", CultureInfo.InvariantCulture));
            builder.AppendLine("Credibility-0 runs: " + summary.trustCollapseCount);

            List<int> sortedEndingIndexes = GetEndingIndexesSortedByCount(summary.endingCounts);
            int printed = 0;

            for (int sortedIndex = 0; sortedIndex < sortedEndingIndexes.Count && printed < 3; sortedIndex++)
            {
                int endingIndex = sortedEndingIndexes[sortedIndex];
                int count = summary.endingCounts[endingIndex];

                if (count <= 0)
                {
                    continue;
                }

                float percent = count * 100f / summary.runCount;
                builder.AppendLine("  " + GetEndingName(endingIndex) + ": " + count +
                                   " (" + percent.ToString("0.0", CultureInfo.InvariantCulture) + "%)");
                printed++;
            }

            builder.AppendLine();
        }

        builder.AppendLine("INTERPRETATION NOTE");
        builder.AppendLine("------------------------------------------------------------");
        builder.AppendLine("The default profiles are deliberately different balance-test archetypes.");
        builder.AppendLine("Their probabilities and Population Weights are design assumptions, not scientific predictions of real teenagers.");
        builder.AppendLine("Change the profile probabilities and weights in the Inspector to model your intended player audience.");
        builder.AppendLine("Every simulation run resolves to exactly one of the 12 endings; Unclear Legacy is the default fallback.");
        builder.AppendLine("============================================================");

        return builder.ToString();
    }

    private void SaveCsvReports(
        int seedUsed,
        int[] overallEndingCounts,
        List<ProfileSimulationSummary> profileSummaries,
        List<SimulationRunResult> detailedResults)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string directory = Application.persistentDataPath;
            Directory.CreateDirectory(directory);

            lastSummaryCsvPath = Path.Combine(
                directory,
                "TeenagerEndingSimulation_" + timestamp + "_Summary.csv");

            StringBuilder summaryCsv = new StringBuilder(10000);
            summaryCsv.AppendLine("Scope,Profile,EndingNumber,EndingName,Count,Percent,AverageMoney,AverageFollowers,AverageCredibility,AverageFactChecks,AverageJobRewards,CredibilityZeroRuns,Seed");

            for (int endingIndex = 0; endingIndex < 12; endingIndex++)
            {
                float percent = simulationCount > 0
                    ? overallEndingCounts[endingIndex] * 100f / simulationCount
                    : 0f;

                summaryCsv.AppendLine(
                    "Overall,All," +
                    (endingIndex + 1) + "," +
                    Csv(GetEndingName(endingIndex)) + "," +
                    overallEndingCounts[endingIndex] + "," +
                    percent.ToString("0.000", CultureInfo.InvariantCulture) +
                    ",,,,,," + seedUsed);
            }

            for (int profileIndex = 0; profileIndex < profileSummaries.Count; profileIndex++)
            {
                ProfileSimulationSummary summary = profileSummaries[profileIndex];

                for (int endingIndex = 0; endingIndex < 12; endingIndex++)
                {
                    float percent = summary.runCount > 0
                        ? summary.endingCounts[endingIndex] * 100f / summary.runCount
                        : 0f;

                    summaryCsv.AppendLine(
                        "Profile," +
                        Csv(summary.profileName) + "," +
                        (endingIndex + 1) + "," +
                        Csv(GetEndingName(endingIndex)) + "," +
                        summary.endingCounts[endingIndex] + "," +
                        percent.ToString("0.000", CultureInfo.InvariantCulture) + "," +
                        summary.AverageMoney.ToString("0.000", CultureInfo.InvariantCulture) + "," +
                        summary.AverageFollowers.ToString("0.000", CultureInfo.InvariantCulture) + "," +
                        summary.AverageCredibility.ToString("0.000", CultureInfo.InvariantCulture) + "," +
                        summary.AverageFactChecks.ToString("0.000", CultureInfo.InvariantCulture) + "," +
                        summary.AverageJobs.ToString("0.000", CultureInfo.InvariantCulture) + "," +
                        summary.trustCollapseCount + "," +
                        seedUsed);
                }
            }

            File.WriteAllText(lastSummaryCsvPath, summaryCsv.ToString(), Encoding.UTF8);

            if (saveDetailedRunCsv)
            {
                lastDetailedCsvPath = Path.Combine(
                    directory,
                    "TeenagerEndingSimulation_" + timestamp + "_DetailedRuns.csv");

                StringBuilder detailCsv = new StringBuilder(Mathf.Max(4096, detailedResults.Count * 100));
                detailCsv.AppendLine("Run,Profile,EndingNumber,EndingName,DaysCompleted,FinalMoney,FinalFollowers,FinalCredibility,FactChecks,JobRewards,TotalJobMoney,TrustCollapseDay,Seed");

                for (int i = 0; i < detailedResults.Count; i++)
                {
                    SimulationRunResult result = detailedResults[i];
                    detailCsv.AppendLine(
                        result.runNumber + "," +
                        Csv(result.profileName) + "," +
                        (result.endingIndex + 1) + "," +
                        Csv(result.endingName) + "," +
                        result.daysCompleted + "," +
                        result.money + "," +
                        result.followers + "," +
                        result.credibility + "," +
                        result.factCheckCount + "," +
                        result.jobRewardCount + "," +
                        result.totalJobMoney + "," +
                        result.trustCollapseDay + "," +
                        seedUsed);
                }

                File.WriteAllText(lastDetailedCsvPath, detailCsv.ToString(), Encoding.UTF8);
            }
            else
            {
                lastDetailedCsvPath = string.Empty;
            }

            Debug.Log("TeenagerEndingSimulation: Summary CSV saved to: " + lastSummaryCsvPath);

            if (!string.IsNullOrWhiteSpace(lastDetailedCsvPath))
            {
                Debug.Log("TeenagerEndingSimulation: Detailed CSV saved to: " + lastDetailedCsvPath);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError("TeenagerEndingSimulation: Could not save CSV files. " + exception);
        }
    }

    private List<int> GetEndingIndexesSortedByCount(int[] counts)
    {
        List<int> indexes = new List<int>();

        for (int i = 0; i < counts.Length; i++)
        {
            indexes.Add(i);
        }

        indexes.Sort(delegate(int left, int right)
        {
            int countCompare = counts[right].CompareTo(counts[left]);
            return countCompare != 0 ? countCompare : left.CompareTo(right);
        });

        return indexes;
    }

    private bool RollChance(System.Random random, float chance)
    {
        return random.NextDouble() < Mathf.Clamp01(chance);
    }

    private string Csv(string value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private TeenagerSimulationProfile CreateProfile(
        string profileName,
        float populationWeight,
        float wordCorrect,
        float wordHalf,
        float wordNonsense,
        float tacticCorrect,
        float tacticHalf,
        float tacticNonsense,
        int minWords,
        int maxWords,
        float minEngagement,
        float maxEngagement,
        float minCredibilityCost,
        float maxCredibilityCost,
        float repeatSubTopicChance,
        float repeatTacticChance,
        float repeatCaptionChance,
        float dailyJobChance,
        int minJobReward,
        int maxJobReward)
    {
        TeenagerSimulationProfile profile = new TeenagerSimulationProfile();
        profile.profileName = profileName;
        profile.populationWeight = populationWeight;
        profile.wordQuality = new QualityProbabilitySet(wordCorrect, wordHalf, wordNonsense);
        profile.tacticQuality = new QualityProbabilitySet(tacticCorrect, tacticHalf, tacticNonsense);
        profile.wordChoicesPerPost = new SimulationIntRange(minWords, maxWords);
        profile.tacticEngagementBonus = new SimulationFloatRange(minEngagement, maxEngagement);
        profile.tacticCredibilityCost = new SimulationFloatRange(minCredibilityCost, maxCredibilityCost);
        profile.repeatSameSubTopicChance = repeatSubTopicChance;
        profile.repeatSameTacticChance = repeatTacticChance;
        profile.repeatSameCaptionChance = repeatCaptionChance;
        profile.dailyJobRewardChance = dailyJobChance;
        profile.jobReward = new SimulationIntRange(minJobReward, maxJobReward);
        return profile;
    }
}

[Serializable]
public class TeenagerSimulationProfile
{
    [Header("Profile Identity")]
    public string profileName = "Teenager";

    [Min(0f)]
    [Tooltip("Relative weight in Weighted Population mode; values do not need to total 100.")]
    public float populationWeight = 1f;

    [Header("Quality Probabilities - No Caption Quality")]
    [Tooltip("Probability of Correct, Half Correct, and Nonsense for each selected fill-in-the-blank word.")]
    public QualityProbabilitySet wordQuality = new QualityProbabilitySet(0.60f, 0.30f, 0.10f);

    [Tooltip("Probability of Correct, Half Correct, and Nonsense for the selected tactic.")]
    public QualityProbabilitySet tacticQuality = new QualityProbabilitySet(0.60f, 0.30f, 0.10f);

    [Tooltip("Number of word choices per post. Each independently rolls Correct, Half Correct, or Nonsense.")]
    public SimulationIntRange wordChoicesPerPost = new SimulationIntRange(2, 4);

    [Header("TacticSO Number Ranges")]
    [Tooltip("Simulated TacticSO.engagementBonus range.")]
    public SimulationFloatRange tacticEngagementBonus = new SimulationFloatRange(-0.10f, 0.20f);

    [Tooltip("Simulated TacticSO.credibilityCost range.")]
    public SimulationFloatRange tacticCredibilityCost = new SimulationFloatRange(0f, 0.02f);

    [Header("Repetition Behavior")]
    [Range(0f, 1f)]
    public float repeatSameSubTopicChance = 0.20f;

    [Range(0f, 1f)]
    public float repeatSameTacticChance = 0.20f;

    [Range(0f, 1f)]
    public float repeatSameCaptionChance = 0.20f;

    [Header("Job Post Behavior")]
    [Range(0f, 1f)]
    [Tooltip("Daily chance of one Job Post reward, added after the post Money formula.")]
    public float dailyJobRewardChance = 0.15f;

    public SimulationIntRange jobReward = new SimulationIntRange(90, 110);
}

[Serializable]
public class QualityProbabilitySet
{
    [Min(0f)]
    public float correctChance = 0.60f;

    [Min(0f)]
    public float halfCorrectChance = 0.30f;

    [Min(0f)]
    public float nonsenseChance = 0.10f;

    public QualityProbabilitySet()
    {
    }

    public QualityProbabilitySet(float correct, float halfCorrect, float nonsense)
    {
        correctChance = correct;
        halfCorrectChance = halfCorrect;
        nonsenseChance = nonsense;
    }

    public PostChoiceQuality Roll(System.Random random)
    {
        float correct = Mathf.Max(0f, correctChance);
        float halfCorrect = Mathf.Max(0f, halfCorrectChance);
        float nonsense = Mathf.Max(0f, nonsenseChance);
        float total = correct + halfCorrect + nonsense;

        if (total <= 0f)
        {
            return PostChoiceQuality.Nonsense;
        }

        float roll = (float)random.NextDouble() * total;

        if (roll < correct)
        {
            return PostChoiceQuality.Correct;
        }

        if (roll < correct + halfCorrect)
        {
            return PostChoiceQuality.HalfCorrect;
        }

        return PostChoiceQuality.Nonsense;
    }
}

[Serializable]
public class SimulationFloatRange
{
    public float min;
    public float max;

    public SimulationFloatRange()
    {
    }

    public SimulationFloatRange(float minimum, float maximum)
    {
        min = minimum;
        max = maximum;
    }

    public float Roll(System.Random random)
    {
        float low = Mathf.Min(min, max);
        float high = Mathf.Max(min, max);
        return Mathf.Lerp(low, high, (float)random.NextDouble());
    }
}

[Serializable]
public class SimulationIntRange
{
    public int min;
    public int max;

    public SimulationIntRange()
    {
    }

    public SimulationIntRange(int minimum, int maximum)
    {
        min = minimum;
        max = maximum;
    }

    public int RollInclusive(System.Random random)
    {
        int low = Mathf.Min(min, max);
        int high = Mathf.Max(min, max);

        if (low == high)
        {
            return low;
        }

        return random.Next(low, high + 1);
    }
}

[Serializable]
public class SimulationRunResult
{
    public int runNumber;
    public int profileIndex;
    public string profileName;
    public int endingIndex;
    public string endingName;
    public int daysCompleted;
    public int money;
    public int followers;
    public int credibility;
    public int factCheckCount;
    public int jobRewardCount;
    public int totalJobMoney;
    public int trustCollapseDay;
}

public class ProfileSimulationSummary
{
    public int profileIndex;
    public string profileName;
    public int runCount;
    public int[] endingCounts = new int[12];
    public int trustCollapseCount;
    public long totalMoney;
    public long totalFollowers;
    public long totalCredibility;
    public long totalFactChecks;
    public long totalJobs;

    public float AverageMoney
    {
        get { return runCount > 0 ? (float)totalMoney / runCount : 0f; }
    }

    public float AverageFollowers
    {
        get { return runCount > 0 ? (float)totalFollowers / runCount : 0f; }
    }

    public float AverageCredibility
    {
        get { return runCount > 0 ? (float)totalCredibility / runCount : 0f; }
    }

    public float AverageFactChecks
    {
        get { return runCount > 0 ? (float)totalFactChecks / runCount : 0f; }
    }

    public float AverageJobs
    {
        get { return runCount > 0 ? (float)totalJobs / runCount : 0f; }
    }

    public void Add(SimulationRunResult result)
    {
        runCount++;
        int endingIndex = Mathf.Clamp(result.endingIndex, 0, 11);
        endingCounts[endingIndex]++;
        totalMoney += result.money;
        totalFollowers += result.followers;
        totalCredibility += result.credibility;
        totalFactChecks += result.factCheckCount;
        totalJobs += result.jobRewardCount;

        if (result.trustCollapseDay > 0)
        {
            trustCollapseCount++;
        }
    }
}

public class SimulationStreakState
{
    public int sameSubTopicStreak;
    public int sameTacticStreak;
    public int sameCaptionStreak;
    public int wrongTacticStreak;
    public int wrongWordChoiceStreak;
}

public class SimulationPenaltyResult
{
    public MetricPenaltyReason reason;
    public int priority;
    public bool alwaysStack;
    public float growthMultiplier;
    public int followerLoss;
    public int moneyLoss;
    public int credibilityLoss;

    public static SimulationPenaltyResult None()
    {
        SimulationPenaltyResult result = new SimulationPenaltyResult();
        result.reason = MetricPenaltyReason.None;
        result.priority = 0;
        result.growthMultiplier = 1f;
        result.followerLoss = 0;
        result.moneyLoss = 0;
        result.credibilityLoss = 0;
        return result;
    }
}
