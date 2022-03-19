using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationScriptv1 : MonoBehaviour
{


    public float openState, closedState, currRot;
    public float dispRot;
    public bool inverse; 

    int inContact = 0;

    private void Start()
    {
        currRot = openState;
    }

    // void Update()
    // {
    //     dispRot = transform.localEulerAngles.x;

    //     if (Input.GetKeyDown(KeyCode.K))
    //     {
    //         // currRot = transform.localEulerAngles.x; 
    //         StartCoroutine(CloseGripperCurve(currRot, openState, closedState, inverse));
    //     }
    // }

    IEnumerator CloseGripperCurve(float currState, float openLim, float closedLim, bool inv)
    {
        //JointMotionCurve;
        if (inv)
        {
            while (currState >= openLim & inContact != 1)
            {
                currState -= 0.1f;
                Vector3 rot = new Vector3(transform.localEulerAngles.x + currState, transform.localEulerAngles.y, transform.localEulerAngles.z);
                transform.rotation = Quaternion.Euler(rot);
                yield return null;
            }
        }
        else
        {
            while (currState < closedLim & inContact != 1)
            {
                currState += 0.1f;
                Vector3 rot = new Vector3(transform.localEulerAngles.x + currState, transform.localEulerAngles.y, transform.localEulerAngles.z);
                transform.rotation = Quaternion.Euler(rot);
                yield return null;
            }
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        inContact = 1;
    }

    private void OnTriggerExit(Collider other)
    {
        inContact = 0;
    }

}
