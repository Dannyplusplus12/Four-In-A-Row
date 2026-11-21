using System;
using System.Collections;
using System.Collections.Generic;
using ConnectFour.Core;
using UnityEngine;
using UnityEngine.UI;

public sealed class BoardView : MonoBehaviour
{
    [SerializeField] private RectTransform boardRoot;
    [SerializeField] private RectTransform boardImage;
    [SerializeField] private RectTransform piecePrefab;
    [SerializeField] private Sprite greenStarSprite;
    [SerializeField] private Sprite redStarSprite;
    [SerializeField] private float cellWidth = 96f;
    [SerializeField] private float cellHeight = 96f;
    [SerializeField] private Vector2 gridOffset;
    [SerializeField] private float gravity = 1800f;
    [SerializeField] private float minimumDuration = 0.12f;
    [SerializeField] private float bounceHeight = 8f;
    [SerializeField] private float ruleEffectDuration = 0.35f;

    public Sprite GreenStarSprite => greenStarSprite;
    public Sprite RedStarSprite => redStarSprite;

    private readonly Dictionary<Guid, RectTransform> pieceViews = new Dictionary<Guid, RectTransform>();
    private GameController cachedController;
    private Coroutine winHighlightCoroutine;
    private Sprite defaultBoardSprite;

    private static readonly string[] BoardMaterialResourcePaths =
    {
        null, // 0: Plastic (default board.png assigned on BoardImage)
        "Boards/board_wood",
        "Boards/board_metal",
        "Boards/board_marble"
    };

    private void Awake()
    {
        cachedController = FindFirstObjectByType<GameController>();
        if (boardImage != null)
        {
            Image frameImage = boardImage.GetComponent<Image>();
            if (frameImage != null) defaultBoardSprite = frameImage.sprite;
        }
        ApplySelectedBoardMaterial();
        PutBoardImageOnTop();
    }

    public void ApplySelectedBoardMaterial()
    {
        if (boardImage == null) return;
        Image frameImage = boardImage.GetComponent<Image>();
        if (frameImage == null) return;

        int materialIndex = PlayerPrefs.GetInt(SoundManager.MaterialKey, 0);
        if (materialIndex > 0 && materialIndex < BoardMaterialResourcePaths.Length)
        {
            Sprite matSprite = Resources.Load<Sprite>(BoardMaterialResourcePaths[materialIndex]);
            if (matSprite != null)
            {
                frameImage.sprite = matSprite;
                return;
            }
        }
        if (defaultBoardSprite != null)
            frameImage.sprite = defaultBoardSprite;
    }

    private void PutBoardImageOnTop()
    {
        if (boardImage == null) return;
        boardImage.SetAsLastSibling();
        // Ensure the board frame never blocks raycasts to pieces in DestroyStarOnTap mode.
        Image frameImage = boardImage.GetComponent<Image>();
        if (frameImage != null)
            frameImage.raycastTarget = false;
    }

    public void StopAnimations()
    {
        StopAllCoroutines();
        winHighlightCoroutine = null;
    }

    public void Animate(IReadOnlyList<PieceMovement> movements, Action completed)
    {
        StartCoroutine(AnimateRoutine(movements, completed));
    }

    public void Rebuild(BoardModel model)
    {
        ClearPieces();
        if (model == null) return;
        for (int column = 0; column < BoardModel.Columns; column++)
        for (int row = 0; row < BoardModel.Rows; row++)
        {
            GridPosition position = new GridPosition(column, row);
            Piece piece = model.GetPiece(position);
            if (piece == null) continue;
            RectTransform view = GetOrCreateView(piece);
            if (view != null)
                view.anchoredPosition = GridToAnchoredPosition(position);
        }
    }

    public void ClearPieces()
    {
        if (winHighlightCoroutine != null)
        {
            StopCoroutine(winHighlightCoroutine);
            winHighlightCoroutine = null;
        }
        foreach (RectTransform view in pieceViews.Values)
            if (view != null) Destroy(view.gameObject);
        pieceViews.Clear();
    }

    public void SetRotationQuarterTurns(int quarterTurns)
    {
        if (boardRoot != null)
            boardRoot.localRotation = Quaternion.Euler(0f, 0f, quarterTurns * 90f);
    }

    public void HighlightWinningPieces(IReadOnlyList<GridPosition> positions, BoardModel model)
    {
        if (positions == null || positions.Count == 0 || model == null) return;
        if (winHighlightCoroutine != null)
            StopCoroutine(winHighlightCoroutine);

        HashSet<Guid> winningIds = new HashSet<Guid>();
        List<RectTransform> views = new List<RectTransform>(positions.Count);
        foreach (GridPosition pos in positions)
        {
            Piece piece = model.GetPiece(pos);
            if (piece != null && pieceViews.TryGetValue(piece.Id, out RectTransform view) && view != null)
            {
                winningIds.Add(piece.Id);
                views.Add(view);
            }
        }

        // Dim non-winning pieces slightly so the winning line stands out immediately
        foreach (KeyValuePair<Guid, RectTransform> kv in pieceViews)
        {
            if (kv.Value == null || winningIds.Contains(kv.Key)) continue;
            Image img = kv.Value.GetComponentInChildren<Image>();
            if (img != null)
            {
                Color c = img.color;
                c.a = 0.45f;
                img.color = c;
            }
        }

        // Bring winning stars above the board frame so their pop animation isn't clipped
        for (int i = 0; i < views.Count; i++)
            views[i].SetAsLastSibling();

        if (views.Count > 0)
            winHighlightCoroutine = StartCoroutine(PulseWinningViewsRoutine(views));
    }

    private IEnumerator PulseWinningViewsRoutine(List<RectTransform> views)
    {
        Vector3 baseScale = piecePrefab != null ? piecePrefab.localScale : Vector3.one;

        // 1. Staggered punchy pop-in across the 4/5 winning stars
        const float stagger = 0.08f;
        const float popTime = 0.24f;
        float introDuration = stagger * (views.Count - 1) + popTime;
        float elapsed = 0f;
        while (elapsed < introDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            for (int i = 0; i < views.Count; i++)
            {
                if (views[i] == null) continue;
                float localT = Mathf.Clamp01((elapsed - i * stagger) / popTime);
                if (localT <= 0f) continue;
                // Elastic overshoot: 1.0 -> 1.44 -> 1.24
                float scaleMult = localT < 0.55f
                    ? Mathf.Lerp(1f, 1.44f, Mathf.SmoothStep(0f, 1f, localT / 0.55f))
                    : Mathf.Lerp(1.44f, 1.24f, Mathf.SmoothStep(0f, 1f, (localT - 0.55f) / 0.45f));
                views[i].localScale = baseScale * scaleMult;
                views[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(localT * Mathf.PI) * 10f * (i % 2 == 0 ? 1f : -1f));
            }
            yield return null;
        }

        // 2. Continuous celebratory bounce & subtle wiggle
        elapsed = 0f;
        while (true)
        {
            elapsed += Time.unscaledDeltaTime;
            for (int i = 0; i < views.Count; i++)
            {
                if (views[i] == null) continue;
                float phase = elapsed * 6.5f - i * 0.45f;
                float pulse = 1.20f + 0.14f * Mathf.Abs(Mathf.Sin(phase));
                float angle = Mathf.Sin(phase) * 7f;
                views[i].localScale = baseScale * pulse;
                views[i].localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            yield return null;
        }
    }

    public void AnimateRuleEffects(TurnResult result, Action completed)
    {
        StartCoroutine(AnimateRuleEffectsRoutine(result, completed));
    }

    private IEnumerator AnimateRuleEffectsRoutine(TurnResult result, Action completed)
    {
        if (result == null)
        {
            completed?.Invoke();
            yield break;
        }

        if (result.RequiresRotation)
            SoundManager.PlayRotate();
        if (result.RemovedPieces.Count > 0)
            SoundManager.PlayDestroy();

        List<AnimationData> movements = new List<AnimationData>(result.Movements.Count);
        foreach (PieceMovement movement in result.Movements)
        {
            if (!pieceViews.TryGetValue(movement.Piece.Id, out RectTransform view))
                view = GetOrCreateView(movement.Piece);
            if (view == null) continue;
            Vector2 from = GridToAnchoredPosition(movement.From);
            Vector2 to = GridToAnchoredPosition(movement.To);
            view.anchoredPosition = from;
            movements.Add(new AnimationData(view, from, to, Vector2.Distance(from, to)));
        }

        List<FadeData> removals = new List<FadeData>(result.RemovedPieces.Count);
        foreach (Piece piece in result.RemovedPieces)
        {
            if (!pieceViews.TryGetValue(piece.Id, out RectTransform view) || view == null) continue;
            CanvasGroup canvasGroup = view.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = view.gameObject.AddComponent<CanvasGroup>();
            removals.Add(new FadeData(piece, view, canvasGroup));
        }

        Quaternion startRotation = boardRoot == null ? Quaternion.identity : boardRoot.localRotation;
        Quaternion targetRotation = result.RequiresRotation
            ? Quaternion.Euler(0f, 0f, result.RotationQuarterTurns * 90f)
            : startRotation;
        float elapsed = 0f;
        while (elapsed < ruleEffectDuration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, ruleEffectDuration));
            float eased = normalized * normalized * (3f - 2f * normalized);
            for (int index = 0; index < movements.Count; index++)
            {
                AnimationData movement = movements[index];
                if (movement.View != null)
                    movement.View.anchoredPosition = Vector2.Lerp(movement.From, movement.To, eased);
            }
            for (int index = 0; index < removals.Count; index++)
            {
                FadeData removal = removals[index];
                if (removal.View != null)
                {
                    removal.CanvasGroup.alpha = 1f - eased;
                    removal.View.localScale = Vector3.one * (1f - eased * 0.25f);
                }
            }
            if (boardRoot != null)
                boardRoot.localRotation = Quaternion.Slerp(startRotation, targetRotation, eased);
            yield return null;
        }

        for (int index = 0; index < movements.Count; index++)
        {
            if (movements[index].View != null)
                movements[index].View.anchoredPosition = movements[index].To;
        }

        for (int index = 0; index < removals.Count; index++)
        {
            FadeData removal = removals[index];
            pieceViews.Remove(removal.Piece.Id);
            if (removal.View != null) Destroy(removal.View.gameObject);
        }
        completed?.Invoke();
    }

    private IEnumerator AnimateRoutine(IReadOnlyList<PieceMovement> movements, Action completed)
    {
        if (movements == null || movements.Count == 0)
        {
            completed?.Invoke();
            yield break;
        }

        List<AnimationData> animations = new List<AnimationData>(movements.Count);
        float duration = minimumDuration;
        foreach (PieceMovement movement in movements)
        {
            RectTransform view = GetOrCreateView(movement.Piece);
            if (view == null) continue;
            Vector2 from = GridToAnchoredPosition(movement.From);
            Vector2 to = GridToAnchoredPosition(movement.To);
            view.anchoredPosition = from;
            float distance = Vector2.Distance(from, to);
            duration = Mathf.Max(duration, Mathf.Sqrt(2f * distance / Mathf.Max(1f, gravity)));
            animations.Add(new AnimationData(view, from, to, distance));
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            foreach (AnimationData animation in animations)
            {
                if (animation.View == null) continue;
                float fallProgress = animation.IsDrop ? Mathf.Clamp01(normalized / 0.82f) : normalized;
                float eased = fallProgress * fallProgress;
                Vector2 position = Vector2.LerpUnclamped(animation.From, animation.To, eased);
                if (animation.IsDrop && normalized > 0.82f)
                {
                    float bounceProgress = (normalized - 0.82f) / 0.18f;
                    float bounceAmount = Mathf.Sin(bounceProgress * Mathf.PI * 2f) * bounceHeight * (1f - bounceProgress);
                    position = animation.To + animation.BounceDirection * bounceAmount;
                }
                animation.View.anchoredPosition = position;
            }
            yield return null;
        }

        foreach (AnimationData animation in animations)
            if (animation.View != null)
                animation.View.anchoredPosition = animation.To;
        SoundManager.PlayDrop();
        completed?.Invoke();
    }

    private RectTransform GetOrCreateView(Piece piece)
    {
        if (piece == null) return null;
        if (pieceViews.TryGetValue(piece.Id, out RectTransform existing) && existing != null) return existing;
        if (piecePrefab == null || boardRoot == null) return null;

        RectTransform view = Instantiate(piecePrefab, boardRoot);
        PutBoardImageOnTop();
        view.name = $"Piece_{piece.OwnerId}_{piece.Id:N}";
        view.anchorMin = new Vector2(0.5f, 0.5f);
        view.anchorMax = new Vector2(0.5f, 0.5f);
        view.pivot = new Vector2(0.5f, 0.5f);
        view.sizeDelta = piecePrefab.sizeDelta;
        bool isBlock = piece.Type == PieceType.Block;
        view.localScale = isBlock ? piecePrefab.localScale * 0.88f : piecePrefab.localScale;
        Image image = view.GetComponentInChildren<Image>();
        if (image != null)
        {
            bool playerOneUsesGreen = GameData.isP1Green;
            bool useGreenSprite = piece.OwnerId == 1 ? playerOneUsesGreen : !playerOneUsesGreen;
            image.sprite = isBlock ? greenStarSprite : useGreenSprite ? greenStarSprite : redStarSprite;
            image.color = isBlock ? new Color(0.22f, 0.24f, 0.29f, 1f) : Color.white;

            if (!isBlock)
            {
                if (cachedController == null) cachedController = FindFirstObjectByType<GameController>();
                BoardPieceInput input = image.gameObject.GetComponent<BoardPieceInput>();
                if (input == null) input = image.gameObject.AddComponent<BoardPieceInput>();
                input.Initialize(piece, cachedController);
                image.raycastTarget = true;
            }
            else
            {
                image.raycastTarget = false;
            }
        }
        pieceViews[piece.Id] = view;
        return view;
    }

    private Vector2 GridToAnchoredPosition(GridPosition position)
    {
        float x = (position.Column - (BoardModel.Columns - 1) * 0.5f) * cellWidth;
        float y = (position.Row - (BoardModel.Rows - 1) * 0.5f) * cellHeight;
        return new Vector2(x, y) + gridOffset;
    }

    private sealed class AnimationData
    {
        public readonly RectTransform View;
        public readonly Vector2 From;
        public readonly Vector2 To;
        public readonly float Distance;
        public readonly bool IsDrop;
        public readonly Vector2 BounceDirection;

        public AnimationData(RectTransform view, Vector2 from, Vector2 to, float distance)
        {
            View = view;
            From = from;
            To = to;
            Distance = distance;
            IsDrop = from != to;
            BounceDirection = IsDrop ? -(to - from).normalized : Vector2.zero;
        }
    }

    private sealed class FadeData
    {
        public readonly Piece Piece;
        public readonly RectTransform View;
        public readonly CanvasGroup CanvasGroup;

        public FadeData(Piece piece, RectTransform view, CanvasGroup canvasGroup)
        {
            Piece = piece;
            View = view;
            CanvasGroup = canvasGroup;
        }
    }
}
