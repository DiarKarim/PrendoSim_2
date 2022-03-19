using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GripperActions : MonoBehaviour
{

    private ArticulationBody[] artBods;
    private Coroutine closeRoutine; 


    private void Start()
    {
        artBods = GetComponentsInChildren<ArticulationBody>();
    }

    private void Update()
    {
        if(Input.GetKey(KeyCode.C))
        {
            if (closeRoutine != null)
                StopCoroutine(closeRoutine);
            closeRoutine = StartCoroutine(ClosingSeq());
        }
    }


    IEnumerator ClosingSeq()
    {
        foreach(ArticulationBody artBod in artBods)
        {
            string Gname = artBod.gameObject.name;

            if (Gname == "Rng_Prox")
            {
                for (float i = 0; i < 90f; i++)
                {
                    RotateTo(artBod, i);
                    yield return new WaitForSeconds(0.001f);
                }
            }
            else
            {
                for (float i = 0; i > -90f; i--)
                {
                    RotateTo(artBod, i);
                    yield return new WaitForSeconds(0.001f);
                }
            }
        }

        yield return null; 
    }

    void RotateTo(ArticulationBody artBod, float targetRotAng)
    {
        var drive = artBod.xDrive;
        drive.target = targetRotAng;
        artBod.xDrive = drive;
    }

}
