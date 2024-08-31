using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class pathLabelScript : MonoBehaviour
{
    [SerializeField] public GameObject CanvasPos;
    [SerializeField] public GameObject Label;
    [SerializeField] public GameObject parent;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SetPos((GameObject, float) node)
    {
        Vector3[] points = new Vector3[parent.GetComponent<LineRenderer>().positionCount];
        parent.GetComponent<LineRenderer>().GetPositions(points);
        CanvasPos.GetComponent<RectTransform>().SetPositionAndRotation(new Vector3()
        {
            x = (points[0].x + points[1].x) / 2,
            y = (points[0].y + points[1].y) / 2,

        }, parent.transform.rotation);
        Label.GetComponent<TMP_Text>().text = node.Item2.ToString();
        //Label.GetComponent<TMP_Text>().color = Color.white;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
