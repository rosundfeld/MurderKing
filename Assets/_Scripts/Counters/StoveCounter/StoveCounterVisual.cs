using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoveCounterVisual : MonoBehaviour
{
    [SerializeField] private StoveCounter stoveCounter;
    [SerializeField] private GameObject stoveGameObject;
    [SerializeField] private GameObject particleGameObject;

    private void Start()
    {
        stoveCounter.OnStateChanged += StoveCounter_OnStateChanged;
    }

    private void StoveCounter_OnStateChanged(object sender, StoveCounter.OnStateChangedEventArgs e)
    {
        bool showVisual = e.state == StoveCounter.State.Frying || e.state == StoveCounter.State.Fried;
        stoveGameObject.SetActive(showVisual);
        particleGameObject.SetActive(showVisual);

        //---------------------------------Alternative way to do it---------------------------------//
        // switch (e.state)
        // {
        //     case StoveCounter.State.Idle:
        //         stoveGameObject.SetActive(false);
        //         particleGameObject.SetActive(false);
        //         break;
        //     case StoveCounter.State.Frying:
        //         stoveGameObject.SetActive(true);
        //         particleGameObject.SetActive(false);
        //         break;
        //     case StoveCounter.State.Fried:
        //         stoveGameObject.SetActive(true);
        //         particleGameObject.SetActive(false);
        //         break;
        //     case StoveCounter.State.Burned:
        //         stoveGameObject.SetActive(true);
        //         particleGameObject.SetActive(true);
        //         break;
        // }
    }
}
