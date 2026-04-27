using System.Collections;
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

    public int scoreValue = 10;

    [Header("Ustawienia Odradzania")]
    public float respawnTime = 30f;
    private bool isCollected = false;

    public string GetInteractPrompt()
    {
        if (isCollected) return string.Empty;

        return $"Zbierz: {itemName}";
    }

    public void Interact()
    {
        if (isCollected) return;

        Debug.Log($"Zebrano: {itemName}");
        CollectibleItemGlob.ScoreCount += scoreValue;
        Debug.Log($"{scoreValue}, {CollectibleItemGlob.ScoreCount}");

        if (score != null)
        {
            score.text = "Wynik: " + CollectibleItemGlob.ScoreCount.ToString();
        }
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        isCollected = true;
        ToggleVisibility(false);
        yield return new WaitForSeconds(respawnTime);
        ToggleVisibility(true);
        isCollected = false;
    }

    private void ToggleVisibility(bool state)
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = state;
        }

        foreach (var c in GetComponentsInChildren<Collider>())
        {
            c.enabled = state;
        }
    }
}