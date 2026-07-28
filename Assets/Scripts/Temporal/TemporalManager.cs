using UnityEngine;

public class TemporalManager : MonoBehaviour
{
    public GameObject echoPrefab;
    private GameObject spawnedEcho;

    void Start()
    {
        var recorder = Object.FindObjectOfType<EchoRecorder>();
        if (recorder != null && echoPrefab != null)
        {
            spawnedEcho = Instantiate(echoPrefab, recorder.transform.position, Quaternion.identity);
            var playback = spawnedEcho.GetComponent<EchoPlayback>();
            if (playback != null)
            {
                playback.recorder = recorder;
            }
        }
        else
        {
            Debug.LogError($"TemporalManager failed to spawn Echo. Recorder found: {recorder != null}, Prefab assigned: {echoPrefab != null}");
        }
    }
}
