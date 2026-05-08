using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class PlaneManager : MonoBehaviour
{
    private ARPlaneManager arPlaneManager;

    void Start()
    {
        arPlaneManager = GetComponent<ARPlaneManager>();
        arPlaneManager.planesChanged += OnPlanesChanged;
    }

    void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        foreach (var plane in args.added)
        {
            Debug.Log("New plane detected: " + plane.trackableId);
        }

        foreach (var plane in args.updated)
        {
            Debug.Log("Plane updated: " + plane.trackableId);
        }

        foreach (var plane in args.removed)
        {
            Debug.Log("Plane removed: " + plane.trackableId);
        }
    }

    void OnDestroy()
    {
        arPlaneManager.planesChanged -= OnPlanesChanged;
    }
}
