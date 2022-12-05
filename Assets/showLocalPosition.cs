using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class showLocalPosition : MonoBehaviour
{
    // Start is called before the first frame update
    public Vector2 localPosition;

    // Update is called once per frame
    void Update()
    {
        localPosition = transform.localPosition;
    }
}
