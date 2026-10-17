using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectPanelSetup : MonoBehaviour
{
    [Header("Header Image")]
    public Image subTitleImage;
    public Sprite onePlayerTitleSprite;
    public Sprite twoPlayerTitleSprite;

    [Header("Player Inputs")]
    public TMP_InputField player1Input;
    public TMP_InputField player2Input;

    [Header("Color Swap References")]
    public Image p1ColorStar;
    public Image p2ColorStar;
    public Image btnSwapLeftStar;
    public Image btnSwapRightStar;
    public Sprite greenStarSprite;
    public Sprite redStarSprite;

    [Header("First Move Stars")]
    public RectTransform p1TurnStar;
    public RectTransform p2TurnStar;
    public RectTransform randomTurnStar;
    public RectTransform turnHighlight;

    [Header("Difficulty")]
    public GameObject difficultyGroup;
    public Slider difficultySlider;

    private void OnEnable()
    {
        ApplyModeSettings();
        UpdateColorDisplay();
        UpdateFirstMoveUI(animate: false);
    }

    private void ApplyModeSettings()
    {
        bool isOnePlayer = (GameData.selectedMode == GameData.GameMode.OnePlayer);

        if (subTitleImage != null)
        {
            subTitleImage.sprite = isOnePlayer ? onePlayerTitleSprite : twoPlayerTitleSprite;
            subTitleImage.SetNativeSize();
        }

        if (player2Input != null)
        {
            player2Input.text = isOnePlayer ? "Computer" : "Player 2";
            player2Input.interactable = !isOnePlayer;
        }

        if (difficultyGroup != null)
        {
            difficultyGroup.SetActive(isOnePlayer);
        }
    }

    public void OnColorSwapClicked()
    {
        GameData.isP1Green = !GameData.isP1Green;
        UpdateColorDisplay();
    }

    private void UpdateColorDisplay()
    {
        Sprite left = GameData.isP1Green ? greenStarSprite : redStarSprite;
        Sprite right = GameData.isP1Green ? redStarSprite : greenStarSprite;

        if (p1ColorStar != null) p1ColorStar.sprite = left;
        if (p2ColorStar != null) p2ColorStar.sprite = right;
        if (btnSwapLeftStar != null) btnSwapLeftStar.sprite = left;
        if (btnSwapRightStar != null) btnSwapRightStar.sprite = right;
    }

    public void OnCycleFirstMoveClicked()
    {
        GameData.firstMove = (GameData.FirstMoveOption)(((int)GameData.firstMove + 1) % 3);
        UpdateFirstMoveUI(animate: true);
    }

    private void UpdateFirstMoveUI(bool animate)
    {
        ResetTurnStar(p1TurnStar);
        ResetTurnStar(p2TurnStar);
        ResetTurnStar(randomTurnStar);

        RectTransform activeTarget = null;
        switch (GameData.firstMove)
        {
            case GameData.FirstMoveOption.Player1: activeTarget = p1TurnStar; break;
            case GameData.FirstMoveOption.Player2: activeTarget = p2TurnStar; break;
            case GameData.FirstMoveOption.Random: activeTarget = randomTurnStar; break;
        }

        if (activeTarget != null)
        {
            Image img = activeTarget.GetComponent<Image>();
            if (img != null) { Color c = img.color; c.a = 1f; img.color = c; }
            activeTarget.localScale = Vector3.one * 1.3f;

            if (turnHighlight != null)
            {
                turnHighlight.SetParent(activeTarget, false);
                turnHighlight.anchoredPosition = Vector2.zero;
                turnHighlight.localScale = Vector3.one;
                turnHighlight.gameObject.SetActive(true);
            }
        }
    }

    private void ResetTurnStar(RectTransform star)
    {
        if (star == null) return;
        star.localScale = Vector3.one;
        Image img = star.GetComponent<Image>();
        if (img != null) { Color c = img.color; c.a = 0.45f; img.color = c; }
    }

    public void OnDifficultyChanged(float val)
    {
        GameData.difficultyLevel = Mathf.RoundToInt(val);
    }
}