using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RandomTextSpawner : MonoBehaviour
{
    [Header("Text Setup")]
    public List<string> textsToSpawn = new List<string>();
    public GameObject textPrefab; 
    public float yOffset = 1.5f;


    public void SpawnRandomText()
    {
        if (textsToSpawn.Count == 0 || textPrefab == null)
        {
            Debug.LogWarning("❌ No texts or prefab assigned.");
            return;
        }

        List<Cube> filledCubes = GridManager.Instance.GetAllFilledCells();
        if (filledCubes.Count == 0)
        {
            Debug.LogWarning("❌ No filled cubes available.");
            return;
        }

        Cube selectedCube = filledCubes[Random.Range(0, filledCubes.Count)];
        Vector3 spawnPosition = selectedCube.transform.position + Vector3.up * yOffset;

        string randomText = textsToSpawn[Random.Range(0, textsToSpawn.Count)];

        GameObject textGO = Instantiate(textPrefab, spawnPosition, textPrefab.transform.rotation);
        if (textGO.TryGetComponent<TextMeshPro>(out var tmp))
        {
            tmp.text = randomText;
            tmp.color = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f); // vibrant random color
        }

        Destroy(textGO, 2f); // Destroy after 2 seconds
    }

}
