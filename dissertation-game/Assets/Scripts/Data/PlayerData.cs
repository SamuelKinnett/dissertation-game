using UnityEngine;

/// <summary>
/// This class stores metadata about the current player, namely their email
/// address and device ID.
/// </summary>
public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance;

    public string Name;
    public string EmailAddress;
    public string DeviceId;

    // Ensure there is only ever one instance of the PlayerData class
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
