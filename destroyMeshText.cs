using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class destroyMeshText : MonoBehaviour
{
    // Start is called before the first frame update
    float DestroyTime = 1f;
    void Start()
    {
        Destroy(gameObject, DestroyTime);
    }

    // Update is called once per frame
   
}
