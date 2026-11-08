
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardButton : MonoBehaviour
{
    public Transform content;
    public GameObject entryPrefab;
    public string version;

    [Header("Button Highlight")]
    public Button[] versionButtons;
    public Color selectedColor = Color.white;
    public Color normalColor = Color.gray;

    public void ShowLeaderboard()
    {
        // Clear previous entries
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        string key = "Leaderboard_" + version;

        if (!PlayerPrefs.HasKey(key))
            return;

        LeaderboardData data = JsonUtility.FromJson<LeaderboardData>(
            PlayerPrefs.GetString(key)
        );

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
}
