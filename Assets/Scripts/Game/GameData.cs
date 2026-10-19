using UnityEngine;

public static class GameData
{
    private const string SavedGameKey = "connect_four_saved_game";

    public enum GameMode
    {
        OnePlayer,
        TwoPlayer
    }

    public enum FirstMoveOption
    {
        Player1 = 0,
        Player2 = 1,
        Random = 2
    }

    // Chế độ chơi hiện tại
    public static GameMode selectedMode = GameMode.TwoPlayer;

    // Màu của Player 1 (true: Xanh, false: Đỏ)
    public static bool isP1Green = true;

    // Ai đi trước
    public static FirstMoveOption firstMove = FirstMoveOption.Player1;

    // Độ khó bot (1: Easy, 2: Medium, 3: Hard)
    public static int difficultyLevel = 1;

    // Tên hiển thị
    public static string player1Name = "Player 1";
    public static string player2Name = "Player 2";
    public static Sprite player1StarSprite;
    public static Sprite player2StarSprite;

    public static bool ResumeRequested { get; private set; }
    public static bool HasSavedGame => PlayerPrefs.HasKey(SavedGameKey);

    public static void BeginNewGame()
    {
        ResumeRequested = false;
        ClearSavedGame();
    }

    public static void RequestResume() => ResumeRequested = true;
    public static void ClearResumeRequest() => ResumeRequested = false;

    public static void SaveGame(ConnectFour.Core.BoardSnapshot snapshot)
    {
        if (snapshot == null || selectedMode != GameMode.OnePlayer) return;
        SavedGameData data = new SavedGameData
        {
            ownerIds = snapshot.OwnerIds,
            gravity = (int)snapshot.Gravity,
            activePlayerId = snapshot.ActivePlayerId
        };
        PlayerPrefs.SetString(SavedGameKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static ConnectFour.Core.BoardSnapshot LoadSavedGame()
    {
        if (!HasSavedGame) return null;
        try
        {
            SavedGameData data = JsonUtility.FromJson<SavedGameData>(PlayerPrefs.GetString(SavedGameKey));
            if (data == null || data.ownerIds == null) return null;
            return new ConnectFour.Core.BoardSnapshot(
                data.ownerIds,
                (ConnectFour.Core.GravityDirection)data.gravity,
                data.activePlayerId);
        }
        catch (System.Exception)
        {
            return null;
        }
    }

    public static void ClearSavedGame()
    {
        PlayerPrefs.DeleteKey(SavedGameKey);
        PlayerPrefs.Save();
    }

    [System.Serializable]
    private sealed class SavedGameData
    {
        public int[] ownerIds;
        public int gravity;
        public int activePlayerId;
    }
}