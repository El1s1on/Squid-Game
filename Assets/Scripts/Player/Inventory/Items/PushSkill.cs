using System.Collections;
using UnityEngine;

public class PushSkill : InventoryItem
{
    [SerializeField] private LayerMask pushableLayer;
    [SerializeField] private Vector3 pushPivot;
    [SerializeField] private float pushForce = 10f;
    [SerializeField] private float pushRange = 2f;

    private Transform ignoreTorso;
    private Transform avatar;
    private Animator animator;
    private PlayerRagdoll ragdoll;

    public override void UseSkill()
    {
        Push();
        base.UseSkill();

        Debug.Log("Skill Used");
    }

    private void Push()
    {
        if (!player)
        {
            Debug.LogError("PushSkill: player reference is missing!");
            return;
        }

        if (!ignoreTorso)
            ignoreTorso = FindChildByName(player.transform, "Torso");

        if (!avatar)
            avatar = FindChildByName(player.transform, "Avatar");

        if (!animator)
            animator = player.GetComponent<Animator>();

        if (!ragdoll)
            ragdoll = player.GetComponent<PlayerRagdoll>();

        if (ragdoll == null) return;
        if (ragdoll.IsRagdoll || onCooldown) return;

        StartCoroutine(PushRoutine());
    }

    private IEnumerator PushRoutine()
    {
        animator.SetTrigger("Push");
        yield return new WaitForSeconds(.1f);

        Vector3 pushCenter = avatar.position + avatar.forward + pushPivot;
        Collider[] pushTargets = Physics.OverlapSphere(pushCenter, pushRange, pushableLayer);

        foreach (Collider target in pushTargets)
        {
            if (target.gameObject.name == "Torso" && target.transform != ignoreTorso)
            {
                PlayerRagdoll targetRagdoll = target.GetComponentInParent<PlayerRagdoll>();
                if (targetRagdoll != null)
                {
                    targetRagdoll.Push(avatar.forward * pushForce);
                }
            }
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
