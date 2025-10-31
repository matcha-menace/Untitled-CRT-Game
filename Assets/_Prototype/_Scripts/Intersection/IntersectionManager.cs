using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntersectionManager : MonoBehaviour
{
    [Header("North")]
    [SerializeField] private GameObject northContactOne;
    [SerializeField] private GameObject northContactTwo;

    public void OnToggleContact(CameraDirection direction, bool isOne)
    {
        var directionsCount = (int)CameraDirection.Count;
        var opposite = Helpers.Wrap((int)CameraDirection.North - 4, directionsCount - 1);
        
        if ((int)direction == opposite)
        {
            northContactOne.SetActive(isOne);
            northContactTwo.SetActive(!isOne);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Services.PlayerController.GetComponent<GuidanceBallBehavior>().SetBallColor(Color.red);
            Services.PlayerController.isInIntersection = true;
            Services.PlayerController.activeIntersectionManager = this;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Services.PlayerController.GetComponent<GuidanceBallBehavior>().ResetBallColor();
            Services.PlayerController.isInIntersection = false;
            Services.PlayerController.activeIntersectionManager = null;
        }
    }
}
