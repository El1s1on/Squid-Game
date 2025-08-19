using UnityEngine;

public enum ItemType
{
    Item,
    Skill
}

public class InventoryItem : MonoBehaviour
{
    [Header("Item Base")]
    public ItemType type;
    public string itemName;
    public float cooldown;
    [HideInInspector] public float currentCooldown;
    [HideInInspector] public bool onCooldown;

    protected GameObject player;
    public bool isBot;

    public void SetPlayer(GameObject player)
    {
        if (player.GetComponent<BotBase>())
            isBot = true;

        this.player = player;
    }

    public virtual void UseSkill()
    {
        if (onCooldown) return;

        onCooldown = true;
        currentCooldown = cooldown;
    }

    public virtual void Equip()
    {
        gameObject.SetActive(true);
    }

    public virtual void Unequip()
    {
        gameObject.SetActive(false);
    }

    public void UpdateCooldown()
    {
        if (onCooldown)
        {
            currentCooldown -= Time.deltaTime;
            if (currentCooldown <= 0)
            {
                onCooldown = false;
                currentCooldown = 0;
            }
        }
    }
}
