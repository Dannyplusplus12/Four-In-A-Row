using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectFourGameController : MonoBehaviour
{
    public const int Columns = 7;
    public const int Rows = 6;

    [Header("Board")]
    [SerializeField] private Transform boardRoot;
    [SerializeField] private ConnectFourCell[] boardCells;
    [SerializeField] private ConnectFourColumnButton[] columnButtons;
    [SerializeField] private RectTransform hoverColumnHighlight;
    [SerializeField] private Sprite playerOnePiece;
    [SerializeField] private Sprite playerTwoPiece;

    [Header("Rules")]
    [SerializeField] private float botMoveDelay = 0.45f;

    [Header("UI")]
    [SerializeField] private ConnectFourGameUI gameUI;

    private readonly int[,] board = new int[Columns, Rows];
    private int currentPlayer;
    private bool gameOver;
    private Coroutine botTurn;

    private bool IsOnePlayer => GameData.selectedMode == GameData.GameMode.OnePlayer;
    private bool IsBotTurn => IsOnePlayer && currentPlayer == 2;

    private void Awake()
    {
        if (boardRoot != null && HasMissingCells())
            boardCells = boardRoot.GetComponentsInChildren<ConnectFourCell>(true);

        if (boardRoot != null && HasMissingColumnButtons())
            columnButtons = boardRoot.GetComponentsInChildren<ConnectFourColumnButton>(true);

        if (gameUI == null) gameUI = FindFirstObjectByType<ConnectFourGameUI>();
    }

    private void Start()
    {
        StartNewGame();
    }

    public void StartNewGame()
    {
        if (botTurn != null) StopCoroutine(botTurn);

        System.Array.Clear(board, 0, board.Length);
        gameOver = false;
        currentPlayer = ResolveFirstPlayer();

        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                ConnectFourCell cell = GetCell(column, row);
                if (cell == null) continue;

                cell.Setup(this, column);
                cell.SetPiece(null);
                cell.SetInteractable(!IsBotTurn);
            }
        }

        for (int column = 0; column < Columns && column < columnButtons.Length; column++)
        {
            if (columnButtons[column] == null) continue;
            columnButtons[column].Setup(this, column);
            columnButtons[column].SetInteractable(!IsBotTurn);
        }

        HideColumnHighlight();

        if (gameUI != null) gameUI.ShowGameStarted(currentPlayer);
        StartBotTurnIfNeeded();
    }

    public void TryDropPiece(int column)
    {
        if (gameOver || IsBotTurn || column < 0 || column >= Columns) return;

        DropPiece(column, currentPlayer);
    }

    public void ShowColumnHighlight(RectTransform columnButton)
    {
        if (hoverColumnHighlight == null || columnButton == null || gameOver || IsBotTurn) return;

        RectTransform highlightParent = hoverColumnHighlight.parent as RectTransform;
        if (highlightParent == null) return;

        Vector3 worldPosition = columnButton.position;
        Vector2 localPosition = highlightParent.InverseTransformPoint(worldPosition);
        hoverColumnHighlight.anchoredPosition = new Vector2(localPosition.x, hoverColumnHighlight.anchoredPosition.y);
        hoverColumnHighlight.gameObject.SetActive(true);
    }

    public void HideColumnHighlight()
    {
        if (hoverColumnHighlight != null) hoverColumnHighlight.gameObject.SetActive(false);
    }

    public void RestartGame()
    {
        StartNewGame();
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void DropPiece(int column, int player)
    {
        int row = FindOpenRow(column);
        if (row < 0) return;

        board[column, row] = player;
        ConnectFourCell cell = GetCell(column, row);
        if (cell != null) cell.SetPiece(player == 1 ? playerOnePiece : playerTwoPiece);

        if (HasWinningLine(column, row, player))
        {
            gameOver = true;
            SetBoardInteractable(false);
            SetColumnButtonsInteractable(false);
            HideColumnHighlight();
            if (gameUI != null) gameUI.ShowWinner(player);
            return;
        }

        if (IsBoardFull())
        {
            gameOver = true;
            SetBoardInteractable(false);
            SetColumnButtonsInteractable(false);
            HideColumnHighlight();
            if (gameUI != null) gameUI.ShowDraw();
            return;
        }

        currentPlayer = currentPlayer == 1 ? 2 : 1;
        if (gameUI != null) gameUI.ShowTurn(currentPlayer);
        SetBoardInteractable(!IsBotTurn);
        SetColumnButtonsInteractable(!IsBotTurn);
        StartBotTurnIfNeeded();
    }

    private void StartBotTurnIfNeeded()
    {
        if (!IsBotTurn || gameOver) return;
        botTurn = StartCoroutine(PlayBotTurn());
    }

    private IEnumerator PlayBotTurn()
    {
        yield return new WaitForSeconds(botMoveDelay);
        if (!gameOver && IsBotTurn) DropPiece(ChooseBotColumn(), 2);
        botTurn = null;
    }

    private int ChooseBotColumn()
    {
        int difficulty = Mathf.Clamp(GameData.difficultyLevel, 1, 3);
        int winningColumn = FindTacticalColumn(2);
        if (winningColumn >= 0) return winningColumn;

        if (difficulty >= 2)
        {
            int blockingColumn = FindTacticalColumn(1);
            if (blockingColumn >= 0) return blockingColumn;
        }

        if (difficulty >= 3)
        {
            int[] preferredColumns = { 3, 2, 4, 1, 5, 0, 6 };
            foreach (int column in preferredColumns)
                if (FindOpenRow(column) >= 0) return column;
        }

        int[] availableColumns = new int[Columns];
        int count = 0;
        for (int column = 0; column < Columns; column++)
            if (FindOpenRow(column) >= 0) availableColumns[count++] = column;

        return count == 0 ? 0 : availableColumns[Random.Range(0, count)];
    }

    private int FindTacticalColumn(int player)
    {
        for (int column = 0; column < Columns; column++)
        {
            int row = FindOpenRow(column);
            if (row < 0) continue;

            board[column, row] = player;
            bool wins = HasWinningLine(column, row, player);
            board[column, row] = 0;
            if (wins) return column;
        }

        return -1;
    }

    private int ResolveFirstPlayer()
    {
        if (GameData.firstMove == GameData.FirstMoveOption.Player2) return 2;
        if (GameData.firstMove == GameData.FirstMoveOption.Random) return Random.Range(1, 3);
        return 1;
    }

    private int FindOpenRow(int column)
    {
        for (int row = 0; row < Rows; row++)
            if (board[column, row] == 0) return row;
        return -1;
    }

    private bool IsBoardFull()
    {
        for (int column = 0; column < Columns; column++)
            if (FindOpenRow(column) >= 0) return false;
        return true;
    }

    private bool HasWinningLine(int column, int row, int player)
    {
        return CountDirection(column, row, 1, 0, player) + CountDirection(column, row, -1, 0, player) >= 3
            || CountDirection(column, row, 0, 1, player) + CountDirection(column, row, 0, -1, player) >= 3
            || CountDirection(column, row, 1, 1, player) + CountDirection(column, row, -1, -1, player) >= 3
            || CountDirection(column, row, 1, -1, player) + CountDirection(column, row, -1, 1, player) >= 3;
    }

    private int CountDirection(int column, int row, int columnStep, int rowStep, int player)
    {
        int count = 0;
        column += columnStep;
        row += rowStep;
        while (column >= 0 && column < Columns && row >= 0 && row < Rows && board[column, row] == player)
        {
            count++;
            column += columnStep;
            row += rowStep;
        }
        return count;
    }

    private ConnectFourCell GetCell(int column, int row)
    {
        int index = row * Columns + column;
        return boardCells != null && index >= 0 && index < boardCells.Length ? boardCells[index] : null;
    }

    private void SetBoardInteractable(bool interactable)
    {
        if (boardCells == null) return;
        foreach (ConnectFourCell cell in boardCells)
            if (cell != null) cell.SetInteractable(interactable);
    }

    private void SetColumnButtonsInteractable(bool interactable)
    {
        if (columnButtons == null) return;
        foreach (ConnectFourColumnButton columnButton in columnButtons)
            if (columnButton != null) columnButton.SetInteractable(interactable);
    }

    private bool HasMissingCells()
    {
        if (boardCells == null || boardCells.Length != Columns * Rows) return true;
        foreach (ConnectFourCell cell in boardCells)
            if (cell == null) return true;
        return false;
    }

    private bool HasMissingColumnButtons()
    {
        if (columnButtons == null || columnButtons.Length != Columns) return true;
        foreach (ConnectFourColumnButton columnButton in columnButtons)
            if (columnButton == null) return true;
        return false;
    }
}