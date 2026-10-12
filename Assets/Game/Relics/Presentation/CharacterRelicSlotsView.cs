using System;
using System.Collections.Generic;
using UnityEngine;
using ValorChronicle.Save.Rules;

namespace ValorChronicle.Relics.Presentation
{
    public sealed class CharacterRelicSlotsView : MonoBehaviour
    {
        private CharacterRelicSlotView[] slots =
            Array.Empty<CharacterRelicSlotView>();
        private bool listenersBound;

        public event Action<int> SlotRequested;

        public IReadOnlyList<CharacterRelicSlotView> Slots => slots;

        public void Configure(CharacterRelicSlotView[] configuredSlots)
        {
            if (configuredSlots == null)
            {
                throw new ArgumentNullException(nameof(configuredSlots));
            }

            if (configuredSlots.Length
                != SaveRules.RelicSlotCountPerCharacter)
            {
                throw new ArgumentException(
                    "Character relic UI requires exactly four slots.",
                    nameof(configuredSlots));
            }

            UnbindListeners();
            slots = (CharacterRelicSlotView[])configuredSlots.Clone();
            for (int index = 0; index < slots.Length; index++)
            {
                if (slots[index] == null)
                {
                    throw new ArgumentException(
                        $"Relic slot {index} is missing.",
                        nameof(configuredSlots));
                }

                if (slots[index].SlotIndex != index)
                {
                    throw new ArgumentException(
                        $"Relic slot {index} has an unexpected index.",
                        nameof(configuredSlots));
                }
            }

            BindListeners();
        }

        public void Render(IReadOnlyList<RelicPresentationModel> models)
        {
            if (models == null)
            {
                throw new ArgumentNullException(nameof(models));
            }

            if (models.Count != slots.Length)
            {
                throw new ArgumentException(
                    "Relic presentation count must match the slot count.",
                    nameof(models));
            }

            for (int index = 0; index < slots.Length; index++)
            {
                slots[index].Render(models[index]);
            }
        }

        public void Clear()
        {
            for (int index = 0; index < slots.Length; index++)
            {
                slots[index].Render(RelicPresentationModel.Empty());
            }
        }

        private void OnDestroy()
        {
            UnbindListeners();
        }

        private void BindListeners()
        {
            if (listenersBound)
            {
                return;
            }

            for (int index = 0; index < slots.Length; index++)
            {
                slots[index].Requested += HandleSlotRequested;
            }

            listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            for (int index = 0; index < slots.Length; index++)
            {
                slots[index].Requested -= HandleSlotRequested;
            }

            listenersBound = false;
        }

        private void HandleSlotRequested(int slotIndex)
        {
            SlotRequested?.Invoke(slotIndex);
        }
    }
}
