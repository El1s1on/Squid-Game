using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private GameObject bloodImpact;
    [SerializeField] private GameObject decal;
    [SerializeField] private float damage = 15f;

    private Vector3 latePosition;
    private const float DecalLifetime = 5f;
    private const string PlayerTag = "Player";
    private const string BodyPartTag = "BodyPart";

    private Transform targetPlayer;
    private PlayerHealth targetPlayerHealth;

    private void Update()
    {
        Vector3 direction = transform.position - latePosition;
        float distance = direction.magnitude;

        if (distance > Mathf.Epsilon)
        {
            direction.Normalize();
            CheckForCollision(direction, distance);
        }
    }

    private void LateUpdate() => latePosition = transform.position;

    public void SetTarget(Transform target)
    {
        targetPlayer = target;
        if (targetPlayer != null)
            targetPlayerHealth = targetPlayer.GetComponent<PlayerHealth>();
        else
            targetPlayerHealth = null;
    }

    private void CheckForCollision(Vector3 direction, float distance)
    {
        Ray ray = new Ray(latePosition, direction);

        if (Physics.Raycast(ray, out RaycastHit hit, distance))
        {
            HandleCollision(hit);
        }
    }

    private void HandleCollision(RaycastHit hit)
    {
        if (TryHandlePlayerCollision(hit)) return;
        if (TryHandleBodyPartCollision(hit)) return;

        SpawnDecal(hit, decal);
    }

    private bool TryHandlePlayerCollision(RaycastHit hit)
    {
        if (!hit.collider.CompareTag(PlayerTag)) return false;

        if (targetPlayer != null && hit.collider.transform != targetPlayer)
        {
            return true;
        }

        if (hit.collider.TryGetComponent<PlayerHealth>(out var playerHealth))
        {
            playerHealth.TakeDamage(damage);
            SpawnDecal(hit, bloodImpact);
            Destroy(gameObject);
            return true;
        }

        return false;
    }

    private bool TryHandleBodyPartCollision(RaycastHit hit)
    {
        if (!hit.collider.CompareTag(BodyPartTag)) return false;

        var playerHealth = hit.collider.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            if (targetPlayer != null && playerHealth.transform != targetPlayer)
            {
                return true;
            }

            playerHealth.TakeDamage(damage);
            SpawnDecal(hit, bloodImpact);
            Destroy(gameObject);
            return true;
        }

        return false;
    }

    private void SpawnDecal(RaycastHit hit, GameObject decalPrefab)
    {
        if (decalPrefab == null) return;

        Quaternion rotation = Quaternion.LookRotation(hit.normal);
        GameObject decalClone = Instantiate(decalPrefab, hit.point, rotation);
        Destroy(decalClone, DecalLifetime);
        Destroy(gameObject);
    }

    public void SetDamage(float newDamage) => damage = newDamage;
}