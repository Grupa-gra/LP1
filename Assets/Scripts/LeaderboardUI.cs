using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Obsługuje panel leaderboard w scenie Menu.
/// Podepnij do GameObject-u z panelem leaderboardu.
///
/// Wymagana hierarchia UI (szczegóły w README poniżej):
///   Canvas
///   └─ MenuRoot          ← główne menu (przyciemniany panel)
///   └─ LeaderboardPanel  ← ten GameObject
///      ├─ TitleText
///      ├─ ScrollView / Viewport / Content   ← tu wstrzykiwane są wiersze
///      └─ CloseButton
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    [Header("Panel References")]
    [Tooltip("Sam panel leaderboard (ten GameObject lub rodzic).")]
    [SerializeField] private GameObject leaderboardPanel;

    [Tooltip("Rodzic wierszy wyników (Content wewnątrz ScrollView).")]
    [SerializeField] private Transform entriesContainer;

    [Tooltip("Prefab pojedynczego wiersza – patrz opis w komentarzu.")]
    [SerializeField] private GameObject entryRowPrefab;
    
    // ---------------------------------------------------------------

    private void Awake()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
    }

    // ---------------------------------------------------------------
    // Publiczne metody – podepnij do przycisków przez Inspector

    /// <summary>Otwiera panel i wypełnia wyniki.</summary>
    public void OpenLeaderboard()
    {
        PopulateEntries();
        leaderboardPanel.SetActive(true);
    }

    /// <summary>Zamyka panel.</summary>
    public void CloseLeaderboard()
    {
        leaderboardPanel.SetActive(false);
    }

    // ---------------------------------------------------------------

    private void PopulateEntries()
    {
        // Usuń stare wiersze
        foreach (Transform child in entriesContainer)
            Destroy(child.gameObject);

        List<ScoreEntry> entries = LeaderboardManager.Instance.GetEntries();

        if (entries.Count == 0)
        {
            // Brak wyników – pokaż placeholder
            GameObject row = Instantiate(entryRowPrefab, entriesContainer);
            SetRowTexts(row, "-", "Brak wyników", "");
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

    /// <summary>
    /// Ustawia TextMeshPro w prefabie wiersza.
    /// Prefab musi mieć dzieci o nazwach: "RankText", "ScoreText", "DateText".
    /// </summary>
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
