using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] Transform playerTransform;
    [SerializeField] Camera mainCamera;
    public float cameraLerpSpeed = 5f;
    public float cameraOffset = 0.3f;
    public float cameraHeight = 20f;

    void Update()
    {
        Vector3 playerPosition = playerTransform.position;
        playerPosition += new Vector3(-5, 0, -5);

        Vector3 targetPosition = new Vector3(playerPosition.x, cameraHeight, playerPosition.z);

        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, cameraLerpSpeed * Time.deltaTime);

    }
}