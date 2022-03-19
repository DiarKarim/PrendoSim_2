using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceRenderer : MonoBehaviour
{
    public ArticulationProxyController[] artiCont;
    public Transform[] driverJoints;

    [Tooltip("X=0, Y=1, Z=2")]
    public int[] rotationAxis;

    public LineRenderer[] forceLines = new LineRenderer[6];
    public GameObject[] forceCylinders;
    float[] digitForce = new float[3];
    public float forceScale = 0.05f;

    void Update()
    {
        if (forceCylinders[0] != null)
        {
            forceCylinders[0].transform.localScale = new Vector3(forceCylinders[0].transform.localScale.x,
            (Mathf.Abs(artiCont[0].currentDigitForce) * forceScale), forceCylinders[0].transform.localScale.z);

            forceCylinders[1].transform.localScale = new Vector3(forceCylinders[1].transform.localScale.x,
            (Mathf.Abs(artiCont[1].currentDigitForce) * forceScale), forceCylinders[1].transform.localScale.z);

            forceCylinders[2].transform.localScale = new Vector3(forceCylinders[2].transform.localScale.x,
            (Mathf.Abs(artiCont[2].currentDigitForce) * forceScale), forceCylinders[2].transform.localScale.z);

        }
    }

    float ForceCompute(ArticulationBody artb, Transform driverRot, int axis)
    {
        float damping = 0.1f;
        float stiffness = 1f;
        float force = 0;

        var drive = artb.xDrive;
        float artRot = drive.target;

        if (axis == 0)
        {
            force = (stiffness * (artRot - Mathf.Abs(driverRot.localEulerAngles.x)) - (damping * artb.angularVelocity.sqrMagnitude));
        }
        if (axis == 1)
        {
            force = (stiffness * (artRot - Mathf.Abs(driverRot.localEulerAngles.y)) - (damping * artb.angularVelocity.sqrMagnitude));
        }
        if (axis == 2)
        {
            force = (stiffness * (artRot - Mathf.Abs(driverRot.localEulerAngles.z)) - (damping * artb.angularVelocity.sqrMagnitude));
        }
        return force;
    }
}