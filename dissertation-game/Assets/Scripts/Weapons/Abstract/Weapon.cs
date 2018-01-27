using UnityEngine;

/// <summary>
/// Abstract weapon class to enable multiple weapons and switching between them during gameplay
/// </summary>
public abstract class Weapon : MonoBehaviour
{
	private int damage;
	private float cooldown;
	private WeaponType weaponType;
	private WeaponState weaponState;

	public int Damage {
		get {
			return damage;
		}
		set {
			damage = value;
		}
	}

	public float Cooldown {
		get {
			return cooldown;
		}
		set {
			cooldown = value;
		}
	}

	public WeaponType WeaponType {
		get {
			return weaponType;
		}
		set {
			weaponType = value;
		}
	}

	public WeaponState WeaponState {
		get {
			return weaponState;
		}
		set {
			weaponState = value;
		}
	}

	/// <summary>
	/// This method should be called when the weapon is fired, and will update the cooldown and other such variables
	/// </summary>
	public virtual void Fire()
	{
	}

	/// <summary>
	/// Plaies the shot effects for this weapon, e.g. animations
	/// </summary>
	public virtual void PlayShotEffects()
	{
	}

	/// <summary>
	/// Determines whether this weapon can fire.
	/// </summary>
	/// <returns><c>true</c> if this weapon can fire; otherwise, <c>false</c>.</returns>
	public virtual bool CanFire()
	{
		return false;
	}
}
