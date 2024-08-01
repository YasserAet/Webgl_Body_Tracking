using UnityEngine;

public class AvatarSync : MonoBehaviour
{
    public Transform[] avatarBones; // Array to hold references to avatar bones
    public PipeServer pipeServer; // Reference to the PipeServer script

    void Update()
    {
        // Debug information
        if (pipeServer == null || avatarBones.Length == 0)
        {
            Debug.LogError("PipeServer or avatarBones not set up.");
            return;
        }

        // Ensure the number of points matches the number of bones
        if (pipeServer.points.Length != avatarBones.Length)
        {
            Debug.LogError("Mismatch between number of tracked points and avatar bones.");
            return;
        }

        // Update each bone based on the corresponding sphere
        for (int i = 0; i < pipeServer.points.Length; i++)
        {
            // Get the position of the tracked point
            Vector3 trackedPointPosition = pipeServer.points[i].transform.position;

            // Debug the position being applied
            Debug.Log($"Updating bone {i} to position: {trackedPointPosition}");

            // Update the bone position
            avatarBones[i].position = trackedPointPosition;
        }
    }
}
