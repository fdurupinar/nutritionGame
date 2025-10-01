// Assets/Scripts/Content/DebriefCardSO.cs
using UnityEngine;

public enum DebriefBin { RedFlag, Okay }

[CreateAssetMenu(menuName = "Content/DebriefCard")]
public class DebriefCardSO : ScriptableObject {
    public string id;
    [TextArea] public string text;
    public DebriefBin correctBin;
    [TextArea] public string explanation;
}
