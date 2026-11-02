
using UnityEngine;
using TMPro;
using System.IO;
using System.Text;

public class PlayerInfoManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] GameObject playerInfoPanel;
    [SerializeField] TMP_InputField ageInput;
    [SerializeField] TMP_Dropdown conditionDropdown;

    string filePath;

    void Start()
    {
        filePath = Path.Combine(Application.dataPath,"PlayerDetails.csv");

        bool completed = PlayerPrefs.GetInt("PlayerInfoCompleted", 0) == 1;

        playerInfoPanel.SetActive(!completed);
    }

    public void SavePlayerDetails()
    {
        if (!int.TryParse(ageInput.text, out int age) || age <= 0)
        {
            Debug.Log("Please enter a valid age.");
            return;
        }

        string condition = conditionDropdown.options[
            conditionDropdown.value
        ].text;

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "Age,VisualCondition\n");
        }

        string row = age + "," + EscapeCSV(condition) + "\n";

        File.AppendAllText(filePath, row, Encoding.UTF8);

        PlayerPrefs.SetInt("PlayerInfoCompleted", 1);
        PlayerPrefs.Save();

        playerInfoPanel.SetActive(false);

        Debug.Log("Player details saved to: " + filePath);
    }

    string EscapeCSV(string value)
    {
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
