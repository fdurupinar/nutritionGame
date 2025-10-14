using UnityEngine;
using TMPro;
using System.Collections; 
public class UserStats : MonoBehaviour
{

    
    [SerializeField] private TextMeshProUGUI _followerText;
    [SerializeField] private TextMeshProUGUI _cashText; 
    [SerializeField] private TextMeshProUGUI _credibilityText;  
    
    [SerializeField] private TextMeshProUGUI _likesText;  
    
    private int _followerCnt;
    public int FollowerCount
    {
        get{
            return _followerCnt;
        }
        set{
            
            if(value < _followerCnt)        
                StartCoroutine(UpdateSlowly(value, ref _followerCnt, 1, 0.5f));
            else
                StartCoroutine(UpdateSlowly(value, ref _followerCnt, -1, 0.5f));
            
            _followerText.text = _followerCnt.ToString();
        }
    }
    private int _cash;
    public int Cash
    {
        get
        {
            return _cash;
        }
        set
        {
            if(value < _cash)        
                StartCoroutine(UpdateSlowly(value, ref _cash, 1, 0.5f));
            else
            StartCoroutine(UpdateSlowly(value, ref _cash, -1, 0.5f));
            _cashText.text = _followerCnt.ToString();
        }
    }
    
    private int _credibility;
    public int Credibility{
        get
        {
            return _credibility;
        }
        set
        {
            if(value < _credibility)        
                StartCoroutine(UpdateSlowly(value, ref _credibility, 1, 0.5f));
            else
                StartCoroutine(UpdateSlowly(value, ref _credibility, -1, 0.5f));
            
            _credibilityText.text = _credibility.ToString();
        }
    }
    
    private int _likes;
    public int Likes {
        get
        {
            return _likes;
        }
        set
        {
            if (value < _likes)
                StartCoroutine(UpdateSlowly(value, ref _likes, 1, 0.5f));
            else
                StartCoroutine(UpdateSlowly(value, ref _likes, -1, 0.5f));
            _likesText.text = _likes.ToString();
        }
    }
   
    public IEnumerator UpdateSlowly(int target, ref int value, int delta, float delay)
    {
        while(value <= target)
        {            
            yield return new WaitForSeconds(delay);
            value+= delta;
        }
        
    } 
    
   
}
