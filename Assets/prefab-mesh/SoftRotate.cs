using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoftRotate : MonoBehaviour
{
    public float rotateSpeed = 1f;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
    }
}
