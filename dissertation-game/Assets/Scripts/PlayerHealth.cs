using UnityEngine;
using UnityEngine.Networking;

public class PlayerHealth : NetworkBehaviour {

	[SerializeField] int maxHealth = 3;

	Player player;
	int health;

	[Server]
	public bool TakeDamage()
	{
		bool died = false;

		if (health <= 0) {
			return died;
		}

		health--;
		died = health <= 0;

		return died;
	}

	private void Awake()
	{

	}

	[ServerCallback]
	private void OnEnable()
	{
		health = maxHealth;
	}

	[ClientRpc]
	void RpcTakeDamage(bool died)
	{
		// Add any death effects here

		if (died) {
			player.Die();
		}
	}
}
