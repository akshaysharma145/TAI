using UnityEngine;
using Unity.FPS.Gameplay;

public class TorchMomentSway : MonoBehaviour
{
    public float swayAmount = 0.04f;
    public float swaySpeed = 6f;

    Vector3 initialPosition;
    PlayerCharacterController player;

    void Start()
    {
        initialPosition = transform.localPosition;
        player = FindObjectOfType<PlayerCharacterController>();
    }

    void Update()
    {
        if (player == null) return;

        Vector3 velocity = player.CharacterVelocity;

        if (velocity.magnitude > 0.1f)
        {
            float swayX = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
            float swayY = Mathf.Cos(Time.time * swaySpeed * 2f) * swayAmount;

            transform.localPosition = initialPosition + new Vector3(swayX, swayY, 0f);
        }
        else
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                initialPosition,
                Time.deltaTime * 5f
            );
        }
    }
}