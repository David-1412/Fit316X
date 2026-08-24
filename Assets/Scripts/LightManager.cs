using System.Collections.Generic;
using UnityEngine;

public class LightManager : MonoBehaviour
{
    [Header("Setup")]
    public Transform mainCrystalSource; // Drag your crystal here
    public Material beamMaterial; // Used to color the light
    public float beamWidth = 0.1f;
    public float maxBeamLength = 50f;

    // We need a pool of LineRenderers so we can draw multiple branching beams
    private List<LineRenderer> beamRenderers = new List<LineRenderer>();

    // A struct is a small data container to track a beam currently traveling
    private struct BeamPath
    {
        public Vector2 startPoint;
        public Vector2 direction;
        public int bounceCount;
    }

    void Update()
    {
        DrawAllBeams();
    }

    void DrawAllBeams()
    {
        // 1. Hide all existing beams first
        foreach (var lr in beamRenderers)
        {
            lr.gameObject.SetActive(false);
        }

        int activeBeamCount = 0;

        // 2. Start the queue with the very first beam from the main crystal
        Queue<BeamPath> beamsToProcess = new Queue<BeamPath>();
        beamsToProcess.Enqueue(new BeamPath
        {
            startPoint = mainCrystalSource.position,
            direction = mainCrystalSource.up,
            bounceCount = 0
        });

        // 3. Process every beam in the queue until none are left
        while (beamsToProcess.Count > 0)
        {
            // Take the next beam out of the queue
            BeamPath currentBeam = beamsToProcess.Dequeue();

            // Safety check so infinite loops don't crash Unity
            if (currentBeam.bounceCount > 50) continue;

            // Shoot the raycast. We add a tiny offset so it doesn't hit the object it's currently inside!
            RaycastHit2D hit = Physics2D.Raycast(currentBeam.startPoint + (currentBeam.direction * 0.05f), currentBeam.direction, maxBeamLength);

            // Get a LineRenderer ready to draw this segment
            LineRenderer lr = GetAvailableLineRenderer(activeBeamCount);
            lr.SetPosition(0, currentBeam.startPoint);
            activeBeamCount++;

            if (hit.collider != null)
            {
                // Draw the line to the hit point
                lr.SetPosition(1, hit.point);

                // Did we hit a Relay Node?
                RelayNode node = hit.collider.GetComponent<RelayNode>();

                if (node != null)
                {
                    // Ask the node if the light hit an "open hole"
                    if (node.CanAcceptLight(hit.normal))
                    {
                        // It accepted! Get all the new directions it spits out
                        List<Vector2> nextDirections = node.GetOutputDirections(hit.normal);

                        // For every new direction, add a new beam to our queue to process
                        foreach (Vector2 dir in nextDirections)
                        {
                            beamsToProcess.Enqueue(new BeamPath
                            {
                                startPoint = hit.point,
                                direction = dir,
                                bounceCount = currentBeam.bounceCount + 1
                            });
                        }
                    }
                }
                else if (hit.collider.CompareTag("Target"))
                {
                    // Hit the altar! 
                    Debug.Log("Light has reached the altar!");
                }
            }
            else
            {
                // Hit nothing, shoot off into the void
                lr.SetPosition(1, currentBeam.startPoint + (currentBeam.direction * maxBeamLength));
            }
        }
    }

    // Helper function to create or reuse LineRenderers (keeps performance fast)
    LineRenderer GetAvailableLineRenderer(int index)
    {
        // If we don't have enough renderers, make a new one
        if (index >= beamRenderers.Count)
        {
            GameObject beamObj = new GameObject("Beam_" + index);
            beamObj.transform.parent = this.transform; // Keep hierarchy clean

            LineRenderer newLr = beamObj.AddComponent<LineRenderer>();
            newLr.positionCount = 2;
            newLr.startWidth = beamWidth;
            newLr.endWidth = beamWidth;

            if (beamMaterial != null) newLr.material = beamMaterial;

            beamRenderers.Add(newLr);
        }

        beamRenderers[index].gameObject.SetActive(true);
        return beamRenderers[index];
    }
}