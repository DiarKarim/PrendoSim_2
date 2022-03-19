using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArticulationManager : MonoBehaviour
{
    public int gripperID = 0;
    public StateObject roboState; 
    // ArticulationBody[] articulationBodies; 
    public int numberOfContacts = 3; 

    int sumOfContactPoints = 0; 

    void Start()
    {
        roboState.Grippers[gripperID].numberOfContacts = numberOfContacts; 
    //     articulationBodies = GetComponentsInChildren<ArticulationBody>(); 
    //     for(int i = 0; i<articulationBodies.Length; i++)
    //     {    
    //         artJOintCnt[i] = articulationBodies[i].GetComponent<ArticulationJointController>();
    //     }
    }

    void Update()
    {
        // Make sure this gripper game object deactivates all other grippers when it gets activated itself:
        



    //     for(int i = 0; i<artJOintCnt.Length; i++)
    //     {
    //         // Vector3 angVel = articulationBodies[i].angularVelocity;
    //         sumOfContactPoints+=artJOintCnt[i].inContact; 
    //     }
    //     if(sumOfContactPoints>3)
    //     {
    //         roboState.activateGravityMass = true;
    //     }
    }
}
