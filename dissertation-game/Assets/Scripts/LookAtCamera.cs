using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{

	Transform mainCameraPosition;

	// Use this for initialization
	void Start()
	{
		mainCameraPosition = Camera.main.transform;
	}
	
	// Update after all other updates have completed
	void LateUpdate()
	{
		if (mainCameraPosition == null) {
			return;
		}

		transform.rotation = Quaternion.LookRotation(transform.position - mainCameraPosition.position);
	}
}
