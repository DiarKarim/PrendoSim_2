using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableScript : MonoBehaviour
{
    StateObject roboState; 

    void Start()
    {
        roboState = Resources.Load<StateObject>("StateObject");
        roboState.onTable = false; 
    }

    private void OnCollisionEnter(Collision other) 
    {
        roboState.onTable = true; 
    }
    private void OnCollisionExit(Collision other) 
    {
        roboState.onTable = false;     
    }

}
