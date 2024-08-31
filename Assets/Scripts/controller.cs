using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class controller : MonoBehaviour
{
    [SerializeField] private GameObject allPathsPrefab;
    [SerializeField] private GameObject resultPathPrefab;
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private GridLayoutGroup tableLayout;
    [SerializeField] private Dropdown startDD;
    [SerializeField] private Dropdown goalDD;

    private List<GameObject> nodes;
    private Dictionary<GameObject, float> distances = new Dictionary<GameObject, float>();
    private GameObject drawing;
    private GameObject startNode;
    private GameObject goalNode;
    private List<string> nodesDict = new List<string>();
    private List<GameObject> complitedNodes = new List<GameObject>();


    public void Start()
    {
        nodes = GameObject.FindGameObjectsWithTag("Node").ToList();

        startDD.AddOptions(nodes.ConvertAll(gameObject => gameObject.GetComponent<NodeScript>().Name));
        goalDD.AddOptions(nodes.Where(node => node != nodes[0]).ToList().ConvertAll(gameObject => gameObject.GetComponent<NodeScript>().Name));
        startDD.onValueChanged.AddListener(index => OnStartDropDownSelected(nodes, index));
        goalDD.onValueChanged.AddListener(index => DestroyCurrentPath());
        foreach (GameObject node in nodes)
        {
            List<GameObject> connectedNodesForNode = node.GetComponent<NodeScript>().ConnectedNodes;

            nodesDict.Add(node.GetComponent<NodeScript>().Name);
            foreach (GameObject conNode in connectedNodesForNode)
            {

                if (!complitedNodes.Contains(conNode))
                {
                    drawing = Instantiate(allPathsPrefab);
                    
                    LineRenderer lineRenderer = drawing.GetComponent<LineRenderer>();
                    List<Vector3> pos = new List<Vector3>();
                    pos.Add(new Vector3(node.transform.position.x, node.transform.position.y));
                    pos.Add(new Vector3(conNode.transform.position.x, conNode.transform.position.y));
                    lineRenderer.SetPositions(pos.ToArray());
                    lineRenderer.GetComponent<pathLabelScript>().SetPos(node.GetComponent<NodeScript>().Edges.Find(node => node.Item1 == conNode));
                    lineRenderer.useWorldSpace = true;

                }
            }
            complitedNodes.Add(node);
        }
    }

    private List<GameObject> getList(GameObject x)
    {
        return nodes.Where(node => node != x).ToList();
    }

    public void DestroyCells()
    {
        List<GameObject> cells = GameObject.FindGameObjectsWithTag("Cell").ToList();
        foreach (GameObject cell in cells)
        {
            Destroy(cell);
        }
    }

    public void DestroyCurrentPath()
    {
        DestroyCells();
        tableLayout.gameObject.SetActive(false);
        List<GameObject> curPath = GameObject.FindGameObjectsWithTag("resultPath").ToList();
        foreach (GameObject el in curPath)
        {
            Destroy(el);
        }
    }

    public void OnStartDropDownSelected(List<GameObject> selectedTacticalTargets, int index)
    {
        DestroyCurrentPath();
        goalDD.ClearOptions();
        Debug.Log(selectedTacticalTargets[index] + " has been selected");
        goalDD.AddOptions(getList(selectedTacticalTargets[index]).ConvertAll(gameObject => gameObject.GetComponent<NodeScript>().Name));
        //startDD.AddOptions(getList(selectedTacticalTargets[index]).ConvertAll(gameObject => gameObject.GetComponent<NodeScript>().Name));
    }


    public void calculate()
    {
        startNode = nodes.FirstOrDefault(node => node.GetComponent<NodeScript>().Name == startDD.GetComponent<Dropdown>().captionText.text); 
        goalNode = nodes.FirstOrDefault(node => node.GetComponent<NodeScript>().Name == goalDD.GetComponent<Dropdown>().captionText.text);
        GameObject[] resPath = GameObject.FindGameObjectsWithTag("resultPath");
        foreach (GameObject r in resPath)
        {
            Destroy(r);
        }
        List<GameObject> result = FindShortestPath(startNode, goalNode, nodes);

        GameObject prevPoint = result[0];
        foreach ( GameObject point in result)
        {
            if (point == prevPoint) continue;
            drawing = Instantiate(resultPathPrefab);
            LineRenderer lineRenderer = drawing.GetComponent<LineRenderer>();
            List<Vector3> pos = new List<Vector3>();
            pos.Add(new Vector3(prevPoint.transform.position.x, prevPoint.transform.position.y));
            pos.Add(new Vector3(point.transform.position.x, point.transform.position.y));
            lineRenderer.SetPositions(pos.ToArray());
            lineRenderer.useWorldSpace = true;
            prevPoint = point;
        }
        tableLayout.gameObject.SetActive(true);
        tableLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        tableLayout.constraintCount = 2;
        DestroyCells();
        //tableLayout.cellSize = new Vector2(150f, 25f);
        GameObject nodeLabel = Instantiate(cellPrefab, tableLayout.transform);
        GameObject weightLabel = Instantiate(cellPrefab, tableLayout.transform);
        nodeLabel.GetComponent<TMP_Text>().text = "Номер узла";
        nodeLabel.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
        weightLabel.GetComponent<TMP_Text>().text = "Вес узла";
        weightLabel.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
        var sortedDict = from entry in distances orderby entry.Value ascending select entry;
        foreach (var node in sortedDict)
        {
            GameObject nodeName = Instantiate(cellPrefab, tableLayout.transform);
            GameObject nodeWeight = Instantiate(cellPrefab, tableLayout.transform);
            nodeName.GetComponent<TMP_Text>().text = node.Key.GetComponent<NodeScript>().Name;
            nodeWeight.GetComponent<TMP_Text>().text = node.Value.ToString();
        }
    }

    public List<GameObject> FindShortestPath(GameObject start, GameObject goal, List<GameObject> allNodes)
    {
        var unvisited = new HashSet<GameObject>();
        var previousNodes = new Dictionary<GameObject, GameObject>();

        foreach (var node in allNodes)
        {
            
            distances[node] = Mathf.Infinity; // Set initial distances to infinity
            unvisited.Add(node); // Add all nodes to unvisited set
        }

        distances[start] = 0; // Distance to the start node is 0

        while (unvisited.Count > 0)
        {
            GameObject currentNode = GetLowestDistanceNode(unvisited, distances);
            unvisited.Remove(currentNode);

            if (currentNode == goal)
            {
                return ConstructPath(previousNodes, start, goal);
            }
            NodeScript nodeEdges = currentNode.GetComponent<NodeScript>();
            foreach (var edge in nodeEdges.Edges)
            {
                float altDistance = distances[currentNode] + edge.Item2; //Item2 - вес ребра

                if (altDistance < distances[edge.Item1]) //Item1 - связанная вершина
                {
                    distances[edge.Item1] = altDistance;
                    previousNodes[edge.Item1] = currentNode;
                }
            }
        }

        return new List<GameObject>(); // Return an empty path if no path exists
    }

    private static GameObject GetLowestDistanceNode(HashSet<GameObject> unvisited, Dictionary<GameObject, float> distances)
    {
        GameObject lowestNode = null;
        float lowestDistance = Mathf.Infinity;

        foreach (var node in unvisited)
        {
            if (distances[node] < lowestDistance)
            {
                lowestDistance = distances[node];
                lowestNode = node;
            }
        }

        return lowestNode;
    }

    private static List<GameObject> ConstructPath(Dictionary<GameObject, GameObject> previousNodes, GameObject start, GameObject goal)
    {
        var path = new List<GameObject>();
        GameObject currentNode = goal;

        while (currentNode != null)
        {
            path.Add(currentNode);
            currentNode = previousNodes.ContainsKey(currentNode) ? previousNodes[currentNode] : null;
        }

        path.Reverse(); // Reverse the path to get it from start to goal
        return path;
    }

}
