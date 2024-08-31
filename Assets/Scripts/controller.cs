using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;
using UnityEngine.UI;
using System.Linq;

public class controller : MonoBehaviour
{
    /// <summary>
    /// Шаблон грани между вершинами
    /// </summary>
    [SerializeField] private GameObject allPathsPrefab;
    /// <summary>
    /// Шаблон грани оптимального пути между вершинами
    /// </summary>
    [SerializeField] private GameObject resultPathPrefab;
    /// <summary>
    /// Шаблон ячейки таблицы вывода результата расчетов
    /// </summary>
    [SerializeField] private GameObject cellPrefab;
    /// <summary>
    /// Таблицу вывода результата расчетов
    /// </summary>
    [SerializeField] private GridLayoutGroup tableLayout;
    /// <summary>
    /// Выпадающий список выбора вершины начала для алгоритма
    /// </summary>
    [SerializeField] private Dropdown startDD;
    /// <summary>
    /// Выпадающий список выбора вершины конца для алгоритма
    /// </summary>
    [SerializeField] private Dropdown goalDD;
    /// <summary>
    /// Выпадающий список выбора вершины начала ребра
    /// </summary>
    [SerializeField] private Dropdown parentDD;
    /// <summary>
    /// Выпадающий список выбора вершины конца ребра
    /// </summary>
    [SerializeField] private Dropdown childDD;
    /// <summary>
    /// Кнопка запуска алгоритма поиска пути
    /// </summary>
    [SerializeField] private GameObject startButton;
    /// <summary>
    /// Кнопка вкл/выкл режима построения графа
    /// </summary>
    [SerializeField] private GameObject modeButton;
    /// <summary>
    /// Шаблон вершины графа
    /// </summary>
    [SerializeField] private GameObject nodePrefab;

    /// <summary>
    /// Переменная хранящая состояние режима построения графа (режим включен(true) или выключен(false))
    /// </summary>
    private bool buildMode = false;
    /// <summary>
    /// Список элементов интерфейса отвечающих за алгоритм расчета пути
    /// </summary>
    private List<GameObject> uiElements;
    /// <summary>
    /// Список элементов интерфейса отвечающих за режим построения графа
    /// </summary>
    private List<GameObject> buidUIElements;
    /// <summary>
    /// Список вершин графа
    /// </summary>
    private List<GameObject> nodes = new List<GameObject>();
    /// <summary>
    /// Коллекция содержащая стоимости до вершин графа
    /// </summary>
    private Dictionary<GameObject, float> distances = new Dictionary<GameObject, float>();

    private GameObject drawing;
    private GameObject startNode;
    private GameObject goalNode;
    private Camera m_Camera;


    public void Start()
    {
        uiElements = GameObject.FindGameObjectsWithTag("UI").ToList();
        buidUIElements = GameObject.FindGameObjectsWithTag("BuildUI").ToList();
        foreach(GameObject eUI in buidUIElements)
        {
            eUI.SetActive(false);
        }
        startDD.AddOptions(nodes.ConvertAll(gameObject => gameObject.GetComponent<NodeScript>().Name));
        goalDD.AddOptions(nodes.Where(node => node != nodes[0]).ToList().ConvertAll(gameObject => gameObject.GetComponent<NodeScript>().Name));
        startDD.onValueChanged.AddListener(index => OnStartDropDownSelected(nodes, index));
        goalDD.onValueChanged.AddListener(index => DestroyCurrentPath());
        parentDD.onValueChanged.AddListener(index => OnParentDropDownSelected(nodes, index));
    }

    private List<GameObject> getList(GameObject x)
    {
        return nodes.Where(node => node != x).ToList();
    }

    /// <summary>
    /// Метод отчистки таблицы с результатами
    /// </summary>
    public void DestroyCells()
    {
        List<GameObject> cells = GameObject.FindGameObjectsWithTag("Cell").ToList();
        foreach (GameObject cell in cells)
        {
            Destroy(cell);
        }
    }

    /// <summary>
    /// Удаляет построенный оптимальный маршрут
    /// </summary>
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

    /// <summary>
    /// Событие, которое вызывается при изменении значения в выпадающем списке выбора вершины старта алгоритма
    /// </summary>
    /// <param name="selectedTargets">Коллекция элементов вершин</param>
    /// <param name="index">Индекс выбранного элемента</param>
    public void OnStartDropDownSelected(List<GameObject> selectedTargets, int index)
    {
        DestroyCurrentPath();
        goalDD.ClearOptions();
        goalDD.AddOptions(getList(selectedTargets[index]).ConvertAll(gameObject => gameObject.GetComponent<NodeScript>().Name));
    }
    /// <summary>
    /// Событие, которое вызывается при изменении значения в выпадающем списке выбора вершины начала ребра
    /// </summary>
    /// <param name="selectedTargets">Коллекция элементов вершин</param>
    /// <param name="index">Индекс выбранного элемента</param>
    public void OnParentDropDownSelected(List<GameObject> selectedTargets, int index)
    {
        childDD.ClearOptions();
        List<GameObject> addedEdges = nodes.Where(node => !selectedTargets[index].GetComponent<NodeScript>().Edges.Any(k => k.Item1.Equals(node))).ToList(); //Получаем список доступных вершин для построения ребра 
        addedEdges.Remove(selectedTargets[index]); //Убираем из списка вершину начала ребра
        if (addedEdges is not null)
        {
            childDD.AddOptions(addedEdges.ConvertAll(gameObject => gameObject.GetComponent<NodeScript>().Name));
        }
    }

    /// <summary>
    /// Метод запуска и визуализации расчета алгоритма (вызывается при нажатии кнопки "Пуск")
    /// </summary>
    public void calculate()
    {
        startNode = nodes.FirstOrDefault(node => node.GetComponent<NodeScript>().Name == startDD.GetComponent<Dropdown>().captionText.text); //Получаем вершину старта
        goalNode = nodes.FirstOrDefault(node => node.GetComponent<NodeScript>().Name == goalDD.GetComponent<Dropdown>().captionText.text);   //Получаем вершину конца
        if (startNode is null || goalNode is null)
        {
            Debug.Log("Одна из вершин не выбрана");
            return;
        }
        GameObject[] resPath = GameObject.FindGameObjectsWithTag("resultPath");
        foreach (GameObject r in resPath)
        {
            Destroy(r);
        }
        List<GameObject> result = FindShortestPath(startNode, goalNode); //Запускаем алгоритм Дейкстры
        
        //Визуализируем оптимальный маршрут
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

        //Визуализируем таблицу с результом
        tableLayout.gameObject.SetActive(true);
        tableLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        tableLayout.constraintCount = 2;
        DestroyCells();
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

    /// <summary>
    /// Алгоритм Дейкстры
    /// </summary>
    /// <param name="start">Вершина начала</param>
    /// <param name="goal">Вершина конца</param>
    /// <returns>Возвращает коллекцию вершин оптимального маршрута</returns>
    public List<GameObject> FindShortestPath(GameObject start, GameObject goal)
    {
        var unvisited = new HashSet<GameObject>();
        var previousNodes = new Dictionary<GameObject, GameObject>();

        foreach (GameObject node in nodes)
        {
            distances[node] = Mathf.Infinity; // Задаем значение стоимости вершин равное бесконечности
            unvisited.Add(node); // Добавляем все вершины в список не посещенных
        }

        distances[start] = 0; // Устанавливаем стоимость вершины начала 0

        while (unvisited.Count > 0)
        {
            GameObject currentNode = GetLowestDistanceNode(unvisited, distances); //Получаем вершину с наименьшей стоимостью
            unvisited.Remove(currentNode); //Удаляем вершину с наименьшей стоимостью из коллекции не посещенных вершин

            if (currentNode == goal)
            {
                return ConstructPath(previousNodes, start, goal); //Завершаем алгоритм если вершина с наименьшей стоимостью является конечной
            }
            NodeScript nodeEdges = currentNode.GetComponent<NodeScript>();
            foreach (var edge in nodeEdges.Edges) //Считаем стоимости для всех связанных вершин
            {
                float altDistance = distances[currentNode] + edge.Item2; //Item2 - вес ребра

                if (altDistance < distances[edge.Item1]) //Item1 - связанная вершина
                {
                    distances[edge.Item1] = altDistance; //Устанавливаем для вершивы стоимость если она нименьшая
                    previousNodes[edge.Item1] = currentNode; 
                }
            }
        }

        return new List<GameObject>(); // Возвращаем пустой список если пути не существует
    }

    /// <summary>
    /// Метод определения вершины с наименьшей стоимостью (ближайшей, при условии что вес ребра это расстояние между связанными ребром вершинами)
    /// </summary>
    /// <param name="unvisited">Коллекция не посещенных вершин</param>
    /// <param name="distances">Коллекция вершин и стоимостей</param>
    /// <returns></returns>
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

        path.Reverse(); 
        return path;
    }

    /// <summary>
    /// Метод вызывается при нажатии на кнопку "Построить ребро", добавляет для выбранной вершины связанную вершину и визуализирует ребро
    /// </summary>
    public void BuildEdge()
    {
        GameObject parentNode = nodes.FirstOrDefault(node => node.GetComponent<NodeScript>().Name == parentDD.GetComponent<Dropdown>().captionText.text);
        GameObject childNode = nodes.FirstOrDefault(node => node.GetComponent<NodeScript>().Name == childDD.GetComponent<Dropdown>().captionText.text);
        parentNode.GetComponent<NodeScript>().Edges.Add((childNode, Mathf.Round(Vector3.Distance(parentNode.transform.position, childNode.transform.position))));
        childNode.GetComponent<NodeScript>().Edges.Add((parentNode, Mathf.Round(Vector3.Distance(parentNode.transform.position, childNode.transform.position))));
        childDD.ClearOptions();
        
        List<GameObject> addedEdges = nodes.Where(node => !parentNode.GetComponent<NodeScript>().Edges.Any(k => k.Item1.Equals(node))).ToList();
        addedEdges.Remove(parentNode);
        if(addedEdges is not null)
        {
            childDD.AddOptions(addedEdges.ConvertAll(gameObject => gameObject.GetComponent<NodeScript>().Name));
        }
        Debug.Log(parentDD.GetComponent<Dropdown>().captionText);

        drawing = Instantiate(allPathsPrefab);
        LineRenderer lineRenderer = drawing.GetComponent<LineRenderer>();
        List<Vector3> pos = new List<Vector3>();
        pos.Add(new Vector3(parentNode.transform.position.x, parentNode.transform.position.y));
        pos.Add(new Vector3(childNode.transform.position.x, childNode.transform.position.y));
        lineRenderer.SetPositions(pos.ToArray());
        lineRenderer.GetComponent<pathLabelScript>().SetPos(parentNode.GetComponent<NodeScript>().Edges.Find(node => node.Item1 == childNode));
        lineRenderer.useWorldSpace = true;
    }


    void Awake()
    {
        m_Camera = Camera.main;
    }

    /// <summary>
    /// Метод вызывается при нажатии на кнопку "Режим построения графа"
    /// </summary>
    public void ChangeMode()
    {
        if (buildMode == false)
        {
            parentDD.ClearOptions();
            childDD.ClearOptions();
            DestroyCells();
            DestroyCurrentPath();
            foreach (GameObject uEl in uiElements)
            {
                uEl.SetActive(false);
            }
            foreach (GameObject uEl in buidUIElements)
            {
                uEl.SetActive(true);
            }
            foreach (GameObject node in nodes) 
            {
                Destroy(node);
            }
            nodes.Clear();
            List<GameObject> paths = GameObject.FindGameObjectsWithTag("Path").ToList();
            foreach (GameObject path in paths)
            {
                Destroy(path);
            }
            paths.Clear();
            startDD.ClearOptions();
            goalDD.ClearOptions();
            modeButton.GetComponent<Image>().color = Color.green;
            buildMode = true;
        }
        else
        {
            startButton.SetActive(true);
            foreach (GameObject uEl in uiElements)
            {
                uEl.SetActive(true);
            }
            foreach (GameObject uEl in buidUIElements)
            {
                uEl.SetActive(false);
            }
            startDD.AddOptions(nodes.ConvertAll(gameObject => gameObject.GetComponent<NodeScript>().Name));
            goalDD.AddOptions(nodes.Where(node => node != nodes[0]).ToList().ConvertAll(gameObject => gameObject.GetComponent<NodeScript>().Name));
            modeButton.GetComponent<Image>().color = Color.white;
            buildMode = false;
        }
    }

    /// <summary>
    /// Метод создающий вершину на сцене в режиме построения графа
    /// </summary>
    private void OnMouseDown()
    {
        Vector2 clickPos = m_Camera.ScreenToWorldPoint(Input.mousePosition);
        if(buildMode == true)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(clickPos, 1f);
            foreach (Collider2D col in colliders)
            {

                bool hasCollider = colliders.Any(c => c.name == "Controller");
                bool hasNode = nodes.Any(node => colliders.Any(c => c.name == node.name));

                if (hasCollider)
                {
                    if(hasNode)
                    {
                        Debug.Log("Создать объект нельзя");
                        return;
                    }
                    else
                    {
                        GameObject newNode = Instantiate(nodePrefab, clickPos, Quaternion.Euler(0, 0, 0));
                        newNode.name = "node"+GameObject.FindGameObjectsWithTag("Node").ToList().Count.ToString();
                        newNode.GetComponent<NodeScript>().Name = GameObject.FindGameObjectsWithTag("Node").ToList().Count.ToString();
                        nodes.Add(newNode);
                        parentDD.ClearOptions();
                        parentDD.AddOptions(nodes.ConvertAll(gameObject => gameObject.GetComponent<NodeScript>().Name));
                        childDD.ClearOptions();
                        childDD.AddOptions(nodes.Where(node => node != nodes[0]).ToList().ConvertAll(gameObject => gameObject.GetComponent<NodeScript>().Name));
                    }
                }

            }
        }
        
    }

}
