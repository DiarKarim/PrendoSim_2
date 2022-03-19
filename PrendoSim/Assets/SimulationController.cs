using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO; 
using TMPro; 

[System.Serializable]
public class DataClass2
{
    // Length of the float is the trial duration (15s) divided by the frame time step (1/60th) plus a few extra frames for safety
    [SerializeField] public List<string> jointData = new List<string>();
    [SerializeField] public List<string> objectInfo = new List<string>(); // name, criticalmass, etc. 
}

// Formerly "RobotHandController"
public class SimulationController  : MonoBehaviour // Formerly "DriverController_v2"
{
    // Instead of animators, we should use coroutines. 
    // This will avoid a lot of the rotation issues I 
    // have been experiencing, which is a result of quaternion 
    //to euler conversion done under the hood by unity between 
    // inspector values (set in the animation) and the actual euler angles of the objects
    DataClass2 expData = new DataClass2(); 

    [Header("   Press 'G' button to start one trial!!!")]
    public GameObject[] GripperPrefab; 
    Transform indexProx, ringProx;
    public GameObject[] TargetObjects;
    public Vector3 ObjectOffset;
    public float graspWaitTime = 1f; 
    public float jointSpeed = 2f; 
    public int activeGripperID = 0; // <= Find a way to make this dynamic i.e. using the UI etc. 
    public bool screenShot, recordData; 
    public float velocityThreshold; 
    public int numberOfSimulations = 10; 
    public TMPro.TMP_Text infoDisplayText; 
    public TMPro.TMP_Text infoSimulationText; 
    public TMPro.TMP_Text infoCounterText; 

    int simulationTrial = 0;

    StateObject roboState; 
    GameObject targObj_prefab; 
    Coroutine graspSimCoroutine, increaseMassRoutine, motionDetectRoutine;
    Coroutine[] closeSim = new Coroutine[6]; 
    bool gripperAnimDone = false; 
    float closeAnimDuration = 1f; 

    bool moved = false;
    float velocity = 0f;
    float targetObjectMass ;
    bool nextTrial = false; 

    private void Start() 
    {
        roboState = Resources.Load<StateObject>("StateObject");
        // Debug.Log(Application.persistentDataPath);
        infoDisplayText.text = "File path: " + Application.persistentDataPath;

    }

    void Update()
    {
        roboState.jointSpeed = jointSpeed; 

        if(simulationTrial <= numberOfSimulations)
        {
            if(Input.GetKeyDown(KeyCode.S) | nextTrial)
            {
                    if(graspSimCoroutine != null)
                    {StopCoroutine(graspSimCoroutine);}
                    graspSimCoroutine = StartCoroutine(GraspController());
                    simulationTrial++; 
                    infoCounterText.text = "Simulation # " + simulationTrial.ToString();
                    nextTrial=false; 
            }
        }

        if(Input.GetKeyDown(KeyCode.B))
        {
            StopAllCoroutines(); 
        }
    }

    // Spawns objects and commands the gripper to close to test whether the grip is stable
    IEnumerator GraspController()
    {
        roboState.onTable = false; 

        // Randomly spawn one of the grippers
        // int randGripper = Random.Range(0,2);
        int randGripper = 0; 
        GameObject gripper = Instantiate(GripperPrefab[randGripper]);

        indexProx = GameObject.FindGameObjectWithTag("idxPrx").GetComponent<Transform>();
        ringProx = GameObject.FindGameObjectWithTag("rngPrx").GetComponent<Transform>();
        Transform spawnPoint = GameObject.FindGameObjectWithTag("spawnloc").GetComponent<Transform>(); 

        ArticulationBody indexArt = indexProx.GetComponent<ArticulationBody>(); 
        ArticulationBody ringArt = ringProx.GetComponent<ArticulationBody>(); 

        float prox_rand = Random.Range(0f, 180f);
        RotateTo(indexArt, prox_rand);
        RotateTo(ringArt, prox_rand);
        // indexProx.localEulerAngles = new Vector3(prox_rand, 90f, -90f);
        // ringProx.localEulerAngles = new Vector3(prox_rand, -90f, 90f);


        // Debug.Log("1. Spawning object ...");
        SpawnObjects(spawnPoint);

        // Animate hand to close 
        // Debug.Log("2. Closing gripper ...");
        CloseGripper();
        yield return new WaitForSeconds(graspWaitTime); // <= This needs to be improved by linking it to the active gripper state

        // After 2 seconds (time for animation to complete), activate target object gravity to see if the grasp holds 
        // (if the objects falls to the table then the grasp is not successfull) 
        Invoke("ActivateGravity",graspWaitTime); 
        yield return new WaitForSeconds(graspWaitTime + 1f);

        // Increase mass 
        increaseMassRoutine = StartCoroutine(IncreaseMass(targObj_prefab,0.01f)); 

        // Monitor object motion
        Rigidbody rb = targObj_prefab.GetComponent<Rigidbody>(); 
        float maxTimer = 0;
        float startTime = Time.time; 
        float maxTime = 5f;
        float maxVelocity = 0f; 

        if(!roboState.onTable)
        {
            while(rb.velocity.magnitude<velocityThreshold & maxTimer<maxTime) // | distanceFromGripper < 0.1f
            {
                maxTimer = Time.time - startTime; 
                infoSimulationText.text = "Vel: " + rb.velocity.magnitude.ToString("F2") + " mm/s" + 
                                          " Mass: " + rb.mass.ToString("F2");
                // Debug.Log("Dist: " + distanceFromGripper.ToString("F3")); 
                maxVelocity = rb.velocity.magnitude; 
                yield return null;
            }

            // // Stop grasp stability test 
            if(increaseMassRoutine!=null)
                StopCoroutine(increaseMassRoutine);
            if(motionDetectRoutine!=null)
                StopCoroutine(motionDetectRoutine);

            string objName = targObj_prefab.gameObject.name;
            string objMass = rb.mass.ToString("F2"); //targetObjectMass.ToString("F2");
            string objVelocity = maxVelocity.ToString("F2");

            // Take screenshot and save simulation data if desired 
            if (screenShot)
            {
                TakeScreenShot(gripper.name +"_"+ objName + "_" + objMass + "_" + objVelocity);
            }
            if(recordData)
            {
                RecordData(Application.persistentDataPath, gripper, targObj_prefab, objMass);
            }

            // Wait for a moment then destroy the target object and start over again 
            yield return new WaitForSeconds(graspWaitTime/2f);

        }

        // Debug.Log("6. Destroying object ..."); 
        Destroy(targObj_prefab); 
        // Debug.Log("7. Opening gripper ...");
        OpenGripper();
        yield return new WaitForSeconds(graspWaitTime/2f);

        Destroy(gripper);        
        nextTrial = true; 
    }

    void RotateTo(ArticulationBody articulation, float angularRotationTarget)
    {
        var drive = articulation.xDrive;
        drive.target = angularRotationTarget;
        articulation.xDrive = drive;
    }

    // *** The following two functions are a pile of crap. Needs to be improved. <== Done
    // Now using the StateObject (roboState) scriptable object we activate the open and states of each joint 
    // inside of the "ArticulationProxyController.cs" script
    void RecordData(string filepath, GameObject gripper, GameObject targetObject, string objectCriticalMass)
    {
        // Gripper state
        foreach(Transform tf in gripper.GetComponentsInChildren<Transform>())
        {
            expData.jointData.Add(tf.name +","+ 
                                  tf.position.x.ToString("F2") +","+ 
                                  tf.position.y.ToString("F2") +","+ 
                                  tf.position.z.ToString("F2") +","+ 
                                  tf.localEulerAngles.x.ToString("F2") +","+ 
                                  tf.localEulerAngles.y.ToString("F2") +","+ 
                                  tf.localEulerAngles.z.ToString("F2") +","+
                                  roboState.Grippers[0].fingerTipForce[0].ToString("F2") +","+
                                  roboState.Grippers[0].fingerTipForce[1].ToString("F2") +","+
                                  roboState.Grippers[0].fingerTipForce[2].ToString("F2"));
        }
        // Target object
        expData.objectInfo.Add(targetObject.name + "," + 
                               targetObject.transform.position.x.ToString("F2") + "," + 
                               targetObject.transform.position.y.ToString("F2") + "," + 
                               targetObject.transform.position.z.ToString("F2") + "," + 
                               targetObject.transform.eulerAngles.x.ToString("F2") +","+ 
                               targetObject.transform.eulerAngles.y.ToString("F2") +","+ 
                               targetObject.transform.eulerAngles.z.ToString("F2") +","+ 
                               objectCriticalMass);
        
        string timeStamp = System.DateTime.Now.ToString("yyyyMMddHHmmss");
        string jsonString = JsonConvert.SerializeObject(expData, Formatting.Indented);
        File.WriteAllText(filepath +"/"+ targetObject.name + "_" + objectCriticalMass +"_"+ timeStamp + ".json", jsonString);

        expData.jointData.Clear(); 
        expData.objectInfo.Clear(); 
    }
    void TakeScreenShot(string fileName)
    {
        ScreenCapture.CaptureScreenshot(Application.persistentDataPath +"/"+
                                        fileName +"_"+ Time.deltaTime.ToString() + ".png");
    }
    IEnumerator IncreaseMass(GameObject targetObject, float massStepSize)
    {   
        float mass=0f; 
        Rigidbody rb = targetObject.GetComponent<Rigidbody>(); 
        while(targetObject!=null)
        {
            mass+=massStepSize; 
            rb.mass = rb.mass+mass * Time.fixedDeltaTime;
            yield return null; 
        }

        // Transform[] tf = targetObject.FindGameObjectsWithTag();  
        // float heightPos = 0f;
        // GameObject anch = new GameObject(); 

        // if(targetObject!=null)
        // {
        //     anch = targetObject.GetComponentInChildren<SpringJoint>().gameObject; 
        // }
        // Vector3 anchPos = anch.transform.position; 

        // while(targetObject!=null)
        // {
        //     heightPos -= massStepSize;

        //     anch.transform.position = new Vector3(anchPos.x, anchPos.y + heightPos, anchPos.z); 

        //     targetObjectMass = Mathf.Abs(anchPos.y - anch.transform.position.y) * 10f; 
        //     // rb.mass += massStepSize; 
        //     // targetObjectMass = rb.mass; 
        //     yield return null; 
        // }
    }
    IEnumerator DetectObjectMotion(GameObject targetObject, float velocityThreshold)
    {
        // Monitor object position and return state (velocity) when a velocity threshold (sensitivity) is exceeded  
        Vector3 prevPos = new Vector3();

        while (targetObject != null)
        {
            velocity = Mathf.Abs(((targetObject.transform.position - prevPos) / Time.fixedDeltaTime).magnitude);
            prevPos = targetObject.transform.position;

            // if (velocity < velocityThreshold)
            // {
            //     yield return null;
            // }
            // else if (velocity >= velocityThreshold & !moved)
            // {
            //     moved = true;
            //     yield return velocity;
            // }

            yield return null;
        }

        // if(velocity>=velocityThreshold)
        //     moved = true;
    }
    void CloseGripper() 
    {
        for(int j = 0; j<roboState.Grippers[activeGripperID].jointsToClose.Length; j++)
        {
            roboState.Grippers[activeGripperID].jointsToClose[j] = 1; // set joints to close; 
        }
    }
    void OpenGripper()
    {
        for(int j = 0; j<roboState.Grippers[activeGripperID].jointsToOpen.Length; j++)
        {
            roboState.Grippers[activeGripperID].jointsToOpen[j] = 1; // set joints to open; 
        }
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
    void SpawnObjects(Transform spawnPoint)
    {
            // Spawn random object at a random orientation but set to around the position of the robot gripper 
        int objectIndex = Random.Range(0, TargetObjects.Length);
        Quaternion randRotation = Random.rotation;
        targObj_prefab = Instantiate(TargetObjects[objectIndex], spawnPoint.position, randRotation); // GripperPrefab[activeGripperID].transform.position + ObjectOffset
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

    #region Button functions
    // public void StartSimulation()
    // {
    //     loopCounter = 0;
    //     if (InpPath.text == string.Empty)
    //     {
    //         Debug.Log("Path empty!!!");
    //         InpPath.text = Application.persistentDataPath;
    //         Debug.Log("Default path: " + InpPath.text);
    //     }

    //     if (BarrettHand.activeInHierarchy)
    //     {
    //         //co = StartCoroutine("LoopSimulation");
    //         co = StartCoroutine(BarrettSimulation());
    //     }
    //     else if (Parallel.activeInHierarchy)
    //     {
    //         co = StartCoroutine(ParallelSimulation());
    //         animatingParallel = true;
    //     }
    //     inSimulation = true;
    // }
    // public void TestSimulation()
    // {
    //     // drySimTest = true;
    //     if (BarrettHand.activeInHierarchy)
    //         co = StartCoroutine(BarrettSimulation());
    //     else if (Parallel.activeInHierarchy)
    //         co = StartCoroutine(ParallelSimulation());
    // }
    // public void StopSimulation()
    // {
    //     StopAllCoroutines();
    //     inSimulation = false;
    // }
    // public void QuitApplication()
    // {
    //     //UnityEditor.EditorApplication.isPlaying = false;
    //     Application.Quit();
    // }
    // public void MaxVisibility()
    // {
    //     /* This function will:  
    //      * 1. Clear the scene
    //      * 2. Load each image 
    //      * 3. Take screen-shots from the prescribed angle(s) 
    //      * 4. Indicate the task is complete
    //     */
    //     StartCoroutine(MaxVisiRoutine());
    // }
    // public void ChangeRobotGripper()
    // {
    //     /* This function will:  
    //     > Change which robo gripper is used
    //     */
    //     if (BarrettHand.activeInHierarchy)
    //     {
    //         BarrettHand.SetActive(false);
    //         Parallel.SetActive(true);
    //         GripperName.text = "Barrett Hand";
    //     }
    //     else if (Parallel.activeInHierarchy)
    //     {
    //         Parallel.SetActive(false);
    //         BarrettHand.SetActive(true);
    //         GripperName.text = "Jaw Gripper";
    //     }

    //     loopCounter = 0;
    // }
    // IEnumerator MaxVisiRoutine()
    // {

    //     // 1. Clear the scene
    //     if (handMesh != null)
    //     { handMesh.enabled = false; }
    //     //PanelCurtain.SetActive(true);

    //     // 2. Load each image
    //     foreach (GameObject tObj in TargetObjects)
    //     {
    //         GameObject tobject = Instantiate(tObj);
    //         yield return new WaitForSeconds(0.1f);
    //         StartCoroutine(TakeImages(paths + tObj.name + "_maxVis_"));

    //         SimCounter.text = "Max Visibility of: " + "\n" + tObj.name + " taken.";
    //         yield return new WaitForSeconds(1.5f);
    //         Destroy(tobject);
    //         yield return new WaitForSeconds(0.5f);
    //     }

    //     if (handMesh != null)
    //     { handMesh.enabled = true; }
    //     //PanelCurtain.SetActive(false);

    //     yield return null;
    // }
    #endregion
}