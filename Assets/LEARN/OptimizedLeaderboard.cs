using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptimizedLeaderboard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private LeaderboardRowUI rowPrefab;

    [Header("Row Settings")]
    [SerializeField] private float itemHeight = 60f;
    [SerializeField] private float spacing = 10f;

    private List<PlayerData> allData = new List<PlayerData>();
    private List<LeaderboardRowUI> pooledRows = new List<LeaderboardRowUI>();

    private int totalSpawnedRows;
    private int previousFirstIndex = -1;

    private float ItemTotalHeight => itemHeight + spacing;

    public void Initialize(List<PlayerData> data)
    {
        allData = data;

        // 1. Gỡ listener cũ để tránh trùng lặp
        scrollRect.onValueChanged.RemoveListener(OnScroll);

        // 2. Tính tổng chiều cao của 5000 phần tử để Scrollbar co giãn đúng
        float totalHeight = allData.Count * ItemTotalHeight;
        content.sizeDelta = new Vector2(content.sizeDelta.x, totalHeight);

        // 3. Tính số dòng cần tạo trong pool (Chiều cao Viewport / Chiều cao dòng + đệm)
        float viewportHeight = scrollRect.viewport.rect.height;
        int visibleCount = Mathf.CeilToInt(viewportHeight / ItemTotalHeight);
        totalSpawnedRows = visibleCount + 4; // Thêm 4 dòng đệm

        // Giới hạn không tạo vượt quá tổng số dữ liệu hiện có
        totalSpawnedRows = Mathf.Min(totalSpawnedRows, allData.Count);

        // 4. Khởi tạo pool ban đầu
        SetupPool();

        // 5. Lắng nghe cuộn
        scrollRect.onValueChanged.AddListener(OnScroll);

        // Cập nhật vị trí hiển thị ban đầu
        UpdateList(0);
    }

    private void SetupPool()
    {
        // Tái sử dụng hoặc tạo mới đủ số lượng trong pool
        while (pooledRows.Count < totalSpawnedRows)
        {
            LeaderboardRowUI row = Instantiate(rowPrefab, content);
            RectTransform rect = row.GetComponent<RectTransform>();
            
            // Thiết lập neo Top-Stretch thủ công
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, itemHeight);

            pooledRows.Add(row);
        }
    }

    private void OnScroll(Vector2 scrollPos)
    {
        // Tính toán vị trí Content đang bị đẩy lên bao nhiêu pixel
        float contentY = content.anchoredPosition.y;
        int firstVisibleIndex = Mathf.FloorToInt(contentY / ItemTotalHeight);
        firstVisibleIndex = Mathf.Clamp(firstVisibleIndex, 0, Mathf.Max(0, allData.Count - totalSpawnedRows));

        if (firstVisibleIndex != previousFirstIndex)
        {
            previousFirstIndex = firstVisibleIndex;
            UpdateList(firstVisibleIndex);
        }
    }

    private void UpdateList(int startIndex)
    {
        for (int i = 0; i < pooledRows.Count; i++)
        {
            int dataIndex = startIndex + i;

            if (dataIndex < allData.Count)
            {
                LeaderboardRowUI row = pooledRows[i];
                row.gameObject.SetActive(true);

                // Đặt vị trí chính xác của từng dòng theo công thức toán
                RectTransform rect = row.GetComponent<RectTransform>();
                float posY = -(dataIndex * ItemTotalHeight);
                rect.anchoredPosition = new Vector2(0, posY);

                // Gán dữ liệu cho dòng
                row.SetData(dataIndex + 1, allData[dataIndex].playerName, allData[dataIndex].score);
            }
            else
            {
                pooledRows[i].gameObject.SetActive(false);
            }
        }
    }
}