using UnityEngine;

public static class GameData
{
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
}