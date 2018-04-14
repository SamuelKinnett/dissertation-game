using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Assets.Scripts.Environment.Enums;

/// <summary>
/// This class is used to store game metadata in a way that can be easily
/// accessed by multiple classes.
/// </summary>
public class GameInstanceData : MonoBehaviour
{
    public static GameInstanceData Instance;

    public int RedTeamId;
    public int BlueTeamId;
    public GameType GameType;

    // Ensure there is only one GameInstanceData
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
