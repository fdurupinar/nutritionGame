using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndingStoryCollectionPanel : MonoBehaviour
{
    [Header("Ending Database")]
    [Tooltip("Assign the EndingStoryDatabase that stores the IDs, titles, and stories for all 12 endings.")]
    public EndingStoryDatabase endingDatabase;

    [Header("C1 R1 | Index 0 | Trust Collapse")]
    [Tooltip("Triggers Trust Collapse when final Credibility is less than or equal to this value. Because Credibility is clamped to a minimum of 0, the default should be 0.")]
    [Range(0, 100)]
    public int trustCollapseMaxCredibility = 0;

    [Header("C2 R1 | Index 1 | Forgotten")]
    [Tooltip("Money must be less than this value.")]
    [Min(0)]
    public int forgottenMoneyBelow = 1250;

    [Tooltip("Followers must be less than this value.")]
    [Min(0)]
    public int forgottenFollowersBelow = 2500;

    [Tooltip("Credibility must be less than this value.")]
    [Range(0, 100)]
    public int forgottenCredibilityBelow = 45;

    [Header("C3 R1 | Index 2 | Noise Empire")]
    [Tooltip("Money must be greater than or equal to this value.")]
    [Min(0)]
    public int noiseEmpireMinMoney = 1700;

    [Tooltip("Followers must be greater than or equal to this value.")]
    [Min(0)]
    public int noiseEmpireMinFollowers = 30000;

    [Tooltip("Credibility must be less than this value.")]
    [Range(0, 100)]
    public int noiseEmpireCredibilityBelow = 40;

    [Header("C1 R2 | Index 3 | Viral Beast")]
    [Tooltip("Followers must be greater than or equal to this value.")]
    [Min(0)]
    public int viralBeastMinFollowers = 30000;

    [Tooltip("Credibility must be less than this value.")]
    [Range(0, 100)]
    public int viralBeastCredibilityBelow = 40;

    [Header("C2 R2 | Index 4 | Quiet Cash")]
    [Tooltip("Money must be greater than or equal to this value.")]
    [Min(0)]
    public int quietCashMinMoney = 1700;

    [Tooltip("Followers must be less than this value.")]
    [Min(0)]
    public int quietCashFollowersBelow = 8000;

    [Tooltip("Credibility must be greater than or equal to this value.")]
    [Range(0, 100)]
    public int quietCashMinCredibility = 40;

    [Header("C3 R2 | Index 5 | Broke Star")]
    [Tooltip("Followers must be greater than or equal to this value.")]
    [Min(0)]
    public int brokeStarMinFollowers = 30000;

    [Tooltip("Money must be less than this value.")]
    [Min(0)]
    public int brokeStarMoneyBelow = 1450;

    [Tooltip("Credibility must be greater than or equal to this value.")]
    [Range(0, 100)]
    public int brokeStarMinCredibility = 40;

    [Header("C1 R3 | Index 6 | Powerhouse")]
    [Tooltip("Money must be greater than or equal to this value.")]
    [Min(0)]
    public int powerhouseMinMoney = 1800;

    [Tooltip("Followers must be greater than or equal to this value.")]
    [Min(0)]
    public int powerhouseMinFollowers = 38000;

    [Tooltip("Credibility must be greater than or equal to this value.")]
    [Range(0, 100)]
    public int powerhouseMinCredibility = 75;

    [Header("C2 R3 | Index 7 | Trusted Voice")]
    [Tooltip("Credibility must be greater than or equal to this value.")]
    [Range(0, 100)]
    public int trustedMinCredibility = 75;

    [Tooltip("Followers must be greater than or equal to this value.")]
    [Min(0)]
    public int trustedMinFollowers = 20000;

    [Tooltip("Money must be greater than or equal to this value.")]
    [Min(0)]
    public int trustedMinMoney = 1450;

    [Header("C3 R3 | Index 8 | Honest Path")]
    [Tooltip("Credibility must be greater than or equal to this value.")]
    [Range(0, 100)]
    public int honestPathMinCredibility = 80;

    [Tooltip("Followers must be less than this value.")]
    [Min(0)]
    public int honestPathFollowersBelow = 12000;

    [Tooltip("Money must be less than this value.")]
    [Min(0)]
    public int honestPathMoneyBelow = 1450;

    [Header("C1 R4 | Index 9 | Responsible Influencer")]
    [Tooltip("Money must be greater than or equal to this value.")]
    [Min(0)]
    public int responsibleInfluencerMinMoney = 1450;

    [Tooltip("Credibility must be greater than or equal to this value.")]
    [Range(0, 100)]
    public int responsibleInfluencerMinCredibility = 55;

    [Tooltip("Followers must be greater than or equal to this value.")]
    [Min(0)]
    public int responsibleInfluencerMinFollowers = 12000;

    [Header("C2 R4 | Index 10 | Paid Lies")]
    [Tooltip("Money must be greater than or equal to this value.")]
    [Min(0)]
    public int paidLiesMinMoney = 1700;

    [Tooltip("Credibility must be less than this value.")]
    [Range(0, 100)]
    public int paidLiesCredibilityBelow = 40;

    [Header("C3 R4 | Index 11 | Unclear Legacy")]
    [TextArea(2, 4)]
    [Tooltip("Unclear Legacy has no separate metric threshold. It is the default ending when none of the previous 11 ending conditions match. This text is only an Inspector note and is not used in calculations.")]
    public string unclearLegacyConditionNote =
        "Default fallback: triggers when none of Index 0-10 conditions match.";

    [Header("12 Ending UI Slots")]
    [Tooltip("Each slot represents one ending. The Locked GameObject and Unlocked GameObject can each be a Button directly.")]
    public EndingStorySlotUI[] storySlots = new EndingStorySlotUI[12];

    [Header("Automatic Button Binding")]
    [Tooltip("When enabled, the script automatically adds click events to every Locked Button and Unlocked Button, so you do not need to configure 24 OnClick events manually.")]
    public bool autoBindSlotButtons = true;

    [Header("Story Panel")]
    [Tooltip("Shared story panel. It opens automatically after clicking an unlocked ending or a test-unlock button.")]
    public GameObject storyPanel;

    [Tooltip("Automatically close the Story Panel when the ending collection screen opens.")]
    public bool hideStoryPanelOnOpen = true;

    [Tooltip("Automatically open the Story Panel when an unlocked ending is clicked.")]
    public bool showStoryPanelWhenUnlockedEndingClicked = true;

    [Tooltip("Close the Story Panel when a locked ending is clicked.")]
    public bool hideStoryPanelWhenLockedEndingClicked = true;

    [Header("Shared Story Text")]
    [Tooltip("Shared ending title text. The selected ending title appears here after clicking an unlocked ending.")]
    public TMP_Text sharedTitleTMP;

    [Tooltip("Shared ending story body text. The selected ending story appears here after clicking an unlocked ending.")]
    public TMP_Text sharedStoryTMP;

    [Header("Legacy Text Fallback")]
    [Tooltip("Optional fallback for a standard UI Text title when TMP is not used.")]
    public Text sharedTitleText;

    [Tooltip("Optional fallback for a standard UI Text story body when TMP is not used.")]
    public Text sharedStoryText;

    [Header("First-Unlock Typewriter Animation")]
    [Tooltip("When enabled, the typewriter animation plays only the first time an ending is unlocked. Later clicks show the complete text instantly.")]
    public bool playTypewriterOnlyOnFirstUnlock = true;

    [Tooltip("Number of characters revealed per second. Higher values reveal the text faster.")]
    public float typewriterCharactersPerSecond = 45f;

    [Tooltip("Use unscaled real time for the typewriter animation. When enabled, the animation still plays while Time.timeScale is 0.")]
    public bool typewriterUseUnscaledTime = true;

    [Tooltip("Use a small panel-pop effect during the reveal. Usually leave this enabled.")]
    public bool useJuicyPanelPop = true;

    [Tooltip("Duration of the Story Panel pop animation.")]
    public float panelPopDuration = 0.18f;

    [Range(0.5f, 1f)]
    [Tooltip("Starting scale of the Story Panel pop animation.")]
    public float panelPopStartScale = 0.92f;

    [Range(1f, 1.3f)]
    [Tooltip("Overshoot scale used during the Story Panel pop animation.")]
    public float panelPopOvershootScale = 1.05f;

    [Tooltip("Short delay before the typewriter animation begins.")]
    public float typewriterStartDelay = 0.05f;

    [Header("Locked Ending Panel")]
    [Tooltip("Panel opened when the player clicks a locked ending.")]
    public GameObject lockedPanel;

    [Tooltip("Automatically close the Locked Panel when the ending collection screen opens.")]
    public bool hideLockedPanelOnOpen = true;

    [Tooltip("Automatically close the Locked Panel when an unlocked ending is clicked.")]
    public bool hideLockedPanelWhenUnlockedEndingClicked = true;

    [Header("Locked Panel Text")]
    [Tooltip("Title text inside the Locked Panel. Optional; when unassigned, the panel opens without changing title text.")]
    public TMP_Text lockedPanelTitleTMP;

    [Tooltip("Description text inside the Locked Panel. Optional; when unassigned, the panel opens without changing story text.")]
    public TMP_Text lockedPanelStoryTMP;

    [Tooltip("Optional standard UI Text fallback for the Locked Panel title.")]
    public Text lockedPanelTitleText;

    [Tooltip("Optional standard UI Text fallback for the Locked Panel description.")]
    public Text lockedPanelStoryText;

    [Tooltip("Title displayed in the Locked Panel when a locked ending is clicked.")]
    public string lockedTitle = "Locked";

    [TextArea(2, 5)]
    [Tooltip("Description displayed in the Locked Panel when a locked ending is clicked.")]
    public string lockedStory = "This ending has not been unlocked yet.";

    [Tooltip("When enabled, the Locked Panel title shows the real ending title. Otherwise, it shows the generic locked title.")]
    public bool showEndingTitleOnLockedPanel = false;

    [Tooltip("Not recommended. When enabled, the Locked Panel reveals the real story for an ending that has not been unlocked.")]
    public bool revealStoryOnLockedPanel = false;

    [Header("Refresh Settings")]
    [Tooltip("Automatically refresh the locked and unlocked states of all endings when this panel is enabled.")]
    public bool refreshWhenEnabled = true;

    [Tooltip("Refresh all ending slots once during Start.")]
    public bool refreshOnStart = true;

    [Tooltip("Automatically display the first unlocked ending when the screen opens. Keep this false when the Story Panel should appear only after a click.")]
    public bool showFirstUnlockedOnStart = false;

    private const string SaveKeyPrefix = "ENDING_UNLOCKED_";

    private Coroutine storyRevealCoroutine;
    private Vector3 storyPanelOriginalScale = Vector3.one;
    private bool storyPanelScaleCached = false;

    private void Awake()
    {
        EnsureSlotArray();
        CacheStoryPanelScale();

        if (autoBindSlotButtons)
        {
            BindAllSlotButtons();
        }
    }

    private void Start()
    {
        if (hideLockedPanelOnOpen)
        {
            CloseLockedPanel();
        }

        if (hideStoryPanelOnOpen)
        {
            CloseStoryPanel();
        }

        if (refreshOnStart)
        {
            RefreshAllSlots();
        }

        if (showFirstUnlockedOnStart)
        {
            ShowFirstUnlockedEnding();
        }
    }

    private void OnEnable()
    {
        if (hideLockedPanelOnOpen)
        {
            CloseLockedPanel();
        }

        if (hideStoryPanelOnOpen)
        {
            CloseStoryPanel();
        }

        if (refreshWhenEnabled)
        {
            RefreshAllSlots();
        }
    }

    /// <summary>
    /// Automatically binds click events to all Locked and Unlocked buttons.
    /// </summary>
    public void BindAllSlotButtons()
    {
        EnsureSlotArray();

        for (int i = 0; i < storySlots.Length; i++)
        {
            BindSlotButtons(i);
        }
    }

    /// <summary>
    /// Automatically binds the Locked and Unlocked buttons for one slot.
    /// </summary>
    private void BindSlotButtons(int index)
    {
        if (!IsValidSlotIndex(index))
        {
            return;
        }

        EndingStorySlotUI slot = storySlots[index];

        if (slot == null || slot.buttonsBound)
        {
            return;
        }

        int capturedIndex = index;

        Button lockedButton = FindButton(slot.lockedGameObject);
        Button unlockedButton = FindButton(slot.unlockedGameObject);

        if (lockedButton != null)
        {
            lockedButton.onClick.AddListener(() => OnLockedSlotButtonClicked(capturedIndex));
        }

        if (unlockedButton != null)
        {
            unlockedButton.onClick.AddListener(() => OnUnlockedSlotButtonClicked(capturedIndex));
        }

        slot.buttonsBound = true;
    }

    /// <summary>
    /// Handles a Locked button click by opening the Locked Panel and showing the shared locked message.
    /// </summary>
    private void OnLockedSlotButtonClicked(int index)
    {
        ShowLockedEndingPanel(index);
    }

    /// <summary>
    /// Handles an Unlocked button click by opening the Story Panel and showing the story instantly without the typewriter animation.
    /// </summary>
    private void OnUnlockedSlotButtonClicked(int index)
    {
        ShowEndingByIndex(index);
    }

    /// <summary>
    /// Refreshes all 12 ending UI slots.
    /// Unlocked ending: hides the Locked object and shows the Unlocked object.
    /// Locked ending: shows the Locked object and hides the Unlocked object.
    /// </summary>
    public void RefreshAllSlots()
    {
        EnsureSlotArray();

        for (int i = 0; i < storySlots.Length; i++)
        {
            RefreshSlot(i);
        }
    }

    /// <summary>
    /// Refreshes one ending slot and updates only its Locked and Unlocked GameObject states.
    /// </summary>
    public void RefreshSlot(int index)
    {
        if (!IsValidSlotIndex(index))
        {
            return;
        }

        EndingStorySlotUI slot = storySlots[index];

        if (slot == null)
        {
            return;
        }

        string endingId = GetEndingIdForSlot(index);
        bool unlocked = IsEndingUnlocked(endingId);

        if (slot.lockedGameObject != null)
        {
            slot.lockedGameObject.SetActive(!unlocked);
        }

        if (slot.unlockedGameObject != null)
        {
            slot.unlockedGameObject.SetActive(unlocked);
        }
    }

    /// <summary>
    /// Displays the selected ending in the shared story text fields.
    /// A normal click on an unlocked ending does not play the typewriter animation.
    /// </summary>
    public void ShowEndingByIndex(int index)
    {
        ShowEndingByIndexInternal(index, false);
    }

    /// <summary>
    /// Number-based Button method: 1 selects the first ending and 12 selects the twelfth ending.
    /// </summary>
    public void ShowEndingByNumber(int endingNumber)
    {
        ShowEndingByIndex(endingNumber - 1);
    }

    /// <summary>
    /// Displays the selected ending.
    /// When playFirstUnlockAnimation is true, the typewriter animation plays once.
    /// </summary>
    private void ShowEndingByIndexInternal(int index, bool playFirstUnlockAnimation)
    {
        if (!IsValidSlotIndex(index))
        {
            Debug.LogWarning("[EndingStoryCollectionPanel] Invalid ending index: " + index);
            return;
        }

        string endingId = GetEndingIdForSlot(index);
        bool unlocked = IsEndingUnlocked(endingId);

        if (!unlocked)
        {
            ShowLockedEndingPanel(index);
            return;
        }

        if (hideLockedPanelWhenUnlockedEndingClicked)
        {
            CloseLockedPanel();
        }

        if (showStoryPanelWhenUnlockedEndingClicked)
        {
            OpenStoryPanel();
        }

        EndingStoryData data = GetEndingDataForSlot(index);

        if (data == null)
        {
            ShowStoryTextInstant("Missing Ending", "This ending data is missing from the database.");
            return;
        }

        if (playFirstUnlockAnimation)
        {
            PlayStoryTypewriter(data.title, data.story);
        }
        else
        {
            ShowStoryTextInstant(data.title, data.story);
        }
    }

    /// <summary>
    /// Called after clicking a Locked button. Opens the Locked Panel and shows the locked message.
    /// </summary>
    public void ShowLockedEndingPanel(int index)
    {
        if (!IsValidSlotIndex(index))
        {
            return;
        }

        StopStoryRevealAnimation();

        if (hideStoryPanelWhenLockedEndingClicked)
        {
            CloseStoryPanel();
        }

        OpenLockedPanel();

        EndingStoryData data = GetEndingDataForSlot(index);

        string titleToShow = lockedTitle;
        string storyToShow = lockedStory;

        if (showEndingTitleOnLockedPanel && data != null && !string.IsNullOrWhiteSpace(data.title))
        {
            titleToShow = data.title;
        }

        if (revealStoryOnLockedPanel && data != null && !string.IsNullOrWhiteSpace(data.story))
        {
            storyToShow = data.story;
        }

        SetLockedPanelText(titleToShow, storyToShow);
    }

    /// <summary>
    /// Displays the first unlocked ending.
    /// </summary>
    public void ShowFirstUnlockedEnding()
    {
        EnsureSlotArray();

        for (int i = 0; i < storySlots.Length; i++)
        {
            string endingId = GetEndingIdForSlot(i);

            if (IsEndingUnlocked(endingId))
            {
                ShowEndingByIndex(i);
                return;
            }
        }
    }

    /// <summary>
    /// Opens the Story Panel.
    /// </summary>
    public void OpenStoryPanel()
    {
        if (storyPanel != null)
        {
            storyPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Closes the Story Panel. This method can be assigned to the panel Close button.
    /// </summary>
    public void CloseStoryPanel()
    {
        StopStoryRevealAnimation();

        if (storyPanel != null)
        {
            storyPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Opens the Locked Panel.
    /// </summary>
    public void OpenLockedPanel()
    {
        if (lockedPanel != null)
        {
            lockedPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Closes the Locked Panel. This method can be assigned to the panel Close button.
    /// </summary>
    public void CloseLockedPanel()
    {
        if (lockedPanel != null)
        {
            lockedPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Unlocks an ending and displays it in the shared story text fields.
    /// The first unlock plays the typewriter animation; later clicks show the story instantly.
    /// Index 0 is the first ending and index 11 is the twelfth ending.
    /// </summary>
    public void UnlockEndingByIndex(int index)
    {
        if (!IsValidSlotIndex(index))
        {
            Debug.LogWarning("[EndingStoryCollectionPanel] Invalid ending index: " + index);
            return;
        }

        string endingId = GetEndingIdForSlot(index);

        if (string.IsNullOrWhiteSpace(endingId))
        {
            Debug.LogWarning("[EndingStoryCollectionPanel] Ending ID is empty at index: " + index);
            return;
        }

        bool wasAlreadyUnlocked = IsEndingUnlocked(endingId);

        PlayerPrefs.SetInt(BuildSaveKey(endingId), 1);
        PlayerPrefs.Save();

        RefreshAllSlots();

        bool shouldPlayTypewriter = playTypewriterOnlyOnFirstUnlock && !wasAlreadyUnlocked;
        ShowEndingByIndexInternal(index, shouldPlayTypewriter);

        EndingStoryData data = GetEndingDataForSlot(index);
        string title = data != null ? data.title : endingId;

        if (wasAlreadyUnlocked)
        {
            Debug.Log("[EndingStoryCollectionPanel] Ending already unlocked, showing instantly: " + title);
        }
        else
        {
            Debug.Log("[EndingStoryCollectionPanel] Ending unlocked permanently: " + title);
        }
    }

    /// <summary>
    /// Number-based Button method: 1 selects the first ending and 12 selects the twelfth ending.
    /// </summary>
    public void UnlockEndingByNumber(int endingNumber)
    {
        UnlockEndingByIndex(endingNumber - 1);
    }

    /// <summary>
    /// Returns whether the specified ending has been unlocked.
    /// </summary>
    public bool IsEndingUnlocked(string endingId)
    {
        if (string.IsNullOrWhiteSpace(endingId))
        {
            return false;
        }

        return PlayerPrefs.GetInt(BuildSaveKey(endingId), 0) == 1;
    }

    /// <summary>
    /// Locks the specified ending again. Intended mainly for testing.
    /// </summary>
    public void LockEndingByIndex(int index)
    {
        if (!IsValidSlotIndex(index))
        {
            Debug.LogWarning("[EndingStoryCollectionPanel] Invalid ending index: " + index);
            return;
        }

        string endingId = GetEndingIdForSlot(index);

        if (string.IsNullOrWhiteSpace(endingId))
        {
            return;
        }

        PlayerPrefs.DeleteKey(BuildSaveKey(endingId));
        PlayerPrefs.Save();

        RefreshAllSlots();

        Debug.Log("[EndingStoryCollectionPanel] Ending locked again: " + endingId);
    }

    /// <summary>
    /// Clears every saved ending unlock. Intended for testing.
    /// </summary>
    public void ClearAllEndingUnlocks()
    {
        EnsureSlotArray();

        for (int i = 0; i < storySlots.Length; i++)
        {
            string endingId = GetEndingIdForSlot(i);

            if (!string.IsNullOrWhiteSpace(endingId))
            {
                PlayerPrefs.DeleteKey(BuildSaveKey(endingId));
            }
        }

        PlayerPrefs.Save();
        RefreshAllSlots();

        CloseLockedPanel();
        CloseStoryPanel();

        Debug.Log("[EndingStoryCollectionPanel] All ending unlocks cleared.");
    }

    /// <summary>
    /// Resolves an ending from the three metrics and unlocks it automatically.
    /// Call this method when the main game ends.
    /// The typewriter animation also plays the first time the resolved ending is unlocked.
    /// </summary>
    public EndingStoryData ResolveAndUnlockEndingFromStats(int money, int credibility, int followers)
    {
        int index = GetEndingIndexFromStats(money, credibility, followers);
        UnlockEndingByIndex(index);
        return GetEndingDataForSlot(index);
    }

    /// <summary>
    /// Returns the matching ending index based on Money, Credibility, and Followers.
    ///
    /// Inspector and UI order:
    /// R1: C1 Index 0 Trust Collapse | C2 Index 1 Forgotten | C3 Index 2 Noise Empire
    /// R2: C1 Index 3 Viral Beast   | C2 Index 4 Quiet Cash | C3 Index 5 Broke Star
    /// R3: C1 Index 6 Powerhouse    | C2 Index 7 Trusted Voice | C3 Index 8 Honest Path
    /// R4: C1 Index 9 Responsible Influencer | C2 Index 10 Paid Lies | C3 Index 11 Unclear Legacy
    /// </summary>
    public int GetEndingIndexFromStats(int money, int credibility, int followers)
    {
        // Money and Followers have no maximum. Only negative values are prevented.
        // Credibility remains on the 0-100 scale.
        money = Mathf.Max(0, money);
        followers = Mathf.Max(0, followers);
        credibility = Mathf.Clamp(credibility, 0, 100);

        // Trust Collapse has the highest priority.
        // When final Credibility reaches the configured threshold, the result is always Trust Collapse.
        if (credibility <= trustCollapseMaxCredibility)
        {
            return 0; // C1 R1 - Trust Collapse
        }

        // The return indexes below must exactly match the order in EndingStoryDatabase.asset.
        // Check rare and specific endings first so broader conditions do not override them.

        if (money >= powerhouseMinMoney && followers >= powerhouseMinFollowers && credibility >= powerhouseMinCredibility)
        {
            return 6; // C1 R3 - Powerhouse
        }

        if (money >= noiseEmpireMinMoney && followers >= noiseEmpireMinFollowers && credibility < noiseEmpireCredibilityBelow)
        {
            return 2; // C3 R1 - Noise Empire
        }

        if (followers >= viralBeastMinFollowers && credibility < viralBeastCredibilityBelow)
        {
            return 3; // C1 R2 - Viral Beast
        }

        if (money >= paidLiesMinMoney && credibility < paidLiesCredibilityBelow)
        {
            return 10; // C2 R4 - Paid Lies
        }

        if (money < forgottenMoneyBelow && followers < forgottenFollowersBelow && credibility < forgottenCredibilityBelow)
        {
            return 1; // C2 R1 - Forgotten
        }

        if (money >= quietCashMinMoney && followers < quietCashFollowersBelow && credibility >= quietCashMinCredibility)
        {
            return 4; // C2 R2 - Quiet Cash
        }

        if (followers >= brokeStarMinFollowers && money < brokeStarMoneyBelow && credibility >= brokeStarMinCredibility)
        {
            return 5; // C3 R2 - Broke Star
        }

        if (credibility >= trustedMinCredibility && followers >= trustedMinFollowers && money >= trustedMinMoney)
        {
            return 7; // C2 R3 - Trusted Voice
        }

        if (credibility >= honestPathMinCredibility && followers < honestPathFollowersBelow && money < honestPathMoneyBelow)
        {
            return 8; // C3 R3 - Honest Path
        }

        if (money >= responsibleInfluencerMinMoney && credibility >= responsibleInfluencerMinCredibility && followers >= responsibleInfluencerMinFollowers)
        {
            return 9; // C1 R4 - Responsible Influencer
        }

        return 11; // C3 R4 - Unclear Legacy / default
    }

    // =========================
    // 12 test buttons: permanently unlock, show Unlocked, hide Locked, and open the Story Panel.
    // The first unlock plays the typewriter animation.
    // =========================

    public void TestUnlockEnding01()
    {
        UnlockEndingByIndex(0);
    }

    public void TestUnlockEnding02()
    {
        UnlockEndingByIndex(1);
    }

    public void TestUnlockEnding03()
    {
        UnlockEndingByIndex(2);
    }

    public void TestUnlockEnding04()
    {
        UnlockEndingByIndex(3);
    }

    public void TestUnlockEnding05()
    {
        UnlockEndingByIndex(4);
    }

    public void TestUnlockEnding06()
    {
        UnlockEndingByIndex(5);
    }

    public void TestUnlockEnding07()
    {
        UnlockEndingByIndex(6);
    }

    public void TestUnlockEnding08()
    {
        UnlockEndingByIndex(7);
    }

    public void TestUnlockEnding09()
    {
        UnlockEndingByIndex(8);
    }

    public void TestUnlockEnding10()
    {
        UnlockEndingByIndex(9);
    }

    public void TestUnlockEnding11()
    {
        UnlockEndingByIndex(10);
    }

    public void TestUnlockEnding12()
    {
        UnlockEndingByIndex(11);
    }

    // =========================
    // 12 display buttons: show an ending without unlocking it.
    // A locked ending opens the Locked Panel.
    // An unlocked ending opens the Story Panel.
    // The typewriter animation does not play.
    // =========================

    public void ShowEnding01()
    {
        ShowEndingByIndex(0);
    }

    public void ShowEnding02()
    {
        ShowEndingByIndex(1);
    }

    public void ShowEnding03()
    {
        ShowEndingByIndex(2);
    }

    public void ShowEnding04()
    {
        ShowEndingByIndex(3);
    }

    public void ShowEnding05()
    {
        ShowEndingByIndex(4);
    }

    public void ShowEnding06()
    {
        ShowEndingByIndex(5);
    }

    public void ShowEnding07()
    {
        ShowEndingByIndex(6);
    }

    public void ShowEnding08()
    {
        ShowEndingByIndex(7);
    }

    public void ShowEnding09()
    {
        ShowEndingByIndex(8);
    }

    public void ShowEnding10()
    {
        ShowEndingByIndex(9);
    }

    public void ShowEnding11()
    {
        ShowEndingByIndex(10);
    }

    public void ShowEnding12()
    {
        ShowEndingByIndex(11);
    }

    // =========================
    // Typewriter animation
    // =========================

    private void PlayStoryTypewriter(string title, string story)
    {
        StopStoryRevealAnimation();

        storyRevealCoroutine = StartCoroutine(StoryTypewriterRoutine(title, story));
    }

    private IEnumerator StoryTypewriterRoutine(string title, string story)
    {
        OpenStoryPanel();
        CloseLockedPanel();

        SetSharedTitleOnly(title);
        PrepareStoryBodyForTypewriter(story);

        if (useJuicyPanelPop)
        {
            yield return PlayStoryPanelPopRoutine();
        }

        if (typewriterStartDelay > 0f)
        {
            yield return WaitForSecondsSmart(typewriterStartDelay);
        }

        int totalCharacters = GetStoryCharacterCount(story);
        float visibleCharacterFloat = 0f;
        int visibleCharacters = 0;

        while (visibleCharacters < totalCharacters)
        {
            float deltaTime = typewriterUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            visibleCharacterFloat += Mathf.Max(1f, typewriterCharactersPerSecond) * deltaTime;

            int newVisibleCharacters = Mathf.Clamp(Mathf.FloorToInt(visibleCharacterFloat), 0, totalCharacters);

            if (newVisibleCharacters != visibleCharacters)
            {
                visibleCharacters = newVisibleCharacters;
                SetStoryVisibleCharacters(story, visibleCharacters);
            }

            yield return null;
        }

        ShowStoryTextInstant(title, story);
        storyRevealCoroutine = null;
    }

    private IEnumerator PlayStoryPanelPopRoutine()
    {
        Transform panelTransform = GetStoryPanelTransform();

        if (panelTransform == null)
        {
            yield break;
        }

        CacheStoryPanelScale();

        float duration = Mathf.Max(0.01f, panelPopDuration);
        float timer = 0f;

        Vector3 startScale = storyPanelOriginalScale * panelPopStartScale;
        Vector3 overshootScale = storyPanelOriginalScale * panelPopOvershootScale;
        Vector3 endScale = storyPanelOriginalScale;

        panelTransform.localScale = startScale;

        while (timer < duration)
        {
            float deltaTime = typewriterUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            timer += deltaTime;

            float t = Mathf.Clamp01(timer / duration);

            if (t < 0.7f)
            {
                float firstPart = Mathf.Clamp01(t / 0.7f);
                panelTransform.localScale = Vector3.LerpUnclamped(startScale, overshootScale, EaseOutCubic(firstPart));
            }
            else
            {
                float secondPart = Mathf.Clamp01((t - 0.7f) / 0.3f);
                panelTransform.localScale = Vector3.LerpUnclamped(overshootScale, endScale, EaseOutCubic(secondPart));
            }

            yield return null;
        }

        panelTransform.localScale = endScale;
    }

    private void StopStoryRevealAnimation()
    {
        if (storyRevealCoroutine != null)
        {
            StopCoroutine(storyRevealCoroutine);
            storyRevealCoroutine = null;
        }

        ResetStoryPanelScale();
        ResetTMPVisibleCharacters();
    }

    private void PrepareStoryBodyForTypewriter(string story)
    {
        if (sharedStoryTMP != null)
        {
            sharedStoryTMP.text = story;
            sharedStoryTMP.ForceMeshUpdate();
            sharedStoryTMP.maxVisibleCharacters = 0;
        }

        if (sharedStoryText != null)
        {
            sharedStoryText.text = string.Empty;
        }
    }

    private void SetStoryVisibleCharacters(string fullStory, int visibleCharacters)
    {
        if (sharedStoryTMP != null)
        {
            sharedStoryTMP.maxVisibleCharacters = visibleCharacters;
        }

        if (sharedStoryText != null)
        {
            int safeLength = Mathf.Clamp(visibleCharacters, 0, fullStory.Length);
            sharedStoryText.text = fullStory.Substring(0, safeLength);
        }
    }

    private int GetStoryCharacterCount(string story)
    {
        if (sharedStoryTMP != null)
        {
            sharedStoryTMP.ForceMeshUpdate();
            return sharedStoryTMP.textInfo.characterCount;
        }

        if (string.IsNullOrEmpty(story))
        {
            return 0;
        }

        return story.Length;
    }

    private IEnumerator WaitForSecondsSmart(float seconds)
    {
        float timer = 0f;

        while (timer < seconds)
        {
            timer += typewriterUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    private void ShowStoryTextInstant(string title, string story)
    {
        StopStoryRevealAnimation();
        SetSharedStoryText(title, story);
    }

    private void SetSharedTitleOnly(string title)
    {
        if (sharedTitleTMP != null)
        {
            sharedTitleTMP.text = title;
        }

        if (sharedTitleText != null)
        {
            sharedTitleText.text = title;
        }
    }

    private void SetSharedStoryText(string title, string story)
    {
        ResetTMPVisibleCharacters();

        if (sharedTitleTMP != null)
        {
            sharedTitleTMP.text = title;
        }

        if (sharedStoryTMP != null)
        {
            sharedStoryTMP.text = story;
            sharedStoryTMP.maxVisibleCharacters = int.MaxValue;
        }

        if (sharedTitleText != null)
        {
            sharedTitleText.text = title;
        }

        if (sharedStoryText != null)
        {
            sharedStoryText.text = story;
        }
    }

    private void ResetTMPVisibleCharacters()
    {
        if (sharedStoryTMP != null)
        {
            sharedStoryTMP.maxVisibleCharacters = int.MaxValue;
        }
    }

    private void CacheStoryPanelScale()
    {
        if (storyPanelScaleCached)
        {
            return;
        }

        Transform panelTransform = GetStoryPanelTransform();

        if (panelTransform != null)
        {
            storyPanelOriginalScale = panelTransform.localScale;
        }
        else
        {
            storyPanelOriginalScale = Vector3.one;
        }

        storyPanelScaleCached = true;
    }

    private void ResetStoryPanelScale()
    {
        Transform panelTransform = GetStoryPanelTransform();

        if (panelTransform != null)
        {
            panelTransform.localScale = storyPanelOriginalScale;
        }
    }

    private Transform GetStoryPanelTransform()
    {
        if (storyPanel != null)
        {
            return storyPanel.transform;
        }

        return null;
    }

    private float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    // =========================
    // Shared helper methods
    // =========================

    private void SetLockedPanelText(string title, string story)
    {
        if (lockedPanelTitleTMP != null)
        {
            lockedPanelTitleTMP.text = title;
        }

        if (lockedPanelStoryTMP != null)
        {
            lockedPanelStoryTMP.text = story;
        }

        if (lockedPanelTitleText != null)
        {
            lockedPanelTitleText.text = title;
        }

        if (lockedPanelStoryText != null)
        {
            lockedPanelStoryText.text = story;
        }
    }

    private Button FindButton(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return null;
        }

        Button button = targetObject.GetComponent<Button>();

        if (button != null)
        {
            return button;
        }

        return targetObject.GetComponentInChildren<Button>(true);
    }

    private string GetEndingIdForSlot(int index)
    {
        EndingStoryData data = GetEndingDataForSlot(index);

        if (data != null && !string.IsNullOrWhiteSpace(data.endingId))
        {
            return data.endingId.Trim();
        }

        return "ending_" + (index + 1).ToString("00");
    }

    private EndingStoryData GetEndingDataForSlot(int index)
    {
        if (endingDatabase == null)
        {
            return null;
        }

        return endingDatabase.GetEndingByIndex(index);
    }

    private string BuildSaveKey(string endingId)
    {
        string databaseSaveId = "DefaultEndingDatabase";

        if (endingDatabase != null && !string.IsNullOrWhiteSpace(endingDatabase.saveId))
        {
            databaseSaveId = endingDatabase.saveId.Trim();
        }

        return SaveKeyPrefix + databaseSaveId + "_" + endingId.Trim();
    }

    private bool IsValidSlotIndex(int index)
    {
        EnsureSlotArray();
        return index >= 0 && index < storySlots.Length;
    }

    private void EnsureSlotArray()
    {
        if (storySlots == null || storySlots.Length != 12)
        {
            Array.Resize(ref storySlots, 12);
        }

        for (int i = 0; i < storySlots.Length; i++)
        {
            if (storySlots[i] == null)
            {
                storySlots[i] = new EndingStorySlotUI();
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureSlotArray();
    }
#endif
}

[Serializable]
public class EndingStorySlotUI
{
    [Header("Locked / Unlocked Button Objects")]
    [Tooltip("Button GameObject shown while the ending is locked. Clicking it opens the Locked Panel.")]
    public GameObject lockedGameObject;

    [Tooltip("Button GameObject shown after the ending is unlocked. Clicking it opens the Story Panel and displays the corresponding story.")]
    public GameObject unlockedGameObject;

    [NonSerialized]
    public bool buttonsBound;
}
