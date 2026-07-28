using UnityEngine;
using Unity.Cinemachine;

public class MapTransition : MonoBehaviour
{
    [SerializeField] PolygonCollider2D mapBoundry;
    [SerializeField] Direction direction;
    [SerializeField] Transform teleportTargetPosition;
    CinemachineConfiner2D confiner;

    enum Direction { Up, Down, Left, Right, Teleport }

    private void Awake()
    {
        confiner = UnityEngine.Object.FindAnyObjectByType<CinemachineConfiner2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            FadeTransition(collision.gameObject);

            MapController_Manual.Instance?.HighlightArea(mapBoundry.name);
            MapController_Dynamic.Instance?.UpdateCurrentArea(mapBoundry.name);
            MinimapHUD.Instance?.UpdateCurrentArea(mapBoundry.name);
        }
    }

    async void FadeTransition(GameObject player)
    {
        PauseController.SetPause(true);

        await ScreenFader.Instance.FadeOut();

        confiner.BoundingShape2D = mapBoundry;
        confiner.InvalidateBoundingShapeCache();
        UpdatePlayerPosition(player);

        await ScreenFader.Instance.FadeIn();

        PauseController.SetPause(false);
    }

    void UpdatePlayerPosition(GameObject player)
    {
        if(direction == Direction.Teleport)
        {
            player.transform.position = teleportTargetPosition.position;

            return;
        }

        Vector3 additivePos = player.transform.position;

        switch (direction)
        {
            case Direction.Up:
                additivePos.y += 2;
                break;
            case Direction.Down:
                additivePos.y += -2;
                break;
            case Direction.Left:
                additivePos.x += -2;
                break;
            case Direction.Right:
                additivePos.x += 2;
                break;
        }

        player.transform.position = additivePos;
    }
}
