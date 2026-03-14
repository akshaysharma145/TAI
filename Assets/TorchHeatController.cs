using UnityEngine;
using System.Collections;

public class TorchHeatController : MonoBehaviour
{
    public Light torchLight;
    public GameObject heatVFX;

    public float maxHeat = 20f;
    public float maxCoolTime = 2f;

    [Range(0,1)]
    public float heatEffectThreshold = 0.75f; // 75%

    float currentHeat = 0f;
    bool isOn = false;
    bool isCooling = false;

    void Update()
    {
        // Toggle with F key
        if (Input.GetKeyDown(KeyCode.F) && !isCooling)
        {
            ToggleTorch();
        }

        if (isOn)
        {
            currentHeat += Time.deltaTime;

            if (currentHeat >= maxHeat)
            {
                StartCoroutine(Overheat());
            }
        }

        HandleHeatVFX();
    }

    void ToggleTorch()
    {
        isOn = !isOn;
        torchLight.enabled = isOn;

        if (!isOn)
        {
            StartCoroutine(CoolDown());
        }
    }

    void HandleHeatVFX()
    {
        float heatPercent = currentHeat / maxHeat;

        if (heatPercent >= heatEffectThreshold)
        {
            if (!heatVFX.activeSelf)
                heatVFX.SetActive(true);
        }
        else
        {
            if (heatVFX.activeSelf)
                heatVFX.SetActive(false);
        }
    }

    IEnumerator Overheat()
    {
        isOn = false;
        torchLight.enabled = false;

        yield return StartCoroutine(CoolDown());
    }

    IEnumerator CoolDown()
    {
        isCooling = true;

        float heatPercent = currentHeat / maxHeat;
        float coolTime = maxCoolTime * heatPercent;

        yield return new WaitForSeconds(coolTime);

        currentHeat = 0f;
        isCooling = false;

        if (heatVFX != null)
            heatVFX.SetActive(false);
    }
    public void ForceTurnOff()
    {
        isOn = false;

        if (torchLight != null)
            torchLight.enabled = false;

        StartCoroutine(CoolDown());
    }
}