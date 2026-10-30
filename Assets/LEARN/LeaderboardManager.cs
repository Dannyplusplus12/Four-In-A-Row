using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PlayerData
{
    public string playerName;
    public int score;

    public PlayerData(string name, int score)
    {
        this.playerName = name;
        this.score = score;
    }
}

public class LeaderboardManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private LeaderboardRowUI rowPrefab;

    [Header("Row Settings")]
    [SerializeField] private float itemHeight = 60f;
    [SerializeField] private float spacing = 10f;
    [SerializeField] private int totalDummyItems = 5000;

    private List<PlayerData> allData = new List<PlayerData>();
    private List<LeaderboardRowUI> pooledRows = new List<LeaderboardRowUI>();

    private int totalSpawnedRows;
    private int previousFirstIndex = -1;
    private float ItemTotalHeight => itemHeight + spacing;

    private void Start()
    {
        allData = GenerateDummyData();
        InitializeLeaderboard();
    }

    private List<PlayerData> GenerateDummyData()
    {
        List<PlayerData> list = new List<PlayerData>();
        string[] sampleNames = { "Shadow", "Phoenix", "Dragon", "Viper", "Echo", "Ghost", "Titan", "Nova" };

        for (int i = 0; i < totalDummyItems; i++)
        {
            string randomName = sampleNames[Random.Range(0, sampleNames.Length)] + "_" + Random.Range(10, 99);
            int randomScore = Random.Range(1000, 100000);
            list.Add(new PlayerData(randomName, randomScore));
        }

        list.Sort((a, b) => b.score.CompareTo(a.score));
        return list;
    }

    private void InitializeLeaderboard()
    {
        // 1. Đặt kích thước ảo cho Content để thanh cuộn hoạt động chuẩn
        float totalHeight = allData.Count * ItemTotalHeight;
        content.sizeDelta = new Vector2(content.sizeDelta.x, totalHeight);

        // 2. Tính số lượng item cần thiết hiển thị trên màn hình
        float viewportHeight = scrollRect.viewport.rect.height;
        if (viewportHeight <= 0) viewportHeight = 600f; // Phòng trường hợp Viewport chưa cập nhật Rect ban đầu

        int visibleCount = Mathf.CeilToInt(viewportHeight / ItemTotalHeight);
        totalSpawnedRows = visibleCount + 5; // Thêm 5 item đệm
        totalSpawnedRows = Mathf.Min(totalSpawnedRows, allData.Count);

        // 3. Xóa các object cũ còn sót trong Content (nếu có)
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
        pooledRows.Clear();

        // 4. Chỉ sinh ĐÚNG số lượng dòng hiển thị vào Pool
        for (int i = 0; i < totalSpawnedRows; i++)
        {
            LeaderboardRowUI row = Instantiate(rowPrefab, content);
            RectTransform rect = row.GetComponent<RectTransform>();

            // Khóa neo của từng row về Top-Stretch
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, itemHeight);

            pooledRows.Add(row);
        }

        // 5. Gắn sự kiện lắng nghe khi cuộn
        scrollRect.onValueChanged.RemoveAllListeners();
        scrollRect.onValueChanged.AddListener(OnScroll);

        // Cập nhật vị trí và dữ liệu ban đầu
        previousFirstIndex = -1;
        UpdateVisibleRows(0);
    }

    private void OnScroll(Vector2 scrollPos)
    {
        float contentY = content.anchoredPosition.y;
        int firstVisibleIndex = Mathf.FloorToInt(contentY / ItemTotalHeight);
        firstVisibleIndex = Mathf.Clamp(firstVisibleIndex, 0, Mathf.Max(0, allData.Count - totalSpawnedRows));

        if (firstVisibleIndex != previousFirstIndex)
        {
            previousFirstIndex = firstVisibleIndex;
            UpdateVisibleRows(firstVisibleIndex);
        }
    }

    private void UpdateVisibleRows(int startIndex)
    {
        for (int i = 0; i < pooledRows.Count; i++)
        {
            int dataIndex = startIndex + i;
            LeaderboardRowUI row = pooledRows[i];

            if (dataIndex < allData.Count)
            {
                row.gameObject.SetActive(true);

                // Dịch chuyển toạ độ Y của từng dòng theo chỉ số dữ liệu
                RectTransform rect = row.GetComponent<RectTransform>();
                float posY = -(dataIndex * ItemTotalHeight);
                rect.anchoredPosition = new Vector2(0, posY);

                row.SetData(dataIndex + 1, allData[dataIndex].playerName, allData[dataIndex].score);
            }
            else
            {
                row.gameObject.SetActive(false);
            }
        }
    }
}