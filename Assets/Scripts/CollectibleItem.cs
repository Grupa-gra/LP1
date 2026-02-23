using TMPro;
using UnityEngine;

public class CollectibleItem : MonoBehaviour, IInteractable
{
    [SerializeField] private string itemName = "Przedmiot";
    [SerializeField] private TextMeshProUGUI score;

    public string GetInteractPrompt() => $"Zbierz: {itemName}";
    public int scoreCount = 0;
    public int scoreValue = 10;

    public void Interact()
    {
        Debug.Log($"Zebrano: {itemName}");
        scoreCount += scoreValue;
        score.text = "Wynik: " + scoreCount.ToString();
        Destroy(gameObject);
    }
}