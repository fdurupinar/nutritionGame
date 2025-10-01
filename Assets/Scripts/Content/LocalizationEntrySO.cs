// Assets/Scripts/Content/LocalizationEntrySO.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Content/LocalizationEntry")]
public class LocalizationEntrySO : ScriptableObject {
    public string key;
    public string en; // add other languages later
}
