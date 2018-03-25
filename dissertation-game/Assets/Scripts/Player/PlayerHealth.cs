using UnityEngine;
using UnityEngine.Networking;

public class PlayerHealth : NetworkBehaviour
{
	[SerializeField] int maxHealth = 3;

	[SyncVar(hook = "OnHealthChanged")] int health;

	Player player;

	/// <summary>
	/// Cause this instance to take damage.
	/// </summary>
	/// <returns><c>true</c> if this instance died from this damage, <c>false</c> otherwise.</returns>
	/// <param name="damage">The damage to deal this instance</param>
	[Server]
	public bool TakeDamage(int damage = 1)
	{
		if (health > 0) {
			health -= damage;

            if (health <= 0)
            {
                RpcTakeDamage(true);
                player.Die();   // Make sure to kill the player object on the server
                return true;
            }
            else
            {
                RpcTakeDamage(false);
                return false;
            }
		}

		// The instance is dead, but this damage didn't cause the death, so we return false.
		return false;
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

	/// <summary>
	/// Remote procedure call to apply damage/death effects on the client.
	/// </summary>
	/// <param name="died">If set to <c>true</c> died.</param>
	[ClientRpc]
	private void RpcTakeDamage(bool died)
	{
		// Add any death effects here
		if (isLocalPlayer) {
			PlayerCanvasController.Instance.FlashDamageEffect();
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
			PlayerCanvasController.Instance.SetHealth(newHealthValue);
		}
	}
}
