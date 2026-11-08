using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;  

[System.Serializable]
public class LeaderboardData
{
    public List<LeaderboardRecord> entries = new List<LeaderboardRecord>();
}

[System.Serializable]
public class LeaderboardRecord
{
    public string playerName;
    public int score;
    public float completionTime;
}

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance;

    [Header("Leaderboard UI")]
    public Transform content;
    public GameObject entryPrefab;

    private string currentVersion = "V1";

    void Awake()
    {
        Instance = this;
    }

    public void SaveScore(string version, string playerName, int score, float time)
    {
        string key = "Leaderboard_" + version;

        LeaderboardData data = LoadData(key);

        // Find this player's existing record.
        LeaderboardRecord existing = data.entries.Find(
            entry => entry.playerName == playerName
        );

        if (existing == null)
        {
            data.entries.Add(new LeaderboardRecord
            {
                playerName = playerName,
                score = score,
                completionTime = time
            });
        }
        else if (score > existing.score ||
                (score == existing.score && time < existing.completionTime))
        {
            existing.score = score;
            existing.completionTime = time;
        }

        // Highest score first, then fastest time.
        data.entries = data.entries
            .OrderByDescending(entry => entry.score)
            .ThenBy(entry => entry.completionTime)
            .ToList();

        PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    LeaderboardData LoadData(string key)
    {
        if (!PlayerPrefs.HasKey(key))
            return new LeaderboardData();

        return JsonUtility.FromJson<LeaderboardData>(
            PlayerPrefs.GetString(key)
        );
    }

    public void DisplayLeaderboard(string version)
    {
        if (content == null)
        {
            FindLeaderboardContent();
        }

        if (content == null)
        {
            Debug.LogError("Cannot display leaderboard. Content is missing!");
            return;
        }

        // Remove previously displayed entries.
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        string key = "Leaderboard_" + version;
        LeaderboardData data = LoadData(key);

        // Create a row for each saved player.
        for (int i = 0; i < data.entries.Count; i++)
        {
            GameObject newEntry = Instantiate(entryPrefab, content);

            LeaderboardEntry entry = newEntry.GetComponent<LeaderboardEntry>();

            LeaderboardRecord record = data.entries[i];

            entry.SetEntry(
                i + 1,
                record.playerName,
                record.score,
                record.completionTime
            );
        }
    }

    void FindLeaderboardContent()
    {
        Transform[] allObjects = Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform obj in allObjects)
        {
            if (obj.name == "LeaderboardScrollView" &&
                obj.gameObject.scene.name == "GYM_MainMenu")
            {
                content = obj.Find("Viewport/Content");

                if (content != null)
                {
                    Debug.Log("Leaderboard Content successfully found!");
                    return;
                }
            }
        }

        Debug.LogError("Leaderboard Content not found!");
    }

    public void RefreshLeaderboardContent()
    {
        FindLeaderboardContent();
    }

    public void ShowV1()
    {
        DisplayLeaderboard("V1");
    }

    public void ShowV2()
    {
        DisplayLeaderboard("V2");
    }

    public void ShowV3()
    {
        DisplayLeaderboard("V3");
    }
}
