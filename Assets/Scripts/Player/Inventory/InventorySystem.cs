using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    private List<InventorySlot> slots = new List<InventorySlot>();
    [SerializeField] private Transform slotsPanel;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private GameObject player;

    private void OnEnable()
    {
        GameEvents.GiveItem += AddItem;
    }

    private void OnDisable()
    {
        GameEvents.GiveItem -= AddItem;
    }

    private void Start()
    {
        InventoryItem[] childItems = GetComponentsInChildren<InventoryItem>(true);
        foreach (var item in childItems)
        {
            item.SetPlayer(player);
            AddItem(item);
        }
    }

    public void AddItem(InventoryItem newItem)
    {
        InventorySlot slot = null;

        if (slotsPanel && slotPrefab)
            slot = Instantiate(slotPrefab, slotsPanel).GetComponent<InventorySlot>();

        InventoryItem itemInstance;

        if (newItem.gameObject.scene.IsValid())
        {
            itemInstance = newItem;
        }
        else
        {
            GameObject itemObject = Instantiate(newItem.gameObject, transform);
            itemInstance = itemObject.GetComponent<InventoryItem>();
        }

        itemInstance.SetPlayer(player);

        if (slot)
        {
            slot.SetItem(itemInstance, this, slots.Count + 1);
            slots.Add(slot);
        }

        if (itemInstance.type == ItemType.Item)
            itemInstance.gameObject.SetActive(false);
    }

    public void DeselectAllSlots()
    {
        foreach (var slot in slots)
        {
            slot.DeselectSlot();
        }
    }

    private void Update()
    {
        UpdateSlotsCooldown();
        HandleHotkeyInput();
    }

    private void UpdateSlotsCooldown()
    {
        foreach (var slot in slots)
        {
            if (slot.currentItem != null && slot.currentItem.type == ItemType.Skill)
            {
                slot.currentItem.UpdateCooldown();
            }
        }
    }

    private void HandleHotkeyInput()
    {
        for (KeyCode key = KeyCode.Alpha0; key <= KeyCode.Alpha9; key++)
        {
            if (Input.GetKeyDown(key))
            {
                int number = key - KeyCode.Alpha0;
                if (number > 0 && number <= slots.Count && number < 10)
                {
                    InventorySlot slot = slots[number - 1];
                    if (slot != null && slot.HasItem())
                    {
                        slot.OnSlotClicked();
                    }
                }
            }
        }
    }
}
