using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlatesCounterVisual : MonoBehaviour
{
    [SerializeField] private Transform counterTopPoint;
    [SerializeField] private Transform plateVisualPrefab;
    [SerializeField] private PlatesCounter platesCounter;

    private List<GameObject> plateVisualGameObjectsList;

    private void Awake()
    {
        plateVisualGameObjectsList = new List<GameObject>();
    }
    private void Start()
    {
        platesCounter.OnPlateSpawned += PlatesCounter_OnPlateSpawned;
        platesCounter.OnPlateRemoved += PlatesCounter_OnPlateRemoved;
    }

    private void PlatesCounter_OnPlateSpawned(object sender, EventArgs e)
    {
        Transform plateTransform = Instantiate(plateVisualPrefab, counterTopPoint);
        float offsetY = 0.1f;
        plateTransform.localPosition = new Vector3(0, offsetY * plateVisualGameObjectsList.Count, 0);

        plateVisualGameObjectsList.Add(plateTransform.gameObject);
    }

    private void PlatesCounter_OnPlateRemoved(object sender, EventArgs e)
    {
        GameObject plateGameObject = plateVisualGameObjectsList[plateVisualGameObjectsList.Count - 1];
        plateVisualGameObjectsList.RemoveAt(plateVisualGameObjectsList.Count - 1);
        Destroy(plateGameObject);
    }
}
