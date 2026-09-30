using UnityEngine;

public class PlayerAvatarCatalog : ScriptableObject
{
    public Sprite[] avatars;
    public static PlayerAvatarCatalog Load() => Resources.Load<PlayerAvatarCatalog>("PlayerAvatarCatalog");
    public static int SelectedIndex => PlayerPrefs.GetInt("Settings_PlayerAvatar", 0);

    public static void Select(int index)
    {
        var catalog = Load();
        if (catalog == null || catalog.avatars == null || index < 0 || index >= catalog.avatars.Length) return;
        PlayerPrefs.SetInt("Settings_PlayerAvatar", index);
        PlayerPrefs.Save();
        foreach (var view in UnityEngine.Object.FindObjectsByType<PlayerAvatarView>(FindObjectsInactive.Include))
            view.Refresh();
    }
}
