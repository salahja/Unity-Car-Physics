using UnityEngine;

using Cinemachine;
 

public class CameraDebugger : MonoBehaviour
{
    void Update()
    {
        var vcam = GetComponent<CinemachineVirtualCamera>();
        if (vcam.Follow == null)
            Debug.LogError("No follow target assigned!");
        else
            Debug.Log($"Camera tracking: {vcam.Follow.position}");
    }
}