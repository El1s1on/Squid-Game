using System.Collections;
using UnityEngine;

public class Guard : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Transform avatar;
    [SerializeField] private AudioClip shootSFX;
    [SerializeField] private float bulletForce = 1000f;
    [SerializeField] private float rotationSpeed = 20f;
    [SerializeField] private float angleThreshold = .5f;
    private Animator animator;
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
    }

    public IEnumerator Eliminate(GameObject player)
    {
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

        while (!playerHealth.IsDied()) 
        {
            yield return Shoot(player.transform, 1000f);
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator Shoot(Transform target, float damage)
    {
        animator.SetBool("Aim", true);
        yield return StartCoroutine(Aim(target));
        animator.SetTrigger("Shoot");

        Transform torso = FindTorso(target);
        Vector3 direction = (torso.position - muzzle.position).normalized;
        Transform projectileClone = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity).transform;
        projectileClone.rotation = Quaternion.LookRotation(direction);
        var projectile = projectileClone.GetComponent<Projectile>();
        projectile.SetDamage(damage);
        projectile.SetTarget(target);
        projectileClone.GetComponent<Rigidbody>().linearVelocity = projectileClone.forward * bulletForce;
        audioSource.PlayOneShot(shootSFX);

        yield return new WaitForSeconds(1f);
        animator.SetBool("Aim", false);
    }

    private IEnumerator Aim(Transform targetToAim)
    {
        while (true)
        {
            Vector3 targetDirection = targetToAim.position - transform.position;
            targetDirection.y = 0;

            if (targetDirection.sqrMagnitude < 0.001f)
                yield break;

            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            float angleToTarget = Quaternion.Angle(transform.rotation, targetRotation);

            if (angleToTarget < angleThreshold)
                yield break;

            yield return null;
        }
    }

    public static Transform FindTorso(Transform player)
    {
        foreach (Transform child in player)
        {
            if (child.name == "Torso")
                return child;

            Transform found = FindTorso(child);
            if (found != null)
                return found;
        }
        return null;
    }
}
