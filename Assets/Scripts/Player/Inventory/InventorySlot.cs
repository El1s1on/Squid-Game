using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemIDText;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private Button slotButton;
    [SerializeField] private Image slotBackground;

    public InventoryItem currentItem { get; private set; }
    public bool isSelected { get; private set; } = false;

    private InventorySystem inventorySystem;
    private Color defaultColor;

    private void Awake()
    {
        slotButton.onClick.AddListener(OnSlotClicked);
        defaultColor = slotBackground.color;
    }

    public void SetItem(InventoryItem item, InventorySystem inventory, int id)
    {
        inventorySystem = inventory;
        currentItem = item;
        itemNameText.text = item.itemName;
        itemIDText.text = id.ToString();
    }

    private void Update()
    {
        UpdateCooldownOverlay();
    }

    private void UpdateCooldownOverlay()
    {
        if (currentItem != null && currentItem.type == ItemType.Skill && currentItem.onCooldown)
        {
            cooldownOverlay.fillAmount = currentItem.currentCooldown / currentItem.cooldown;
        }
        else
        {
            if(currentItem.type == ItemType.Skill)
                slotBackground.color = defaultColor;

            cooldownOverlay.fillAmount = 0;
        }
    }

    public void OnSlotClicked()
    {
        if (currentItem == null) return;

        if (currentItem.type == ItemType.Skill)
        {
            currentItem.UseSkill();
            inventorySystem.DeselectAllSlots();
            slotBackground.color = selectedColor;
            return;
        }

        ToggleSelection();
    }

    public void ToggleSelection()
    {
        if (isSelected)
            DeselectSlot();
        else
            SelectSlot();
    }

    public void SelectSlot()
    {
        if (currentItem == null || currentItem.type == ItemType.Skill) return;

        inventorySystem.DeselectAllSlots();

        isSelected = true;
        slotBackground.color = selectedColor;
        currentItem.Equip();
    }

    public void DeselectSlot()
    {
        isSelected = false;
        
        if (currentItem != null && currentItem.type == ItemType.Item)
        {
            slotBackground.color = defaultColor;
            currentItem.Unequip();
        }
    }

    public bool HasItem()
    {
        return currentItem != null;
    }
}