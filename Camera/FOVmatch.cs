using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FOVmatch : MonoBehaviour
{
    [SerializeField] Camera _TargetCam;
    [SerializeField] Camera _ThisCam;

    void Update()
    {
        _ThisCam.fieldOfView = _TargetCam.fieldOfView;
    }
}
