using UnityEngine;

public class CollectibleItem : MonoBehaviour, IInteractable
{
    [SerializeField] private string itemName = "Przedmiot";

    public string GetInteractPrompt() => $"Zbierz: {itemName}";

    public void Interact()
    {
        Debug.Log($"Zebrano: {itemName}");

        Destroy(gameObject);
    }
}