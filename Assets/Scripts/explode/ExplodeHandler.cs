 
 
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;

public class ExplodeHandler : MonoBehaviour
{
    [SerializeField]
    GameObject originalObject; // Fixed typo in variable name ("orignal" → "original")
    [SerializeField]
    GameObject model;
    Rigidbody[] rigidBodies; // Changed from "RigidBody" to "Rigidbody"

    private void Awake()
    {
        rigidBodies = model.GetComponentsInChildren<Rigidbody>(true); // Removed "GameObject." (incorrect usage)
    }

    void Start() {
    //    Explode(Vector3.forward);
     }

    public void Explode(Vector3 externalForce)
    {
        originalObject.SetActive(false); // Corrected "setActie" to "SetActive" (case-sensitive)
        foreach (Rigidbody rb in rigidBodies)
        {
            rb.transform.parent = null;
            rb.GetComponent<MeshCollider>().enabled = true;
            rb.gameObject.SetActive(true); // Corrected "GameObject" to "gameObject" (lowercase)

            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.AddForce(Vector3.up * 200 + externalForce, ForceMode.Force);
            rb.AddTorque(Random.insideUnitSphere * 0.5f, ForceMode.Impulse);
        }
    }
}

/*
public class CarExplode : MonoBehaviour
{
    [SerializeField]
    GameObject orignalObject;
    [SerializeField]
    GameObject model;
    Rigidbody[] rigidBodies;
    private void Awake()
    {
        rigidBodies = model.GameObject.GetComponentsInChildren<Rigidbody>(true);
    }
    void Start() { }

    public void Explode(Vector3 externalForce)
    {
        orignalObject.setActive(false);
        foreach (Rigidbody rb in rigidBodies)
        {
            rb.transform.parent = null;
            rb.GetComponent<MeshCollider>().enabled = true;
            rb.GameObject.setActive(true);

            rb.isKinematic = false;

            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.AddForce(Vector3.up * 208 + externalForce, ForceMode.Force);
            rb.AddTorque(Random.insideUnitSphere * 0.5f, ForceMode.Impulse);
        }


    }

}
*/