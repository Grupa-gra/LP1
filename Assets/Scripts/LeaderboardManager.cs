using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class ScoreEntry
{
    public int score;
    public string date; // "yyyy-MM-dd HH:mm"

    public ScoreEntry(int score, string date)
    {
        this.score = score;
        this.date  = date;
    }
}

[Serializable]
public class LeaderboardData
{
    public List<ScoreEntry> entries = new List<ScoreEntry>();
}

/// <summary>
/// Singleton odpowiedzialny za zapis/odczyt top-10 wyników do pliku JSON.
/// Plik leaderboard.json trafia do Application.persistentDataPath.
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    private const int MaxEntries = 5;
    private const string FileName = "leaderboard.json";

    private LeaderboardData data = new LeaderboardData();
    private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    // ---------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    // ---------------------------------------------------------------
    // Publiczne API

    /// <summary>
    /// Próbuje dodać wynik. Zwraca true, jeśli wynik trafił na tablicę.
    /// </summary>
    public bool TryAddScore(int score)
    {
        bool qualifies = data.entries.Count < MaxEntries
                      || score > data.entries[data.entries.Count - 1].score;

        if (!qualifies) return false;

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        data.entries.Add(new ScoreEntry(score, timestamp));

        // Sortuj malejąco i przytnij do top-10
        data.entries.Sort((a, b) => b.score.CompareTo(a.score));
        if (data.entries.Count > MaxEntries)
            data.entries.RemoveRange(MaxEntries, data.entries.Count - MaxEntries);

        Save();
        return true;
    }

    /// <summary>
    /// Zwraca kopię listy wpisów (maksymalnie 10, posortowane malejąco).
    /// </summary>
    public List<ScoreEntry> GetEntries() => new List<ScoreEntry>(data.entries);

    // ---------------------------------------------------------------
    // Zapis / odczyt

    private void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(FilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError("[LeaderboardManager] Błąd zapisu: " + e.Message);
        }
    }

    private void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                data = JsonUtility.FromJson<LeaderboardData>(json) ?? new LeaderboardData();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[LeaderboardManager] Błąd odczytu: " + e.Message);
            data = new LeaderboardData();
        }
    }
}
