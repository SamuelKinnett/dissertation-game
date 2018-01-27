using UnityEngine;
using UnityEngine.Networking;

public class PlayerHealth : NetworkBehaviour
{

	[SerializeField] int maxHealth = 3;

	[SyncVar(hook = "OnHealthChanged")] int health;

	Player player;

	[Server]
	public bool TakeDamage()
	{
		bool died = false;

		if (health <= 0) {
			return died;
		}

		health--;
		died = health <= 0;

		RpcTakeDamage(died);

		return died;
	}

	private void Awake()
	{
		player = GetComponent<Player>();
	}

	[ServerCallback]
	private void OnEnable()
	{
		health = maxHealth;
	}

	[ServerCallback]
	private void Start()
	{
		health = maxHealth;
	}

	[ClientRpc]
	private void RpcTakeDamage(bool died)
	{
		// Add any death effects here
		if (isLocalPlayer) {
			PlayerCanvasController.playerCanvasController.FlashDamageEffect();
		}

		if (died) {
			player.Die();
		}
	}

	/// <summary>
	/// Callback to update the player health locally
	/// </summary>
	/// <param name="newHealthValue">The new health value.</param>
	private void OnHealthChanged(int newHealthValue)
	{
		health = newHealthValue;

		if (isLocalPlayer) {
			PlayerCanvasController.playerCanvasController.SetHealth(newHealthValue);
		}
	}
}
