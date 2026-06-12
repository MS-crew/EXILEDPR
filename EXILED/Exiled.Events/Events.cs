// -----------------------------------------------------------------------
// <copyright file="Events.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events
{
    using System;
    using System.Diagnostics;

    using CentralAuth;

    using Exiled.API.Enums;
    using Exiled.API.Features;
    using Exiled.API.Features.Core.UserSettings;
    using Exiled.Events.Features;

    using InventorySystem.Items.Pickups;
    using InventorySystem.Items.Usables;

    using PlayerRoles.Ragdolls;
    using PlayerRoles.RoleAssign;

    using Respawning;

    using UnityEngine.SceneManagement;

    using UserSettings.ServerSpecific;

    /// <summary>
    /// Patch and unpatch events into the game.
    /// </summary>
    public sealed class Events : Plugin<Config>
    {
        /// <summary>
        /// Gets the plugin instance.
        /// </summary>
        public static Events Instance { get; private set; }

        /// <inheritdoc/>
        public override PluginPriority Priority { get; } = PluginPriority.First;

        /// <summary>
        /// Gets the <see cref="Features.Patcher"/> used to employ all patches.
        /// </summary>
        public Patcher Patcher { get; private set; }

        /// <inheritdoc/>
        public override void OnEnabled()
        {
            Instance = this;
            base.OnEnabled();

            Stopwatch watch = Stopwatch.StartNew();

            Patch();

            watch.Stop();

            Log.Info($"{(Config.UseDynamicPatching ? "Non-event" : "All")} patches completed in {watch.Elapsed}");
            PlayerAuthenticationManager.OnInstanceModeChanged -= RoleAssigner.CheckLateJoin;

            CustomNetworkManager.OnClientStarted += Handlers.Internal.ClientStarted.OnClientStarted;
            SceneManager.sceneUnloaded += Handlers.Internal.SceneUnloaded.OnSceneUnloaded;
            MapGeneration.SeedSynchronizer.OnGenerationFinished += Handlers.Internal.MapGenerated.OnMapGenerated;
            UsableItemsController.ServerOnUsingCompleted += Handlers.Internal.Round.OnServerOnUsingCompleted;
            Handlers.Server.WaitingForPlayers += Handlers.Internal.Round.OnWaitingForPlayers;
            Handlers.Server.RestartingRound += Handlers.Internal.Round.OnRestartingRound;
            Handlers.Server.RoundStarted += Handlers.Internal.Round.OnRoundStarted;
            Handlers.Player.ChangingRole += Handlers.Internal.Round.OnChangingRole;
            Handlers.Player.SpawningRagdoll += Handlers.Internal.Round.OnSpawningRagdoll;
            Handlers.Scp049.ActivatingSense += Handlers.Internal.Round.OnActivatingSense;
            Handlers.Player.Verified += Handlers.Internal.Round.OnVerified;
            Handlers.Map.ChangedIntoGrenade += Handlers.Internal.ExplodingGrenade.OnChangedIntoGrenade;
            Handlers.Warhead.Detonated += Handlers.Internal.Round.OnWarheadDetonated;

            RoleAssigner.OnPlayersSpawned += Handlers.Server.OnAllPlayersSpawned;
            CharacterClassManager.OnRoundStarted += Handlers.Server.OnRoundStarted;
            WaveManager.OnWaveSpawned += Handlers.Server.OnRespawnedTeam;
            InventorySystem.InventoryExtensions.OnItemAdded += Handlers.Player.OnItemAdded;
            InventorySystem.InventoryExtensions.OnItemRemoved += Handlers.Player.OnItemRemoved;

            RagdollManager.OnRagdollSpawned += Handlers.Internal.RagdollList.OnSpawnedRagdoll;
            RagdollManager.OnRagdollRemoved += Handlers.Internal.RagdollList.OnRemovedRagdoll;
            ItemPickupBase.OnPickupAdded += Handlers.Internal.PickupEvent.OnSpawnedPickup;
            ItemPickupBase.OnPickupDestroyed += Handlers.Internal.PickupEvent.OnRemovedPickup;

            AdminToys.AdminToyBase.OnAdded += Handlers.Internal.AdminToyList.OnAddedAdminToys;
            AdminToys.AdminToyBase.OnRemoved += Handlers.Internal.AdminToyList.OnRemovedAdminToys;

            ServerSpecificSettingsSync.ServerOnSettingValueReceived += SettingBase.OnSettingUpdated;

            LabApi.Events.Handlers.PlayerEvents.Kicked += Handlers.Player.OnKicked;
            LabApi.Events.Handlers.PlayerEvents.ItemUsageEffectsApplying += Handlers.Player.OnUsingItemCompleted;
            LabApi.Events.Handlers.PlayerEvents.CancellingUsingItem += Handlers.Player.OnCancellingItemUse;
            LabApi.Events.Handlers.PlayerEvents.CancelledUsingItem += Handlers.Player.OnCancelledItemUse;
            LabApi.Events.Handlers.PlayerEvents.UnlockingWarheadButton += Handlers.Player.OnActivatingWarheadPanel;
            LabApi.Events.Handlers.PlayerEvents.Dying += Handlers.Player.OnDying;
            LabApi.Events.Handlers.PlayerEvents.ChangingRole += Handlers.Player.OnChangingRole;
            LabApi.Events.Handlers.PlayerEvents.ChangedRole += Handlers.Player.OnChangedRole;
            LabApi.Events.Handlers.PlayerEvents.Spawned += Handlers.Player.OnSpawned;
            LabApi.Events.Handlers.PlayerEvents.Spawning += Handlers.Player.OnSpawning;
            LabApi.Events.Handlers.PlayerEvents.DroppingItem += Handlers.Player.OnDroppingItem;
            LabApi.Events.Handlers.PlayerEvents.DroppedItem += Handlers.Player.OnDroppedItem;
            LabApi.Events.Handlers.PlayerEvents.PickingUpItem += Handlers.Player.OnPickingUpItem;
            LabApi.Events.Handlers.PlayerEvents.PickingUpAmmo += Handlers.Player.OnPickingUpItemAmmo;
            LabApi.Events.Handlers.PlayerEvents.PickingUpArmor += Handlers.Player.OnPickingUpItemArmor;
            LabApi.Events.Handlers.PlayerEvents.Cuffing += Handlers.Player.OnHandcuffing;
            LabApi.Events.Handlers.PlayerEvents.Cuffed += Handlers.Player.OnHandCuffed;
            LabApi.Events.Handlers.PlayerEvents.RoomChanged += Handlers.Player.OnRoomChanged;
            LabApi.Events.Handlers.PlayerEvents.Escaping += Handlers.Player.OnEscaping;
            LabApi.Events.Handlers.Scp106Events.TeleportingPlayer += Handlers.Player.OnEnteringPocketDimension;
            LabApi.Events.Handlers.PlayerEvents.EnteredPocketDimension += Handlers.Player.OnEnteredPocketDimension;
            LabApi.Events.Handlers.PlayerEvents.ReloadedWeapon += Handlers.Player.OnReloadedWeapon;
            LabApi.Events.Handlers.PlayerEvents.UnloadedWeapon += Handlers.Player.OnUnloadedWeapon;
            LabApi.Events.Handlers.PlayerEvents.ReloadingWeapon += Handlers.Player.OnReloadingWeapon;
            LabApi.Events.Handlers.PlayerEvents.UnloadingWeapon += Handlers.Player.OnUnloadingWeapon;
            LabApi.Events.Handlers.PlayerEvents.GroupChanging += Handlers.Player.OnChangingGroup;
            LabApi.Events.Handlers.PlayerEvents.GroupChanged += Handlers.Player.OnChangedGroup;
            LabApi.Events.Handlers.PlayerEvents.InteractingElevator += Handlers.Player.OnInteractingElevator;
            LabApi.Events.Handlers.PlayerEvents.InteractingLocker += Handlers.Player.OnInteractingLocker;
            LabApi.Events.Handlers.PlayerEvents.UpdatingEffect += Handlers.Player.OnReceivingEffect;
            LabApi.Events.Handlers.PlayerEvents.UpdatedEffect += Handlers.Player.OnReceivedEffect;
            LabApi.Events.Handlers.PlayerEvents.UsingRadio += Handlers.Player.OnUsingRadio;
            LabApi.Events.Handlers.PlayerEvents.UsedRadio += Handlers.Player.OnUsedRadio;
            LabApi.Events.Handlers.PlayerEvents.FlippingCoin += Handlers.Player.OnFlippingCoin;
            LabApi.Events.Handlers.PlayerEvents.FlippedCoin += Handlers.Player.OnFlippedCoin;
            LabApi.Events.Handlers.PlayerEvents.TogglingFlashlight += Handlers.Player.OnTogglingFlashlight;
            LabApi.Events.Handlers.PlayerEvents.TogglingWeaponFlashlight += Handlers.Player.OnTogglingWeaponFlashlight;
            LabApi.Events.Handlers.PlayerEvents.ToggledWeaponFlashlight += Handlers.Player.OnToggledWeaponFlashlight;
            LabApi.Events.Handlers.PlayerEvents.DryFiringWeapon += Handlers.Player.OnDryfiringWeapon;
            LabApi.Events.Handlers.PlayerEvents.SendingVoiceMessage += Handlers.Player.OnVoiceChatting;
            LabApi.Events.Handlers.PlayerEvents.ReceivingVoiceMessage += Handlers.Player.OnReceivingVoiceMessage;
            LabApi.Events.Handlers.PlayerEvents.TogglingNoclip += Handlers.Player.OnTogglingNoClip;
            LabApi.Events.Handlers.PlayerEvents.ToggledNoclip += Handlers.Player.OnToggledNoClip;
            LabApi.Events.Handlers.PlayerEvents.TogglingRadio += Handlers.Player.OnTogglingRadio;
            LabApi.Events.Handlers.PlayerEvents.ToggledRadio += Handlers.Player.OnToggledRadio;

            LabApi.Events.Handlers.Scp127Events.Talking += Handlers.Scp127.OnTalking;
            LabApi.Events.Handlers.Scp127Events.Talked += Handlers.Scp127.OnTalked;
            LabApi.Events.Handlers.Scp127Events.GainingExperience += Handlers.Scp127.OnGainingExperience;
            LabApi.Events.Handlers.Scp127Events.GainExperience += Handlers.Scp127.OnGainedExperience;

            LabApi.Events.Handlers.ServerEvents.ProjectileExploding += Handlers.Map.OnSpawningGrenadeEffect;

            ServerConsole.ReloadServerName();
        }

        /// <inheritdoc/>
        public override void OnDisabled()
        {
            base.OnDisabled();

            Unpatch();

            CustomNetworkManager.OnClientStarted -= Handlers.Internal.ClientStarted.OnClientStarted;
            SceneManager.sceneUnloaded -= Handlers.Internal.SceneUnloaded.OnSceneUnloaded;
            MapGeneration.SeedSynchronizer.OnGenerationFinished -= Handlers.Internal.MapGenerated.OnMapGenerated;
            UsableItemsController.ServerOnUsingCompleted -= Handlers.Internal.Round.OnServerOnUsingCompleted;
            Handlers.Server.WaitingForPlayers -= Handlers.Internal.Round.OnWaitingForPlayers;
            Handlers.Server.RestartingRound -= Handlers.Internal.Round.OnRestartingRound;
            Handlers.Server.RoundStarted -= Handlers.Internal.Round.OnRoundStarted;
            Handlers.Player.ChangingRole -= Handlers.Internal.Round.OnChangingRole;
            Handlers.Player.SpawningRagdoll -= Handlers.Internal.Round.OnSpawningRagdoll;
            Handlers.Scp049.ActivatingSense -= Handlers.Internal.Round.OnActivatingSense;
            Handlers.Player.Verified -= Handlers.Internal.Round.OnVerified;
            Handlers.Map.ChangedIntoGrenade -= Handlers.Internal.ExplodingGrenade.OnChangedIntoGrenade;

            CharacterClassManager.OnRoundStarted -= Handlers.Server.OnRoundStarted;
            RoleAssigner.OnPlayersSpawned -= Handlers.Server.OnAllPlayersSpawned;
            InventorySystem.InventoryExtensions.OnItemAdded -= Handlers.Player.OnItemAdded;
            InventorySystem.InventoryExtensions.OnItemRemoved -= Handlers.Player.OnItemRemoved;
            WaveManager.OnWaveSpawned -= Handlers.Server.OnRespawnedTeam;
            RagdollManager.OnRagdollSpawned -= Handlers.Internal.RagdollList.OnSpawnedRagdoll;
            RagdollManager.OnRagdollRemoved -= Handlers.Internal.RagdollList.OnRemovedRagdoll;
            ItemPickupBase.OnPickupAdded -= Handlers.Internal.PickupEvent.OnSpawnedPickup;
            ItemPickupBase.OnPickupDestroyed -= Handlers.Internal.PickupEvent.OnRemovedPickup;

            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= SettingBase.OnSettingUpdated;

            LabApi.Events.Handlers.PlayerEvents.Kicked -= Handlers.Player.OnKicked;
            LabApi.Events.Handlers.PlayerEvents.ItemUsageEffectsApplying -= Handlers.Player.OnUsingItemCompleted;
            LabApi.Events.Handlers.PlayerEvents.CancellingUsingItem -= Handlers.Player.OnCancellingItemUse;
            LabApi.Events.Handlers.PlayerEvents.CancelledUsingItem -= Handlers.Player.OnCancelledItemUse;
            LabApi.Events.Handlers.PlayerEvents.UnlockingWarheadButton -= Handlers.Player.OnActivatingWarheadPanel;
            LabApi.Events.Handlers.PlayerEvents.Dying -= Handlers.Player.OnDying;
            LabApi.Events.Handlers.PlayerEvents.ChangingRole -= Handlers.Player.OnChangingRole;
            LabApi.Events.Handlers.PlayerEvents.ChangedRole -= Handlers.Player.OnChangedRole;
            LabApi.Events.Handlers.PlayerEvents.Spawned -= Handlers.Player.OnSpawned;
            LabApi.Events.Handlers.PlayerEvents.Spawning -= Handlers.Player.OnSpawning;
            LabApi.Events.Handlers.PlayerEvents.DroppingItem -= Handlers.Player.OnDroppingItem;
            LabApi.Events.Handlers.PlayerEvents.DroppedItem -= Handlers.Player.OnDroppedItem;
            LabApi.Events.Handlers.PlayerEvents.PickingUpItem -= Handlers.Player.OnPickingUpItem;
            LabApi.Events.Handlers.PlayerEvents.PickingUpAmmo -= Handlers.Player.OnPickingUpItemAmmo;
            LabApi.Events.Handlers.PlayerEvents.PickingUpArmor -= Handlers.Player.OnPickingUpItemArmor;
            LabApi.Events.Handlers.PlayerEvents.Cuffing -= Handlers.Player.OnHandcuffing;
            LabApi.Events.Handlers.PlayerEvents.Cuffed -= Handlers.Player.OnHandCuffed;
            LabApi.Events.Handlers.PlayerEvents.RoomChanged -= Handlers.Player.OnRoomChanged;
            LabApi.Events.Handlers.PlayerEvents.Escaping -= Handlers.Player.OnEscaping;
            LabApi.Events.Handlers.Scp106Events.TeleportingPlayer -= Handlers.Player.OnEnteringPocketDimension;
            LabApi.Events.Handlers.PlayerEvents.EnteredPocketDimension -= Handlers.Player.OnEnteredPocketDimension;
            LabApi.Events.Handlers.PlayerEvents.ReloadedWeapon -= Handlers.Player.OnReloadedWeapon;
            LabApi.Events.Handlers.PlayerEvents.UnloadedWeapon -= Handlers.Player.OnUnloadedWeapon;
            LabApi.Events.Handlers.PlayerEvents.ReloadingWeapon -= Handlers.Player.OnReloadingWeapon;
            LabApi.Events.Handlers.PlayerEvents.UnloadingWeapon -= Handlers.Player.OnUnloadingWeapon;
            LabApi.Events.Handlers.PlayerEvents.GroupChanging -= Handlers.Player.OnChangingGroup;
            LabApi.Events.Handlers.PlayerEvents.GroupChanged -= Handlers.Player.OnChangedGroup;
            LabApi.Events.Handlers.PlayerEvents.InteractingElevator -= Handlers.Player.OnInteractingElevator;
            LabApi.Events.Handlers.PlayerEvents.InteractingLocker -= Handlers.Player.OnInteractingLocker;
            LabApi.Events.Handlers.PlayerEvents.UpdatingEffect -= Handlers.Player.OnReceivingEffect;
            LabApi.Events.Handlers.PlayerEvents.UpdatedEffect -= Handlers.Player.OnReceivedEffect;
            LabApi.Events.Handlers.PlayerEvents.UsingRadio -= Handlers.Player.OnUsingRadio;
            LabApi.Events.Handlers.PlayerEvents.UsedRadio -= Handlers.Player.OnUsedRadio;
            LabApi.Events.Handlers.PlayerEvents.FlippingCoin -= Handlers.Player.OnFlippingCoin;
            LabApi.Events.Handlers.PlayerEvents.FlippedCoin -= Handlers.Player.OnFlippedCoin;
            LabApi.Events.Handlers.PlayerEvents.TogglingFlashlight -= Handlers.Player.OnTogglingFlashlight;
            LabApi.Events.Handlers.PlayerEvents.TogglingWeaponFlashlight -= Handlers.Player.OnTogglingWeaponFlashlight;
            LabApi.Events.Handlers.PlayerEvents.ToggledWeaponFlashlight -= Handlers.Player.OnToggledWeaponFlashlight;
            LabApi.Events.Handlers.PlayerEvents.DryFiringWeapon -= Handlers.Player.OnDryfiringWeapon;
            LabApi.Events.Handlers.PlayerEvents.SendingVoiceMessage -= Handlers.Player.OnVoiceChatting;
            LabApi.Events.Handlers.PlayerEvents.ReceivingVoiceMessage -= Handlers.Player.OnReceivingVoiceMessage;
            LabApi.Events.Handlers.PlayerEvents.TogglingNoclip -= Handlers.Player.OnTogglingNoClip;
            LabApi.Events.Handlers.PlayerEvents.ToggledNoclip -= Handlers.Player.OnToggledNoClip;
            LabApi.Events.Handlers.PlayerEvents.TogglingRadio -= Handlers.Player.OnTogglingRadio;
            LabApi.Events.Handlers.PlayerEvents.ToggledRadio -= Handlers.Player.OnToggledRadio;

            LabApi.Events.Handlers.Scp127Events.Talking -= Handlers.Scp127.OnTalking;
            LabApi.Events.Handlers.Scp127Events.Talked -= Handlers.Scp127.OnTalked;
            LabApi.Events.Handlers.Scp127Events.GainingExperience -= Handlers.Scp127.OnGainingExperience;
            LabApi.Events.Handlers.Scp127Events.GainExperience -= Handlers.Scp127.OnGainedExperience;

            LabApi.Events.Handlers.ServerEvents.ProjectileExploding -= Handlers.Map.OnSpawningGrenadeEffect;
        }

        /// <summary>
        /// Patches all events.
        /// </summary>
        public void Patch()
        {
            try
            {
                Patcher = new Patcher();
#if DEBUG
                bool lastDebugStatus = HarmonyLib.Harmony.DEBUG;
                HarmonyLib.Harmony.DEBUG = true;
#endif
                Patcher.PatchAll(!Config.UseDynamicPatching, out int failedPatch);

                if (failedPatch == 0)
                    Log.Debug("Events patched successfully!");
                else
                    Log.Error($"Patching failed! There are {failedPatch} broken patches.");
#if DEBUG
                HarmonyLib.Harmony.DEBUG = lastDebugStatus;
#endif
            }
            catch (Exception exception)
            {
                Log.Error($"Patching failed!\n{exception}");
            }
        }

        /// <summary>
        /// Unpatches all events.
        /// </summary>
        public void Unpatch()
        {
            Log.Debug("Unpatching events...");
            Patcher.UnpatchAll();
            Patcher = null;
            Log.Debug("All events have been unpatched complete. Goodbye!");
        }
    }
}