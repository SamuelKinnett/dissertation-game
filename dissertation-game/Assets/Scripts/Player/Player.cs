using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

[System.Serializable]
public class ToggleEvent : UnityEvent<bool>
{

}

public class Player : NetworkBehaviour
{
	[SyncVar(hook = "OnNameChanged")] public string PlayerName;
	[SyncVar(hook = "OnColourChanged")] public Color PlayerColour;

	[SerializeField] ToggleEvent onToggleShared;
	[SerializeField] ToggleEvent onToggleLocal;
	[SerializeField] ToggleEvent onToggleRemote;
	[SerializeField] float respawnTime = 5.0f;
	[SerializeField] Text playerNameText;

	GameObject mainCamera;

	public void Die()
	{
		if (isLocalPlayer) {
			PlayerCanvasController.playerCanvasController.WriteGameStatusText("You Died!");
			PlayerCanvasController.playerCanvasController.PlayDeathAudio();
		}

		DisablePlayer();

		Invoke("Respawn", respawnTime);
	}

	private void Start()
	{
		mainCamera = Camera.main.gameObject;

		EnablePlayer();
	}

	private void EnablePlayer()
	{
		if (isLocalPlayer) {
			PlayerCanvasController.playerCanvasController.Initialise();
			mainCamera.SetActive(false);
		}

		onToggleShared.Invoke(true);

		if (isLocalPlayer) {
			onToggleLocal.Invoke(true);
		} else {
			onToggleRemote.Invoke(true);
		}
	}

	private void DisablePlayer()
	{
		if (isLocalPlayer) {
			PlayerCanvasController.playerCanvasController.HideCrosshair(true);
			mainCamera.SetActive(true);
		}

		onToggleShared.Invoke(false);

		if (isLocalPlayer) {
			onToggleLocal.Invoke(false);
		} else {
			onToggleRemote.Invoke(false);
		}
	}

	private void Respawn()
	{
		if (isLocalPlayer) {
			Transform spawn = NetworkManager.singleton.GetStartPosition();
			transform.position = spawn.position;
			transform.rotation = spawn.rotation;
		}

		EnablePlayer();
	}

	private void OnNameChanged(string value)
	{
		PlayerName = value;
		gameObject.name = PlayerName;
		playerNameText.text = PlayerName;
	}

	public void OnColourChanged(Color value)
	{
		PlayerColour = value;
		// Change colour of player model here
	}
}
