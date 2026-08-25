using UnityEngine;

public interface IRayInteractable
{
    bool CanShow(Actor actor);
    void OnLookEnter(Actor actor);
    void OnLookExit(Actor actor);
    bool CanInteract(Actor actor);
    void OnInteractServer(Actor actor);
}
