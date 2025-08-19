using DG.Tweening;
using UnityEngine;

public class Door : ProximityPromptBase
{
    [SerializeField] private Transform doorRoot;

    protected override void PromptTriggered(GameObject instigator)
    {
        Vector3 toPlayer = instigator.transform.position - doorRoot.position;
        float dot = Vector3.Dot(doorRoot.forward, toPlayer);

        float doorOpenAngle = dot > 0 ? 90f : -90f;
        Vector3 doorRotation = toggleState ? new Vector3(0, doorOpenAngle, 0) : Vector3.zero;
        doorRoot.DOLocalRotate(doorRotation, 1f);
    }
}
