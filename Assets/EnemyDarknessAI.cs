using UnityEngine;
using UnityEngine.AI;

public class EnemyDarknessAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Light torchLight;

    [Header("Distance Settings")]
    public float activationDistance = 25f;
    public float deactivateDistance = 50f;
    public float lightCheckDistance = 30f;

    [Header("Movement Speeds")]
    public float normalSpeed = 3f;
    public float darkSpeed = 6f;

    NavMeshAgent agent;

    bool activated = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        CheckActivation();

        if (!activated)
        {
            FreezeEnemy();
            return;
        }

        if (IsInTorchLight())
        {
            FreezeEnemy();
        }
        else
        {
            MoveEnemy();
        }
    }

    void CheckActivation()
    {
        float dist = Vector3.Distance(player.position, transform.position);

        if (!activated && dist <= activationDistance)
        {
            activated = true;
        }

        if (activated && dist >= deactivateDistance)
        {
            activated = false;
            FreezeEnemy();
        }
    }

    bool IsInTorchLight()
    {
        if (torchLight == null) return false;

        Vector3 dir = transform.position - torchLight.transform.position;
        float distance = dir.magnitude;

        if (distance > lightCheckDistance)
            return false;

        float angle = Vector3.Angle(torchLight.transform.forward, dir);

        if (angle > torchLight.spotAngle * 0.5f)
            return false;

        RaycastHit hit;

        if (Physics.Raycast(torchLight.transform.position, dir.normalized, out hit, lightCheckDistance))
        {
            if (hit.transform == transform)
                return true;
        }

        return false;
    }

    void FreezeEnemy()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    void MoveEnemy()
    {
        agent.isStopped = false;

        float lookAngle = Vector3.Angle(player.forward, transform.position - player.position);

        if (lookAngle > 120f)
            agent.speed = darkSpeed;
        else
            agent.speed = normalSpeed;

        agent.SetDestination(player.position);
    }
}