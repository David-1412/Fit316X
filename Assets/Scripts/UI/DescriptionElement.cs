using UnityEngine;

public class DescriptionElement : MonoBehaviour
{
    public enum DisplayType {
        Title,
        Description,
        CollectableImage,
        LoreContainer,
        Subtitle,
        Body
    }

    public DisplayType type;
}
