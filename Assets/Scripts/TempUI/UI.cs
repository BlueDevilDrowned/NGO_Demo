using UnityEngine;

public class UI : MonoBehaviour
{
    bool state=true;
    public CanvasGroup canvasGroup;
    void Awake()
    {
        state=true;
        canvasGroup.alpha=1;
        canvasGroup.interactable=true;
    }
    public void ChaneState()
    {
        if(state)
        {
            state=false;
            canvasGroup.alpha=0;
            canvasGroup.interactable=false;
        }
        else
        {
            state=true;
            canvasGroup.alpha=1;
            canvasGroup.interactable=true;
        }
    }
}
