using UnityEngine;
using UnityEngine.UI;

public class BriefingController : MonoBehaviour
{
    public InputField NameField;
    public InputField EmailField;
    public InputField DeviceIdField;
    public Button ConsentButton;
    public Text BriefingText;

    public string DateUntilAnonymisation;
    public string DateUntilUniDeletion;

    // Use this for initialization
    void Start()
    {
        DontDestroyOnLoad(this);

        ConsentButton.enabled = false;

        PlayerData.Instance.DeviceId = SystemInfo.deviceUniqueIdentifier;
        DeviceIdField.text = PlayerData.Instance.DeviceId;

        // Insert the dates
        BriefingText.text = BriefingText.text
            .Replace("{DateUntilAnonymisation}", DateUntilAnonymisation)
            .Replace("{DateUntilUniDeletion}", DateUntilUniDeletion);
    }

    // Update is called once per frame
    void Update()
    {
        if (EmailField.text.Length > 0 && NameField.text.Length > 0)
        {
            ConsentButton.enabled = true;
        }
        else
        {
            ConsentButton.enabled = false;
        }
    }

    public void CloseBriefing()
    {
        if (EmailField.text.Length > 0 && NameField.text.Length > 0)
        {
            PlayerData.Instance.Name = NameField.text;
            PlayerData.Instance.EmailAddress = EmailField.text;
            gameObject.SetActive(false);
        }
    }
}
