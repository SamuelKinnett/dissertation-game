using UnityEngine;
using System.Collections;

public class Pistol : Weapon
{
	[SerializeField] Animator gunAnimator;

	private bool isPlayingFiringAnimation;
	private float elapsedTime;

	public override void Fire()
	{
		WeaponState = WeaponState.Cooldown;
	}

	public override void PlayShotEffects()
	{
		gunAnimator.SetBool("SlideOpen", true);
		isPlayingFiringAnimation = true;
	}

	public override bool CanFire()
	{
		// The timing code should ideally be in a SyncVar to prevent hacking of the fire rate,
		// but this isn't presently an issue given the academic nature of the program and the
		// environment in which it will be tested.

		// Code for ammo could go here
		return WeaponState == WeaponState.Ready;
	}

	// Use this for initialization
	private void Start()
	{
		Damage = 1;
		Cooldown = 0.3f;
		WeaponType = WeaponType.Hitscan;
		WeaponState = WeaponState.Ready;
	}
	
	// Update is called once per frame
	private void Update()
	{
		if (isPlayingFiringAnimation) {
			if (gunAnimator.GetCurrentAnimatorStateInfo(0).IsName("Slide_Open")) {
				// Spawn shell
				gunAnimator.SetBool("SlideOpen", false);
				isPlayingFiringAnimation = false;
			}
		}

		// If the weapon is currently "cooling down"
		if (WeaponState == WeaponState.Cooldown) {
			elapsedTime += Time.deltaTime;

			if (elapsedTime >= Cooldown) {
				WeaponState = WeaponState.Ready;
				elapsedTime = 0.0f;
			}
		}
	}
}
