using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenController : MonoBehaviour
{
    public Image background;
    public Text text;
    public float lerpAmount;

    private bool loaded;

    private void Awake()
    {
        loaded = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (loaded)
        {
            background.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Lerp(background.color.a, 0.0f, lerpAmount));
            text.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Lerp(text.color.a, 0.0f, lerpAmount));
            if (background.color.a <= 0.05f)
            {
                Destroy(gameObject);
            }
        }
    }

    public void DestroyLoadingScreen()
    {
        loaded = true;
    }
}
