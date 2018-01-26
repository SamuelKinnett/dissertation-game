using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : MonoBehaviour
{
	public Animator gunAnimator;

	private bool isFiring;

	public void Update()
	{
		if (isFiring) {
			if (gunAnimator.GetCurrentAnimatorStateInfo(0).IsName("Slide_Open")) {
				// Spawn shell
				gunAnimator.SetBool("SlideOpen", false);
				isFiring = false;
			}
		}
	}

	public void Fire()
	{
		gunAnimator.SetBool("SlideOpen", true);
		isFiring = true;
	}
}
