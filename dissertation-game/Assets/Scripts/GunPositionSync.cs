using UnityEngine;
using UnityEngine.Networking;

public class GunPositionSync : NetworkBehaviour
{

	[SerializeField] Transform cameraTransform;
	// [SerializeField] Transform handMount;
	[SerializeField] Transform gunPivot;
	[SerializeField] float threshold = 10f;
	[SerializeField] float smoothing = 5f;

	[SyncVar] float pitch;
	Vector3 lastOffset;
	float lastSyncedPitch;

	// Use this for initialization
	void Start()
	{
		if (isLocalPlayer) {
			gunPivot.parent = cameraTransform;
		} else {
			// lastOffset = handMount.position - transform.position;
		}
	}
	
	// Update is called once per frame
	void Update()
	{
		if (isLocalPlayer) {
			pitch = cameraTransform.localRotation.eulerAngles.x;
			if (Mathf.Abs(lastSyncedPitch - pitch) >= threshold) {
				CmdUpdatePitch(pitch);
				lastSyncedPitch = pitch;
			}
		} else {
			Quaternion newRotation = Quaternion.Euler(pitch, 0.0f, 0.0f);

			// Vector3 currentOffset = handMount.position - transform.position;
			// gunPivot.localPosition += currentOffset - lastOffset;
			// lastOffset = currentOffset;

			gunPivot.localRotation = Quaternion.Lerp(gunPivot.localRotation, newRotation, Time.deltaTime * smoothing);
		}
	}

	[Command]
	void CmdUpdatePitch(float newPitch)
	{
		pitch = newPitch;
	}
}
