using UnityEngine;

public class ShotEffectsManager : MonoBehaviour {

	[SerializeField] ParticleSystem muzzleFlash;
	[SerializeField] AudioSource gunAudio;
	[SerializeField] GameObject impactPrefab;

	ParticleSystem impactEffect;

	// Create the impact effect
	public void Initialise()
	{
		impactEffect = Instantiate(impactPrefab).GetComponent<ParticleSystem>();
	}

	// Play muzzle flash and audio
	public void PlayShotEffects()
	{
		muzzleFlash.Stop(true);
		muzzleFlash.Play(true);
		gunAudio.Stop();
		gunAudio.Play();
	}

	// Play impact effect at target position
	public void PlayImpactEffect(Vector3 impactPosition, Vector3 normal)
	{
		impactEffect.transform.position = impactPosition;
		impactEffect.transform.LookAt(impactEffect.transform.position + normal);
		impactEffect.Stop();
		impactEffect.Play();
	}
}
