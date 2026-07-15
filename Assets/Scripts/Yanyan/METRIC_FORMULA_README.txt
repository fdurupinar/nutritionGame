# Misinformation Game — Metric Formula README

This README explains where the balancing values are stored, what each setting means, and how the current Money, Followers, Likes, Credibility, tactic-fit, fact-check, and repetition formulas work.

---

## 1. Files to Edit

### A. Main balancing file — edit this for almost all numbers

**File name**

MisinformationMetricFormulaProfile.asset

**Current package path**

Assets/Scripts/Yanyan/Forumla/MisinformationMetricFormulaProfile.asset

> The folder is currently spelled Forumla in the project package.

**How to edit**

1. Open the Unity Project window.
2. Go to Assets/Scripts/Yanyan/Forumla/.
3. Select MisinformationMetricFormulaProfile.
4. Change the values in the Inspector.

This ScriptableObject contains the global formula values, weights, scores, curves, fact-check settings, and repetition penalties.

---

### B. Caption, blank-word, and tactic-fit data

**File name**

DailyPostData.json

**Current package path**

Assets/Scripts/Yanyan/Save/DailyPostData.json

Edit this file when changing:

- Topics and subtopics
- Caption sentences
- Number and order of blanks
- Word choices
- Correct words for each blank position
- Half-correct words
- Neutral words
- Wrong words
- Nonsense words
- Ideal tactics for a caption
- Neutral tactics for a caption
- Bad tactics for a caption

**Recommended editor**

Tools > Misinformation Game > Daily Post JSON Editor

The JSON decides which category a word or tactic belongs to. It does **not** decide the numeric score of that category. The numeric values come from MisinformationMetricFormulaProfile.asset.

---

### C. Individual tactic-card values

Each tactic card is a TacticSO ScriptableObject.

To find all of them in Unity, search the Project window for:

t:TacticSO

Important fields on each tactic card:

- Engagement Bonus — improves follower growth and part of the money gain.
- Credibility Cost — directly reduces Credibility.
- Tactic ID Dropdown — must match the tactic names used in DailyPostData.json.
- Level — required player level.
- Enabled Flag — whether the card can be used.

Examples:

- Engagement Bonus = 0.15 means a 15% raw engagement bonus before the Formula Profile conversion.
- Credibility Cost = 0.08 becomes an 8-point Credibility loss when the conversion setting is 100.

---

### D. Runtime formula code — normally do not edit for balancing

**File name**

MisinformationMetricEngine.cs

**Current package path**

Assets/Scripts/Yanyan/Forumla/MisinformationMetricEngine.cs

This script performs the calculations. For normal balancing, change the Formula Profile asset instead of editing this script.

---

## 2. Overall Calculation Order

When the player publishes a post, the system does this:

1. Score the caption template.
2. Score every selected blank word by its exact blank position.
3. Average the selected-word scores.
4. Score the selected tactic as Ideal, Neutral, Bad, or unlisted.
5. Combine caption, words, and tactic fit into one quality score.
6. Convert quality into a growth multiplier using the curve.
7. calculate repetition streaks.
8. Apply the strongest active repetition/mistake penalty.
9. Reduce reach if Credibility is low.
10. Calculate Followers, Money, Likes, and Credibility.
11. Check whether a Fact Check event triggers.
12. Clamp the final metrics to their allowed ranges.
13. Save the repetition history for the next day.

---

## 3. Caption and Word Scoring

### Ordered blank scoring

Every blank position has its own scoring lists.

Example sentence:

I [eat] an apple [today].

Correct setup:

- Blank 1 Correct Words: eat
- Blank 2 Correct Words: today

Results:

- eat first and today second = correct
- today first and eat second = wrong or nonsense, depending on the lists

The same word can have a different category in different blank positions.

### Current word scores

These values are edited under:

MisinformationMetricFormulaProfile > Quality Scores

| Category | Current value | Meaning |
|---|---:|---|
| Correct | 1.00 | Best answer for that blank |
| Half Correct | 0.50 | Understandable and partly effective, but not the best |
| Neutral | 0.00 | Makes sense but adds no quality bonus |
| Wrong | -0.65 | Incorrect or poor choice |
| Nonsense | -1.00 | Makes the sentence nonsensical or extremely inappropriate |

### Word-score formula

If there are multiple blanks:

Average Word Score = sum of all selected-word scores / number of blanks

Example:

- Blank 1 = Correct = 1.00
- Blank 2 = Half Correct = 0.50

Average Word Score = (1.00 + 0.50) / 2 = 0.75

A single Wrong or Nonsense answer also marks the post as having a Wrong Word Choice for the coach and repetition system.

---

## 4. Tactic-Fit Scoring

Tactic fit is configured separately for every caption in DailyPostData.json.

Each caption can contain:

- idealTacticTypes
- neutralTacticTypes
- badTacticTypes

Current tactic-fit scores:

| Category | Current value | Meaning |
|---|---:|---|
| Ideal tactic | 1.00 | Best tactic for this caption |
| Neutral tactic | 0.00 | Acceptable, but no quality bonus |
| Bad tactic | -0.75 | Wrong tactic for this caption |
| Unlisted tactic | Neutral by default | Controlled by Default Unlisted Tactic Fit Quality |

Only captions contain tactic-fit lists. Topics and subtopics do not.

---

## 5. Final Quality Formula

Edit these values under:

MisinformationMetricFormulaProfile > Quality Weights

Current weights:

| Component | Current weight | Meaning |
|---|---:|---|
| Caption Quality Weight | 0.25 | Quality assigned to the caption template |
| Word Choice Quality Weight | 0.55 | Average quality of all selected blank words |
| Tactic Fit Weight | 0.20 | How well the tactic matches the caption |

Formula:

Combined Quality = (Caption Score × Caption Weight + Word Score × Word Weight + Tactic Score × Tactic Weight) / Total Weight

The result is clamped between -1 and 1.

The current weights total 1.00, so the division does not change the result. The code still divides by the total weight, so the formula remains valid if the weights are changed.

Example:

- Caption Score = 1.00
- Word Score = 0.50
- Tactic Score = 0.00

Combined Quality = (1.00 × 0.25) + (0.50 × 0.55) + (0.00 × 0.20)

Combined Quality = 0.525

---

## 6. Quality-to-Growth Curve

Edit under:

MisinformationMetricFormulaProfile > Quality To Growth Curve

The curve converts Combined Quality into a multiplier used for Followers.

Current curve points:

| Combined Quality | Growth multiplier |
|---:|---:|
| -1.00 | 0.05 |
| -0.50 | 0.25 |
| 0.00 | 0.65 |
| 0.50 | 1.00 |
| 1.00 | 1.55 |

Meaning:

- Very poor post = almost no normal growth
- Average post = reduced growth
- Good post = normal growth
- Excellent post = up to 155% of normal growth before other multipliers

Other settings:

- Min Quality Growth Multiplier — lowest curve output allowed.
- Max Quality Growth Multiplier — highest curve output allowed.

Current values:

- Minimum = -0.50
- Maximum = 2.00

A negative minimum allows a custom curve to make extremely bad posts lose Followers. The current curve itself does not currently go below 0.05.

---

## 7. Base Metric Values

Edit under:

MisinformationMetricFormulaProfile > Base Post Gains

| Field | Current value | Meaning |
|---|---:|---|
| Base Followers Per Post | 8 | Starting Followers before multipliers |
| Base Money Per Post | 2 | Starting Money for every post |
| Base Likes Per Post | 6 | Starting Likes for every post |
| Money Per Follower Gained | 0.12 | Additional Money for each net Follower gained |
| Likes Per Follower Gained | 0.80 | Additional Likes for each positive Follower gained |

---

## 8. Tactic Card Impact

Edit the conversion strengths under:

MisinformationMetricFormulaProfile > Tactic Card Impact

Edit each card's raw values on its TacticSO asset.

### Tactic follower multiplier

Tactic Growth Multiplier = max(0, 1 + Engagement Bonus × Tactic Engagement Bonus To Growth Multiplier)

Current conversion value:

Tactic Engagement Bonus To Growth Multiplier = 1.00

Example:

- Engagement Bonus = 0.15
- Conversion = 1.00

Tactic Growth Multiplier = 1 + 0.15 × 1.00 = 1.15

The tactic gives 15% more raw follower growth.

### Tactic money multiplier

Tactic Money Multiplier = max(0, 1 + Engagement Bonus × Tactic Engagement Bonus To Money Multiplier)

Current conversion value:

Tactic Engagement Bonus To Money Multiplier = 0.50

Example:

- Engagement Bonus = 0.15

Tactic Money Multiplier = 1 + 0.15 × 0.50 = 1.075

The tactic gives 7.5% more base Money.

### Tactic Credibility loss

Tactic Credibility Loss = round(Credibility Cost × Tactic Credibility Cost To Credibility Loss)

Current conversion value:

Tactic Credibility Cost To Credibility Loss = 100

Example:

- Credibility Cost = 0.08

Credibility Loss = round(0.08 × 100) = 8

---

## 9. Low-Credibility Reach Formula

Edit under:

MisinformationMetricFormulaProfile > Low Credibility Reach Loss

When enabled:

Credibility Reach Multiplier = linear interpolation from Zero-Credibility Multiplier to 1.00

Current value:

Zero Credibility Growth Multiplier = 0.35

Examples when maximum Credibility is 100:

| Current Credibility | Approximate reach multiplier |
|---:|---:|
| 100 | 1.00 |
| 75 | 0.84 |
| 50 | 0.675 |
| 25 | 0.5125 |
| 0 | 0.35 |

Low Credibility therefore makes future posts less effective.

---

## 10. Followers Formula

Followers Delta = round(Base Followers × Tactic Growth Multiplier × Quality Growth Multiplier × Repetition Growth Multiplier × Credibility Reach Multiplier) - Repetition Follower Loss

Current Base Followers is 8.

The result can be positive, zero, or negative depending on the curve and penalties.

---

## 11. Money Formula

Money Delta = round(Base Money × Tactic Money Multiplier + Followers Delta × Money Per Follower Gained) - Repetition Money Loss

Important:

- Money uses the final Follower delta.
- A negative Follower delta can reduce Money.
- Fact Check losses are applied afterward.

---

## 12. Likes Formula

Likes Delta = round(Base Likes + max(0, Followers Delta) × Likes Per Follower Gained)

Important:

- Negative Follower changes do not create negative extra Likes.
- Likes are clamped to at least zero after being applied.

---

## 13. Credibility Formula

Edit under:

MisinformationMetricFormulaProfile > Credibility Formula

Credibility begins with the tactic's direct cost:

Credibility Delta = -round(Tactic Credibility Cost × Credibility Cost Conversion)

Then the post is classified as effective or ineffective.

### Effective misinformation

If:

Combined Quality >= Quality Score Threshold For Effective Post

then:

Additional Credibility Loss = round(Successful Misinformation Credibility Loss × max(0, Quality Growth Multiplier))

Current values:

- Threshold = 0.25
- Successful Misinformation Credibility Loss = 2

This represents a convincing misinformation post spreading farther and creating more long-term reputational risk.

### Bad or confusing post

If:

Combined Quality < threshold

then:

Additional Credibility Loss = round(Wrong Post Credibility Loss × absolute value of Combined Quality)

Current value:

- Wrong Post Credibility Loss = 5

Finally:

Final Credibility Delta = Tactic Loss - Quality-Based Loss - Repetition Credibility Loss - Fact Check Credibility Loss

---

## 14. Fact Check Events

Edit under:

MisinformationMetricFormulaProfile > Fact Check Events

Current values:

| Field | Current value | Meaning |
|---|---:|---|
| Enable Fact Check Events | On | Allows Fact Check penalties |
| Fact Check Credibility Threshold | 35 | Triggers when projected Credibility is 35 or below |
| Fact Check Cooldown Days | 3 | Minimum days between Fact Checks |
| Fact Check Credibility Loss | 6 | Additional fixed Credibility loss |
| Fact Check Follower Loss Percent | 0.08 | Loses 8% of projected Followers |
| Fact Check Money Loss Percent | 0.03 | Loses 3% of projected Money |

Percentages use decimal form:

- 0.08 = 8%
- 0.03 = 3%
- 0.20 = 20%

Fact Check calculation:

1. Calculate the normal post result.
2. Calculate projected Credibility.
3. If projected Credibility is at or below the threshold and cooldown has passed, trigger Fact Check.
4. Remove the configured percentage of projected Followers and Money.
5. Remove the fixed extra Credibility amount.

---

## 15. Repetition and Mistake Penalties

Edit under:

MisinformationMetricFormulaProfile > Repetition / Wrong Choice Penalties

Current priority:

1. Wrong Word Choice — priority 50
2. Wrong Tactic — priority 40
3. Same Caption Template — priority 40
4. Same Tactic — priority 30
5. Same Subtopic — priority 20

Use Only Highest Priority Penalty is currently enabled.

This means only one penalty normally applies per post.

### Important tie behavior

Wrong Tactic and Same Caption Template both have priority 40.

In the current engine, Same Caption Template is checked before Wrong Tactic. Therefore, if both are active with the same priority, Same Caption Template remains the selected main penalty.

To make Wrong Tactic always win that tie, change its priority from 40 to 41.

### Meaning of every penalty field

| Field | Meaning |
|---|---|
| Reason | Type of repetition or mistake |
| Priority | Higher value wins when only the strongest penalty is used |
| Free Streak Days | Number of consecutive uses allowed before punishment begins |
| Decay Per Extra Day | How quickly the growth multiplier falls |
| Min Growth Multiplier | Lowest growth multiplier this penalty can reach |
| Exponential Penalty Growth | How fast direct metric losses increase |
| Follower Loss Base | Base value used for direct Follower loss |
| Money Loss Base | Base value used for direct Money loss |
| Credibility Loss Base | Base value used for direct Credibility loss |

### Penalty activation

Extra Streak = Current Streak - Free Streak Days

The penalty is inactive while Extra Streak is 0 or lower.

### Growth-decay formula

Repetition Growth Multiplier = max(Min Growth Multiplier, e^(-Decay Per Extra Day × Extra Streak))

### Direct metric-loss formula

Direct Loss = round(Base Loss × (e^(Exponential Penalty Growth × Extra Streak) - 1))

The direct-loss formula is calculated separately for Followers, Money, and Credibility.

---

## 16. Current Penalty Defaults

### Wrong Word Choice

| Field | Value |
|---|---:|
| Priority | 50 |
| Free Streak Days | 1 |
| Decay Per Extra Day | 0.55 |
| Min Growth Multiplier | -0.65 |
| Exponential Penalty Growth | 0.45 |
| Follower Loss Base | 5 |
| Money Loss Base | 1 |
| Credibility Loss Base | 4 |

This is the strongest penalty.

### Wrong Tactic

| Field | Value |
|---|---:|
| Priority | 40 |
| Free Streak Days | 1 |
| Decay Per Extra Day | 0.45 |
| Min Growth Multiplier | -0.35 |
| Exponential Penalty Growth | 0.38 |
| Follower Loss Base | 3 |
| Money Loss Base | 0 |
| Credibility Loss Base | 3 |

### Same Caption Template

| Field | Value |
|---|---:|
| Priority | 40 |
| Free Streak Days | 2 |
| Decay Per Extra Day | 0.40 |
| Min Growth Multiplier | -0.25 |
| Exponential Penalty Growth | 0.34 |
| Follower Loss Base | 3 |
| Money Loss Base | 0 |
| Credibility Loss Base | 2 |

### Same Tactic

| Field | Value |
|---|---:|
| Priority | 30 |
| Free Streak Days | 3 |
| Decay Per Extra Day | 0.28 |
| Min Growth Multiplier | 0.10 |
| Exponential Penalty Growth | 0.25 |
| Follower Loss Base | 2 |
| Money Loss Base | 0 |
| Credibility Loss Base | 1 |

### Same Subtopic

| Field | Value |
|---|---:|
| Priority | 20 |
| Free Streak Days | 3 |
| Decay Per Extra Day | 0.18 |
| Min Growth Multiplier | 0.20 |
| Exponential Penalty Growth | 0.18 |
| Follower Loss Base | 1 |
| Money Loss Base | 0 |
| Credibility Loss Base | 1 |

---

## 17. Metric Ranges

Edit under:

MisinformationMetricFormulaProfile > Metric Range

Current values:

| Setting | Current value |
|---|---:|
| Clamp Money | On |
| Maximum Money | 100 |
| Clamp Followers | On |
| Maximum Followers | 100 |
| Clamp Credibility | On |
| Maximum Credibility | 100 |

When clamping is enabled, the metric remains between 0 and its maximum.

If Followers should behave like a large social-media count instead of a 0–100 ending score, increase Max Followers or turn off Clamp Followers.

---

## 18. JSON Scoring Settings

Edit under:

MisinformationMetricFormulaProfile > JSON-Based Scoring

Recommended current settings:

| Setting | Current value | Meaning |
|---|---|---|
| Use JSON Scoring | On | Reads caption/word categories from DailyPostData.json |
| Use Blank Words As Correct Answers | On | Uses each bracket answer as its fallback correct answer |
| Allow Any Blank Word As Correct | Off | Preserves exact blank order |
| Default Unlisted Word Quality | Wrong | An unclassified word is treated as Wrong |
| Default Caption Quality | Correct | A caption without captionQuality is treated as Correct |
| Use JSON Tactic Fit | On | Reads caption-level tactic-fit lists |
| Default Unlisted Tactic Fit Quality | Neutral | Unlisted tactics are accepted without bonus |

Do not turn on Allow Any Blank Word As Correct for ordered captions.

---

## 19. Optional Global Word Lists

Edit under:

MisinformationMetricFormulaProfile > Optional Global Word Lists

These lists override caption-level and blank-level scoring everywhere.

Examples:

- A word in Global Nonsense Words is Nonsense in every caption.
- A word in Global Correct Words is Correct in every caption.

Use these lists carefully. They can override the intended ordered-blank result.

For most content, leave these lists empty and configure words per blank in DailyPostData.json.

---

## 20. Save and History Settings

The MisinformationMetricEngine scene component contains:

- Save History To Player Prefs
- Save Prefix

Recommended:

- Save History To Player Prefs = On
- Save Prefix = MisinformationMetrics_

Do not change the prefix after release unless you intentionally want the game to stop reading the old repetition history.

To clear test history:

1. Select the GameObject with MisinformationMetricEngine.
2. Open the component menu.
3. Choose Reset Metric History.

---

## 21. What to Change for Common Balancing Goals

### Increase overall progression

Increase:

- Base Followers Per Post
- Base Money Per Post
- Money Per Follower Gained
- Quality curve values

### Make word choices matter more

Increase:

- Word Choice Quality Weight
- Difference between Correct, Half Correct, Wrong, and Nonsense scores

Decrease:

- Caption Quality Weight or Tactic Fit Weight

### Make tactics matter more

Increase:

- Tactic Fit Weight
- Tactic Engagement Bonus To Growth Multiplier
- Engagement Bonus on individual TacticSO assets

### Make misinformation more costly

Increase:

- Tactic Credibility Cost To Credibility Loss
- Successful Misinformation Credibility Loss
- Wrong Post Credibility Loss
- Fact Check losses

### Make repetition harsher

Decrease:

- Free Streak Days
- Min Growth Multiplier

Increase:

- Decay Per Extra Day
- Exponential Penalty Growth
- Follower, Money, or Credibility Loss Base

### Make repetition more forgiving

Increase:

- Free Streak Days
- Min Growth Multiplier

Decrease:

- Decay Per Extra Day
- Exponential Penalty Growth
- Direct loss bases

### Trigger Fact Checks earlier

Increase:

- Fact Check Credibility Threshold

Example:

- Threshold 35 triggers at 35 or below.
- Threshold 50 triggers at 50 or below, so it triggers earlier.

---

## 22. Recommended Rule for the Team

Use the JSON editor to decide **what is Correct, Half Correct, Neutral, Wrong, Nonsense, Ideal, Neutral, or Bad**.

Use MisinformationMetricFormulaProfile.asset to decide **how much each category changes the game**.

Do not edit MisinformationMetricEngine.cs for ordinary balancing.
