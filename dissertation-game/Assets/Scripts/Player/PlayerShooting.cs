using UnityEngine;
using UnityEngine.Networking;

public class PlayerShooting : NetworkBehaviour
{
	[SerializeField] float shotCooldown = 0.3f;
	[SerializeField] Transform firePosition;
	[SerializeField] ShotEffectsManager shotEffectsManager;

	[SyncVar(hook = "OnScoreChanged")] int score;

	float elapsedTime;
	bool canShoot;

	private void Start()
	{
		shotEffectsManager.Initialise();

		if (isLocalPlayer) {
			canShoot = true;
		}
	}

	[ServerCallback]
	private void OnEnable()
	{
		score = 0;
	}

	private void Update()
	{
		if (!canShoot) {
			return;
		}

		elapsedTime += Time.deltaTime;

		if (Input.GetButtonDown("Fire1") && elapsedTime > shotCooldown) {
			elapsedTime = 0.0f;
			CmdFireShot(firePosition.position, firePosition.forward);
		}
	}

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
				if (enemy.TakeDamage()) {
					// We've killed an enemy
					score++;
				}
			}
		}

		RpcProcessShotEffects(result, hit.point, hit.normal);
	}

	[ClientRpc]
	private void RpcProcessShotEffects(bool result, Vector3 point, Vector3 normal)
	{
		shotEffectsManager.PlayShotEffects();

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
