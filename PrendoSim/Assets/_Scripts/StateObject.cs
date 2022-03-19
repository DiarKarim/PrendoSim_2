using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RobotGripper
{
    public string GripperName; 
    public int numberOfContacts; 
    public int numberOfJoints;
    public int[] jointsToClose; 
    public int[] jointsToOpen; 
    public float[] fingerTipForce; 
}

[CreateAssetMenu(fileName = "StateObject", menuName = "Robot States")]
public class StateObject : ScriptableObject
{
    public RobotGripper[] Grippers = new RobotGripper[2]; 

    public bool SpawnObjects = false;
    public bool pGripperOpen = false;
    public bool pGripperClose = false; 
    public bool inContact = false;
    public float maxTime = 5f;
    public bool gripperClosed; 
    public int[] fingerTipContacts = new int[3]{0,0,0}; 
    public bool userFriendly; 
    public bool handClosed = false;
    public bool activateGravityMass = false;
    // public int[] stopHandRoutine = new int[3]{0,0,0};  
    public bool stopHandRoutine = false; 
    public float[] contactForce = new float[10]; 
    public int[] stopGripper = new int[10]; // For a maximum of three contact points (i.e. gripper's 5 fingertips)
    public bool closeHand, openHand, stopHand, recordData; 
    public float jointSpeed = 1f; 
    public bool onTable; 

    public void ResetForceParams()
    {
        for(int i = 0; i < contactForce.Length; i++)
        {
            contactForce[i] = 0;
        }
        for(int i = 0; i < stopGripper.Length; i++)
        {
            stopGripper[i] = 0;
        }
    }
}
