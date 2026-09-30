using UnityEngine;
using UnityEngine.UI;

/// <summary>Resolved fact checks remain visible but cannot be opened again.</summary>
[RequireComponent(typeof(Button))]
public class FactCheckEntryState : MonoBehaviour
{
    void OnEnable() => Refresh();

    public void Refresh()
    {
        GetComponent<Button>().interactable = FactCheckEventStore.Current != null && !FactCheckResponseFeedback.HasResolvedCurrentEvent;
    }

    public static void RefreshScene(UnityEngine.SceneManagement.Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var entry in root.GetComponentsInChildren<FactCheckEntryState>(true))
                entry.Refresh();
    }

    public static void RefreshAll()
    {
        foreach (var entry in FindObjectsByType<FactCheckEntryState>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            entry.Refresh();
    }
}
