using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class OptionsUIController : MonoBehaviour
{
    private const string SoundEnabledKey = SoundManager.SoundEnabledKey;
    private const string MaterialKey = SoundManager.MaterialKey;

    private static readonly string[] MaterialNames = { "Plastic", "Wood", "Metal", "Marble" };
    private static readonly string[] PreviewResourcePaths =
    {
        null,
        "Boards/board_wood",
        "Boards/board_metal",
        "Boards/board_marble"
    };

    [SerializeField] private Toggle soundToggle;
    [SerializeField] private Image speakerIcon;
    [SerializeField] private TMP_Dropdown materialDropdown;
    [SerializeField] private Image materialPreviewImage;
    [SerializeField] private Sprite defaultPlasticBoardSprite;

    private void Awake()
    {
        ResolveControls();
        ApplySavedSoundState();
    }

    private void OnEnable()
    {
        ResolveControls();
        ApplySavedSoundState();
        SyncUI();
    }

    private void ResolveControls()
    {
        if (speakerIcon == null)
        {
            Transform speaker = transform.Find("SpeakerIcon");
            if (speaker != null) speakerIcon = speaker.GetComponent<Image>();
        }
        if (materialDropdown == null)
        {
            Transform dd = transform.Find("MaterialDropdown");
            if (dd != null) materialDropdown = dd.GetComponent<TMP_Dropdown>();
        }
        if (materialPreviewImage == null)
        {
            Transform preview = transform.Find("MaterialPreview");
            if (preview != null) materialPreviewImage = preview.GetComponent<Image>();
        }
        if (defaultPlasticBoardSprite == null && materialPreviewImage != null)
            defaultPlasticBoardSprite = materialPreviewImage.sprite;
    }

    private void SyncUI()
    {
        bool soundOn = GetSavedSoundState();
        if (soundToggle != null)
            soundToggle.SetIsOnWithoutNotify(soundOn);
        UpdateSpeakerVisual(soundOn);

        int savedMat = Mathf.Clamp(GetSavedMaterialIndex(), 0, MaterialNames.Length - 1);
        if (materialDropdown != null)
        {
            materialDropdown.ClearOptions();
            materialDropdown.AddOptions(new List<string>(MaterialNames));
            materialDropdown.SetValueWithoutNotify(savedMat);
            materialDropdown.RefreshShownValue();
        }
        UpdateMaterialPreview(savedMat);
    }

    private void UpdateMaterialPreview(int materialIndex)
    {
        if (materialPreviewImage == null) return;
        if (materialIndex > 0 && materialIndex < PreviewResourcePaths.Length)
        {
            Sprite loaded = Resources.Load<Sprite>(PreviewResourcePaths[materialIndex]);
            if (loaded != null)
            {
                materialPreviewImage.sprite = loaded;
                return;
            }
        }
        if (defaultPlasticBoardSprite != null)
            materialPreviewImage.sprite = defaultPlasticBoardSprite;
    }

    private void UpdateSpeakerVisual(bool enabled)
    {
        if (speakerIcon == null) return;
        speakerIcon.color = enabled ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.45f);
    }

    public void ApplySavedSoundState()
    {
        AudioListener.volume = GetSavedSoundState() ? 1f : 0f;
    }

    public void OnSoundChanged(bool enabled)
    {
        SoundManager.SetSoundEnabled(enabled);
        UpdateSpeakerVisual(enabled);
        if (enabled) SoundManager.PlayClick();
    }

    public void ToggleSound()
    {
        OnSoundChanged(!GetSavedSoundState());
    }

    public void OnMaterialChanged(int materialIndex)
    {
        int clamped = Mathf.Clamp(materialIndex, 0, MaterialNames.Length - 1);
        PlayerPrefs.SetInt(MaterialKey, clamped);
        PlayerPrefs.Save();
        if (materialDropdown != null && materialDropdown.value != clamped)
        {
            materialDropdown.SetValueWithoutNotify(clamped);
            materialDropdown.RefreshShownValue();
        }
        UpdateMaterialPreview(clamped);
        SoundManager.PlayClick();
    }

    public void NextMaterial()
    {
        int next = (GetSavedMaterialIndex() + 1) % MaterialNames.Length;
        OnMaterialChanged(next);
    }

    public void PreviousMaterial()
    {
        int prev = (GetSavedMaterialIndex() - 1 + MaterialNames.Length) % MaterialNames.Length;
        OnMaterialChanged(prev);
    }

    public bool GetSavedSoundState()
    {
        return PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;
    }

    public int GetSavedMaterialIndex()
    {
        return PlayerPrefs.GetInt(MaterialKey, 0);
    }

    public void BackToMainMenu()
    {
        MenuUIManager menu = FindFirstObjectByType<MenuUIManager>();
        if (menu != null)
            menu.ShowMainMenu();
    }
}
