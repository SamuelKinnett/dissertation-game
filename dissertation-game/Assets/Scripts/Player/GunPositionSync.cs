using UnityEngine;
using UnityEngine.Networking;

public class GunPositionSync : NetworkBehaviour
{
	[SerializeField] Transform cameraTransform;
	[SerializeField] Transform gunPivot;
	[SerializeField] float threshold = 10f;
	[SerializeField] float smoothing = 5f;

	[SyncVar] float weaponPitch;

	float lastSyncedPitch;

	// Use this for initialization
	private void Start()
	{
		if (isLocalPlayer) {
			gunPivot.parent = cameraTransform;
		}
	}
	
	// Update is called once per frame
	private void Update()
	{
		if (isLocalPlayer) {
			// Calculate the new weapon pitch and send an update command to the server if the new pitch is greater than the
			// last synced pitch, accounting for the threshold.
			weaponPitch = cameraTransform.localRotation.eulerAngles.x;
			if (Mathf.Abs(lastSyncedPitch - weaponPitch) >= threshold) {
				CmdUpdatePitch(weaponPitch);
				lastSyncedPitch = weaponPitch;
			}
		} else {
			// Update the pitch of the weapon model for the player by interpolating between the current and new rotations.
			Quaternion newRotation = Quaternion.Euler(weaponPitch, 0.0f, 0.0f);

			gunPivot.localRotation = Quaternion.Lerp(gunPivot.localRotation, newRotation, Time.deltaTime * smoothing);
		}
	}

	/// <summary>
	/// Server command to update the weapon pitch
	/// </summary>
	/// <param name="newPitch">New pitch.</param>
	[Command]
	private void CmdUpdatePitch(float newPitch)
	{
		weaponPitch = newPitch;
	}
}
