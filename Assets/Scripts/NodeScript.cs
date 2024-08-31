using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NodeScript : MonoBehaviour
{
    [SerializeField] public List<GameObject> ConnectedNodes;
    [SerializeField] public string Name;
    [SerializeField] public GameObject Label;
    public List<(GameObject, float)> Edges = new List<(GameObject, float)>();
    //private long Id;

    // Start is called before the first frame update
    void Start()
    {
        Label.GetComponent<TMP_Text>().text = Name;
        //Id = gameObject.GetInstanceID();
        foreach (GameObject conNode in ConnectedNodes)
        {
            Edges.Add((conNode, Mathf.Round( Vector3.Distance(gameObject.transform.position, conNode.transform.position))));
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
