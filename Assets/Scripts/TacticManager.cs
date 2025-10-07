using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;


public class TacticManager : MonoBehaviour
{

    public Animator TacticPanel;
    [Header("Grid Settings")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform gridParent;

    [Header("Grid Settings")]
    
    [SerializeField] private List<TacticSO> tacticsToCreate; // Changed to a list of TacticSO

    private Card _currentlySelectedCard;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        int numberOfCards = tacticsToCreate.Count;
        PopulateGrid(numberOfCards);
    }


    public void OpenCardView() {
        TacticPanel.SetBool("isHidden", false);
       
    }

    public void CloseCardView() {
        TacticPanel.SetBool("isHidden", true);

    }
    public void PopulateGrid(int count) {
        // Clear any existing cards in the grid before populating
        foreach(Transform child in gridParent) {
            Destroy(child.gameObject);
        }

        // Loop through the TacticSO list
        foreach(TacticSO tacticData in tacticsToCreate) {
            if(!tacticData.enabledFlag) continue; // Skip disabled tactics

            GameObject newCardObj = Instantiate(cardPrefab, gridParent);
            Card cardComponent = newCardObj.GetComponent<Card>();

            // Pass the TacticSO data to the new card
            cardComponent.Setup(tacticData, this);
        }
    }


    public void OnCardSelected(Card card) {
        if(_currentlySelectedCard != null) {
            _currentlySelectedCard.Deselect();
        }

        if(_currentlySelectedCard == card) {
            _currentlySelectedCard = null;
            Debug.Log("Card deselected.");
        }
        else {
            _currentlySelectedCard = card;
            _currentlySelectedCard.Select();
            // You can now access the selected tactic's data
            Debug.Log($"Selected Tactic: {_currentlySelectedCard.TacticData.displayName}");
        }
    }
}
