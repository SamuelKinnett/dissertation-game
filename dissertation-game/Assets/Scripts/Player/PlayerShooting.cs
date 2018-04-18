using System;

using UnityEngine;
using UnityEngine.Networking;

using Newtonsoft.Json;

using Assets.Scripts.Extensions;

public class PlayerShooting : NetworkBehaviour
{
    public Transform FirePosition;
    public ShotEffectsManager ShotEffectsManager;
    public Weapon CurrentWeapon;

    [SyncVar(hook = "OnKillsChanged")]
    private int kills;

    private Player player;
    private bool canShoot;

    private void Start()
    {
        player = GetComponent<Player>();
        ShotEffectsManager.Initialise();

        if (isLocalPlayer)
        {
            canShoot = true;
        }
    }

    private void Update()
    {
        if (canShoot)
        {
            if (Input.GetButtonDown("Fire1") && CurrentWeapon.CanFire())
            {
                CurrentWeapon.Fire();
                CmdFireShot(FirePosition.position, FirePosition.forward);
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

        bool result = Physics.Raycast(ray, out hit, 50f);

        if (result)
        {
            PlayerHealth enemyHealth = hit.transform.GetComponent<PlayerHealth>();

            if (enemyHealth != null)
            {
                Player enemyPlayer = hit.transform.GetComponent<Player>();

                // Log the shot to the database
                int shotId =
                    DatabaseManager.Instance.AddNewShot(
                        player.PlayerId,
                        JsonConvert.SerializeObject(origin.ToTuple()),
                        JsonConvert.SerializeObject(direction.ToTuple()),
                        enemyPlayer.PlayerId,
                        JsonConvert.SerializeObject(enemyPlayer.transform.position.ToTuple()));

                // Only damage enemies, not team-mates
                if (enemyPlayer.PlayerTeam != player.PlayerTeam)
                {
                    if (enemyHealth.TakeDamage(CurrentWeapon.Damage))
                    {
                        // We've killed an enemy
                        // Log the kill to the database
                        DatabaseManager.Instance.AddKill(
                            player.PlayerId,
                            enemyPlayer.PlayerId,
                            shotId);

                        ++kills;
                    }
                }
            }
        }
        else
        {
            // Log the shot to the database
            DatabaseManager.Instance.AddNewShot(
                player.PlayerId,
                JsonConvert.SerializeObject(origin.ToTuple()),
                JsonConvert.SerializeObject(direction.ToTuple()));
        }

        RpcProcessShotEffects(result, hit.point, hit.normal);
    }

    [ClientRpc]
    private void RpcProcessShotEffects(bool result, Vector3 point, Vector3 normal)
    {
        ShotEffectsManager.PlayShotEffects();

        if (isLocalPlayer)
        {
            // Currently only play the animations on the player's side
            CurrentWeapon.PlayShotEffects();
        }

        if (result)
        {
            ShotEffectsManager.PlayImpactEffect(point, normal);
        }
    }

    /// <summary>
    /// Callback to update the player score locally
    /// </summary>
    /// <param name="newScore">The new score value.</param>
    private void OnKillsChanged(int newScore)
    {
        kills = newScore;

        PlayerCanvasController.Instance.UpdatePlayerKillsOnScoreboard(player, kills);

        if (isLocalPlayer)
        {
            PlayerCanvasController.Instance.SetScore(newScore);
        }
    }
}
