using UnityEngine;
using UnityEngine.UI;

public class PlayerCanvasController : MonoBehaviour
{

	public static PlayerCanvasController playerCanvasController;

	[Header("Component References")]
	[SerializeField] Image crosshair;
	[SerializeField] UIFader damageImage;
	[SerializeField] Text gameStatusText;
	[SerializeField] Text healthValue;
	[SerializeField] Text scoreValue;
	[SerializeField] Text logText;
	[SerializeField] AudioSource deathAudio;

	//Ensure there is only one PlayerCanvasController
	void Awake()
	{
		if (playerCanvasController == null) {
			playerCanvasController = this;
		} else if (playerCanvasController != this) {
			Destroy(gameObject);
		}
	}

	public void Initialise()
	{
		crosshair.enabled = true;
		gameStatusText.text = "";
	}

	/// <summary>
	/// Shows or hides the crosshair.
	/// </summary>
	/// <param name="setHidden">If set to <c>true</c> then hide the crosshair.</param>
	public void HideCrosshair(bool setHidden)
	{
		crosshair.enabled = !setHidden;
	}

	/// <summary>
	/// Flashes the damage effect.
	/// </summary>
	public void FlashDamageEffect()
	{
		damageImage.Flash();
	}

	/// <summary>
	/// Plays the death audio.
	/// </summary>
	public void PlayDeathAudio()
	{
		if (!deathAudio.isPlaying) {
			deathAudio.Play();
		}
	}

	/// <summary>
	/// Sets the displayed score.
	/// </summary>
	/// <param name="amount">The new score value.</param>
	public void SetScore(int amount)
	{
		scoreValue.text = amount.ToString();
	}

	/// <summary>
	/// Sets the displayed health.
	/// </summary>
	/// <param name="amount">The new health value.</param>
	public void SetHealth(int amount)
	{
		healthValue.text = amount.ToString();
	}

	/// <summary>
	/// Writes the game status text.
	/// </summary>
	/// <param name="textToWrite">The text to write.</param>
	public void WriteGameStatusText(string textToWrite)
	{
		gameStatusText.text = textToWrite;
	}

	/// <summary>
	/// Write text to the log.
	/// </summary>
	/// <param name="textToWrite">Text to write.</param>
	/// <param name="duration">Duration in seconds before the text will be cleared.</param>
	public void WriteLogText(string textToWrite, float duration)
	{
		CancelInvoke();
		logText.text = textToWrite;
		Invoke("ClearLogText", duration);
	}

	/// <summary>
	/// Clears the log text.
	/// </summary>
	void ClearLogText()
	{
		logText.text = "";
	}
}
