using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetStaticDataManager : MonoBehaviour
{
    private void Awake()
    {
        ResetAll();
    }

    public static void ResetAll()
    {
        CuttingCounter.ResetStaticData();
        BaseCounter.ResetStaticData();
        TrashCounter.ResetStaticData();
        Player.ResetStaticData();
    }
}
