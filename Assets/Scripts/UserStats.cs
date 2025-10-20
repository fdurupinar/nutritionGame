using UnityEngine;
using TMPro;
using System.Collections;
using System; // Required for using Action<>

public class UserStats : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _followerText;
    [SerializeField] private TextMeshProUGUI _cashText;
    [SerializeField] private TextMeshProUGUI _credibilityText;
    [SerializeField] private TextMeshProUGUI _likesText;
    AudioManager _audioManager;



    private int _followerCnt;
    public int FollowerCount
    {
        get => _followerCnt;
        set => StartCoroutine(UpdateStatCoroutine(value, _followerText, val => _followerCnt = val, _followerCnt, 0.02f));
    }

    private int _cash;
    public int Cash
    {
        get => _cash;
        set => StartCoroutine(UpdateStatCoroutine(value, _cashText, val => _cash = val, _cash, 0.02f));
    }

    private int _credibility = 100;
    public int Credibility
    {
        get => _credibility;
        set => StartCoroutine(UpdateStatCoroutine(value, _credibilityText, val => _credibility = val, _credibility, 0.5f));
    }

    private int _likes;
    public int Likes
    {
        get => _likes;
        set => StartCoroutine(UpdateStatCoroutine(value, _likesText, val => _likes = val, _likes, 0.02f));
    }

    void Start()
    {
        _audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
    }

    /// <summary>
    /// Animates a stat from its current value to a target value, updating the UI text.
    /// </summary>    
    private IEnumerator UpdateStatCoroutine(int targetValue, TextMeshProUGUI textElement, Action<int> setter, int currentValue, float delay)
    {
        if (currentValue == targetValue)
            yield break; // No change needed

        // Determine direction and set a fixed delay between steps
        int step = (targetValue > currentValue) ? 1 : -1;


        while (currentValue != targetValue)
        {
            currentValue += step;
            setter(currentValue); // Update the actual variable using the delegate
            textElement.text = currentValue.ToString();
            _audioManager.PlayEngagamentNotification();
            yield return new WaitForSeconds(delay);

        }
    }
}