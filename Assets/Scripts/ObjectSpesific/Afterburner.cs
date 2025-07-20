using UnityEngine;

public class Afterburner : MonoBehaviour
{
    public GameObject instancePrefab;
    public int count = 10;
    public float spacing = 0.5f;
    public float scaleFalloff = 0.9f; // Her kopya %90 olacak

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CreateInstances();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void CreateInstances()
    {
        for (int i = 0; i < count; i++)
        {
            GameObject copy = Instantiate(instancePrefab, transform);
            float t = (float)i / (count - 1);

            // Pozisyon
            copy.transform.localPosition = new Vector3(0, 0, i * spacing);

            // Ölçek (konik küçülme)
            float scale = Mathf.Pow(scaleFalloff, i);
            copy.transform.localScale = new Vector3(scale, scale, 1);
        }
    }
}
