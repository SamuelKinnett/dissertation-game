using System;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class QuestionController : MonoBehaviour
{
    public static QuestionController Instance;

    public RectTransform Panel;
    public ToggleGroup Question1;
    public ToggleGroup Question2;
    public ToggleGroup Question3;
    public Button SubmitButton;

    private bool allQuestionsAnswered;

    private Player localPlayer;

    //Ensure there is only one QuestionController
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (Question1.AnyTogglesOn() && Question2.AnyTogglesOn() && Question3.AnyTogglesOn())
        {
            allQuestionsAnswered = true;
            SubmitButton.interactable = true;
        }
    }

    public void SubmitAnswers()
    {
        if (allQuestionsAnswered)
        {
            var answers = string.Empty;

            answers += $"{Question1.ActiveToggles().Single(t => t.isOn).name},";
            answers += $"{Question2.ActiveToggles().Single(t => t.isOn).name},";
            answers += Question3.ActiveToggles().Single(t => t.isOn).name;

            localPlayer.SubmitAnswers(answers);
            Panel.gameObject.SetActive(false);
        }
    }

    public void InitialiseQuestions(Player localPlayer)
    {
        Panel.gameObject.SetActive(true);
        SubmitButton.interactable = false;

        allQuestionsAnswered = false;

        this.localPlayer = localPlayer;
    }
}
