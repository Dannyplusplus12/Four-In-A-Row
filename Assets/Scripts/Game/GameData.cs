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

    public enum RuleMode
    {
        Classic,
        SwipeGravity,
        DecayAfterEightTurns,
        RotateAfterTwoTurns,
        BlockAfterFourTurns,
        FiveStarWin,
        DestroyStarOnTap
    }

    // Chế độ chơi hiện tại
    public static GameMode selectedMode = GameMode.OnePlayer;
    public static RuleMode selectedRuleMode = RuleMode.Classic;
    public static string selectedModeTitle = "Classic";
    public static string selectedModeDescription = "Tap a line to drop a piece";

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
    public static bool ModeSelectionRequested { get; private set; }
    public static bool HasSavedGame => PlayerPrefs.HasKey(SavedGameKey);

    public static void BeginNewGame()
    {
        ResumeRequested = false;
        ClearSavedGame();
    }

    public static void RequestResume() => ResumeRequested = true;
    public static void ClearResumeRequest() => ResumeRequested = false;
    public static void RequestModeSelection() => ModeSelectionRequested = true;
    public static void ClearModeSelectionRequest() => ModeSelectionRequested = false;

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

    public static ConnectFour.Core.BoardRuleMode GetCoreRuleMode()
    {
        return (ConnectFour.Core.BoardRuleMode)selectedRuleMode;
    }

    public static string GetRuleInstruction()
    {
        switch (selectedRuleMode)
        {
            case RuleMode.SwipeGravity: return "Swipe to change gravity (counts as a turn)";
            case RuleMode.DecayAfterEightTurns: return "Your oldest piece disappears after 8 turns";
            case RuleMode.RotateAfterTwoTurns: return "Board rotates after each player's 4th turn";
            case RuleMode.BlockAfterFourTurns: return "A fixed block appears after every 4 turns";
            case RuleMode.FiveStarWin: return "Connect 5 stars to win";
            case RuleMode.DestroyStarOnTap: return "Tap any star to destroy it; this counts as a turn";
            default: return "Tap a line to drop a piece";
        }
    }

    [System.Serializable]
    private sealed class SavedGameData
    {
        public int[] ownerIds;
        public int gravity;
        public int activePlayerId;
    }
}