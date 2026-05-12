using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Panel References")]
    [Tooltip("Sam panel leaderboard (ten GameObject lub rodzic).")]
    [SerializeField] private GameObject leaderboardPanel;

    [Tooltip("Rodzic wierszy wyników (Content wewnątrz ScrollView).")]
    [SerializeField] private Transform entriesContainer;

    [Tooltip("Prefab pojedynczego wiersza – patrz opis w komentarzu.")]
    [SerializeField] private GameObject entryRowPrefab;

    private void Awake()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
    }

    public void OpenLeaderboard()
    {
        PopulateEntries();
        leaderboardPanel.SetActive(true);
    }

    public void CloseLeaderboard()
    {
        leaderboardPanel.SetActive(false);
    }

    private void PopulateEntries()
    {
        foreach (Transform child in entriesContainer)
            Destroy(child.gameObject);

        List<ScoreEntry> entries = LeaderboardManager.Instance.GetEntries();

        if (entries.Count == 0)
        {
            GameObject row = Instantiate(entryRowPrefab, entriesContainer);
            SetRowTexts(row, "-", "No scores yet", "");
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            GameObject row = Instantiate(entryRowPrefab, entriesContainer);
            SetRowTexts(row,
                rank:  (i + 1).ToString(),
                score: entries[i].score.ToString(),
                date:  entries[i].date);
        }
    }

    private void SetRowTexts(GameObject row, string rank, string score, string date)
    {
        SetTMP(row, "RankText",  rank);
        SetTMP(row, "ScoreText", score);
        SetTMP(row, "DateText",  date);
    }

    private void SetTMP(GameObject root, string childName, string text)
    {
        Transform t = root.transform.Find(childName);
        if (t == null) { Debug.LogWarning($"[LeaderboardUI] Brak dziecka '{childName}' w prefabie wiersza."); return; }
        TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.text = text;
    }
}