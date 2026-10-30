using UnityEngine;
using TMPro; // Nếu dùng Text thường thì dùng: using UnityEngine.UI;

public class LeaderboardRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text scoreText;

    public void SetData(int rank, string playerName, int score)
    {
        rankText.text = $"#{rank}";
        nameText.text = playerName;
        scoreText.text = score.ToString("N0"); // Định dạng số có dấu phẩy (vd: 12,500)
    }
}