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

    private readonly Dictionary<Guid, RectTransform> pieceViews = new Dictionary<Guid, RectTransform>();

    private void Awake()
    {
        PutBoardImageOnTop();
    }

    private void PutBoardImageOnTop()
    {
        if (boardImage != null)
            boardImage.SetAsLastSibling();
    }

    public void Animate(IReadOnlyList<PieceMovement> movements, Action completed)
    {
        StartCoroutine(AnimateRoutine(movements, completed));
    }

    public void Rebuild(BoardModel model)
    {
        ClearPieces();
        for (int column = 0; column < BoardModel.Columns; column++)
        for (int row = 0; row < BoardModel.Rows; row++)
        {
            GridPosition position = new GridPosition(column, row);
            Piece piece = model.GetPiece(position);
            if (piece == null) continue;
            RectTransform view = GetOrCreateView(piece);
            view.anchoredPosition = GridToAnchoredPosition(position);
        }
    }

    public void ClearPieces()
    {
        foreach (RectTransform view in pieceViews.Values)
            if (view != null) Destroy(view.gameObject);
        pieceViews.Clear();
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
            animation.View.anchoredPosition = animation.To;
        completed?.Invoke();
    }

    private RectTransform GetOrCreateView(Piece piece)
    {
        if (pieceViews.TryGetValue(piece.Id, out RectTransform existing)) return existing;
        RectTransform view = Instantiate(piecePrefab, boardRoot);
        if (boardImage != null)
            boardImage.SetAsLastSibling();
        view.name = $"Piece_{piece.OwnerId}_{piece.Id:N}";
        view.anchorMin = new Vector2(0.5f, 0.5f);
        view.anchorMax = new Vector2(0.5f, 0.5f);
        view.pivot = new Vector2(0.5f, 0.5f);
        view.sizeDelta = piecePrefab.sizeDelta;
        view.localScale = piecePrefab.localScale;
        Image image = view.GetComponentInChildren<Image>();
        if (image != null)
        {
            bool playerOneUsesGreen = GameData.isP1Green;
            bool useGreenSprite = piece.OwnerId == 1 ? playerOneUsesGreen : !playerOneUsesGreen;
            image.sprite = useGreenSprite ? greenStarSprite : redStarSprite;
            image.color = Color.white;
        }
        pieceViews.Add(piece.Id, view);
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
}
