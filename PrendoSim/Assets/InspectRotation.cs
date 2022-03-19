﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InspectRotation : MonoBehaviour
{ 
    [SerializeField]
    float eulerAngX;
    [SerializeField]
    float eulerAngY;
    [SerializeField]
    float eulerAngZ;
 
 
    void Update() {
 
        eulerAngX = transform.localEulerAngles.x;
        eulerAngY = transform.localEulerAngles.y;
        eulerAngZ = transform.localEulerAngles.z;
 
    }
}
