using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Populates the existing main-menu Settings panel without changing its navigation.
public class SoundSettingsPanel : MonoBehaviour
{
    private Slider volumeSlider;
    private TextMeshProUGUI volumeLabel;
    private TextMeshProUGUI muteLabel;
    private Button muteButton;
    private TMP_FontAsset font;
    private Sprite buttonSprite;
    private Image[] avatarFrames;
    private TextMeshProUGUI[] avatarLabels;

    private void Awake()
    {
        var group = transform.Find("VertcialGroupButtons");
        var sample = group.GetComponentInChildren<TextMeshProUGUI>(true);
        font = sample.font;
        buttonSprite = group.GetComponentInChildren<Image>(true).sprite;
        group.gameObject.SetActive(false);
        GetComponent<Image>().color = GamePalette.Background;

        Label(transform, "SettingsTitle", "Settings", .1f, .82f, .78f, .92f, 44);
        BuildAvatarSelector();
        var surface = Image(transform, "SoundControls", GamePalette.Secondary, .08f, .24f, .92f, .53f);
        volumeLabel = Label(surface.transform, "VolumeLabel", "", .07f, .79f, .93f, .96f, 28);
        BuildSlider(surface.transform);
        muteButton = Button(surface.transform, "Mute", .07f, .15f, .93f, .39f, out muteLabel);
        muteButton.onClick.AddListener(() => { GameSoundSettings.SetMuted(!GameSoundSettings.Muted); Refresh(); });

        var reset = Button(transform, "ResetSound", .22f, .12f, .78f, .2f, out var resetLabel);
        resetLabel.text = "Reset sound settings";
        reset.onClick.AddListener(() => { GameSoundSettings.ResetDefaults(); Refresh(); });
        Label(transform, "SavedNote", "Your preferences are saved automatically.", .1f, .045f, .9f, .105f, 21);

        // Reuse the authored Back button and its existing main-menu callbacks.
        var back = transform.Find("Button");
        Place((RectTransform)back, .78f, .84f, .94f, .93f);
        var backImage = back.GetComponent<Image>();
        backImage.sprite = buttonSprite;
        backImage.type = UnityEngine.UI.Image.Type.Sliced;
        backImage.color = GamePalette.Primary;
        back.GetComponent<Button>().transition = Selectable.Transition.None;
        Label(back, "BackLabel", "Back", 0f, 0f, 1f, 1f, 25);
        back.SetAsLastSibling();
    }

    private void OnEnable()
    {
        transform.SetAsLastSibling();
        GameSoundSettings.Apply();
        Refresh();
    }

    private void OnDisable() => PlayerPrefs.Save();

    private void Refresh()
    {
        if (avatarFrames != null)
            for (int i = 0; i < avatarFrames.Length; i++)
            {
                bool selected = i == Mathf.Clamp(PlayerAvatarCatalog.SelectedIndex, 0, avatarFrames.Length - 1);
                avatarFrames[i].color = selected ? GamePalette.Primary : GamePalette.Background;
                avatarLabels[i].text = selected ? "Selected" : (i + 1).ToString();
            }
        if (volumeSlider == null) return;
        volumeSlider.SetValueWithoutNotify(GameSoundSettings.Volume);
        volumeLabel.text = "Master volume  " + Mathf.RoundToInt(GameSoundSettings.Volume * 100f) + "%";
        muteLabel.text = GameSoundSettings.Muted ? "All sound: OFF" : "All sound: ON";
        muteButton.GetComponent<Image>().color = GameSoundSettings.Muted ? GamePalette.Surface : GamePalette.Primary;
    }

    private void BuildAvatarSelector()
    {
        var catalog = PlayerAvatarCatalog.Load();
        if (catalog == null || catalog.avatars == null || catalog.avatars.Length == 0) return;
        var surface = Image(transform, "AvatarSelector", GamePalette.Secondary, .08f, .56f, .92f, .80f);
        Label(surface.transform, "AvatarTitle", "Choose your avatar", .05f, .76f, .95f, .97f, 28);
        avatarFrames = new Image[catalog.avatars.Length];
        avatarLabels = new TextMeshProUGUI[catalog.avatars.Length];
        float step = .92f / catalog.avatars.Length;
        for (int i = 0; i < catalog.avatars.Length; i++)
        {
            int index = i;
            float left = .04f + i * step;
            var frame = Image(surface.transform, "Avatar " + (i + 1), GamePalette.Background, left, .08f, left + step - .015f, .72f);
            avatarFrames[i] = frame;
            var portrait = Image(frame.transform, "Portrait", Color.white, .08f, .23f, .92f, .96f);
            portrait.sprite = catalog.avatars[i];
            portrait.type = UnityEngine.UI.Image.Type.Simple;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            avatarLabels[i] = Label(frame.transform, "Selection", "", 0f, .01f, 1f, .22f, 17);
            var button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => { PlayerAvatarCatalog.Select(index); Refresh(); });
        }
    }

    private void BuildSlider(Transform parent)
    {
        var root = new GameObject("MasterVolume", typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);
        Place((RectTransform)root.transform, .09f, .59f, .91f, .76f);
        Image(root.transform, "Track", GamePalette.Background, 0f, .35f, 1f, .65f);
        var fill = Image(root.transform, "Fill", GamePalette.Primary, 0f, .35f, 1f, .65f);
        var handleArea = new GameObject("HandleArea", typeof(RectTransform));
        handleArea.transform.SetParent(root.transform, false);
        Place((RectTransform)handleArea.transform, 0f, 0f, 1f, 1f);
        var handle = Image(handleArea.transform, "Handle", GamePalette.Text, 0f, .15f, 0f, .85f);
        handle.rectTransform.sizeDelta = new Vector2(24f, 0f);
        volumeSlider = root.GetComponent<Slider>();
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.fillRect = fill.rectTransform;
        volumeSlider.handleRect = handle.rectTransform;
        volumeSlider.targetGraphic = handle;
        volumeSlider.direction = Slider.Direction.LeftToRight;
        volumeSlider.transition = Selectable.Transition.None;
        volumeSlider.onValueChanged.AddListener(value => { GameSoundSettings.SetVolume(value); Refresh(); });
    }

    private TextMeshProUGUI Label(Transform parent, string name, string text, float x0, float y0, float x1, float y1, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshProUGUI>();
        Place(label.rectTransform, x0, y0, x1, y1);
        label.font = font;
        label.fontSize = size;
        label.color = GamePalette.Text;
        label.text = text;
        label.alignment = TextAlignmentOptions.Midline;
        label.raycastTarget = false;
        return label;
    }

    private Image Image(Transform parent, string name, Color color, float x0, float y0, float x1, float y1)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        Place(image.rectTransform, x0, y0, x1, y1);
        image.sprite = buttonSprite;
        image.type = UnityEngine.UI.Image.Type.Sliced;
        image.color = color;
        return image;
    }

    private Button Button(Transform parent, string name, float x0, float y0, float x1, float y1, out TextMeshProUGUI label)
    {
        var image = Image(parent, name, GamePalette.Primary, x0, y0, x1, y1);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        label = Label(image.transform, "Label", "", .03f, .05f, .97f, .95f, 27);
        return button;
    }

    private static void Place(RectTransform rect, float x0, float y0, float x1, float y1)
    {
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
