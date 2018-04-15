using UnityEngine;
using UnityEngine.UI;

public class BriefingController : MonoBehaviour
{
    public InputField NameField;
    public InputField EmailField;
    public InputField DeviceIdField;
    public Scrollbar BriefingScrollbar;
    public Button ConsentButton;
    public Text BriefingText;

    public string DateUntilAnonymisation;
    public string DateUntilDeletion;

    private bool briefingRead;

    // Use this for initialization
    void Start()
    {
        DontDestroyOnLoad(this);

        ConsentButton.interactable = false;
        briefingRead = false;
        BriefingScrollbar.onValueChanged.AddListener(ScrollbarUpdated);

        PlayerData.Instance.DeviceId = SystemInfo.deviceUniqueIdentifier;
        DeviceIdField.text = PlayerData.Instance.DeviceId;

        // Insert the dates
        BriefingText.text = BriefingText.text
            .Replace("{DateUntilAnonymisation}", DateUntilAnonymisation)
            .Replace("{DateUntilDeletion}", DateUntilDeletion);
    }

    // Update is called once per frame
    void Update()
    {
        if (EmailField.text.Length > 0 && NameField.text.Length > 0 && briefingRead)
        {
            ConsentButton.interactable = true;
        }
        else
        {
            ConsentButton.interactable = false;
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

    public void ScrollbarUpdated(float newValue)
    {
        if (newValue <= 0)
        {
            briefingRead = true;
        }
    }
}
