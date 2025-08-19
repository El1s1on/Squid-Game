using System.Collections;
using UnityEngine;

public class Knife : InventoryItem
{
    [Header("Knife Posing")]
    [SerializeField] private GameObject knifeModel;
    [SerializeField] private Vector3 knifePosition;
    [SerializeField] private Vector3 knifeRotation;

    [Header("Knife Attack")]
    [SerializeField] private AudioClip slashSFX;
    [SerializeField] private float hitTime;
    [SerializeField] private float attackTime;
    [SerializeField] private int maxAttacks;
    private AudioSource audioSource;
    private bool readyForAttack = true;
    private int attackIndex = 0;

    [Header("Knife Hit")]
    [SerializeField] private GameObject bloodImpact;
    [SerializeField] private LayerMask hitableLayer;
    [SerializeField] private Vector3 hitPivot;
    [SerializeField] private float hitRadius;
    [SerializeField] private float damage;
    private Transform ignoreTorso;
    private Transform avatar;
    private Animator animator;
    private PlayerRagdoll playerRagdoll;
    
    public override void Equip()
    {
        if (animator == null)
            animator = player.GetComponent<Animator>();
        
        if(audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (playerRagdoll == null)
            playerRagdoll = player.GetComponent<PlayerRagdoll>();

        Transform rightArm = FindChildByName(player.transform, "Right Arm");
        ignoreTorso = FindChildByName(player.transform, "Torso");
        avatar = FindChildByName(player.transform, "Avatar");
        
        knifeModel.transform.parent = rightArm;
        knifeModel.transform.localPosition = knifePosition;
        knifeModel.transform.localEulerAngles = knifeRotation;
        animator.SetBool("Knife", true);
        readyForAttack = true;
        base.Equip();
    }

    public override void Unequip()
    {
        if (animator == null)
            animator = player.GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        knifeModel.transform.parent = transform;
        animator.SetBool("Knife", false);

        base.Unequip();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && readyForAttack && !isBot)
            Attack();
    }

    public void Attack() => StartCoroutine(AttackCoroutine());

    private IEnumerator AttackCoroutine()
    {
        if (playerRagdoll.IsRagdoll) yield break;

        readyForAttack = false;
        animator.SetTrigger("Attack");
        animator.SetInteger("AttackIndex", attackIndex);
        attackIndex++;

        if(attackIndex >= maxAttacks)
            attackIndex = 0;

        yield return new WaitForSeconds(hitTime);

        audioSource.PlayOneShot(slashSFX, Random.Range(0.85f, 1f));

        Vector3 hitCenter = transform.position + avatar.forward + hitPivot;
        Collider[] hitTargets = Physics.OverlapSphere(hitCenter, hitRadius, hitableLayer);

        foreach (Collider target in hitTargets)
        {
            PlayerRagdoll targetRagdoll = target.transform.root.GetComponent<PlayerRagdoll>();

            if (target.gameObject.name == "Torso" && target.transform != ignoreTorso && !targetRagdoll.IsRagdoll)
            {
                PlayerHealth targetHealth = target.GetComponentInParent<PlayerHealth>();
                if (targetHealth != null)
                {
                    GameObject bloodVFX = Instantiate(bloodImpact, target.transform.position, Quaternion.identity);
                    targetHealth.TakeDamage(damage);
                    targetRagdoll.Push(avatar.forward * 25f);
                    Destroy(bloodVFX, 3f);
                }
            }
        }

        yield return new WaitForSeconds(attackTime - hitTime);
        readyForAttack = true;
    }

    private void OnDrawGizmos()
    {
        if (avatar != null && !readyForAttack)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + avatar.forward + hitPivot, hitRadius);
        }
    }

    public Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform found = FindChildByName(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
}
