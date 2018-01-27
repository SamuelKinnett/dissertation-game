using UnityEngine;

public class ShotEffectsManager : MonoBehaviour
{
	[SerializeField] ParticleSystem muzzleFlash;
	[SerializeField] AudioSource gunAudio;
	[SerializeField] GameObject impactPrefab;

	ParticleSystem impactEffect;

	/// <summary>
	/// Create the impact effect
	/// </summary>
	public void Initialise()
	{
		impactEffect = Instantiate(impactPrefab).GetComponent<ParticleSystem>();
	}

	/// <summary>
	/// Play muzzle flash and audio
	/// </summary>
	public void PlayShotEffects()
	{
		muzzleFlash.Stop(true);
		muzzleFlash.Play(true);
		gunAudio.Stop();
		gunAudio.Play();
	}

	/// <summary>
	/// Play impact effect at target position
	/// </summary>
	/// <param name="impactPosition">Impact position.</param>
	/// <param name="normal">Normal of the impact.</param>
	public void PlayImpactEffect(Vector3 impactPosition, Vector3 normal)
	{
		impactEffect.transform.position = impactPosition;
		impactEffect.transform.LookAt(impactEffect.transform.position + normal);
		impactEffect.Stop();
		impactEffect.Play();
	}
}
