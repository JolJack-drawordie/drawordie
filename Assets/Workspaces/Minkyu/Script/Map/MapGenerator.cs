using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public GameObject monsterPrefab;
    public GameObject elitePrefab;
    public GameObject bossPrefab;

    public Transform nodeParent;

    void Start()
    {
        CreateNode(monsterPrefab, new Vector2(-200, -150));
        CreateNode(monsterPrefab, new Vector2(200, -150));

        CreateNode(elitePrefab, new Vector2(0, 50));

        CreateNode(bossPrefab, new Vector2(0, 250));
    }

    void CreateNode(GameObject prefab, Vector2 pos)
    {
        GameObject node = Instantiate(prefab, nodeParent);

        RectTransform rt = node.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
    }
}