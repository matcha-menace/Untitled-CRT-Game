using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Compass : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CameraSwitcher cameraSwitcher;
    private float targetRotationZ;
    [SerializeField] private float rotationSpeed;
    
    void Update()
    {
        targetRotationZ = playerController.currentCameraDirection switch
        {
            CameraDirection.North => 270,
            CameraDirection.NorthEast => 225,
            CameraDirection.East => 180,
            CameraDirection.SouthEast => 135,
            CameraDirection.South => 90,
            CameraDirection.SouthWest => 45,
            CameraDirection.West => 0,
            CameraDirection.NorthWest => 315,
        };

        if (!cameraSwitcher.isDefault)
        {
            targetRotationZ = 270;
        }
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(-90, 0, targetRotationZ), rotationSpeed * Time.deltaTime);
    }
}
