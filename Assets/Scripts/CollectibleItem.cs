using TMPro;
using UnityEngine;


public static class CollectibleItemGlob
{
    public static int ScoreCount = 0;

}
public class CollectibleItem : MonoBehaviour, IInteractable
{
    [SerializeField] private string itemName = "Przedmiot";
    [SerializeField] private TextMeshProUGUI score;
    
    public string GetInteractPrompt() => $"Zbierz: {itemName}";
    public int scoreValue = 10;

    public void Interact()
    {
        Debug.Log($"Zebrano: {itemName}");
        CollectibleItemGlob.ScoreCount += scoreValue;
        Debug.Log($"{scoreValue}, {CollectibleItemGlob.ScoreCount}");

    score.text = "Wynik: " + CollectibleItemGlob.ScoreCount.ToString();
        Destroy(gameObject);
    }
}