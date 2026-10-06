using System;
using UnityEngine;

namespace ValorChronicle.Characters.Presentation
{
    public sealed class CharacterScreenCoordinator : MonoBehaviour
    {
        private GameObject rosterRoot;
        private GameObject upgradeRoot;
        private GameObject lookupRoot;
        private CharacterRosterScreenController rosterController;
        private CharacterUpgradeScreenController upgradeController;
        private CharacterLookupScreenController lookupController;
        private bool subscribed;

        public bool IsShowingUpgrade =>
            upgradeRoot != null && upgradeRoot.activeSelf;
        public bool IsShowingLookup =>
            lookupRoot != null && lookupRoot.activeSelf;

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
            CharacterUpgradeScreenController configuredUpgradeController,
            GameObject configuredLookupRoot,
            CharacterLookupScreenController configuredLookupController)
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
            lookupRoot = configuredLookupRoot
                ?? throw new ArgumentNullException(
                    nameof(configuredLookupRoot));
            lookupController = configuredLookupController
                ?? throw new ArgumentNullException(
                    nameof(configuredLookupController));
            if (ReferenceEquals(rosterRoot, upgradeRoot)
                || ReferenceEquals(rosterRoot, lookupRoot)
                || ReferenceEquals(upgradeRoot, lookupRoot))
            {
                throw new ArgumentException(
                    "Roster, upgrade, and lookup roots must be different "
                        + "objects.");
            }

            if (isActiveAndEnabled)
            {
                Subscribe();
            }

            ShowRoster();
        }

        public void ShowRoster()
        {
            if (rosterRoot == null
                || upgradeRoot == null
                || lookupRoot == null)
            {
                return;
            }

            lookupRoot.SetActive(false);
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
            lookupRoot.SetActive(false);
            upgradeRoot.SetActive(true);
        }

        private void HandleReturnRequested()
        {
            ShowRoster();
            rosterController.RefreshFromAuthoritativeProfile(
                resetControls: false);
        }

        private void HandleAwakeningLookupRequested(string characterId)
        {
            ShowLookup(characterId, CharacterLookupMode.Awakening);
        }

        private void HandleSkillsLookupRequested(string characterId)
        {
            ShowLookup(characterId, CharacterLookupMode.Skills);
        }

        private void ShowLookup(
            string characterId,
            CharacterLookupMode mode)
        {
            if (!lookupController.TryOpen(characterId, mode))
            {
                return;
            }

            rosterRoot.SetActive(false);
            upgradeRoot.SetActive(false);
            lookupRoot.SetActive(true);
        }

        private void HandleLookupReturnRequested()
        {
            rosterRoot.SetActive(false);
            lookupRoot.SetActive(false);
            upgradeRoot.SetActive(true);
        }

        private void Subscribe()
        {
            if (subscribed
                || rosterController == null
                || upgradeController == null
                || lookupController == null)
            {
                return;
            }

            rosterController.SelectedCharacterChanged +=
                HandleSelectedCharacterChanged;
            upgradeController.ReturnRequested += HandleReturnRequested;
            upgradeController.AuthoritativeProfileRefreshRequested +=
                HandleAuthoritativeProfileRefreshRequested;
            upgradeController.AwakeningLookupRequested +=
                HandleAwakeningLookupRequested;
            upgradeController.SkillsLookupRequested +=
                HandleSkillsLookupRequested;
            lookupController.ReturnRequested += HandleLookupReturnRequested;
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
            upgradeController.AwakeningLookupRequested -=
                HandleAwakeningLookupRequested;
            upgradeController.SkillsLookupRequested -=
                HandleSkillsLookupRequested;
            lookupController.ReturnRequested -= HandleLookupReturnRequested;
            subscribed = false;
        }

        private void HandleAuthoritativeProfileRefreshRequested()
        {
            rosterController.RefreshFromAuthoritativeProfile(
                resetControls: false);
        }
    }
}
