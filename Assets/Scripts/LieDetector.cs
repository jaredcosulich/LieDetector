using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LieDetector : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ChangeColor(Color c)
    {
        List<Material> m = new List<Material>();
        GameObject.Find("Sphere").GetComponent<Renderer>().GetMaterials(m);
        m[0].SetColor("_Color", c);
    }
}
