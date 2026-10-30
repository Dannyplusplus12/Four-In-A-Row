using UnityEngine;

public sealed class OptionsUIController : MonoBehaviour
{
    private const string SoundEnabledKey = "Options.SoundEnabled";
    private const string MaterialKey = "Options.Material";

    public void OnSoundChanged(bool enabled)
    {
        PlayerPrefs.SetInt(SoundEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        AudioListener.volume = enabled ? 1f : 0f;
    }

    public void OnMaterialChanged(int materialIndex)
    {
        PlayerPrefs.SetInt(MaterialKey, materialIndex);
        PlayerPrefs.Save();
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