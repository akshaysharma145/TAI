using UnityEngine;
using System.Collections;

public class TorchOverheatBehavior : MonoBehaviour
{
    TorchHeatController heatController;

    void Start()
    {
        heatController = GetComponent<TorchHeatController>();
    }

    public IEnumerator Overheat()
    {
        heatController.ForceTurnOff();

        yield return new WaitForSeconds(2f);
    }
}