using UnityEngine;
using UnityEngine.Networking;

public class PlayerShooting : NetworkBehaviour
{
	[SerializeField] int scoreToWin = 5;
	[SerializeField] Transform firePosition;
	[SerializeField] ShotEffectsManager shotEffectsManager;
	[SerializeField] Weapon currentWeapon;

	[SyncVar(hook = "OnScoreChanged")] int score;

	Player player;
	bool canShoot;

	private void Start()
	{
		player = GetComponent<Player>();
		shotEffectsManager.Initialise();

		if (isLocalPlayer) {
			canShoot = true;
		}
	}

	/// <summary>
	/// Raises the enable event.
	/// </summary>
	[ServerCallback]
	private void OnEnable()
	{
		score = 0;
	}

	private void Update()
	{
		if (canShoot) {
			if (Input.GetButtonDown("Fire1") && currentWeapon.CanFire()) {
				currentWeapon.Fire();
				CmdFireShot(firePosition.position, firePosition.forward);
			}
		}
	}

	/// <summary>
	/// Server command to fire a shot
	/// </summary>
	/// <param name="origin">Origin of the shot.</param>
	/// <param name="direction">Direction of the shot.</param>
	[Command]
	private void CmdFireShot(Vector3 origin, Vector3 direction)
	{
		RaycastHit hit;

		Ray ray = new Ray(origin, direction);
		Debug.DrawRay(ray.origin, ray.direction * 30f, Color.red, 1f);

		bool result = Physics.Raycast(ray, out hit, 50f);

		if (result) {
			PlayerHealth enemy = hit.transform.GetComponent<PlayerHealth>();

			if (enemy != null) {
				if (enemy.TakeDamage(currentWeapon.Damage)) {
					// We've killed an enemy
					if (++score >= scoreToWin) {
						player.Won();
					}
				}
			}
		}

		RpcProcessShotEffects(result, hit.point, hit.normal);
	}

	[ClientRpc]
	private void RpcProcessShotEffects(bool result, Vector3 point, Vector3 normal)
	{
		shotEffectsManager.PlayShotEffects();

		if (isLocalPlayer) {
			// Currently only play the animations on the player's side
			currentWeapon.PlayShotEffects();
		}

		if (result) {
			shotEffectsManager.PlayImpactEffect(point, normal);
		}
	}

	/// <summary>
	/// Callback to update the player score locally
	/// </summary>
	/// <param name="newScore">The new score value.</param>
	private void OnScoreChanged(int newScore)
	{
		score = newScore;

		if (isLocalPlayer) {
			PlayerCanvasController.playerCanvasController.SetScore(newScore);
		}
	}
}
