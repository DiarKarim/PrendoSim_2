using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Formerly "RobotHandController"
public class DriverController_v2  : MonoBehaviour 
{
    // Instead of animators, we should use coroutines. 
    // This will avoid a lot of the rotation issues I 
    // have been experiencing, which is a result of quaternion 
    //to euler conversion done under the hood by unity between 
    // inspector values (set in the animation) and the actual euler angles of the objects
    
    [Header("   Press 'G' button to start one trial!!!")]
    public Transform indexProx; 
    public Transform ringProx;
    public Transform ProxyGripper;
    public GameObject[] TargetObjects;
    public Vector3 ObjectOffset;
    public float graspWaitTime = 1f; 
    public float jointSpeed = 0.65f; 
    
    GameObject targObj_prefab; 
    Coroutine graspSimCoroutine;
    Coroutine[] closeSim = new Coroutine[6]; 
    bool gripperAnimDone = false; 
    float closeAnimDuration = 1f; 

    private void Start() {
        // myAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.G))
        {
                if(graspSimCoroutine != null)
                {StopCoroutine(graspSimCoroutine);}
                graspSimCoroutine = StartCoroutine(GraspController());
        }
    }

    // *** This whole function is a pile of crap. Needs to be improved. <== Done
    void CloseGripper()
    {
        int idx = 0; 
        foreach(Coroutine closeR in closeSim)
        {
            if(closeR != null)
            {StopCoroutine(closeR);}

            // myAnimator[idx].enabled = true;
            idx++; 
        }

        // idx = 0; 
        // foreach(Animator myAnim in myAnimator)
        // {
        //     float currentFloat = myAnim.GetFloat("closeTime");
        //     closeSim[idx] = StartCoroutine(AnimateGripper(myAnim, currentFloat, closeAnimDuration, jointSpeed));
        //     idx++; 
        // }
        // Debug.Log("Close time: " + currentFloat.ToString("F2"));
    }

    // *** This whole function is a pile of crap. Needs to be improved. <== Done
    void OpenGripper()
    {
        int idx = 0; 
        foreach(Coroutine closeR in closeSim)
        {
            if(closeR != null)
            {StopCoroutine(closeR);}

            // myAnimator[idx].enabled = true;
            idx++; 
        }

        idx = 0; 
        // foreach(Animator myAnim in myAnimator)
        // {
        //     float currentFloat = myAnim.GetFloat("closeTime");
        //     closeSim[idx] = StartCoroutine(AnimateGripper(myAnim, currentFloat, closeAnimDuration, jointSpeed/1.5f));
        //     idx++; 
        // }
        // graspSimCoroutine = StartCoroutine(AnimateGripper(myAnim, currentFloat, closeAnimDuration, jointSpeed/2f));
    }

    // Spawns objects and commands the gripper to close to test whether the grip is stable
    IEnumerator GraspController()
    {
        SpawnObjects();

        // Create random index and ring finger prox joint angles and assing them to the joints 
        float prox_rand = Random.Range(90f, 269);
        indexProx.localEulerAngles = new Vector3(prox_rand, 90f, -90f);
        ringProx.localEulerAngles = new Vector3(prox_rand, -90f, 90f);

        // float RingProx_rand = Random.Range(5f, -90f);
        // indexProx.localEulerAngles = new Vector3(prox_rand, indexProx.localEulerAngles.y, indexProx.localEulerAngles.z);
        // ringProx.localEulerAngles = new Vector3(-1 *prox_rand, ringProx.localEulerAngles.y, ringProx.localEulerAngles.z);
        
        // Animate hand to close 
        CloseGripper();

        // while(myAnimator.GetCurrentAnimatorStateInfo(0).IsName("closeAnim"))
        // while(myAnimator[0].IsInTransition(0))
        //     yield return null; 
        while(!gripperAnimDone)
            yield return null;  
        gripperAnimDone = false; 

        // After 2 seconds (time for animation to complete), activate target object gravity to see if the grasp holds 
        // (if the objects falls to the table then the grasp is not successfull) 
        // Invoke("ActivateGravity", 2f);
        Invoke("ActivateGravity",graspWaitTime); 
        yield return new WaitForSeconds(graspWaitTime + 2f);

        // Animate hand to open
        OpenGripper();
        while(!gripperAnimDone)
            yield return null;  
    }

    void ActivateGravity()
    {
        Rigidbody rb = targObj_prefab.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; 
        rb.useGravity = true;
        rb.drag = 0.05f;
        rb.angularDrag = 0.05f;
    }

    void SpawnObjects()
    {
            // Spawn random object at a random orientation but set to around the position of the robot gripper 
        int objectIndex = Random.Range(0, TargetObjects.Length);
        Quaternion randRotation = Random.rotation;
        targObj_prefab = Instantiate(TargetObjects[objectIndex], ProxyGripper.position + ObjectOffset, randRotation);
    }

    IEnumerator AnimateGripper(Animator animator, float graspStart, float duration, float speedFactor)
    {
        float startTime = Time.time; 
        float elapsedTime = graspStart; 

        // I don't like this condition (Think of something more robust for detecting whether the gripper should close or open)
        if(elapsedTime<duration/2) // Either go from 0 to end of animation time or ...
        {
            while(elapsedTime<duration)
            {
                animator.SetFloat("closeTime", (elapsedTime * speedFactor)); // * Time.fixedDeltaTime
                elapsedTime = Time.time - startTime; 
                yield return null; 
            }
            gripperAnimDone = true; 
        }
        else // ... or reverse to time 0 of the animation clip 
        {
            while(elapsedTime>=0f)
            {
                animator.SetFloat("closeTime", (elapsedTime * speedFactor)); //  * Time.fixedDeltaTime
                elapsedTime -= Time.time - startTime; 
                yield return null; 
            }
            gripperAnimDone = true; 
        }
    }
}