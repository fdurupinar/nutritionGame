using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class PlayerAvatarView : MonoBehaviour
{
    private void OnEnable() => Refresh();

    public void Refresh()
    {
        var catalog = PlayerAvatarCatalog.Load();
        if (catalog == null || catalog.avatars == null || catalog.avatars.Length == 0) return;
        var sprite = catalog.avatars[Mathf.Clamp(PlayerAvatarCatalog.SelectedIndex, 0, catalog.avatars.Length - 1)];
        if (sprite == null) return;
        var image = GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
    }
}
