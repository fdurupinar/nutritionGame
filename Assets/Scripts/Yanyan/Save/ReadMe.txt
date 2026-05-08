DayManager 
Store key locally
	-	SAVED_DAY_KEY = "SavedCurrentDay"
Set Max day
Show current day

Need / Connect with
	-	DaySceneLink uses DayManager.Instance to show day text, advance day, and reset day
	-	DailyPostFillBlankManager reads DayManager.Instance.currentDay to lock/unlock topic buttons
	-	TacticManager reads DayManager.Instance.currentDay to decide which tactic cards are available for the current day


DaySceneLink
Scene Day Text – display the current Day
Advance Buttons – when click add 1 to DayManager's current day (to the local key)
Reset buttons – when click reset DayManager's current day to “1”( to the local key)
Objects to active - As long as not day 1 active a list of objects

Need / Connect with
	-	Needs DayManager in the scene
	-	Advance Buttons list should include any button that should add +1 day
	-	Reset Buttons list should include any button that should reset the saved day
	-	Do not also add DayManager.AdvanceDay manually to the same button if it is already in this list, or the day may add twice



* Funda’s scripts change the values of the three metrics 

GlobalStatManager
Store key locally
	-	SAVE_CASH = "User_Cash"
	-	SAVE_FOLLOWERS = "User_Followers"
	-	SAVE_CRED = "User_Credibility"
Save and load the three metrics
Keep the saved metric values when changing scenes

Need / Connect with
	-	UserStats reads GlobalStatManager.Instance values when the scene starts
	-	TacticManager saves the updated values into GlobalStatManager after tactic publish
	-	StatSceneLink uses GlobalStatManager to display and reset the saved metric values


StatSceneLink
Display the three metric
	-	Cash Text 
	-	Follower Text 
	-	Creditability Text
Reset Button – Rest all three metric to default

Need / Connect with
	-	Needs GlobalStatManager in the scene
	-	Can also reset DayManager day back to day 1 if the reset logic is connected
	-	Should be used on the scene that shows saved Cash, Followers, and Credibility


UserStats
Store the active player stat values in the current scene
	-	Cash
	-	FollowerCount
	-	Credibility
	-	Likes
	-	Level
Animate the number changes on the UI
Play stat update sound when the values change

Need / Connect with
	-	Needs GlobalStatManager to load saved Cash, Followers, and Credibility
	-	TacticManager uses UserStats to update Cash, Followers, Likes, and Credibility after tactic publish
	-	TacticManager finds this script by GameObject tag "Player", so the object with UserStats should have Player tag
	-	Uses AudioManager if stat sound is needed


DailyPostData.json
Store Daily Post content data
Each topic has
	-	topicId
	-	subTopics list
Each sub-topic has
	-	subTopicId
	-	subTopicName
	-	sentence
	-	blankWords
	-	wordChoices

Need / Connect with
	-	DailyPostFillBlankManager reads this JSON file
	-	The topicId must match the Topic Id typed in DailyPostFillBlankManager's Topic Buttons list
	-	Each blankWords item must exist inside the sentence text
	-	The player sees subTopicName as the sub-topic button text


DailyPostFillBlankManager
Controls the Daily Post fill-in-the-blank flow
Reads all sub-topic, sentence, blank, and word choice data from DailyPostData.json
Topic buttons are set manually in Unity
Topic buttons unlock by day
When a topic button is clicked
	-	Find the matching topicId in the JSON
	-	Open the SubTopicPanel
	-	Spawn sub-topic buttons using Sub Topic Button Prefab
When a sub-topic button is clicked
	-	Open the FillBlankPanel
	-	Show the sentence with blanks inside the sentence line
	-	Spawn word choice buttons using Word Choice Button Prefab
When a word button is clicked
	-	Fill the next blank in order
	-	Hide or disable that word choice
When all blanks are filled
	-	Next Button becomes interactable
When Next Button is clicked
	-	Build currentCompletedSentence
	-	Call On Next event

Need / Connect with
	-	Needs DailyPostData.json assigned to Daily Post Json File
	-	Needs TopicPanel, SubTopicPanel, FillBlankPanel assigned
	-	Needs topicButtons list set in Inspector
		-	Unlock Day
		-	Topic Button
		-	Topic Id, must match JSON topicId
	-	Needs Sub Topic Button Parent and Sub Topic Button Prefab
	-	Needs Sentence Text TMP object
	-	Needs Word Choice Parent and Word Choice Button Prefab
	-	Needs Next Button
	-	Can use Undo Last Button and Reset Blanks Button if assigned
	-	Reads DayManager.Instance.currentDay to lock/unlock topic buttons
	-	On Next should open the TacticCardPanel only
	-	TacticManager should later read currentCompletedSentence when the tactic Publish button is clicked


DailyPostPanelFlow
Open the next panel after FillBlankPanel
Usually used after the Daily Post Next button is clicked
OpenTacticCardPanel turns on the TacticCardPanel

Need / Connect with
	-	DailyPostFillBlankManager On Next should call DailyPostPanelFlow.OpenTacticCardPanel
	-	No need to hide FillBlankPanel here if another script or panel setup already handles it
	-	Should not display tactic scroll directly


DailyPostToTacticScrollLink
Bridge script for sending DailyPostFillBlankManager.currentCompletedSentence to TacticManager
This was for the older flow where the sentence was shown immediately after Next

Need / Connect with
	-	If using current flow, this script is not needed on the Next button
	-	Current flow should show the completed sentence only after the tactic Publish button is clicked
	-	Can keep the script file, but do not connect it to On Next if using the tactic-card-before-publish flow


TacticManager
Controls tactic card selection, tactic publish, tactic scroll animation, tactic image, comments, and metric changes
Loads all TacticSO files from Resources/Content/Tactics
Uses dayConfigs to decide which tactic cards are available for the current day and previous days
Uses UserStats.Level to only show tactics for the current level
Spawns Card prefab into the grid
When a card is clicked
	-	Card tells TacticManager it was selected
When Publish button is clicked
	-	Use selected tactic card data
	-	Play/open tactic scroll animator with TacticPanel SetBool("isHidden", false)
	-	Show selected tactic image on content panel
	-	Display text in the tactic scroll
	-	Show comments based on selected tactic type
	-	Update Cash, Followers, Likes, and Credibility using UserStats
	-	Save the updated metrics with GlobalStatManager
	-	Remove the used tactic card and repopulate the grid

Need / Connect with
	-	Needs DayManager to know current day
	-	Needs UserStats on a GameObject tagged "Player"
	-	Needs GlobalStatManager to save metric changes
	-	Needs CommentManager to show comments after publishing
	-	Needs Card prefab with Card.cs on it
	-	Needs TacticSO data in Resources/Content/Tactics
	-	Needs dayConfigs set in Inspector
	-	Needs TacticPanel Animator, content panel, content text, select card button, and grid parent assigned
	-	For Daily Post flow, TacticManager should have a DailyPostFillBlankManager reference and use currentCompletedSentence for the scroll text after Publish is clicked
	-	The tactic scroll object should not be SetActive false if it is controlled by animator and just moved off screen


Card
Script on each tactic card prefab
Receives one TacticSO from TacticManager
Shows tactic name, type, cost, bonus, color, image, and debunking/back text if assigned
Handles card selected / deselected visual state
When clicked, tells TacticManager which card was selected

Need / Connect with
	-	Needs TacticManager because Card calls TacticManager.OnCardSelected(this)
	-	Needs TacticSO data from TacticManager.PopulateGrid
	-	Can work with CardFlip if the prefab supports card front/back flipping


CardFlip
Controls flipping the tactic card front/back
Right click can flip the card to show the back/debunking side
Left click can select the card when face up

Need / Connect with
	-	Needs Card.cs on the same card prefab or same object setup
	-	Needs front/back card UI objects assigned
	-	Uses Card data for selection and back text display


TacticSO
ScriptableObject data for one tactic card
Stores tactic card content
	-	id
	-	displayName
	-	type
	-	text
	-	debunkingText
	-	level
	-	engagementBonus
	-	credibilityCost
	-	enabledFlag
	-	tacticImage

Need / Connect with
	-	TacticManager loads TacticSO from Resources/Content/Tactics
	-	Card displays the TacticSO data
	-	TacticManager uses tacticImage, text, type, engagementBonus, credibilityCost, and level
	-	CommentManager uses tactic type indirectly because TacticManager passes selectedTactic.type to comments


CommentManager
Controls the comment scroll UI
Loads CommentLineSO files from Resources/Content/Comments
Finds comments with the same type as the selected tactic
Displays comments one by one with delay
Can clear old comments
Can auto scroll comment box to bottom

Need / Connect with
	-	Needs CommentLineSO data in Resources/Content/Comments
	-	TacticManager calls DisplayCommentsRoutine(selectedTactic.type, delay)
	-	TacticManager may call ClearComments when starting a new publish to avoid old comments staying
	-	Uses AudioManager if comment notification sound is needed


CommentLineSO
ScriptableObject data for one comment line
Stores
	-	category
	-	id
	-	type
	-	commenterName
	-	text

Need / Connect with
	-	CommentManager loads it from Resources/Content/Comments
	-	type should match TacticSO.type if it should appear for that tactic


AudioManager
Controls simple UI sounds
Can play
	-	Click sound
	-	Card select sound
	-	Engagement notification sound
	-	Comment notification sound

Need / Connect with
	-	Card can use AudioManager for select sound
	-	UserStats can use AudioManager for stat update sound
	-	CommentManager can use AudioManager for comment notification sound
	-	Usually placed on one AudioManager GameObject with AudioSource


Overall Daily Post Flow
Main Screen
	-	Player clicks Daily Post
	-	Scene loads to Daily Post scene
TopicPanel
	-	Player chooses one unlocked topic
SubTopicPanel
	-	Script spawns sub-topic buttons from DailyPostData.json
	-	Player chooses one sub-topic
FillBlankPanel
	-	Sentence and word choices come from JSON
	-	Player fills all blanks by clicking words in order
	-	Next Button stays interactable false until all blanks are filled
	-	Next builds currentCompletedSentence
TacticCardPanel
	-	Player chooses one tactic card
	-	Player clicks Publish
TacticManager
	-	Uses the selected tactic for image, comments, animation, and metrics
	-	Uses DailyPostFillBlankManager.currentCompletedSentence for the post text in the tactic scroll
Result / Metrics
	-	TacticManager updates UserStats
	-	GlobalStatManager saves the new values
	-	StatSceneLink can display the saved values
	-	DaySceneLink advance button can add +1 to current day