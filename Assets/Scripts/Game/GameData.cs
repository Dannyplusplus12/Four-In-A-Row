using ConnectFour.Core;
using UnityEngine;

public static class GameData
{
    private const string SavedGameKey = "connect_four_saved_game";
    private const string TotalStarsKey = "connect_four_total_stars";

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

    // Current game mode
    public static GameMode selectedMode = GameMode.OnePlayer;
    public static RuleMode selectedRuleMode = RuleMode.Classic;
    public static string selectedModeTitle = "Classic";
    public static string selectedModeDescription = "Tap a line to drop a piece";

    // Player 1 color (true: Green, false: Red)
    public static bool isP1Green = true;

    // Who goes first
    public static FirstMoveOption firstMove = FirstMoveOption.Player1;

    // Bot difficulty (1: Easy, 2: Medium, 3: Hard)
    public static int difficultyLevel = 1;

    // Mode star reward (1..3) kept separate from bot difficulty so selecting a mode doesn't overwrite AI level
    public static int modeStarReward = 1;

    // Display names & sprites
    public static string player1Name = "Player 1";
    public static string player2Name = "Player 2";
    public static Sprite player1StarSprite;
    public static Sprite player2StarSprite;

    // 2-Player BO3 (Best of 3 - first to 2 wins) scoreboard
    public const int TwoPlayerWinsToWinSeries = 2;
    public static int player1Score;
    public static int player2Score;
    public static int twoPlayerDraws;
    public static int TwoPlayerCurrentRound => player1Score + player2Score + twoPlayerDraws + 1;
    public static bool IsTwoPlayerSeriesOver => player1Score >= TwoPlayerWinsToWinSeries || player2Score >= TwoPlayerWinsToWinSeries;
    public static int? TwoPlayerSeriesWinnerId =>
        player1Score >= TwoPlayerWinsToWinSeries ? 1 :
        player2Score >= TwoPlayerWinsToWinSeries ? 2 : (int?)null;

    public static void ResetTwoPlayerScores()
    {
        player1Score = 0;
        player2Score = 0;
        twoPlayerDraws = 0;
    }

    public static void RecordTwoPlayerResult(int? winnerId, bool isDraw)
    {
        if (isDraw)
        {
            twoPlayerDraws++;
            return;
        }
        if (winnerId == 1) player1Score++;
        else if (winnerId == 2) player2Score++;
    }

    public static bool ResumeRequested { get; private set; }
    public static bool ModeSelectionRequested { get; private set; }
    public static bool HasSavedGame => PlayerPrefs.HasKey(SavedGameKey);
    public static int TotalStars => PlayerPrefs.GetInt(TotalStarsKey, 0);
    public const int DefaultRequiredStarsToWin = 30;
    public static int RequiredStarsToWin { get; private set; } = DefaultRequiredStarsToWin;

    public static void SetRequiredStarsToWin(int requiredStars)
    {
        RequiredStarsToWin = Mathf.Max(1, requiredStars);
    }

    public static void AddStars(int amount)
    {
        if (amount <= 0) return;
        PlayerPrefs.SetInt(TotalStarsKey, TotalStars + amount);
        PlayerPrefs.Save();
    }

    public static void ResetStars()
    {
        PlayerPrefs.SetInt(TotalStarsKey, 0);
        PlayerPrefs.Save();
    }

    public static void BeginNewGame()
    {
        ResumeRequested = false;
        ClearSavedGame();
    }

    public static void RequestResume() => ResumeRequested = true;
    public static void ClearResumeRequest() => ResumeRequested = false;
    public static void RequestModeSelection() => ModeSelectionRequested = true;
    public static void ClearModeSelectionRequest() => ModeSelectionRequested = false;

    public static int ResolveStartingPlayerId()
    {
        switch (firstMove)
        {
            case FirstMoveOption.Player2:
                return 2;
            case FirstMoveOption.Random:
                return Random.Range(0, 2) == 0 ? 1 : 2;
            default:
                return 1;
        }
    }

    public static void SaveGame(BoardSnapshot snapshot)
    {
        if (snapshot == null || selectedMode != GameMode.OnePlayer) return;
        SavedGameData data = new SavedGameData
        {
            ownerIds = snapshot.OwnerIds,
            gravity = (int)snapshot.Gravity,
            activePlayerId = snapshot.ActivePlayerId,
            ruleMode = (int)snapshot.RuleMode,
            rotationQuarterTurns = snapshot.RotationQuarterTurns,
            totalTurnCount = snapshot.TotalTurnCount,
            player1TurnCount = snapshot.Player1TurnCount,
            player2TurnCount = snapshot.Player2TurnCount,
            pieceOrder = snapshot.PieceOrder,
            isP1Green = isP1Green,
            difficultyLevel = difficultyLevel,
            modeStarReward = modeStarReward
        };
        PlayerPrefs.SetString(SavedGameKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static BoardSnapshot LoadSavedGame()
    {
        if (!HasSavedGame) return null;
        try
        {
            SavedGameData data = JsonUtility.FromJson<SavedGameData>(PlayerPrefs.GetString(SavedGameKey));
            if (data == null || data.ownerIds == null) return null;

            RuleMode savedRuleMode = (RuleMode)Mathf.Clamp(data.ruleMode, 0, (int)RuleMode.DestroyStarOnTap);
            selectedRuleMode = savedRuleMode;
            selectedModeTitle = GetRuleModeTitle(savedRuleMode);
            selectedModeDescription = GetRuleInstruction();
            isP1Green = data.isP1Green;
            if (data.difficultyLevel >= 1 && data.difficultyLevel <= 3)
                difficultyLevel = data.difficultyLevel;
            if (data.modeStarReward >= 1 && data.modeStarReward <= 3)
                modeStarReward = data.modeStarReward;

            return new BoardSnapshot(
                data.ownerIds,
                (GravityDirection)data.gravity,
                data.activePlayerId,
                (BoardRuleMode)savedRuleMode,
                data.rotationQuarterTurns,
                data.totalTurnCount,
                data.player1TurnCount,
                data.player2TurnCount,
                data.pieceOrder);
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

    public static BoardRuleMode GetCoreRuleMode()
    {
        return (BoardRuleMode)selectedRuleMode;
    }

    public static string GetRuleModeTitle(RuleMode mode)
    {
        switch (mode)
        {
            case RuleMode.SwipeGravity: return "Swipe Gravity";
            case RuleMode.DecayAfterEightTurns: return "Decay";
            case RuleMode.RotateAfterTwoTurns: return "Rotating Board";
            case RuleMode.BlockAfterFourTurns: return "Blocked";
            case RuleMode.FiveStarWin: return "Five In A Row";
            case RuleMode.DestroyStarOnTap: return "Destroy Star";
            default: return "Classic";
        }
    }

    public static string GetRuleInstruction()
    {
        switch (selectedRuleMode)
        {
            case RuleMode.SwipeGravity: return "Swipe to change gravity (counts as a turn)";
            case RuleMode.DecayAfterEightTurns: return "Your oldest star disappears after 8 turns";
            case RuleMode.RotateAfterTwoTurns: return "Board rotates after each player's 4th turn";
            case RuleMode.BlockAfterFourTurns: return "A fixed block appears after every 4 turns";
            case RuleMode.FiveStarWin: return "Connect 5 stars to win";
            case RuleMode.DestroyStarOnTap: return "Tap any star to destroy it (counts as a turn)";
            default: return "Tap a column to drop a star";
        }
    }

    [System.Serializable]
    private sealed class SavedGameData
    {
        public int[] ownerIds;
        public int gravity;
        public int activePlayerId;
        public int ruleMode;
        public int rotationQuarterTurns;
        public int totalTurnCount;
        public int player1TurnCount;
        public int player2TurnCount;
        public int[] pieceOrder;
        public bool isP1Green = true;
        public int difficultyLevel = 1;
        public int modeStarReward = 1;
    }
}
