using Assets.Scripts.Player.Enums;
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
    public Text RedTeamTimerText;
    public Text BlueTeamTimeText;
    public AudioSource DeathAudio;
    public Slider RedTeamCapturePercentageSlider;
    public Slider BlueTeamCapturePercentageSlider;

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
        BlueTeamTimeText.text = ConvertTimeToString(GameTimeManager.Instance.BlueTeamCaptureTimeRemaining);
    }

    public void Initialise()
    {
        Crosshair.enabled = true;
        GameStatusText.text = "";
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

    /// <summary>
    /// Clears the log text.
    /// </summary>
    void ClearLogText()
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
