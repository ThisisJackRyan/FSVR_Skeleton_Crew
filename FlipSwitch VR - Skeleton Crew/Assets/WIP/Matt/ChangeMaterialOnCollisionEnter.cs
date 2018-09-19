using UnityEngine;
using System.Collections;

public class ChangeMaterialOnCollisionEnter : MonoBehaviour {

    public Material ReplacementMaterial;
    //public float TimeDelay = 1;
    //public bool ChangeShadow = true;

    private bool isInitialized;
    private Material mat;
    MeshRenderer mshRend;


    void Start() {
        isInitialized = true;
        mshRend = GetComponent<MeshRenderer>();
        mat = mshRend.sharedMaterial;
        //Invoke("ReplaceObject", TimeDelay);
    }

    void OnEnable() {
        if (isInitialized) {
            mshRend.sharedMaterial = mat;
            //Invoke("ReplaceObject", TimeDelay);
        }
    }

    private void OnCollisionEnter(Collision other) {
        OnTriggerEnter(other.collider);
     }

    private void OnTriggerEnter(Collider other) {
        if (other.tag == "MaskRemover") {
            ReplaceObject();
        }
    }



    void ReplaceObject() {
        mshRend.sharedMaterial = ReplacementMaterial;
    }
}
