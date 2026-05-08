using UnityEngine;
using TMPro;
using System.Collections;
using System;

public class UserStats : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _followerText;
    [SerializeField] private TextMeshProUGUI _cashText;
    [SerializeField] private TextMeshProUGUI _credibilityText;
    [SerializeField] private TextMeshProUGUI _likesText;
    AudioManager _audioManager;

    // Track running coroutines so they don't overlap and fight each other
    private Coroutine _followerRoutine;
    private Coroutine _cashRoutine;
    private Coroutine _credibilityRoutine;
    private Coroutine _likesRoutine;

    private int _followerCnt;
    public int FollowerCount
    {
        get => _followerCnt;
        set
        {
            if (_followerRoutine != null) StopCoroutine(_followerRoutine);
            _followerRoutine = StartCoroutine(UpdateStatCoroutine(value, _followerText, val => _followerCnt = val, _followerCnt, 0.02f));
        }
    }

    private int _cash;
    public int Cash
    {
        get => _cash;
        set
        {
            if (_cashRoutine != null) StopCoroutine(_cashRoutine);
            _cashRoutine = StartCoroutine(UpdateStatCoroutine(value, _cashText, val => _cash = val, _cash, 0.02f));
        }
    }

    private int _credibility = 100;
    public int Credibility
    {
        get => _credibility;
        set
        {
            if (_credibilityRoutine != null) StopCoroutine(_credibilityRoutine);
            // Changed from 0.5f to 0.02f to match the fast rolling speed of the others
            _credibilityRoutine = StartCoroutine(UpdateStatCoroutine(value, _credibilityText, val => _credibility = val, _credibility, 0.02f));
        }
    }

    private int _likes;
    public int Likes
    {
        get => _likes;
        set
        {
            if (_likesRoutine != null) StopCoroutine(_likesRoutine);
            _likesRoutine = StartCoroutine(UpdateStatCoroutine(value, _likesText, val => _likes = val, _likes, 0.02f));
        }
    }

    public int Level { get; set; }

    void Start()
    {
        if (GlobalStatManager.Instance != null)
        {
            this.Cash = GlobalStatManager.Instance.currentCash;
            this.FollowerCount = GlobalStatManager.Instance.currentFollowers;
            this.Credibility = GlobalStatManager.Instance.currentCredibility;
        }

        GameObject audioObj = GameObject.Find("AudioManager");
        if (audioObj != null)
            _audioManager = audioObj.GetComponent<AudioManager>();

        Level = 1;
    }

    private IEnumerator UpdateStatCoroutine(int targetValue, TextMeshProUGUI textElement, Action<int> setter, int currentValue, float delay)
    {
        if (currentValue == targetValue) yield break;

        // Calculate a step size so large numbers don't take forever to finish
        int difference = Mathf.Abs(targetValue - currentValue);
        int stepAmount = Mathf.Max(1, difference / 40); // Guarantees it finishes in roughly ~1 second

        int step = (targetValue > currentValue) ? stepAmount : -stepAmount;

        while (currentValue != targetValue)
        {
            // Prevent overshooting the target
            if (Mathf.Abs(targetValue - currentValue) <= Mathf.Abs(step))
            {
                currentValue = targetValue;
            }
            else
            {
                currentValue += step;
            }

            setter(currentValue);

            if (textElement != null)
                textElement.text = currentValue.ToString();

            if (_audioManager != null)
                _audioManager.PlayEngagamentNotification();

            yield return new WaitForSeconds(delay);
        }
    }
}