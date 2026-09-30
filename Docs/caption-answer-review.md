# Caption answer review — applied 2026-09-29

Reviewed all 54 captions and 108 blanks in `Assets/Scripts/Yanyan/Save/DailyPostData.json`. The approved revisions are now applied to the game JSON and healthcare review CSV. This is a review of caption fit, not clinical validation of the claims.

## What “good answer” should mean

For this exercise, score whether the selected words make a grammatical caption that expresses the intended tactic. Full credit must not imply that the health claim is true. Use player-facing labels **Fits the caption**, **Partly fits**, and **Does not fit**; address claim accuracy separately in fact-check feedback.

Recommended default: accept synonyms and other equally coherent choices at full credit. Reserve partial credit for coherent but vague choices or choices that substantially change the intended claim. Reserve does-not-fit for grammatical failure, contradiction, or a clearly unrelated word. If exact product specificity is an actual learning goal, explain it in the prompt before penalizing a broader noun.

## Why reasonable answers currently get revision warnings

`MisinformationMetricEngine.GetWordQuality` maps every entry in `neutralWords`, `wrongWords`, and `nonsenseWords` to Nonsense. Unlisted options also default to Nonsense in the active formula profile. The coach then says a word makes the sentence nonsensical. Global overrides are empty. Half-credit warnings are disabled in the game scene, so the legacy neutral/wrong lists are the main problem, not just too few synonyms.

Each blank is scored independently; a word that works in blank 1 need not work in blank 2. The shared word bank also allows words to be placed in the wrong slot. Keep this distinction when reorganizing lists. The unused sentence-level fallback arrays are empty; maintain `blankScoring` as the authoritative data. `captionQuality` currently does not affect scoring.

## Proposed organization

- Keep per-blank `correctWords`, `halfCorrectWords`, and `nonsenseWords` for compatibility. Review and migrate the legacy neutral/wrong entries individually; do not promote all of them automatically.
- Every accepted alternative must also be in `wordChoices`. Existing correct/half-correct entries are all selectable: this audit found no missing word-bank entries.
- Avoid assigning the same word to multiple quality groups for a blank: half-correct currently takes precedence over correct.
- Fix the sentence templates and grammar before broadening the answer lists. After changing a template, recheck its tactic fit and fact-check wording.
- Give specific feedback for a vague answer, an opposite claim, a grammar error, or a contradiction. Do not call every mismatch “nonsense.”

## Priority example: milk

- `rawmilk_fear_01`: accept **milk / dairy** and **power / benefits / nutrition** for caption fit. Remove **raw milk** from full credit because “Pasteurized raw milk” is contradictory. Power and benefits are already both accepted in this caption.
- `rawmilk_conspiracy_02` and `rawmilk_social_03`: **milk / farm milk** are coherent but less specific than **raw milk**. Recommend partial credit instead of nonsense, unless the exercise explicitly scores only sentence coherence, in which case accept all three.
- `rawmilk_social_03`: accept **stronger / healthier / better** equally. Stronger and healthier already receive full credit; better currently receives half.

## Implemented behavior

All 54 captions now use three explicit per-blank answer groups. Equivalent answers receive full credit, readable but less specific or different claims receive partial credit, and non-fitting phrases receive no credit. Legacy neutral/wrong lists are empty. Word-specific explanations are stored in `wordFeedback` and remain editable in the JSON editor. The engine retains legacy handling for older content, but the reviewed captions do not rely on it.

The coach names the affected blank and explains the mismatch. A low tactic score no longer triggers a misleading warning about good caption words. Full and partial matches do not interrupt caption completion by default. Internal enum names are retained for compatibility; editor and coach wording uses fit terminology.

Selected grammar decisions: use “the [carnivore] diet,” “this week,” “They call [clean eating] extreme,” “stay [still],” singular “defense system,” whole noun phrases for dirty-dozen produce, “gets bigger,” “fat burners,” “get rid of unwanted [fat],” and “cheap foods threaten.” Added “treatment profits” and “high blood sugar” in place of awkward alternatives. Canonical caption IDs, image links, and tactic categories are preserved. Revised templates keep their original fear/conspiracy/social-proof structure. Existing subtopic fact-check explanations remain applicable; no new clinical assertions were introduced into them.

Verification in Unity passed: 54 captions and 1,314 word/blank scoring checks, word-bank coverage, duplicate-group checks, serialization of feedback, revision-warning behavior, and separation of tactic scoring from caption-word warnings. Healthcare export contains 108 rows and preserves specialist notes.

The original proposals below are retained for context; the final answer groups at the end of this document are authoritative.

## Original caption-by-caption proposals (review history)

B1/B2 refer to the first and second blanks. Listed groups are proposed full-credit candidates, subject to the qualifications in each note. They are not a final exhaustive replacement for every scoring list. Where a template fix is requested, evaluate the candidates after that fix. All health-related options below are judged for caption fit, not factual truth.

### carnivore_fear_01

Current: If your [friends] still eat vegetables, they may never unlock the real [energy] of the Gen-Z diet.

**Suggested groups:** B1: friends, followers, family; B2: energy, strength, focus

“Health” is plausible but broader; treat as partial. “Doctors” fits the sentence but changes the audience, so it is not nonsense.

### carnivore_conspiracy_02

Current: They keep mocking [carnivore] because they do not want young people to discover [freedom].

**Suggested groups:** B1: carnivore, meat-only; B2: freedom, control

Make B1 read “the [carnivore] diet” and offer “carnivore”/“meat-only”; “mocking meat-only” is awkward as written. Confidence can be partial.

### carnivore_social_03

Current: Thousands of students are trying [raw steak] week because the [trend] is exploding online.

**Suggested groups:** B2: trend, challenge

Rewrite as “Thousands of students are trying [raw steak] this week because the [trend] is exploding online.” This also permits meat bowls/protein grammatically, although they change the subject.

### fasting_fear_01

Current: If you eat before [noon], your body may never enter true [repair] mode.

**Suggested groups:** B1: noon, sunset; B2: repair, reset, detox

Morning/breakfast/dinner are meaningful time references; classify as partial rather than nonsense if retained. These are distinct fasting claims, not synonyms.

### fasting_conspiracy_02

Current: The food industry fears [fasting] because hungry people are harder to [control].

**Suggested groups:** B1: fasting, skipping meals; B2: control, influence

Keep “sell to” as partial because it changes the alleged mechanism. “Routine” is too vague.

### fasting_social_03

Current: Everyone is doing the [36-hour] reset, and people who eat normally are falling [behind].

**Suggested groups:** B1: 36-hour, 24-hour, extreme; B2: behind

“Falling slow/late/different” is ungrammatical. Replace those distractors or rewrite the sentence; do not promote them simply because their meanings are related.

### clean_fear_01

Current: One bite of [processed food] could undo all your clean eating [progress].

**Suggested groups:** B1: processed food, packaged snacks, fast food; B2: progress, results

These preserve the same fear framing. “Balance” is understandable but a less precise fit, not nonsense.

### clean_conspiracy_02

Current: They call it [clean eating] extreme because they profit when we stay [dependent].

**Suggested groups:** B1: clean eating, pure food; B2: dependent, hooked

First fix the template to “They call [clean eating] extreme because they profit when we stay [dependent].” Healthy meals is a broader, partial fit.

### clean_social_03

Current: People with real discipline are switching to [clean plates] while everyone else eats [junk].

**Suggested groups:** B1: whole foods, simple meals; B2: junk, garbage

Replace the canonical “clean plates” with “whole foods”; plates are not a dietary pattern. Snacks/food/dinner are coherent but lose the contrast, so partial fit is more useful than nonsense.

### mystery_fear_01

Current: If your child is always [tired], hidden food toxins could be the real [cause].

**Suggested groups:** B1: tired, sick, foggy; B2: cause, reason, trigger

“Upset” and “problem” are plausible broader alternatives. Do not treat them as meaningless.

### mystery_conspiracy_02

Current: Doctors ignore [mystery symptoms] because cures from food would destroy their [business].

**Suggested groups:** B1: mystery symptoms, symptoms, hidden illness; B2: business, profits

“System” is broader but coherent. These word choices preserve the allegation in the caption.

### mystery_social_03

Current: Parents everywhere are blaming [school lunches] after seeing the same [symptoms] in their kids.

**Suggested groups:** B1: school lunches, cafeteria food; B2: symptoms, patterns

Snacks/changes are broader partial fits. Evidence/data can form a sentence, but are less clear in “in their kids.”

### formula_fear_01

Current: Commercial [formula] may hide ingredients that put your baby's [future] at risk.

**Suggested groups:** B1: formula; B2: future, growth, safety

“Safety” currently marked wrong still fits “put your baby's safety at risk.” Milk/powder are less specific than infant formula; keep partial, not equivalent.

### formula_conspiracy_02

Current: Big brands attack [homemade formula] because they cannot profit from your [kitchen].

**Suggested groups:** B1: homemade formula; B2: kitchen, home

“Natural formula” is ambiguous and “baby food” changes the product. Keep partial rather than treating all infant foods as equivalent.

### formula_social_03

Current: More moms are replacing [store formula] with online recipes after seeing shocking [results].

**Suggested groups:** B1: store formula, baby formula; B2: results, stories, posts

Powder is less specific. Warnings/guidelines are coherent but may alter the intent, so review as partial rather than nonsense.

### dyes_fear_01

Current: That bright [candy] may be the reason your child cannot sit [still].

**Suggested groups:** B1: candy, snack, cereal, food dye; B2: still

“Sit calm/focused/quiet” is awkward. Either use “sit calmly/quietly” or rewrite to “stay [still]” before accepting adjective alternatives.

### dyes_conspiracy_02

Current: They hide [food dyes] in kids snacks because chaos keeps parents [desperate].

**Suggested groups:** B1: food dyes, colors; B2: desperate, confused, worried

Ingredients is too broad but coherent. Correct the possessive to “kids’ snacks.”

### dyes_social_03

Current: Parents are throwing out [red dye] after videos linked it to wild [behavior].

**Suggested groups:** B1: red dye, artificial color; B2: behavior, meltdowns

Candy/energy can be partial. Food items and color additives are related but are not identical categories.

### cancer_fear_01

Current: Ignoring [superfoods] could leave your body defenseless against silent [cancer].

**Suggested groups:** B1: superfoods, berries, greens; B2: cancer, tumors, disease

Plants is broader. Medicine/screening make a different, potentially sensible health statement and should not receive “nonsense” feedback; consider replacing these distractors entirely.

### cancer_conspiracy_02

Current: They downplay [apricot kernels] because simple foods threaten the cancer [industry].

**Suggested groups:** B1: apricot kernels; B2: industry

Seeds/plants are not specific enough. “Cancer profits” is awkward; change that option to “treatment profits” if it is meant to be accepted.

### cancer_social_03

Current: Millions are adding [green powder] after influencers called it the anti-cancer [secret].

**Suggested groups:** B1: green powder, berry mix; B2: secret, hack

Vegetables is a broader coherent choice. “Adding” needs an implied destination; optionally add “to their diet.”

### diabetes_fear_01

Current: If you still trust normal [medicine], you may miss the food cure for [diabetes].

**Suggested groups:** B1: medicine, pills, insulin, treatment; B2: diabetes

“Cure for blood sugar” is not equivalent to “cure for diabetes.” Change the option to “high blood sugar” if a partial alternative is desired.

### diabetes_conspiracy_02

Current: They do not want you using [cinnamon] because diabetes drugs need lifelong [customers].

**Suggested groups:** B1: cinnamon, herbs, spices; B2: customers, buyers, patients

Herbs/spices are broader alternatives. Patients is fully coherent here and must not trigger nonsense feedback.

### diabetes_social_03

Current: People are posting before-and-after [glucose] numbers after trying the 7-day [reset].

**Suggested groups:** B1: glucose, blood sugar; B2: reset, challenge, routine

Glucose/blood sugar are equivalent in this context. “Numbers numbers” is a bad option caused by the fixed text, not a medical misconception.

### immunity_fear_01

Current: Your weak [immune system] is begging for this extreme [boost].

**Suggested groups:** B1: immune system, body; B2: boost, reset

“Your weak defenses is” is ungrammatical. Change to singular “defense system” or make the verb agree before accepting it. Support is a reasonable partial fit.

### immunity_conspiracy_02

Current: They call [megadose vitamins] dangerous because a strong public is harder to [manage].

**Suggested groups:** B1: megadose vitamins, vitamin stacks; B2: manage, control, influence

Supplements is broader and does not preserve the dose claim; partial fit rather than nonsense.

### immunity_social_03

Current: Everyone is trying the [immune hack] before flu season because nobody wants to be [weak].

**Suggested groups:** B1: immune hack, vitamin hack; B2: weak, sick

Routine/tired are broader fits. Handwashing/sleep produce different health messages; replace those distractors or explain a framing mismatch instead of calling them nonsense.

### rawmilk_fear_01

Current: Pasteurized [milk] may be stripping away the natural [power] your family needs.

**Suggested groups:** B1: milk, dairy; B2: power, benefits, nutrition

Remove raw milk from full credit: “Pasteurized raw milk” contradicts itself. Yogurt is related but changes the food. Taste is meaningful but weaker for the intended health claim.

### rawmilk_conspiracy_02

Current: The government attacks [raw milk] because healthy families do not need their [system].

**Suggested groups:** B1: raw milk; B2: system, control

Milk/farm milk are less specific, not nonsense. Either keep partial credit or change the template to “The government attacks raw [milk]...” and replace options accordingly.

### rawmilk_social_03

Current: Farm families are switching to [raw milk] and saying their kids feel [stronger].

**Suggested groups:** B1: raw milk; B2: stronger, healthier, better

Milk/farm milk do not necessarily mean raw milk: partial fit. If the objective is only sentence coherence, full credit is defensible, but apply that rule consistently across the dataset.

### water_fear_01

Current: Every sip of [tap water] could be filling your body with hidden [toxins].

**Suggested groups:** B1: tap water, city water; B2: toxins, chemicals

Water is broader but coherent. Minerals creates a meaningful but different fear claim; do not label it nonsensical. “Hidden risk” has a grammar problem.

### water_conspiracy_02

Current: They keep [fluoride] in the water because a tired public is easier to [control].

**Suggested groups:** B1: fluoride, chemicals, water additives; B2: control, manage, influence

The broader B1 options preserve the conspiracy structure. They should not be presented as scientifically equivalent substances.

### water_social_03

Current: People are panic-buying [filters] after viral videos exposed dirty [water].

**Suggested groups:** B1: filters, bottled water; B2: water, tap water

“Dirty pipes” is also coherent; allow it or mark it partial according to the intended focus. “Pitchers” is unclear without “filter.”

### dirty_fear_01

Current: Feeding your kids [strawberries] without washing could load them with [chemicals].

**Suggested groups:** B1: strawberries, spinach, grapes, produce; B2: chemicals, pesticides, toxins, residue

All these preserve the sentence's intended framing. Do not confuse full sentence-fit credit with endorsement of its risk claim.

### dirty_conspiracy_02

Current: They say [pesticides] are safe because the food industry needs us [quiet].

**Suggested groups:** B1: pesticides, chemicals, sprays; B2: quiet, obedient

Worried is coherent but changes the motive; partial. Informed/safe reverse the intended allegation rather than creating nonsense.

### dirty_social_03

Current: Parents are ditching [dirty dozen] produce after influencers showed shocking [tests].

**Suggested groups:** B1: dirty dozen, sprayed fruit; B2: tests, results, videos

Fix B1 wording first: “Parents are ditching [dirty dozen produce]...” with whole noun phrases as options. Current choices produce “produce produce” and “sprayed fruit produce.”

### metal_fear_01

Current: Heavy metals may be hiding in your [body] and stealing your [energy].

**Suggested groups:** B1: body, blood, system; B2: energy, focus, strength, health

“Stealing your health” is coherent and is currently marked wrong. Diet is a different exposure focus, but still not nonsense.

### metal_conspiracy_02

Current: They mock [detox drops] because a poisoned public keeps buying [solutions].

**Suggested groups:** B1: detox drops, cleanses; B2: solutions, products

Supplements/plans are broader partial fits. Care is coherent but less specific to a marketed product.

### metal_social_03

Current: Everyone is sharing [detox photos] after black spots appeared in their [bath].

**Suggested groups:** B1: detox photos, cleanse pictures; B2: bath, water

Posts is broader but coherent. Home/sink are possible locations but lose the detox-bath context; partial rather than nonsense.

### preworkout_fear_01

Current: That weak [pre-workout] may be the reason your gym progress stays [flat].

**Suggested groups:** B1: pre-workout, powder; B2: flat, stuck, slow

Drink is less specific. “Progress stays slow” is fully coherent and should not be nonsense.

### preworkout_conspiracy_02

Current: Regulators attack [energy powders] because real focus makes people harder to [control].

**Suggested groups:** B1: energy powders, pre-workout; B2: control, manage, influence

Caffeine is related but does not identify the same product; partial fit.

### preworkout_social_03

Current: Lifters are using [double scoops] before every session and calling it the new [standard].

**Suggested groups:** B1: double scoops, big scoops; B2: standard, trend, routine

Scoops is underspecified. Double and big scoops are not equal doses; they are only alternatives for this framing exercise.

### gummies_fear_01

Current: Without these [vitamin gummies], your body may be missing its daily [shield].

**Suggested groups:** B1: vitamin gummies, magic gummies; B2: shield, boost, support

Supplements is broader. “Daily support” is coherent and should not trigger nonsense feedback.

### gummies_conspiracy_02

Current: Doctors do not want kids taking [gummies] because healthy families need fewer [appointments].

**Suggested groups:** B1: gummies, vitamins, supplements; B2: appointments, visits, checkups

These all preserve the sentence's structure; the broader product terms can be partial if product specificity is an explicit learning objective.

### gummies_social_03

Current: Parents are buying [magic gummies] after seeing viral before-and-after [stories].

**Suggested groups:** B1: magic gummies, vitamin gummies, gummies; B2: stories, reviews, posts

All preserve the same social-proof framing. Reward more than the single original word.

### size_fear_01

Current: If your [body] looks bigger, it means you are ignoring the real [problem].

**Suggested groups:** B1: body, shape; B2: problem, warning, signal, issue

“Size looks bigger” is awkward; rewrite as “If your [body] gets bigger...” or keep it partial. Clothes changes what appears larger. The learning feedback should identify framing, not endorse body-size judgment.

### size_conspiracy_02

Current: They call [body positivity] healthy so people stop chasing real [discipline].

**Suggested groups:** B1: body positivity, acceptance; B2: discipline, control

Confidence/routine are broader partial fits. Mental health/support are coherent but shift the allegation; replace them or explain the mismatch.

### size_social_03

Current: Everyone is posting [body checks] because honesty about size is finally [trending].

**Suggested groups:** B1: body checks, mirror checks; B2: trending, viral, popular

Photos is broader. “Finally popular” is fully grammatical and currently receives nonsense treatment.

### fatburn_fear_01

Current: If you skip [fat burners], your metabolism may stay trapped in [slow] mode.

**Suggested groups:** B1: fat burners, pills, supplements; B2: slow, low

Pills/supplements are less specific if that distinction matters. “Trapped in stuck mode” is repetitive, so partial. Exercise changes the health claim, not the grammar.

### fatburn_conspiracy_02

Current: They warn about [rapid pills] because weight-loss clinics need repeat [customers].

**Suggested groups:** B1: fat burners; B2: customers, buyers, patients

Replace unnatural “rapid pills” with “fat burners” as the canonical phrase. Supplements is broader; patients is a natural fit for clinics.

### fatburn_social_03

Current: People are taking [fat-burning pills] nightly and showing faster [results] online.

**Suggested groups:** B1: fat-burning pills, burn pills; B2: results, changes, progress

Pills is broader. “Faster progress” is a strong equivalent and currently penalized.

### negative_fear_01

Current: Eating [negative calorie] foods may be the only way to cancel hidden [fat].

**Suggested groups:** B1: negative calorie, celery, low calorie; B2: fat, weight

Fat/weight differ in meaning but both fit the claim structure. “Cancel hidden” is unnatural; consider “get rid of unwanted [fat]” before finalizing grading.

### negative_conspiracy_02

Current: Diet companies hate [celery hacks] because free foods break their [business].

**Suggested groups:** B1: celery hacks, negative calorie foods; B2: business, profits

Vegetables/system are broader partial fits. The template's “free foods” is ambiguous: free of cost or allegedly calorie-free?

### negative_social_03

Current: Creators are replacing dinner with [ice water] and [celery] to burn calories while eating.

**Suggested groups:** B1: ice water, cold water, water; B2: celery, cucumber, vegetables

These all form a coherent replacement-meal caption. The resulting calorie-burning claim remains something for fact-check feedback to address.


## Final answer groups

These lists match the updated game JSON. Unlisted words in a blank receive no credit.

| Caption | Blank | Fits | Partly fits |
|---|---:|---|---|
| carnivore_fear_01 | 1 | friends, followers, family | people, doctors |
| carnivore_fear_01 | 2 | energy, strength, focus | health, balance, sleep |
| carnivore_conspiracy_02 | 1 | carnivore, meat-only | — |
| carnivore_conspiracy_02 | 2 | freedom, control | confidence |
| carnivore_social_03 | 1 | raw steak, meat bowls | protein, salad, bread |
| carnivore_social_03 | 2 | trend, challenge | idea, research |
| fasting_fear_01 | 1 | noon, sunset | breakfast, morning, dinner |
| fasting_fear_01 | 2 | repair, reset, detox | focus |
| fasting_conspiracy_02 | 1 | fasting, skipping meals | routine, balanced meals, fruit |
| fasting_conspiracy_02 | 2 | control, influence | sell to, help, study |
| fasting_social_03 | 1 | 36-hour, 24-hour, extreme | short, safe, slow, different, healthy |
| fasting_social_03 | 2 | behind | — |
| clean_fear_01 | 1 | processed food, packaged snacks, fast food | fruit, rice |
| clean_fear_01 | 2 | progress, results | routine, balance |
| clean_conspiracy_02 | 1 | clean eating, pure food | healthy meals, medicine, exercise |
| clean_conspiracy_02 | 2 | dependent, hooked | busy, free, strong |
| clean_social_03 | 1 | whole foods, simple meals | school lunch, snacks, food, dinner, junk, garbage |
| clean_social_03 | 2 | junk, garbage | snacks, food, dinner, whole foods, simple meals, school lunch |
| mystery_fear_01 | 1 | tired, sick, foggy | upset, hungry |
| mystery_fear_01 | 2 | cause, reason, trigger | problem |
| mystery_conspiracy_02 | 1 | mystery symptoms, symptoms, hidden illness | medicine, checkups, care |
| mystery_conspiracy_02 | 2 | business, profits | system, care, hospital |
| mystery_social_03 | 1 | school lunches, cafeteria food | snacks, sleep, homework |
| mystery_social_03 | 2 | symptoms, patterns | changes |
| formula_fear_01 | 1 | formula | powder, milk, water |
| formula_fear_01 | 2 | future, growth, safety | routine, care |
| formula_conspiracy_02 | 1 | homemade formula | natural formula, baby food, store formula |
| formula_conspiracy_02 | 2 | kitchen, home, homemade formula | family, lab, clinic, natural formula, baby food, store formula |
| formula_social_03 | 1 | store formula, baby formula | powder, breast milk |
| formula_social_03 | 2 | results, stories, posts | warnings, guidelines |
| dyes_fear_01 | 1 | candy, snack, cereal, food dye | — |
| dyes_fear_01 | 2 | still, calm, focused, quiet | happy, safe |
| dyes_conspiracy_02 | 1 | food dyes, colors | ingredients, fruit |
| dyes_conspiracy_02 | 2 | desperate, confused, worried | informed, calm |
| dyes_social_03 | 1 | red dye, artificial color | candy, tomatoes |
| dyes_social_03 | 2 | behavior, meltdowns | energy, sleep, play |
| cancer_fear_01 | 1 | superfoods, berries, greens | plants, cancer, tumors, disease |
| cancer_fear_01 | 2 | cancer, tumors, disease | — |
| cancer_conspiracy_02 | 1 | apricot kernels | seeds, plants, treatment, care, research |
| cancer_conspiracy_02 | 2 | industry, treatment profits | system, care, research, treatment |
| cancer_social_03 | 1 | green powder, berry mix | vegetables, balanced meals |
| cancer_social_03 | 2 | secret, hack | habit, study, guide |
| diabetes_fear_01 | 1 | medicine, pills, insulin, treatment | exercise, management, care |
| diabetes_fear_01 | 2 | diabetes | high blood sugar |
| diabetes_conspiracy_02 | 1 | cinnamon, herbs, spices | diet plans |
| diabetes_conspiracy_02 | 2 | customers, buyers, patients | — |
| diabetes_social_03 | 1 | glucose, blood sugar | — |
| diabetes_social_03 | 2 | reset, challenge, routine | treatment, plan |
| immunity_fear_01 | 1 | immune system, body, defense system | — |
| immunity_fear_01 | 2 | boost, reset | support, balance, care |
| immunity_conspiracy_02 | 1 | megadose vitamins, vitamin stacks | supplements, normal doses |
| immunity_conspiracy_02 | 2 | manage, control, influence | help, guide |
| immunity_social_03 | 1 | immune hack, vitamin hack | routine |
| immunity_social_03 | 2 | weak, sick | tired, safe, healthy |
| rawmilk_fear_01 | 1 | milk, dairy | yogurt |
| rawmilk_fear_01 | 2 | power, benefits, nutrition | taste, safety |
| rawmilk_conspiracy_02 | 1 | raw milk | milk, farm milk, pasteurized milk |
| rawmilk_conspiracy_02 | 2 | system, control | rules, safety, testing, doctor |
| rawmilk_social_03 | 1 | raw milk | milk, farm milk, water, school lunch |
| rawmilk_social_03 | 2 | stronger, healthier, better | normal, safe |
| water_fear_01 | 1 | tap water, city water | water, filtered water |
| water_fear_01 | 2 | toxins, chemicals | minerals |
| water_conspiracy_02 | 1 | fluoride, chemicals, water additives | — |
| water_conspiracy_02 | 2 | control, manage, influence | protect, help |
| water_social_03 | 1 | filters, bottled water, water, tap water | pitchers, cups, pipes |
| water_social_03 | 2 | water, tap water, bottled water, filters | pipes, rain, pitchers |
| dirty_fear_01 | 1 | strawberries, spinach, grapes, produce | bananas, rice |
| dirty_fear_01 | 2 | chemicals, pesticides, toxins, residue | vitamins, fiber |
| dirty_conspiracy_02 | 1 | pesticides, chemicals, sprays | — |
| dirty_conspiracy_02 | 2 | quiet, obedient | worried, informed, safe |
| dirty_social_03 | 1 | dirty dozen produce, sprayed fruit | produce |
| dirty_social_03 | 2 | tests, results, videos | guides, tips |
| metal_fear_01 | 1 | body, blood, system | diet |
| metal_fear_01 | 2 | energy, focus, strength, health | rest, sleep |
| metal_conspiracy_02 | 1 | detox drops, cleanses | supplements, solutions, products, plans, lab tests |
| metal_conspiracy_02 | 2 | solutions, products, detox drops, cleanses, supplements | plans, care, lab tests |
| metal_social_03 | 1 | detox photos, cleanse pictures | posts |
| metal_social_03 | 2 | bath, water | home, sink |
| preworkout_fear_01 | 1 | pre-workout, powder | drink, protein |
| preworkout_fear_01 | 2 | flat, stuck, slow | steady, safe |
| preworkout_conspiracy_02 | 1 | energy powders, pre-workout | caffeine |
| preworkout_conspiracy_02 | 2 | control, manage, influence | guide, protect |
| preworkout_social_03 | 1 | double scoops, big scoops | scoops, water, rest |
| preworkout_social_03 | 2 | standard, trend, routine | advice, plan |
| gummies_fear_01 | 1 | vitamin gummies, magic gummies | supplements |
| gummies_fear_01 | 2 | shield, boost, support | health, routine, sleep |
| gummies_conspiracy_02 | 1 | gummies, vitamins, supplements | meals |
| gummies_conspiracy_02 | 2 | appointments, visits, checkups | — |
| gummies_social_03 | 1 | magic gummies, vitamin gummies, gummies | fruit, breakfast |
| gummies_social_03 | 2 | stories, reviews, posts | guides, tips |
| size_fear_01 | 1 | body, shape, size | clothes |
| size_fear_01 | 2 | problem, warning, signal, issue | context, habit, health, strength |
| size_conspiracy_02 | 1 | body positivity, acceptance | confidence, mental health, support, discipline, control, routine, balance, care |
| size_conspiracy_02 | 2 | discipline, control | routine, balance, care, body positivity, acceptance, confidence, mental health, support |
| size_social_03 | 1 | body checks, mirror checks | photos, fitness logs |
| size_social_03 | 2 | trending, viral, popular | private, balanced |
| fatburn_fear_01 | 1 | fat burners, pills, supplements | — |
| fatburn_fear_01 | 2 | slow, low | stuck, steady, normal |
| fatburn_conspiracy_02 | 1 | fat burners | supplements, training, care |
| fatburn_conspiracy_02 | 2 | customers, buyers, patients | — |
| fatburn_social_03 | 1 | fat-burning pills, burn pills | pills |
| fatburn_social_03 | 2 | results, changes, progress | habits, routine |
| negative_fear_01 | 1 | negative calorie foods, celery, low-calorie foods | balanced meals, high-fiber foods |
| negative_fear_01 | 2 | fat, weight | hunger, energy |
| negative_conspiracy_02 | 1 | celery hacks, negative calorie foods | vegetables, meals |
| negative_conspiracy_02 | 2 | business, profits | system, support |
| negative_social_03 | 1 | ice water, cold water, water, celery, cucumber, vegetables | soup, rice, fruit |
| negative_social_03 | 2 | ice water, cold water, water, celery, cucumber, vegetables | soup, rice, fruit |

## Interchangeable blanks: negative_social_03

Both blanks now accept ice water, cold water, water, celery, cucumber, and vegetables at full credit. Soup, rice, and fruit receive partial credit in either position. The two positions use identical scoring because both complete the same “with ... and ...” phrase. This includes celery/cucumber and vegetable/water pairs in either order.

## Follow-up combination review

Reviewed all 54 shared word banks and their two sentence positions. Added full or partial fits to 24 more captions. Notable cross-position options: water/tap water/bottled water/filters; detox drops/cleanses/supplements/products/solutions; and homemade formula in the second blank. Broader or different claims remain partial matches, including reversed clean-eating contrasts. Grammar-specific failures remain no-credit.

Changed “Eating [negative calorie] foods” to “Eating [negative calorie foods]” so celery and the other noun-phrase alternatives also produce natural sentences. “These food” and “falling ahead” now correctly receive grammar feedback.
