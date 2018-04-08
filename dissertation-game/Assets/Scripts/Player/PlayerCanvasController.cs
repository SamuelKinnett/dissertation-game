using System.Collections.Generic;

using Assets.Scripts.Player.Enums;
using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCanvasController : MonoBehaviour
{
    public static PlayerCanvasController Instance;

    [Header("Component References")]
    public Image Crosshair;
    public UIFader DamageImage;
    public Text GameStatusText;
    public Text HealthValue;
    public Text ScoreValue;
    public Text LogText;
    public Text RedTeamLabelText;
    public Text RedTeamTimerText;
    public Text BlueTeamLabelText;
    public Text BlueTeamTimerText;
    public AudioSource DeathAudio;
    public Slider RedTeamCapturePercentageSlider;
    public Slider BlueTeamCapturePercentageSlider;

    public ScoreboardController ScoreboardController;
    public LoadingScreenController LoadingScreenController;

    // Used to force show the scoreboard when the game is over
    public bool ShowScoreboard;

    //Ensure there is only one PlayerCanvasController
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        RedTeamTimerText.text = ConvertTimeToString(GameTimeManager.Instance.RedTeamCaptureTimeRemaining);
        BlueTeamTimerText.text = ConvertTimeToString(GameTimeManager.Instance.BlueTeamCaptureTimeRemaining);

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ScoreboardController.gameObject.SetActive(true);
        }
        else if (Input.GetKeyUp(KeyCode.Tab))
        {
            ScoreboardController.gameObject.SetActive(false);
        }
    }

    public void Initialise()
    {
        Crosshair.enabled = true;
        GameStatusText.text = "";
        RedTeamLabelText.color = StaticColours.RedTeamColour;
        RedTeamTimerText.color = StaticColours.RedTeamColour;
        BlueTeamLabelText.color = StaticColours.BlueTeamColour;
        BlueTeamTimerText.color = StaticColours.BlueTeamColour;
    }

    /// <summary>
    /// Shows or hides the crosshair.
    /// </summary>
    /// <param name="setHidden">If set to <c>true</c> then hide the crosshair.</param>
    public void HideCrosshair(bool setHidden)
    {
        Crosshair.enabled = !setHidden;
    }

    /// <summary>
    /// Flashes the damage effect.
    /// </summary>
    public void FlashDamageEffect()
    {
        DamageImage.Flash();
    }

    /// <summary>
    /// Plays the death audio.
    /// </summary>
    public void PlayDeathAudio()
    {
        if (!DeathAudio.isPlaying)
        {
            DeathAudio.Play();
        }
    }

    /// <summary>
    /// Called when the player is loaded in, destroys the loading screen.
    /// </summary>
    public void PlayerLoaded()
    {
        if (LoadingScreenController != null)
        {
            LoadingScreenController.DestroyLoadingScreen();
        }
    }

    /// <summary>
    /// Sets the displayed score.
    /// </summary>
    /// <param name="amount">The new score value.</param>
    public void SetScore(int amount)
    {
        ScoreValue.text = amount.ToString();
    }

    /// <summary>
    /// Sets the displayed health.
    /// </summary>
    /// <param name="amount">The new health value.</param>
    public void SetHealth(int amount)
    {
        HealthValue.text = amount.ToString();
    }

    public void SetRedTeamPercentage(float percentage)
    {
        RedTeamCapturePercentageSlider.value = percentage;
    }

    public void SetBlueTeamPercentage(float percentage)
    {
        BlueTeamCapturePercentageSlider.value = percentage;
    }

    /// <summary>
    /// Writes the game status text.
    /// </summary>
    /// <param name="textToWrite">The text to write.</param>
    public void WriteGameStatusText(string textToWrite)
    {
        GameStatusText.text = textToWrite;
    }

    /// <summary>
    /// Write text to the log.
    /// </summary>
    /// <param name="textToWrite">Text to write.</param>
    /// <param name="duration">Duration in seconds before the text will be cleared.</param>
    public void WriteLogText(string textToWrite, float duration)
    {
        CancelInvoke();
        LogText.text = textToWrite;
        Invoke("ClearLogText", duration);
    }

    public void AddPlayerToScoreboard(Player player)
    {
        ScoreboardController.AddPlayer(player.PlayerId, player.PlayerTeam);
        ScoreboardController.UpdatePlayerName(player.PlayerId, player.PlayerName);
    }

    public void RemovePlayerFromScoreboard(Player player)
    {
        ScoreboardController.RemovePlayer(player.PlayerId);
    }

    public void UpdatePlayerNameOnScoreboard(Player player)
    {
        ScoreboardController.UpdatePlayerName(player.PlayerId, player.PlayerName);
    }

    public void UpdatePlayerKillsOnScoreboard(Player player, int newKills)
    {
        ScoreboardController.UpdatePlayerKills(player.PlayerId, newKills);
    }

    public void UpdatePlayerDeathsOnScoreboard(Player player)
    {
        ScoreboardController.UpdatePlayerDeaths(player.PlayerId, player.Deaths);
    }

    public void UpdatePlayerTeamOnScoreboard(Player player)
    {
        ScoreboardController.UpdatePlayerTeam(player.PlayerId, player.PlayerTeam);
    }

    public void SetHidePlayerSpecificElements(bool isHidden)
    {
        Crosshair.gameObject.SetActive(!isHidden);
        HealthValue.gameObject.SetActive(!isHidden);
        ScoreValue.gameObject.SetActive(!isHidden);
        LoadingScreenController.gameObject.SetActive(!isHidden);
    }

    /// <summary>
    /// Clears the log text.
    /// </summary>
    private void ClearLogText()
    {
        LogText.text = "";
    }

    private string ConvertTimeToString(float timeInSeconds)
    {
        var minutes = (int)timeInSeconds / 60;
        var seconds = ((int)timeInSeconds % 60).ToString("00");

        return $"{minutes}:{seconds}";
    }
}
