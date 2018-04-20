using UnityEngine;
using System.Collections;

// This class is used to fade in and out groups of UI
// elements.  It contains a variety of functions for
// fading in different ways.

// Heavily modified from the original code found at: 
// https://unity3d.com/learn/tutorials/topics/multiplayer-networking/merry-fragmas-30-ui-graphics-and-animations?playlist=29690
[RequireComponent(typeof(CanvasGroup))]
public class UIFader : MonoBehaviour
{
    public float FadeSpeed = 1f;              // The amount the alpha of the UI elements changes per second.
    public CanvasGroup GroupToFade;           // All the groups of UI elements that will fade in and out.

    private bool fadeIn;    // Is the group currently fading in

    void Reset()
    {
        //Attempt to grab the CanvasGroup on this object
        GroupToFade = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        if (fadeIn)
        {
            var fadeAmount = FadeSpeed * Time.deltaTime;

            GroupToFade.alpha = Mathf.Min(1f, GroupToFade.alpha + fadeAmount);

            if (GroupToFade.alpha >= 1)
            {
                fadeIn = false;
            }
        }
        else
        {
            if (GroupToFade.alpha > 0)
            {
                var fadeAmount = FadeSpeed * Time.deltaTime;

                GroupToFade.alpha = Mathf.Max(0f, GroupToFade.alpha - fadeAmount);
            }
        }
    }

    public void Flash()
    {
        fadeIn = true;
    }
}