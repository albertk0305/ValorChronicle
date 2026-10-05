using System;
using UnityEngine;

namespace ValorChronicle.Characters.Presentation
{
    public sealed class CharacterScreenCoordinator : MonoBehaviour
    {
        private GameObject rosterRoot;
        private GameObject upgradeRoot;
        private CharacterRosterScreenController rosterController;
        private CharacterUpgradeScreenController upgradeController;
        private bool subscribed;

        public bool IsShowingUpgrade =>
            upgradeRoot != null && upgradeRoot.activeSelf;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            GameObject configuredRosterRoot,
            GameObject configuredUpgradeRoot,
            CharacterRosterScreenController configuredRosterController,
            CharacterUpgradeScreenController configuredUpgradeController)
        {
            Unsubscribe();
            rosterRoot = configuredRosterRoot
                ?? throw new ArgumentNullException(
                    nameof(configuredRosterRoot));
            upgradeRoot = configuredUpgradeRoot
                ?? throw new ArgumentNullException(
                    nameof(configuredUpgradeRoot));
            rosterController = configuredRosterController
                ?? throw new ArgumentNullException(
                    nameof(configuredRosterController));
            upgradeController = configuredUpgradeController
                ?? throw new ArgumentNullException(
                    nameof(configuredUpgradeController));
            if (ReferenceEquals(rosterRoot, upgradeRoot))
            {
                throw new ArgumentException(
                    "Roster and upgrade roots must be different objects.");
            }

            if (isActiveAndEnabled)
            {
                Subscribe();
            }

            ShowRoster();
        }

        public void ShowRoster()
        {
            if (rosterRoot == null || upgradeRoot == null)
            {
                return;
            }

            upgradeRoot.SetActive(false);
            rosterRoot.SetActive(true);
        }

        private void HandleSelectedCharacterChanged(string characterId)
        {
            if (!upgradeController.TryPresent(characterId))
            {
                ShowRoster();
                return;
            }

            rosterRoot.SetActive(false);
            upgradeRoot.SetActive(true);
        }

        private void HandleReturnRequested()
        {
            ShowRoster();
            rosterController.RefreshFromAuthoritativeProfile(
                resetControls: false);
        }

        private void Subscribe()
        {
            if (subscribed
                || rosterController == null
                || upgradeController == null)
            {
                return;
            }

            rosterController.SelectedCharacterChanged +=
                HandleSelectedCharacterChanged;
            upgradeController.ReturnRequested += HandleReturnRequested;
            upgradeController.AuthoritativeProfileRefreshRequested +=
                HandleAuthoritativeProfileRefreshRequested;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            rosterController.SelectedCharacterChanged -=
                HandleSelectedCharacterChanged;
            upgradeController.ReturnRequested -= HandleReturnRequested;
            upgradeController.AuthoritativeProfileRefreshRequested -=
                HandleAuthoritativeProfileRefreshRequested;
            subscribed = false;
        }

        private void HandleAuthoritativeProfileRefreshRequested()
        {
            rosterController.RefreshFromAuthoritativeProfile(
                resetControls: false);
        }
    }
}
