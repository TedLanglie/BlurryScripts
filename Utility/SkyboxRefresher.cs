using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxRefresher : MonoBehaviour
{
    void Awake()
    {
        DynamicGI.UpdateEnvironment();
    }
}
