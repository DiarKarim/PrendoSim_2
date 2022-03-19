
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArticulationProxyController : MonoBehaviour
{
    public string producedForce;
    public float currentDigitForce;
    public AnimationCurve JointMotionCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));

    public Transform driver;

    public float gripLimit;
    public float offset;
    public Vector3 rotationAxis = new Vector3(1, 0, 0);

    // public float rotationSpeed = 1f;
    public bool invert;
    public bool actuate;
    public bool computeForce;
    public int uniqueJointID = 0; // used in roboState.contactForce array variable 
    public int inContact = 0;
    public int gripperID = 0; 
    public int forceTipID = 0;

    StateObject roboState;
    ExperimentState expState;
    Vector3 initRot;
    ArticulationBody articulation;
    Coroutine closeRoutine, openRoutine, driverRoutine;

    float[] minmaxResult = new float[2];
    float minVal, maxVal;
    float prevMinVal = 10f;
    float prevMaxVal = 0f;
    float driverRotState; // This makes "driver" transform obsolete and computes contact forces
    float rotationalGoal = 0f;
    float minRot, maxRot, currRot;
    float sumContactForce = 0f;
    float rotVal = 0f;

    int indx = 0;
    bool allRoutinesStopped;
    float jointMoveStepSize = 0.1f;

    void Start()
    {
        roboState = Resources.Load<StateObject>("StateObject");
        expState = Resources.Load<ExperimentState>("ExperimentState");

        articulation = GetComponent<ArticulationBody>();
        minRot = articulation.xDrive.lowerLimit;
        maxRot = articulation.xDrive.upperLimit;

        jointMoveStepSize = (0.085f * maxRot)/100f * roboState.jointSpeed;

        roboState.ResetForceParams();
    }

    // map what ever the value is that comes in from the driver to the joint limits of the articulation body 
    void Update()
    {
        // Function to monitor and check grip force limit is reached, then stop moving the gripper
        MonitorContactForce();

        if(computeForce)
        {
            roboState.Grippers[gripperID].fingerTipForce[forceTipID] = currentDigitForce; 
        }

        if (actuate)
        {
            // This sets the "rotationalGoal" inside another coroutine (see CloseGripperCurve)
            JointOpenCloser();
        }
        if (invert)
        {
            RotateTo((-1 * (rotationalGoal + offset))); // * rotationSpeed
        }
        else
        {
            RotateTo((rotationalGoal + offset)); // * rotationSpeed
        }
    }

    void JointOpenCloser()
    {
        if (roboState.Grippers[gripperID].jointsToClose[uniqueJointID] == 1 | Input.GetKeyDown(KeyCode.K))
        {
            roboState.ResetForceParams();
            currRot = articulation.xDrive.target;
            if (closeRoutine != null)
            { StopCoroutine(closeRoutine); }
            closeRoutine = StartCoroutine(CloseGripperCurve(currRot, minRot, maxRot));
            roboState.Grippers[gripperID].jointsToClose[uniqueJointID] = 0;  
        }
        if (roboState.Grippers[gripperID].jointsToOpen[uniqueJointID] == 1 | Input.GetKeyDown(KeyCode.L))
        {
            // Reset stop gripper condition
            allRoutinesStopped = false;
            roboState.ResetForceParams();
            currRot = articulation.xDrive.target;
            if (openRoutine != null)
            {StopCoroutine(openRoutine);}
            openRoutine = StartCoroutine(OpenGripperCurve(currRot, minRot, maxRot));
            roboState.Grippers[gripperID].jointsToOpen[uniqueJointID] = 0; 
        }
    }

    void MonitorContactForce()
    {
        currentDigitForce = roboState.contactForce[uniqueJointID];
        sumContactForce = 0;
        foreach (int fs in roboState.contactForce)
        {
            sumContactForce += fs;
        }
        producedForce = currentDigitForce.ToString("F2") + "\t" +
                        sumContactForce.ToString("F2");
        int contactsMade = 0;
        foreach (int conti in roboState.stopGripper)
        {
            contactsMade += conti;
        }
        if (contactsMade >= 3 & !allRoutinesStopped) // #*** change this to number of desired contact fingertips i.e. 3 with BarrettHand
        {
            if (computeForce)
            {
                if (driverRoutine != null)
                { StopCoroutine(driverRoutine); }
                currRot = articulation.xDrive.target;
                driverRoutine = StartCoroutine(DriverForce(currRot, maxRot));
            }

            if (sumContactForce >= gripLimit)
            {
                StopAllCoroutines(); // <== Not sure I like this approach. 
                allRoutinesStopped = true;
            }
        }

    }

    IEnumerator DriverForce(float driverState, float maxLim)
    {
        while (driverState < maxLim + 10f | roboState.contactForce[uniqueJointID] < gripLimit)
        {
            driverState += jointMoveStepSize;
            // Debug.Log(driverState.ToString("F1"));

            roboState.contactForce[uniqueJointID] = ForceCompute(driverState);
            yield return null;
        }
    }

    IEnumerator CloseGripperCurve(float currState, float minLim, float maxLim)
    {
        //JointMotionCurve;
        if (currState < maxLim)
        {
            while (currState < maxLim) // & sumContactForce < gripLimit
            {
                currState += jointMoveStepSize;
                rotationalGoal = currState;
                yield return null;
            }
        }
        // *********************************************************************************************
        // Inlcude some sort of force measurements in this i.e. close until a certain force is reached
        // by including another float that increases until a certain proxy discrepancy is produced
        // *********************************************************************************************
    }

    IEnumerator OpenGripperCurve(float currState, float minLim, float maxLim)
    {
        while (currState >= minLim) // & inContact != 1
        {
            currState -= jointMoveStepSize;
            rotationalGoal = currState;
            yield return null;
        }
    }

    float ForceCompute(float driverState)
    {
        float damping = 0.25f;
        float stiffness = 20f;
        float force = 0;

        float artRot = articulation.xDrive.target;

        return force = (stiffness * Mathf.Abs((driverState - artRot) -
                                    (damping * articulation.angularVelocity.sqrMagnitude)));
    }

    IEnumerator CloseGripperCurve(float currState, float openLim, float closedLim, bool inv)
    {
        //JointMotionCurve;
        if (inv)
        {
            while (currState >= openLim & inContact != 1)
            {
                currState -= 0.1f;
                rotationalGoal = currState;
                // Vector3 rot = new Vector3(transform.localEulerAngles.x + currState, transform.localEulerAngles.y, transform.localEulerAngles.z);
                // transform.rotation = Quaternion.Euler(rot);
                yield return null;
            }
        }
        else
        {
            while (currState < closedLim & inContact != 1)
            {
                currState += 0.1f;
                rotationalGoal = currState;
                // Vector3 rot = new Vector3(transform.localEulerAngles.x + currState, transform.localEulerAngles.y, transform.localEulerAngles.z);
                // transform.rotation = Quaternion.Euler(rot);
                yield return null;
            }
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "targetObject")
        {
            inContact = 1;
            if (computeForce)
                roboState.stopGripper[uniqueJointID] = 1;
            // Debug.Log("Finger contacted object !!! " + gameObject.name);
        }
    }
    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.tag == "targetObject")
        {
            inContact = 0;

            if (computeForce)
                roboState.stopGripper[uniqueJointID] = 0;
            // Debug.Log("Finger contacted object !!! " + gameObject.name);
        }
    }

    void MinMaxVal(float inpV, int idx)
    {
        if (inpV < prevMinVal)
        {
            prevMinVal = inpV;
        }
        if (inpV > prevMaxVal)
        {
            prevMaxVal = inpV;
        }

        minmaxResult[0] = prevMinVal;
        minmaxResult[1] = prevMaxVal;
    }
    void RotateTo(float angularRotationTarget)
    {
        var drive = articulation.xDrive;
        drive.target = angularRotationTarget;
        articulation.xDrive = drive;
    }
}















// if(debugging)
// {
// // Debug.Log(driver.localEulerAngles.x + "\t" + 
// //           expState.Clamp0360(driver.localEulerAngles.x)); 
// MinMaxVal(driver.localEulerAngles.x, indx);
// // 1. get diff from  360 and restric raw val by it's max so that it doesn't jump
// float tval = 360f - (driver.localEulerAngles.x % minmaxResult[1]); 
// float val = minmaxResult[1] - driver.localEulerAngles.x; 
// Debug.Log("Val: " + tval); 
// rotationalGoal = val;
// }

// if(rotationAxis.x != 0)
// {
//     MinMaxVal(driver.localEulerAngles.x, indx);
//     rotationalGoal = driver.localEulerAngles.x.Map(minmaxResult[0], minmaxResult[1], minRot, maxRot);
//     // Debug.Log(driver.localEulerAngles.x);
// }
// else if(rotationAxis.y != 0)
// {
//     MinMaxVal(driver.localEulerAngles.y, indx);
//     rotationalGoal = driver.localEulerAngles.y.Map(minmaxResult[0], minmaxResult[1], minRot, maxRot);
//     // Debug.Log(driver.localEulerAngles.y);
// }
// else if(rotationAxis.z != 0)
// {
//     MinMaxVal(driver.localEulerAngles.z, indx);
//     rotationalGoal = driver.localEulerAngles.z.Map(minmaxResult[0], minmaxResult[1], minRot, maxRot);
//     // Debug.Log(driver.localEulerAngles.z);
// }
// indx++; 
// if(indx>99)
// {
//     indx = 0; 
// }