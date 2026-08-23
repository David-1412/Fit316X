using System.Collections.Generic;

public interface IInteractable
{
    void Interact();
    bool CanInteract();
}

public class InteractionOption
{
    public string Name;
    public System.Action OnSelect;
}

public interface IMultiInteractable : IInteractable
{
    List<InteractionOption> GetInteractionOptions();
}