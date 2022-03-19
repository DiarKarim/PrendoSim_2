using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotHandController : MonoBehaviour
{
    // Instead of animators, we should use coroutines. 
    // This will avoid a lot of the rotation issues I 
    // have been experiencing, which is a result of quaternion 
    //to euler conversion done under the hood by unity between 
    // inspector values (set in the animation) and the actual euler angles of the objects
    Animator myAnimator;

    [Header("   Press 'M' button to close and 'N' to open gripper!!!")]
    public Transform indexProx, ringProx;
    public Transform Gripper;
    public GameObject[] TargetObjects;
    public Vector3 ObjectOffset;
    public float graspWaitTime = 1f; 
    public float jointSpeed = 0.65f; 

    GameObject targObj_prefab; 
    Coroutine graspSimCoroutine; 

    private void Start() {
        myAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.C))
        {
            myAnimator.SetBool("CloseHand", true);
            myAnimator.SetBool("OpenHand", false);
            myAnimator.SetBool("IdleHand", false);
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            myAnimator.SetBool("OpenHand", true);
            myAnimator.SetBool("CloseHand", false);
            myAnimator.SetBool("IdleHand", false);
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            myAnimator.SetBool("IdleHand", true);
            myAnimator.SetBool("OpenHand", false);
            myAnimator.SetBool("CloseHand", false);
        }

        if(Input.GetKeyDown(KeyCode.M))
        {
            myAnimator.enabled = true;
            if(graspSimCoroutine != null)
                StopCoroutine(graspSimCoroutine); 

            float currentFloat = myAnimator.GetFloat("closeTime");
            Debug.Log("Close time: " + currentFloat.ToString("F2"));
            graspSimCoroutine = StartCoroutine(AnimateGripper(myAnimator, currentFloat, 2f, jointSpeed));
        }
        if(Input.GetKeyDown(KeyCode.N))
        {
            myAnimator.enabled = true;
            if(graspSimCoroutine != null)
                StopCoroutine(graspSimCoroutine); 

            float currentFloat = myAnimator.GetFloat("closeTime");
            Debug.Log("Open time: " + currentFloat.ToString("F2"));
            graspSimCoroutine = StartCoroutine(AnimateGripper(myAnimator, currentFloat, 2f, jointSpeed/2f));
        }
        if(Input.GetKeyDown(KeyCode.V)) // If a certain total grasp force threshold is exceeded then stop closing the gripper 
        {
            if(graspSimCoroutine != null)
                StopCoroutine(graspSimCoroutine); 

            if(myAnimator.enabled)
                myAnimator.enabled = false;
            else
                myAnimator.enabled = true; 
        }

        if(Input.GetKeyDown(KeyCode.G))
        {
            if(graspSimCoroutine != null)
                StopCoroutine(graspSimCoroutine); 
           graspSimCoroutine = StartCoroutine(GraspController());
        }
    }

    void CloseGripper()
    {
        myAnimator.enabled = true;
        if(graspSimCoroutine != null)
            StopCoroutine(graspSimCoroutine); 

        float currentFloat = myAnimator.GetFloat("closeTime");
        Debug.Log("Close time: " + currentFloat.ToString("F2"));
        graspSimCoroutine = StartCoroutine(AnimateGripper(myAnimator, currentFloat, 2f, jointSpeed));
    }
    void OpenGripper()
    {
        myAnimator.enabled = true;
        if(graspSimCoroutine != null)
            StopCoroutine(graspSimCoroutine); 

        float currentFloat = myAnimator.GetFloat("closeTime");
        Debug.Log("Open time: " + currentFloat.ToString("F2"));
        graspSimCoroutine = StartCoroutine(AnimateGripper(myAnimator, currentFloat, 2f, jointSpeed/2f));
    }

    // Spawns objects and commands the gripper to close to test whether the grip is stable
    IEnumerator GraspController()
    {
        SpawnObjects();

        // Create random index and ring finger prox joint angles and assing them to the joints 
        float prox_rand = Random.Range(1f, 179);
        // float RingProx_rand = Random.Range(5f, -90f);
        indexProx.localEulerAngles = new Vector3(prox_rand, indexProx.localEulerAngles.y, indexProx.localEulerAngles.z);
        ringProx.localEulerAngles = new Vector3(-1 *prox_rand, ringProx.localEulerAngles.y, ringProx.localEulerAngles.z);
        
        // Animate hand to close 
        CloseGripper();
        // myAnimator.SetBool("CloseHand", true);
        // myAnimator.SetBool("OpenHand", false);
        // myAnimator.SetBool("IdleHand", false);

        // while(myAnimator.GetCurrentAnimatorStateInfo(0).IsName("closeAnim"))
        while(myAnimator.IsInTransition(0))
            yield return null; 

        // After 2 seconds (time for animation to complete), activate target object gravity to see if the grasp holds 
        // (if the objects falls to the table then the grasp is not successfull) 
        // Invoke("ActivateGravity", 2f);
        Invoke("ActivateGravity",graspWaitTime); 
        yield return new WaitForSeconds(graspWaitTime + 2f);

        // Animate hand to open
        OpenGripper();
        // myAnimator.SetBool("CloseHand", false);
        // myAnimator.SetBool("OpenHand", true);
        // myAnimator.SetBool("IdleHand", false);

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
        targObj_prefab = Instantiate(TargetObjects[objectIndex], Gripper.position + ObjectOffset, randRotation);
    }


    IEnumerator AnimateGripper(Animator animator, float graspStart, float duration, float speedFactor)
    {
        float startTime = Time.time; 
        float elapsedTime = graspStart; 

        if(elapsedTime<duration/2) // Either go from 0 to end of animation time or ...
        {
            while(elapsedTime<duration)
            {
                animator.SetFloat("closeTime", (elapsedTime * speedFactor)); // * Time.fixedDeltaTime
                elapsedTime = Time.time - startTime; 
                yield return null; 
            }
        }
        else // ... or reverse to time 0 of the animation clip 
        {
            while(elapsedTime>=0f)
            {
                animator.SetFloat("closeTime", (elapsedTime * speedFactor)); //  * Time.fixedDeltaTime
                elapsedTime -= Time.time - startTime; 
                yield return null; 
            }
        }
        
    }

    IEnumerator CloseSequence(int rotationAngle)
    {
        for (int i = 0; i < 140; i++)
        {
            //thumbTargAng = 0 - i;
            yield return null;
        }
    }
    IEnumerator OpenSequence(int rotationAngle)
    {
        for (int i = 0; i < 140; i++)
        {
            //thumbTargAng = -140 + i;
            yield return null;
        }
    }
}
