using Microsoft.Xna.Framework.Input;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityOverhaul.Content.UIs.StorageUIs
{
    /// <summary>
    /// 通用箱子UI交互逻辑，处理所有槽位的鼠标交互
    /// 通过 <see cref="IChestStorage"/> 接口统一不同存储模式
    /// </summary>
    internal class ChestInteraction
    {
        private readonly Player player;
        private readonly IChestStorage storage;

        private const int SlotSize = 32;
        private const int SlotPadding = 4;

        //交互状态
        public int HoveredSlot { get; private set; } = -1;

        //关闭按钮
        public bool IsCloseButtonHovered { get; private set; } = false;
        public const int CloseButtonSize = 32;

        //音效冷却
        private int soundCooldown = 0;
        private const int SoundCooldownMax = 15;
        private int lastQuickTransferSlot = -1;

        public ChestInteraction(Player player, IChestStorage storage) {
            this.player = player;
            this.storage = storage;
        }

        /// <summary>
        /// 更新关闭按钮悬停
        /// </summary>
        public bool UpdateCloseButton(Point mousePoint, Vector2 panelPosition, int panelWidth, bool mouseLeftRelease) {
            Rectangle buttonRect = new Rectangle(
                (int)(panelPosition.X + panelWidth - CloseButtonSize - 10),
                (int)(panelPosition.Y + 10),
                CloseButtonSize,
                CloseButtonSize
            );

            IsCloseButtonHovered = buttonRect.Contains(mousePoint);
            return IsCloseButtonHovered && mouseLeftRelease;
        }

        /// <summary>
        /// 更新槽位交互
        /// </summary>
        public void UpdateSlotInteraction(Point mousePoint, Vector2 storageStartPos,
            bool leftPressed, bool leftHeld, bool rightPressed, bool rightHeld) {
            if (soundCooldown > 0) {
                soundCooldown--;
            }

            HoveredSlot = -1;

            int slotsPerRow = storage.SlotsPerRow;
            int slotRows = storage.SlotRows;

            for (int row = 0; row < slotRows; row++) {
                for (int col = 0; col < slotsPerRow; col++) {
                    int index = row * slotsPerRow + col;
                    Rectangle slotRect = new Rectangle(
                        (int)(storageStartPos.X + col * (SlotSize + SlotPadding)),
                        (int)(storageStartPos.Y + row * (SlotSize + SlotPadding)),
                        SlotSize,
                        SlotSize
                    );

                    if (slotRect.Contains(mousePoint)) {
                        HoveredSlot = index;
                        break;
                    }
                }
                if (HoveredSlot != -1) break;
            }

            if (HoveredSlot == -1) {
                lastQuickTransferSlot = -1;
                return;
            }

            Item slotItem = storage.GetItem(HoveredSlot);
            if (slotItem != null && slotItem.type > ItemID.None && slotItem.stack > 0) {
                Main.HoverItem = slotItem;
                Main.hoverItemName = slotItem.Name;
            }

            KeyboardState keyboard = Keyboard.GetState();
            bool shiftPressed = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);

            if (shiftPressed && leftPressed) {
                QuickTransferToInventory(HoveredSlot);
                return;
            }

            if (!shiftPressed) {
                lastQuickTransferSlot = -1;
            }

            if (leftPressed) {
                HandleLeftClick(slotItem);
            }

            if (rightPressed) {
                HandleRightClick(slotItem);
            }

            if (rightHeld) {
                HandleDragPlace(slotItem);
            }

            if (shiftPressed && Main.mouseItem.type == ItemID.None) {
                GatherSameItems(HoveredSlot);
            }
        }

        private void HandleLeftClick(Item slotItem) {
            if (Main.mouseItem.type == ItemID.None) {
                if (slotItem != null && slotItem.type > ItemID.None) {
                    Main.mouseItem = slotItem.Clone();
                    storage.SetItem(HoveredSlot, new Item());
                    PlaySound(SoundID.Grab);
                }
            }
            else {
                if (slotItem == null || slotItem.type == ItemID.None) {
                    storage.SetItem(HoveredSlot, Main.mouseItem.Clone());
                    Main.mouseItem.TurnToAir();
                    PlaySound(SoundID.Grab);
                }
                else if (slotItem.type == Main.mouseItem.type && slotItem.stack < slotItem.maxStack) {
                    int spaceLeft = slotItem.maxStack - slotItem.stack;
                    int amountToAdd = Math.Min(spaceLeft, Main.mouseItem.stack);

                    slotItem.stack += amountToAdd;
                    Main.mouseItem.stack -= amountToAdd;

                    if (Main.mouseItem.stack <= 0) {
                        Main.mouseItem.TurnToAir();
                    }

                    storage.SetItem(HoveredSlot, slotItem);
                    PlaySound(SoundID.Grab);
                }
                else {
                    Item temp = slotItem.Clone();
                    storage.SetItem(HoveredSlot, Main.mouseItem.Clone());
                    Main.mouseItem = temp;
                    PlaySound(SoundID.Grab);
                }
            }
        }

        private void HandleRightClick(Item slotItem) {
            if (Main.mouseItem.type == ItemID.None) {
                if (slotItem != null && slotItem.type > ItemID.None) {
                    int halfStack = (slotItem.stack + 1) / 2;
                    Main.mouseItem = slotItem.Clone();
                    Main.mouseItem.stack = halfStack;
                    slotItem.stack -= halfStack;

                    if (slotItem.stack <= 0) {
                        storage.SetItem(HoveredSlot, new Item());
                    }
                    else {
                        storage.SetItem(HoveredSlot, slotItem);
                    }

                    PlaySound(SoundID.Grab, 0.1f);
                }
            }
            else {
                if (slotItem == null || slotItem.type == ItemID.None) {
                    Item newItem = Main.mouseItem.Clone();
                    newItem.stack = 1;
                    storage.SetItem(HoveredSlot, newItem);
                    Main.mouseItem.stack--;

                    if (Main.mouseItem.stack <= 0) {
                        Main.mouseItem.TurnToAir();
                    }

                    PlaySound(SoundID.Grab, 0.1f);
                }
                else if (slotItem.type == Main.mouseItem.type && slotItem.stack < slotItem.maxStack) {
                    slotItem.stack++;
                    Main.mouseItem.stack--;

                    if (Main.mouseItem.stack <= 0) {
                        Main.mouseItem.TurnToAir();
                    }

                    storage.SetItem(HoveredSlot, slotItem);
                    PlaySound(SoundID.Grab, 0.1f);
                }
            }
        }

        private void HandleDragPlace(Item slotItem) {
            if (Main.mouseItem.type == ItemID.None) return;

            if (slotItem == null || slotItem.type == ItemID.None) {
                Item newItem = Main.mouseItem.Clone();
                newItem.stack = 1;
                storage.SetItem(HoveredSlot, newItem);
                Main.mouseItem.stack--;

                if (Main.mouseItem.stack <= 0) {
                    Main.mouseItem.TurnToAir();
                }
            }
            else if (slotItem.type == Main.mouseItem.type && slotItem.stack < slotItem.maxStack) {
                slotItem.stack++;
                Main.mouseItem.stack--;

                if (Main.mouseItem.stack <= 0) {
                    Main.mouseItem.TurnToAir();
                }

                storage.SetItem(HoveredSlot, slotItem);
            }
        }

        private void QuickTransferToInventory(int slotIndex) {
            int totalSlots = storage.TotalSlots;
            if (slotIndex < 0 || slotIndex >= totalSlots) return;

            Item item = storage.GetItem(slotIndex);
            if (item == null || item.type <= ItemID.None || item.stack <= 0) return;

            Item leftover = player.GetItem(player.whoAmI, item.Clone(),
                GetItemSettings.InventoryUIToInventorySettings);

            bool success = false;
            bool partialSuccess = false;

            if (leftover == null || leftover.stack == 0) {
                storage.SetItem(slotIndex, new Item());
                success = true;
            }
            else if (leftover.stack < item.stack) {
                item.stack = leftover.stack;
                storage.SetItem(slotIndex, item);
                partialSuccess = true;
            }

            if ((success || partialSuccess) && CanPlaySound()) {
                PlayQuickTransferSound(success ? 0f : -0.2f);
            }

            if (success || partialSuccess) {
                lastQuickTransferSlot = slotIndex;
            }
        }

        private void GatherSameItems(int targetSlot) {
            Item targetItem = storage.GetItem(targetSlot);
            if (targetItem == null || targetItem.type == ItemID.None || targetItem.stack >= targetItem.maxStack) {
                return;
            }

            bool gathered = false;
            int totalSlots = storage.TotalSlots;

            for (int i = 0; i < totalSlots; i++) {
                if (i == targetSlot) continue;
                if (targetItem.stack >= targetItem.maxStack) break;

                Item otherItem = storage.GetItem(i);
                if (otherItem != null && otherItem.type == targetItem.type) {
                    int spaceLeft = targetItem.maxStack - targetItem.stack;
                    int amountToTransfer = Math.Min(spaceLeft, otherItem.stack);

                    targetItem.stack += amountToTransfer;
                    otherItem.stack -= amountToTransfer;

                    if (otherItem.stack <= 0) {
                        storage.SetItem(i, new Item());
                    }
                    else {
                        storage.SetItem(i, otherItem);
                    }

                    gathered = true;
                }
            }

            if (gathered) {
                storage.SetItem(targetSlot, targetItem);
                PlaySound(SoundID.Grab);
            }
        }

        private void PlaySound(SoundStyle sound, float pitch = 0f) {
            if (CanPlaySound()) {
                SoundEngine.PlaySound(sound with { Pitch = pitch });
                soundCooldown = SoundCooldownMax;
            }
        }

        private void PlayQuickTransferSound(float pitch = 0f) {
            SoundEngine.PlaySound(SoundID.Grab with { Pitch = pitch });
            soundCooldown = SoundCooldownMax;
        }

        private bool CanPlaySound() => soundCooldown <= 0;

        /// <summary>
        /// 重置交互状态
        /// </summary>
        public void Reset() {
            HoveredSlot = -1;
            IsCloseButtonHovered = false;
            soundCooldown = 0;
            lastQuickTransferSlot = -1;
        }
    }
}
