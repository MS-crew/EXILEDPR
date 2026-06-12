// -----------------------------------------------------------------------
// <copyright file="Player.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Exiled.API.Enums;
    using Exiled.API.Features;
    using Exiled.API.Features.Items;
    using Exiled.API.Features.Pools;
    using Exiled.API.Features.Roles;
#pragma warning disable IDE0079
#pragma warning disable IDE0060
#pragma warning disable SA1623 // Property summary documentation should match accessors

    using Exiled.Events.EventArgs.Player;
    using Exiled.Events.Features;

    using InventorySystem;
    using InventorySystem.Items;
    using InventorySystem.Items.Pickups;
    using InventorySystem.Items.Usables.Scp1344;

    using LabApi.Events.Arguments.PlayerEvents;
    using LabApi.Events.Arguments.Scp106Events;
    using LabApi.Events.Arguments.ServerEvents;
    using LabApi.Events.Handlers;

    using PlayerRoles;

    using static InventorySystem.Items.Radio.RadioMessages;

    /// <summary>
    /// Player related events.
    /// </summary>
    public class Player
    {
        /// <summary><inheritdoc/></summary>
        internal static readonly Dictionary<API.Features.Player, ChangingRoleEventArgs> CachedChangingRoleEvents = new();

        /// <summary>
        /// Invoked after a player triggers the attack as an SCP.
        /// </summary>
        public static Event<HitEventArgs> Hit { get; set; } = new();

        /// <summary>
        /// Invoked before authenticating a <see cref="API.Features.Player"/>.
        /// </summary>
        public static Event<PreAuthenticatingEventArgs> PreAuthenticating { get; set; } = new();

        /// <summary>
        /// Invoked before reserved slot is finalized for a <see cref="API.Features.Player"/>.
        /// </summary>
        public static Event<ReservedSlotsCheckEventArgs> ReservedSlot { get; set; } = new();

        /// <summary>
        /// Invoked before kicking a <see cref="API.Features.Player"/> from the server.
        /// </summary>
        public static Event<KickingEventArgs> Kicking { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> has been kicked from the server.
        /// </summary>
        public static Event<KickedEventArgs> Kicked { get; set; } = new();

        /// <summary>
        /// Invoked before banning a <see cref="API.Features.Player"/> from the server.
        /// </summary>
        public static Event<BanningEventArgs> Banning { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> has been banned from the server.
        /// </summary>
        public static Event<BannedEventArgs> Banned { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> is changing danger state.
        /// </summary>
        public static Event<ChangingDangerStateEventArgs> ChangingDangerState { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> earns an achievement.
        /// </summary>
        /// <remarks>
        /// Will not fire for certain client-side achievements.
        /// </remarks>
        public static Event<EarningAchievementEventArgs> EarningAchievement { get; set; } = new();

        /// <summary>
        /// Invoked before the player starts to use an <see cref="API.Features.Items.Usable"/>. In other words, it is invoked just before the animation starts.
        /// </summary>
        /// <remarks>
        /// Will be invoked even if the <see cref="API.Features.Items.Usable"/> is on cooldown.
        /// Candies are the only <see cref="API.Features.Items.Usable"/> that do not invoke this event.
        /// </remarks>
        public static Event<UsingItemEventArgs> UsingItem { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> finishes using a <see cref="API.Features.Items.Usable"/>. In other words, it is invoked after the animation finishes but before the <see cref="API.Features.Items.Usable"/> is actually used.
        /// </summary>
        public static Event<UsingItemCompletedEventArgs> UsingItemCompleted { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> uses an <see cref="API.Features.Items.Usable"/>.
        /// </summary>
        /// <remarks>
        /// Invoked after <see cref="UsingItem"/>, if a player's class has
        /// changed during their health increase, won't fire.
        /// </remarks>
        public static Event<UsedItemEventArgs> UsedItem { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> consumes an <see cref="API.Features.Items.Consumable"/>. In other words, it is invoked before the consumable item logic are applied.
        /// </summary>
        public static Event<ConsumingItemEventArgs> ConsumingItem { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> has stopped the use of a <see cref="API.Features.Items.Usable"/>.
        /// </summary>
        public static Event<CancellingItemUseEventArgs> CancellingItemUse { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> has stopped the use of a <see cref="API.Features.Items.Usable"/>.
        /// </summary>
        public static Event<CancelledItemUseEventArgs> CancelledItemUse { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/>'s aspect ratio has changed.
        /// </summary>
        public static Event<ChangedRatioEventArgs> ChangedRatio { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> interacted with something.
        /// </summary>
        public static Event<InteractedEventArgs> Interacted { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> is saved from death by the Anti-SCP-207 effect.
        /// </summary>
        public static Event<SavingByAntiScp207EventArgs> SavingByAntiScp207 { get; set; } = new();

        /// <summary>
        /// Invoked before spawning a <see cref="API.Features.Player"/> <see cref="API.Features.Ragdoll"/>.
        /// </summary>
        public static Event<SpawningRagdollEventArgs> SpawningRagdoll { get; set; } = new();

        /// <summary>
        /// Invoked after spawning a <see cref="API.Features.Player"/> <see cref="API.Features.Ragdoll"/>.
        /// </summary>
        public static Event<SpawnedRagdollEventArgs> SpawnedRagdoll { get; set; } = new();

        /// <summary>
        /// Invoked before activating the warhead panel.
        /// </summary>
        public static Event<ActivatingWarheadPanelEventArgs> ActivatingWarheadPanel { get; set; } = new();

        /// <summary>
        /// Invoked before activating a workstation.
        /// </summary>
        public static Event<ActivatingWorkstationEventArgs> ActivatingWorkstation { get; set; } = new();

        /// <summary>
        /// Invoked before deactivating a workstation.
        /// </summary>
        public static Event<DeactivatingWorkstationEventArgs> DeactivatingWorkstation { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> has joined the server.
        /// </summary>
        public static Event<JoinedEventArgs> Joined { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> has been verified.
        /// </summary>
        public static Event<VerifiedEventArgs> Verified { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> has left the server.
        /// </summary>
        public static Event<LeftEventArgs> Left { get; set; } = new();

        /// <summary>
        /// Invoked before destroying a <see cref="API.Features.Player"/>.
        /// </summary>
        public static Event<DestroyingEventArgs> Destroying { get; set; } = new();

        /// <summary>
        /// Invoked before hurting a <see cref="API.Features.Player"/>.
        /// </summary>
        public static Event<HurtingEventArgs> Hurting { get; set; } = new();

        /// <summary>
        /// Invoked after hurting a <see cref="API.Features.Player"/>.
        /// </summary>
        public static Event<HurtEventArgs> Hurt { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> heals.
        /// </summary>
        public static Event<HealingEventArgs> Healing { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> has healed.
        /// </summary>
        public static Event<HealedEventArgs> Healed { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> dies.
        /// </summary>
        public static Event<DyingEventArgs> Dying { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> died.
        /// </summary>
        public static Event<DiedEventArgs> Died { get; set; } = new();

        /// <summary>
        /// Invoked before changing a <see cref="API.Features.Player"/> role.
        /// </summary>
        /// <remarks>If <see cref="ChangingRoleEventArgs.IsAllowed"/> is set to <see langword="false"/> when Escape is <see langword="true"/>, tickets will still be given to the escapee's team even though they will 'fail' to escape. Use <see cref="Escaping"/> to block escapes instead.</remarks>
        public static Event<ChangingRoleEventArgs> ChangingRole { get; set; } = new();

        /// <summary>
        /// Invoked after changed a <see cref="API.Features.Player"/> role.
        /// </summary>
        public static Event<ChangedRoleEventArgs> ChangedRole { get; set; } = new();

        /// <summary>
        /// Invoked afer throwing an <see cref="API.Features.Items.Throwable"/>.
        /// </summary>
        public static Event<ThrownProjectileEventArgs> ThrownProjectile { get; set; } = new();

        /// <summary>
        /// Invoked before receving a throwing request an <see cref="API.Features.Items.Throwable"/>.
        /// </summary>
        public static Event<ThrowingRequestEventArgs> ThrowingRequest { get; set; } = new();

        /// <summary>
        /// Invoked before dropping an <see cref="API.Features.Items.Item"/>.
        /// </summary>
        public static Event<DroppingItemEventArgs> DroppingItem { get; set; } = new();

        /// <summary>
        /// Invoked after dropping an <see cref="API.Features.Items.Item"/>.
        /// </summary>
        public static Event<DroppedItemEventArgs> DroppedItem { get; set; } = new();

        /// <summary>
        /// Invoked before dropping a null <see cref="API.Features.Items.Item"/>.
        /// </summary>
        public static Event<DroppingNothingEventArgs> DroppingNothing { get; set; } = new();

        /// <summary>
        /// Invoked before playing an AudioLog.
        /// </summary>
        public static Event<PlayingAudioLogEventArgs> PlayingAudioLog { get; set; } = new();

        /// <summary>
        /// Invoked before picking up an <see cref="API.Features.Items.Item"/>.
        /// </summary>
        public static Event<PickingUpItemEventArgs> PickingUpItem { get; set; } = new();

        /// <summary>
        /// Invoked before handcuffing a <see cref="API.Features.Player"/>.
        /// </summary>
        public static Event<HandcuffingEventArgs> Handcuffing { get; set; } = new();

        /// <summary>
        /// Invoked after hand cuffed a <see cref="API.Features.Player"/>.
        /// </summary>
        public static Event<HandCuffedEventArgs> HandCuffed { get; set; } = new();

        /// <summary>
        /// Invoked before freeing a handcuffed <see cref="API.Features.Player"/>.
        /// </summary>
        public static Event<RemovingHandcuffsEventArgs> RemovingHandcuffs { get; set; } = new();

        /// <summary>
        /// Invoked after freeing a handcuffed <see cref="API.Features.Player"/>.
        /// </summary>
        public static Event<RemovedHandcuffsEventArgs> RemovedHandcuffs { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> escapes.
        /// </summary>
        public static Event<EscapingEventArgs> Escaping { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> escapes.
        /// </summary>
        public static Event<EscapedEventArgs> Escaped { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> begins speaking to the intercom.
        /// </summary>
        public static Event<IntercomSpeakingEventArgs> IntercomSpeaking { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> gets shot.
        /// </summary>
        public static Event<ShotEventArgs> Shot { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> shoots a weapon.
        /// </summary>
        public static Event<ShootingEventArgs> Shooting { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> sends a gun sound to nearby players.
        /// </summary>
        public static Event<SendingGunSoundEventArgs> SendingGunSound { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> receives a gun sound.
        /// </summary>
        public static Event<ReceivingGunSoundEventArgs> ReceivingGunSound { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> enters the pocket dimension.
        /// </summary>
        public static Event<EnteringPocketDimensionEventArgs> EnteringPocketDimension { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> enters the pocket dimension.
        /// </summary>
        public static Event<EnteredPocketDimensionEventArgs> EnteredPocketDimension { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> escapes the pocket dimension.
        /// </summary>
        public static Event<EscapingPocketDimensionEventArgs> EscapingPocketDimension { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> fails to escape the pocket dimension.
        /// </summary>
        public static Event<FailingEscapePocketDimensionEventArgs> FailingEscapePocketDimension { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> enters killer collision.
        /// </summary>
        public static Event<EnteringKillerCollisionEventArgs> EnteringKillerCollision { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> reloads a weapon.
        /// </summary>
        public static Event<ReloadingWeaponEventArgs> ReloadingWeapon { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> reloads a weapon.
        /// </summary>
        public static Event<ReloadedWeaponEventArgs> ReloadedWeapon { get; set; } = new();

        /// <summary>
        /// Invoked before spawning a <see cref="API.Features.Player"/>.
        /// </summary>
        public static Event<SpawningEventArgs> Spawning { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> has spawned.
        /// </summary>
        public static Event<SpawnedEventArgs> Spawned { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> held <see cref="API.Features.Items.Item"/> changes.
        /// </summary>
        public static Event<ChangedItemEventArgs> ChangedItem { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> held <see cref="API.Features.Items.Item"/> changes.
        /// </summary>
        public static Event<ChangingItemEventArgs> ChangingItem { get; set; } = new();

        /// <summary>
        /// Invoked before changing a <see cref="API.Features.Player"/> group.
        /// </summary>
        public static Event<ChangingGroupEventArgs> ChangingGroup { get; set; } = new();

        /// <summary>
        /// Invoked after changed a <see cref="API.Features.Player"/> group.
        /// </summary>
        public static Event<ChangedGroupEventArgs> ChangedGroup { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> interacts with a door.
        /// </summary>
        /// <seealso cref="Handlers.Item.KeycardInteracting"/>
        public static Event<InteractingDoorEventArgs> InteractingDoor { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> interacts with an elevator.
        /// </summary>
        public static Event<InteractingElevatorEventArgs> InteractingElevator { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> interacts with a locker.
        /// </summary>
        public static Event<InteractingLockerEventArgs> InteractingLocker { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> triggers a tesla gate.
        /// </summary>
        public static Event<TriggeringTeslaEventArgs> TriggeringTesla { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> unlocks a generator.
        /// </summary>
        public static Event<UnlockingGeneratorEventArgs> UnlockingGenerator { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> opens a generator.
        /// </summary>
        public static Event<OpeningGeneratorEventArgs> OpeningGenerator { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> closes a generator.
        /// </summary>
        public static Event<ClosingGeneratorEventArgs> ClosingGenerator { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> turns on the generator by switching lever.
        /// </summary>
        public static Event<ActivatingGeneratorEventArgs> ActivatingGenerator { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> turns off the generator by switching lever.
        /// </summary>
        public static Event<StoppingGeneratorEventArgs> StoppingGenerator { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> receives a status effect.
        /// </summary>
        public static Event<ReceivingEffectEventArgs> ReceivingEffect { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> received a status effect.
        /// </summary>
        public static Event<ReceivedEffectEventArgs> ReceivedEffect { get; set; } = new();

        /// <summary>
        /// Invoked before muting a user.
        /// </summary>
        public static Event<IssuingMuteEventArgs> IssuingMute { get; set; } = new();

        /// <summary>
        /// Invoked before unmuting a user.
        /// </summary>
        public static Event<RevokingMuteEventArgs> RevokingMute { get; set; } = new();

        /// <summary>
        /// Invoked before a user's radio battery charge is changed.
        /// </summary>
        public static Event<UsingRadioEventArgs> UsingRadio { get; set; } = new();

        /// <summary>
        /// Invoked after a user's radio battery charge is changed.
        /// </summary>
        public static Event<UsedRadioEventArgs> UsedRadio { get; set; } = new();

        /// <summary>
        /// Invoked before a user's radio preset is changed.
        /// </summary>
        public static Event<ChangingRadioPresetEventArgs> ChangingRadioPreset { get; set; } = new();

        /// <summary>
        /// Invoked after a user's radio preset is changed.
        /// </summary>
        public static Event<ChangedRadioPresetEventArgs> ChangedRadioPreset { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> MicroHID state is changed.
        /// </summary>
        public static Event<ChangingMicroHIDStateEventArgs> ChangingMicroHIDState { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> MicroHID energy is changed.
        /// </summary>
        public static Event<UsingMicroHIDEnergyEventArgs> UsingMicroHIDEnergy { get; set; } = new();

        /// <summary>
        /// Invoked before dropping ammo.
        /// </summary>
        public static Event<DroppingAmmoEventArgs> DroppingAmmo { get; set; } = new();

        /// <summary>
        /// Invoked after dropping ammo.
        /// </summary>
        public static Event<DroppedAmmoEventArgs> DroppedAmmo { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> interacts with a shooting target.
        /// </summary>
        public static Event<InteractingShootingTargetEventArgs> InteractingShootingTarget { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> damages a shooting target.
        /// </summary>
        public static Event<DamagingShootingTargetEventArgs> DamagingShootingTarget { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> flips a coin.
        /// </summary>
        public static Event<FlippingCoinEventArgs> FlippingCoin { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> flips a coin.
        /// </summary>
        public static Event<FlippedCoinEventArgs> FlippedCoin { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> toggles the flashlight.
        /// </summary>
        public static Event<TogglingFlashlightEventArgs> TogglingFlashlight { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> unloads a weapon.
        /// </summary>
        public static Event<UnloadingWeaponEventArgs> UnloadingWeapon { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> unloads a weapon.
        /// </summary>
        public static Event<UnloadedWeaponEventArgs> UnloadedWeapon { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> triggers an aim action.
        /// </summary>
        public static Event<AimingDownSightEventArgs> AimingDownSight { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> toggles the weapon's flashlight.
        /// </summary>
        public static Event<TogglingWeaponFlashlightEventArgs> TogglingWeaponFlashlight { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> toggles the weapon's flashlight.
        /// </summary>
        public static Event<ToggledWeaponFlashlightEventArgs> ToggledWeaponFlashlight { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> dryfires a weapon.
        /// </summary>
        public static Event<DryfiringWeaponEventArgs> DryfiringWeapon { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> presses the voicechat key.
        /// </summary>
        public static Event<VoiceChattingEventArgs> VoiceChatting { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> receives a voice message.
        /// </summary>
        public static Event<ReceivingVoiceMessageEventArgs> ReceivingVoiceMessage { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> makes noise.
        /// </summary>
        public static Event<MakingNoiseEventArgs> MakingNoise { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> jumps.
        /// </summary>
        public static Event<JumpingEventArgs> Jumping { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> lands.
        /// </summary>
        public static Event<LandingEventArgs> Landing { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> presses the transmission key.
        /// </summary>
        public static Event<TransmittingEventArgs> Transmitting { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> changes move state.
        /// </summary>
        public static Event<ChangingMoveStateEventArgs> ChangingMoveState { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> changed spectated player.
        /// </summary>
        public static Event<ChangingSpectatedPlayerEventArgs> ChangingSpectatedPlayer { get; set; } = new();

        /// <summary>
        /// Invoked when a <see cref="API.Features.Player"/> changes rooms.
        /// </summary>
        public static Event<RoomChangedEventArgs> RoomChanged { get; set; } = new();

        /// <summary>
        /// Invoked when a <see cref="API.Features.Player"/> changes zones.
        /// </summary>
        public static Event<ZoneChangedEventArgs> ZoneChanged { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> toggles the NoClip mode.
        /// </summary>
        public static Event<TogglingNoClipEventArgs> TogglingNoClip { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> toggled the NoClip mode.
        /// </summary>
        public static Event<ToggledNoClipEventArgs> ToggledNoClip { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> toggles overwatch.
        /// </summary>
        public static Event<TogglingOverwatchEventArgs> TogglingOverwatch { get; set; } = new();

        /// <summary>
        /// Invoked before turning the <see cref="API.Features.Items.Radio" /> on/off.
        /// </summary>
        public static Event<TogglingRadioEventArgs> TogglingRadio { get; set; } = new();

        /// <summary>
        /// Invoked after turning the <see cref="API.Features.Items.Radio" /> on/off.
        /// </summary>
        public static Event<ToggledRadioEventArgs> ToggledRadio { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> searches a Pickup.
        /// </summary>
        public static Event<SearchingPickupEventArgs> SearchingPickup { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> send a message in AdminChat.
        /// </summary>
        public static Event<SendingAdminChatMessageEventsArgs> SendingAdminChatMessage { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> damage a Window.
        /// </summary>
        public static Event<DamagingWindowEventArgs> DamagingWindow { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> damage a Door.
        /// </summary>
        public static Event<DamagingDoorEventArgs> DamagingDoor { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="T:Exiled.API.Features.Player" /> has an item added to their inventory.
        /// </summary>
        public static Event<ItemAddedEventArgs> ItemAdded { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="T:Exiled.API.Features.Player" /> has an item removed from their inventory.
        /// </summary>
        public static Event<ItemRemovedEventArgs> ItemRemoved { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> enters in an environmental hazard.
        /// </summary>
        public static Event<EnteringEnvironmentalHazardEventArgs> EnteringEnvironmentalHazard { get; set; } = new();

        /// <summary>
        /// Invoked when a <see cref="API.Features.Player"/> stays on an environmental hazard.
        /// </summary>
        public static Event<StayingOnEnvironmentalHazardEventArgs> StayingOnEnvironmentalHazard { get; set; } = new();

        /// <summary>
        /// Invoked when a <see cref="API.Features.Player"/> exists from an environmental hazard.
        /// </summary>
        public static Event<ExitingEnvironmentalHazardEventArgs> ExitingEnvironmentalHazard { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/>'s nickname is changed.
        /// </summary>
        public static Event<ChangingNicknameEventArgs> ChangingNickname { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> sends valid command.
        /// </summary>
        public static Event<SendingValidCommandEventArgs> SendingValidCommand { get; set; } = new();

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> sends valid command.
        /// </summary>
        public static Event<SentValidCommandEventArgs> SentValidCommand { get; set; } = new();

        /// <summary>
        /// Invoked before a player's emotion changed.
        /// </summary>
        public static Event<ChangingEmotionEventArgs> ChangingEmotion { get; set; } = new();

        /// <summary>
        /// Invoked after a player's emotion changed.
        /// </summary>
        public static Event<ChangedEmotionEventArgs> ChangedEmotion { get; set; } = new();

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/>'s rotates the revolver.
        /// </summary>
        public static Event<RotatingRevolverEventArgs> RotatingRevolver { get; set; } = new();

        /// <summary>
        /// Invoked before disruptor's mode is changed.
        /// </summary>
        public static Event<ChangingDisruptorModeEventArgs> ChangingDisruptorMode { get; set; } = new();

        /// <summary>
        /// Invoked before the player explode with the micro HID.
        /// </summary>
        public static Event<ExplodingMicroHIDEventArgs> ExplodingMicroHID { get; set; } = new();

        /// <summary>
        /// Invoked before the micro HID opens a door.
        /// </summary>
        public static Event<MicroHIDOpeningDoorEventArgs> MicroHIDOpeningDoor { get; set; } = new();

        /// <summary>
        /// Invoked before player interacts with coffee cup.
        /// </summary>
        [Obsolete("Never available (for now).")]
        public static Event<DrinkingCoffeeEventArgs> DrinkingCoffee { get; set; } = new();

        /// <summary>
        /// Invoked before Emergency Release Button is pressed.
        /// </summary>
        public static Event<InteractingEmergencyButtonEventArgs> InteractingEmergencyButton { get; set; } = new();

        /// <summary>
        /// Invoked after transmission has ended.
        /// </summary>
        public static Event<Scp1576TransmissionEndedEventArgs> Scp1576TransmissionEnded { get; set; } = new();

        /// <summary>
        /// Invoked before new information about wearables is sent to clients.
        /// </summary>
        public static Event<ChangingWearablesEventArgs> ChangingWearables { get; set; } = new();

        /// <summary>
        /// Called before a player's emotion changed.
        /// </summary>
        /// <param name="ev">The <see cref="ChangingEmotionEventArgs"/> instance.</param>
        public static void OnChangingEmotion(ChangingEmotionEventArgs ev) => ChangingEmotion.InvokeSafely(ev);

        /// <summary>
        /// Called after a player's emotion changed.
        /// </summary>
        /// <param name="ev">The <see cref="ChangedEmotionEventArgs"/> instance.</param>
        public static void OnChangedEmotion(ChangedEmotionEventArgs ev) => ChangedEmotion.InvokeSafely(ev);

        /// <summary>
        /// Called before reserved slot is resolved for a <see cref="API.Features.Player"/>.
        /// </summary>
        /// <param name="ev">The <see cref="ReservedSlotsCheckEventArgs"/> instance.</param>
        public static void OnReservedSlot(ReservedSlotsCheckEventArgs ev) => ReservedSlot.InvokeSafely(ev);

        /// <summary>
        /// Called before kicking a <see cref="API.Features.Player"/> from the server.
        /// </summary>
        /// <param name="ev">The <see cref="KickingEventArgs"/> instance.</param>
        public static void OnKicking(KickingEventArgs ev) => Kicking.InvokeSafely(ev);

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> has been kicked from the server.
        /// </summary>
        /// <param name="labEv">The <see cref="KickedEventArgs"/> instance.</param>
        public static void OnKicked(PlayerKickedEventArgs labEv)
        {
            if (!Kicked.HasSubscribers)
                return;

            Kicked.InvokeSafely(new KickedEventArgs(labEv.Player, labEv.Issuer, labEv.Reason));
        }

        /// <summary>
        /// Called before banning a <see cref="API.Features.Player"/> from the server.
        /// </summary>
        /// <param name="ev">The <see cref="BanningEventArgs"/> instance.</param>
        public static void OnBanning(BanningEventArgs ev) => Banning.InvokeSafely(ev);

        /// <summary>
        /// Called before a player's danger state changes.
        /// </summary>
        /// <param name="ev">The <see cref="ChangingDangerStateEventArgs"/> instance.</param>
        public static void OnChangingDangerState(ChangingDangerStateEventArgs ev) => ChangingDangerState.InvokeSafely(ev);

        /// <summary>
        /// Called after a player has been banned from the server.
        /// </summary>
        /// <param name="ev">The <see cref="BannedEventArgs"/> instance.</param>
        public static void OnBanned(BannedEventArgs ev) => Banned.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> earns an achievement.
        /// </summary>
        /// <param name="ev">The <see cref="EarningAchievementEventArgs"/> instance.</param>
        public static void OnEarningAchievement(EarningAchievementEventArgs ev) => EarningAchievement.InvokeSafely(ev);

        /// <summary>
        /// Called before using a usable item.
        /// </summary>
        /// <param name="ev">The <see cref="UsingItemEventArgs"/> instance.</param>
        public static void OnUsingItem(UsingItemEventArgs ev) => UsingItem.InvokeSafely(ev);

        /// <summary>
        /// Called before completed using of a usable item.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerItemUsageEffectsApplyingEventArgs"/> instance.</param>
        public static void OnUsingItemCompleted(PlayerItemUsageEffectsApplyingEventArgs labEv)
        {
            if (!UsingItemCompleted.HasSubscribers)
                return;

            Usable usable = API.Features.Items.Item.Get<Usable>(labEv.UsableItem.Serial);

            UsingItemCompletedEventArgs exiledEv = new(labEv.Player, usable, labEv.ContinueProcess);
            UsingItemCompleted.InvokeSafely(exiledEv);

            labEv.ContinueProcess = exiledEv.ContinueProcess;
            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> used a <see cref="API.Features.Items.Usable"/> item.
        /// </summary>
        /// <param name="ev">The <see cref="UsedItemEventArgs"/> instance.</param>
        public static void OnUsedItem(UsedItemEventArgs ev) => UsedItem.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> consumes a <see cref="API.Features.Items.Consumable"/> item.
        /// </summary>
        /// <param name="ev">The <see cref="ConsumingItemEventArgs"/> instance.</param>
        public static void OnConsumingItem(ConsumingItemEventArgs ev) => ConsumingItem.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> has stopped the use of a <see cref="API.Features.Items.Usable"/> item.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerCancellingUsingItemEventArgs"/> instance.</param>
        public static void OnCancellingItemUse(PlayerCancellingUsingItemEventArgs labEv)
        {
            if (!CancellingItemUse.HasSubscribers)
                return;

            CancellingItemUseEventArgs exiledEv = new(labEv.Player, labEv.UsableItem.Base, labEv.IsAllowed);

            CancellingItemUse.InvokeSafely(exiledEv);

            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> has stopped the use of a <see cref="API.Features.Items.Usable"/> item.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerCancelledUsingItemEventArgs"/> instance.</param>
        public static void OnCancelledItemUse(PlayerCancelledUsingItemEventArgs labEv)
        {
            if (!CancelledItemUse.HasSubscribers)
                return;

            CancelledItemUse.InvokeSafely(new(labEv.Player, labEv.UsableItem.Base));
        }

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/>'s aspect ratio changes.
        /// </summary>
        /// <param name="ev">The <see cref="ChangedRatioEventArgs"/> instance.</param>
        public static void OnChangedRatio(ChangedRatioEventArgs ev) => ChangedRatio.InvokeSafely(ev);

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> interacted with something.
        /// </summary>
        /// <param name="ev">The <see cref="InteractedEventArgs"/> instance.</param>
        public static void OnInteracted(InteractedEventArgs ev) => Interacted.InvokeSafely(ev);

        /// <summary>
        /// Called before spawning a <see cref="API.Features.Player"/> ragdoll.
        /// </summary>
        /// <param name="ev">The <see cref="SpawningRagdollEventArgs"/> instance.</param>
        public static void OnSpawningRagdoll(SpawningRagdollEventArgs ev) => SpawningRagdoll.InvokeSafely(ev);

        /// <summary>
        /// Called after spawning a <see cref="API.Features.Player"/> ragdoll.
        /// </summary>
        /// <param name="ev">The <see cref="SpawnedRagdollEventArgs"/> instance.</param>
        public static void OnSpawnedRagdoll(SpawnedRagdollEventArgs ev) => SpawnedRagdoll.InvokeSafely(ev);

        /// <summary>
        /// Called before activating the warhead panel.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerUnlockingWarheadButtonEventArgs"/> instance.</param>
        public static void OnActivatingWarheadPanel(PlayerUnlockingWarheadButtonEventArgs labEv)
        {
            if (!ActivatingWarheadPanel.HasSubscribers)
                return;

            ActivatingWarheadPanelEventArgs exiledEv = new(labEv.Player, labEv.IsAllowed);
            ActivatingWarheadPanel.InvokeSafely(exiledEv);

            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called before activating a workstation.
        /// </summary>
        /// <param name="ev">The <see cref="ActivatingWorkstation"/> instance.</param>
        public static void OnActivatingWorkstation(ActivatingWorkstationEventArgs ev) => ActivatingWorkstation.InvokeSafely(ev);

        /// <summary>
        /// Called before deactivating a workstation.
        /// </summary>
        /// <param name="ev">The <see cref="DeactivatingWorkstationEventArgs"/> instance.</param>
        public static void OnDeactivatingWorkstation(DeactivatingWorkstationEventArgs ev) => DeactivatingWorkstation.InvokeSafely(ev);

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> has left the server.
        /// </summary>
        /// <param name="ev">The <see cref="LeftEventArgs"/> instance.</param>
        public static void OnLeft(LeftEventArgs ev) => Left.InvokeSafely(ev);

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> died.
        /// </summary>
        /// <param name="ev">The <see cref="DiedEventArgs"/> instance.</param>
        public static void OnDied(DiedEventArgs ev) => Died.InvokeSafely(ev);

        /// <summary>
        /// Called before changing a <see cref="API.Features.Player"/> role.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerChangingRoleEventArgs"/> instance.</param>
        /// <remarks>If <see cref="ChangingRoleEventArgs.IsAllowed"/> is set to <see langword="false"/> when Escape is <see langword="true"/>, tickets will still be given to the escapee's team even though they will 'fail' to escape. Use <see cref="Escaping"/> to block escapes instead.</remarks>
        public static void OnChangingRole(PlayerChangingRoleEventArgs labEv)
        {
            API.Features.Player player = labEv.Player;
            if (player == null || (!player.IsVerified && !player.IsNPC))
                return;

            ChangingRoleEventArgs exiledEv = new(player, labEv.NewRole, labEv.ChangeReason, labEv.SpawnFlags, labEv.IsAllowed);

            if (ChangingRole.HasSubscribers)
            {
                ChangingRole.InvokeSafely(exiledEv);

                labEv.IsAllowed = exiledEv.IsAllowed;
                labEv.NewRole = exiledEv.NewRole;
                labEv.ChangeReason = (RoleChangeReason)exiledEv.Reason;
                labEv.SpawnFlags = exiledEv.SpawnFlags;
            }

            CachedChangingRoleEvents[player] = exiledEv;
        }

        /// <summary>
        /// Called after changed a <see cref="API.Features.Player"/> role.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerChangedRoleEventArgs"/> instance.</param>
        public static void OnChangedRole(PlayerChangedRoleEventArgs labEv)
        {
            API.Features.Player player = labEv.Player;

            player.Role = Role.Create(labEv.NewRole);
            player.MaxHealth = default;

            if (player.Role.Type == RoleTypeId.Scp173)
                Scp173Role.TurnedPlayers.Remove(player);

            if (CachedChangingRoleEvents.TryGetValue(player, out ChangingRoleEventArgs cachedEv))
            {
                CachedChangingRoleEvents.Remove(player);
                ChangeInventory(cachedEv);
            }

            if (ChangedRole.HasSubscribers)
                ChangedRole.InvokeSafely(new(labEv.Player, player.Role, labEv.OldRole, labEv.ChangeReason, labEv.SpawnFlags));

            static void ChangeInventory(ChangingRoleEventArgs ev)
            {
                try
                {
                    if (ev is null)
                        return;

                    if (ev.ShouldPreserveInventory || ev.Reason == SpawnReason.Destroyed)
                        return;

                    Inventory inventory = ev.Player.Inventory;
                    if (InventoryItemProvider.KeepItemsAfterEscaping && ev.Reason == SpawnReason.Escaped)
                    {
                        List<ItemPickupBase> list = new();

                        HashSet<ushort> hashSet = HashSetPool<ushort>.Pool.Get();
                        foreach (KeyValuePair<ushort, ItemBase> item2 in inventory.UserInventory.Items)
                        {
                            if (item2.Value is Scp1344Item scp1344Item)
                                scp1344Item.Status = Scp1344Status.Idle;
                            else
                                hashSet.Add(item2.Key);
                        }

                        foreach (ushort item in hashSet)
                            list.Add(inventory.ServerDropItem(item));

                        HashSetPool<ushort>.Pool.Return(hashSet);
                        InventoryItemProvider.PreviousInventoryPickups[ev.Player.ReferenceHub] = list;
                    }
                    else
                    {
                        while (inventory.UserInventory.Items.Count > 0)
                            inventory.ServerRemoveItem(inventory.UserInventory.Items.ElementAt(0).Key, null);

                        inventory.UserInventory.ReserveAmmo.Clear();
                        inventory.SendAmmoNextFrame = true;
                    }

                    foreach (KeyValuePair<ItemType, ushort> ammo in ev.Ammo)
                        inventory.ServerAddAmmo(ammo.Key, ammo.Value);

                    foreach (ItemType item in ev.Items)
                    {
                        ItemBase itemBase = inventory.ServerAddItem(item, ItemAddReason.StartingItem);
                        InventoryItemProvider.OnItemProvided?.Invoke(ev.Player.ReferenceHub, itemBase);
                    }

                    PlayerEvents.OnReceivedLoadout(new PlayerReceivedLoadoutEventArgs(ev.Player.ReferenceHub, ev.Items, ev.Ammo, !ev.ShouldPreserveInventory));
                    InventoryItemProvider.InventoriesToReplenish.Enqueue(ev.Player.ReferenceHub);
                }
                catch (Exception exception)
                {
                    Log.Error($"{"ChangedRoleEvent"}.{nameof(ChangeInventory)}: {exception}");
                }
            }
        }

        /// <summary>
        /// Called before throwing a grenade.
        /// </summary>
        /// <param name="ev">The <see cref="ThrownProjectileEventArgs"/> instance.</param>
        public static void OnThrownProjectile(ThrownProjectileEventArgs ev) => ThrownProjectile.InvokeSafely(ev);

        /// <summary>
        /// Called before receving a throwing request.
        /// </summary>
        /// <param name="ev">The <see cref="ThrowingRequestEventArgs"/> instance.</param>
        public static void OnThrowingRequest(ThrowingRequestEventArgs ev) => ThrowingRequest.InvokeSafely(ev);

        /// <summary>
        /// Called before dropping an item.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerDroppingItemEventArgs"/> instance.</param>
        public static void OnDroppingItem(PlayerDroppingItemEventArgs labEv)
        {
            if (!DroppingItem.HasSubscribers)
                return;

            DroppingItemEventArgs exiledEv = new(labEv.Player, labEv.Item.Base, labEv.Throw, labEv.IsAllowed);
            DroppingItem.InvokeSafely(exiledEv);

            labEv.Throw = exiledEv.IsThrown;
            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called after dropping an item.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerDroppedItemEventArgs"/> instance.</param>
        public static void OnDroppedItem(PlayerDroppedItemEventArgs labEv)
        {
            if (!DroppedItem.HasSubscribers)
                return;

            DroppedItemEventArgs exiledEv = new(labEv.Player, labEv.Pickup.Base, labEv.Throw);
            DroppedItem.InvokeSafely(exiledEv);

            labEv.Throw = exiledEv.WasThrown;
        }

        /// <summary>
        /// Called before dropping a null item.
        /// </summary>
        /// <param name="ev">The <see cref="DroppingNothingEventArgs"/> instance.</param>
        public static void OnDroppingNothing(DroppingNothingEventArgs ev) => DroppingNothing.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> plays an AudioLog.
        /// </summary>
        /// <param name="ev">The <see cref="PlayingAudioLogEventArgs"/> instance.</param>
        public static void OnPlayingAudioLog(PlayingAudioLogEventArgs ev) => PlayingAudioLog.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> picks up an item.
        /// </summary>
        /// <param name="labEv">The <see cref="PickingUpItemEventArgs"/> instance.</param>
        public static void OnPickingUpItem(PlayerPickingUpItemEventArgs labEv)
        {
            if (!PickingUpItem.HasSubscribers)
                return;

            PickingUpItemEventArgs exiledEv = new(labEv.Player, labEv.Pickup.Base, labEv.IsAllowed);
            PickingUpItem.InvokeSafely(exiledEv);

            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> picks up an item.
        /// </summary>
        /// <param name="labEv">The <see cref="PickingUpItemEventArgs"/> instance.</param>
        public static void OnPickingUpItemAmmo(PlayerPickingUpAmmoEventArgs labEv)
        {
            if (!PickingUpItem.HasSubscribers)
                return;

            PickingUpItemEventArgs exiledEv = new(labEv.Player, labEv.AmmoPickup.Base, labEv.IsAllowed);
            PickingUpItem.InvokeSafely(exiledEv);

            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> picks up an item.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerPickingUpArmorEventArgs"/> instance.</param>
        public static void OnPickingUpItemArmor(PlayerPickingUpArmorEventArgs labEv)
        {
            if (!PickingUpItem.HasSubscribers)
                return;

            PickingUpItemEventArgs exiledEv = new(labEv.Player, labEv.BodyArmorPickup.Base, labEv.IsAllowed);
            PickingUpItem.InvokeSafely(exiledEv);

            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called before handcuffing a <see cref="API.Features.Player"/>.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerCuffingEventArgs"/> instance.</param>
        public static void OnHandcuffing(PlayerCuffingEventArgs labEv)
        {
            if (!Handcuffing.HasSubscribers)
                return;

            HandcuffingEventArgs exiledEv = new(labEv.Player, labEv.Target, labEv.IsAllowed);
            Handcuffing.InvokeSafely(exiledEv);

            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called after hand cuffed a <see cref="API.Features.Player"/>.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerCuffedEventArgs"/> instance.</param>
        public static void OnHandCuffed(PlayerCuffedEventArgs labEv)
        {
            if (!HandCuffed.HasSubscribers)
                return;

            HandCuffed.InvokeSafely(new(labEv.Player, labEv.Target));
        }

        /// <summary>
        /// Called before freeing a handcuffed <see cref="API.Features.Player"/>.
        /// </summary>
        /// <param name="ev">The <see cref="RemovingHandcuffsEventArgs"/> instance.</param>
        public static void OnRemovingHandcuffs(RemovingHandcuffsEventArgs ev) => RemovingHandcuffs.InvokeSafely(ev);

        /// <summary>
        /// Called after freeing a handcuffed <see cref="API.Features.Player"/>.
        /// </summary>
        /// <param name="ev">The <see cref="RemovedHandcuffsEventArgs"/> instance.</param>
        public static void OnRemovedHandcuffs(RemovedHandcuffsEventArgs ev) => RemovedHandcuffs.InvokeSafely(ev);

        /// <summary>
        /// Called when a <see cref="API.Features.Player"/> changes rooms.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerRoomChangedEventArgs"/> instance.</param>
        public static void OnRoomChanged(PlayerRoomChangedEventArgs labEv)
        {
            bool roomFlag = RoomChanged.HasSubscribers;
            bool zoneFlag = ZoneChanged.HasSubscribers;

            if (!roomFlag && !zoneFlag)
                return;

            RoomChangedEventArgs exiledEv = new(labEv.Player, labEv.OldRoom?.Base, labEv.NewRoom?.Base);
            if (roomFlag)
                RoomChanged.InvokeSafely(exiledEv);

            if (!zoneFlag)
                return;

            ZoneType oldZone = exiledEv.OldRoom?.Zone ?? ZoneType.Unspecified;
            ZoneType newZone = exiledEv.NewRoom?.Zone ?? ZoneType.Unspecified;

            if (oldZone != newZone)
                OnZoneChanged(new ZoneChangedEventArgs(exiledEv.Player, exiledEv.OldRoom, exiledEv.NewRoom, oldZone, newZone));
        }

        /// <summary>
        /// Called when a <see cref="API.Features.Player"/> changes zones.
        /// </summary>
        /// <param name="ev">The <see cref="ZoneChangedEventArgs"/> instance.</param>
        public static void OnZoneChanged(ZoneChangedEventArgs ev) => ZoneChanged.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> escapes.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerEscapingEventArgs"/> instance.</param>
        public static void OnEscaping(PlayerEscapingEventArgs labEv)
        {
            if (!Escaping.HasSubscribers)
                return;

            EscapingEventArgs exiledEv = new(labEv.Player, labEv.NewRole, (EscapeScenario)labEv.EscapeScenario, labEv.EscapeZone, labEv.IsAllowed);
            Escaping.InvokeSafely(exiledEv);

            labEv.NewRole = exiledEv.NewRole;
            labEv.IsAllowed = exiledEv.IsAllowed;
            labEv.EscapeScenario = (Escape.EscapeScenarioType)exiledEv.EscapeScenario;
        }

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> escapes.
        /// </summary>
        /// <param name="ev">The <see cref="EscapedEventArgs"/> instance.</param>
        public static void OnEscaped(EscapedEventArgs ev) => Escaped.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> begins speaking to the intercom.
        /// </summary>
        /// <param name="ev">The <see cref="IntercomSpeakingEventArgs"/> instance.</param>
        public static void OnIntercomSpeaking(IntercomSpeakingEventArgs ev) => IntercomSpeaking.InvokeSafely(ev);

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> shoots a weapon.
        /// </summary>
        /// <param name="ev">The <see cref="ShotEventArgs"/> instance.</param>
        public static void OnShot(ShotEventArgs ev) => Shot.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> shoots a weapon.
        /// </summary>
        /// <param name="ev">The <see cref="ShootingEventArgs"/> instance.</param>
        public static void OnShooting(ShootingEventArgs ev) => Shooting.InvokeSafely(ev);

        /// <summary>
        /// Called before the server sends a gun sound to nearby players.
        /// </summary>
        /// <param name="ev">The <see cref="SendingGunSoundEventArgs"/> instance.</param>
        public static void OnSendingGunSound(SendingGunSoundEventArgs ev) => SendingGunSound.InvokeSafely(ev);

        /// <summary>
        /// Called when a <see cref="API.Features.Player"/> receives a gun sound.
        /// </summary>
        /// <param name="ev">The <see cref="ReceivingGunSoundEventArgs"/> instance.</param>
        public static void OnReceivingGunSound(ReceivingGunSoundEventArgs ev) => ReceivingGunSound.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> enters the pocket dimension.
        /// </summary>
        /// <param name="labEv">The <see cref="Scp106TeleportingPlayerEvent"/> instance.</param>
        public static void OnEnteringPocketDimension(Scp106TeleportingPlayerEvent labEv)
        {
            if (!EnteringPocketDimension.HasSubscribers)
                return;

            EnteringPocketDimensionEventArgs exiledEv = new(labEv.Target, labEv.Player, labEv.IsAllowed);
            EnteringPocketDimension.InvokeSafely(exiledEv);

            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> successfully entered the pocket dimension.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerEnteredPocketDimensionEventArgs "/> instance.</param>
        public static void OnEnteredPocketDimension(PlayerEnteredPocketDimensionEventArgs labEv)
        {
            if (!EnteredPocketDimension.HasSubscribers)
                return;

            EnteredPocketDimension.InvokeSafely(new EnteredPocketDimensionEventArgs(labEv.Player));
        }

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> escapes the pocket dimension.
        /// </summary>
        /// <param name="ev">The <see cref="EscapingPocketDimensionEventArgs"/> instance.</param>
        public static void OnEscapingPocketDimension(EscapingPocketDimensionEventArgs ev) => EscapingPocketDimension.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> fails to escape the pocket dimension.
        /// </summary>
        /// <param name="ev">The <see cref="FailingEscapePocketDimensionEventArgs"/> instance.</param>
        public static void OnFailingEscapePocketDimension(FailingEscapePocketDimensionEventArgs ev) => FailingEscapePocketDimension.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> enters killer collision.
        /// </summary>
        /// <param name="ev">The <see cref="EnteringKillerCollisionEventArgs"/> instance.</param>
        public static void OnEnteringKillerCollision(EnteringKillerCollisionEventArgs ev) => EnteringKillerCollision.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> reloads a weapon.
        /// </summary>
        /// <param name="labEv">The <see cref="ReloadingWeaponEventArgs"/> instance.</param>
        public static void OnReloadingWeapon(PlayerReloadingWeaponEventArgs labEv)
        {
            if (!ReloadingWeapon.HasSubscribers)
                return;

            ReloadingWeaponEventArgs exiledEv = new(labEv.Player, labEv.FirearmItem.Base, labEv.IsAllowed);
            ReloadingWeapon.InvokeSafely(exiledEv);

            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> reloads a weapon.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerReloadedWeaponEventArgs"/> instance.</param>
        public static void OnReloadedWeapon(PlayerReloadedWeaponEventArgs labEv)
        {
            if (!ReloadedWeapon.HasSubscribers)
                return;

            ReloadedWeapon.InvokeSafely(new ReloadedWeaponEventArgs(labEv.Player, labEv.FirearmItem.Base));
        }

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> unloads a weapon.
        /// </summary>
        /// <param name="labEv">The <see cref="UnloadedWeaponEventArgs"/> instance.</param>
        public static void OnUnloadedWeapon(PlayerUnloadedWeaponEventArgs labEv)
        {
            if (!UnloadingWeapon.HasSubscribers)
                return;

            UnloadedWeapon.InvokeSafely(new UnloadedWeaponEventArgs(labEv.Player, labEv.FirearmItem.Base));
        }

        /// <summary>
        /// Called before spawning a <see cref="API.Features.Player"/>.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerSpawningEventArgs"/> instance.</param>
        public static void OnSpawning(PlayerSpawningEventArgs labEv)
        {
            if (!Spawning.HasSubscribers)
                return;

            SpawningEventArgs exiledEv = new(labEv.Player, labEv.SpawnLocation, labEv.HorizontalRotation, labEv.UseSpawnPoint, labEv.IsAllowed);
            Spawning.InvokeSafely(exiledEv);

            labEv.UseSpawnPoint = exiledEv.UseSpawnPoint;
            labEv.SpawnLocation = exiledEv.Position;
            labEv.HorizontalRotation = exiledEv.HorizontalRotation;
            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> has spawned.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerSpawnedEventArgs"/> instance.</param>
        public static void OnSpawned(PlayerSpawnedEventArgs labEv)
        {
            if (!Spawned.HasSubscribers)
                return;

            Spawned.InvokeSafely(new(labEv.Player, labEv.Role, labEv.UseSpawnPoint, labEv.SpawnLocation, labEv.HorizontalRotation));
        }

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> held item changes.
        /// </summary>
        /// <param name="ev">The <see cref="ChangedItemEventArgs"/> instance.</param>
        public static void OnChangedItem(ChangedItemEventArgs ev) => ChangedItem.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> held item changes.
        /// </summary>
        /// <param name="ev">The <see cref="ChangingItemEventArgs"/> instance.</param>
        public static void OnChangingItem(ChangingItemEventArgs ev) => ChangingItem.InvokeSafely(ev);

        /// <summary>
        /// Called before changing a <see cref="API.Features.Player"/> group.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerGroupChangingEventArgs"/> instance.</param>
        public static void OnChangingGroup(PlayerGroupChangingEventArgs labEv)
        {
            if (!ChangingGroup.HasSubscribers)
                return;

            ChangingGroupEventArgs exiledEv = new(labEv.Player, labEv.Group, labEv.IsAllowed);
            ChangingGroup.InvokeSafely(exiledEv);

            labEv.Group = exiledEv.NewGroup;
            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called after changed a <see cref="API.Features.Player"/> group.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerGroupChangedEventArgs"/> instance.</param>
        public static void OnChangedGroup(PlayerGroupChangedEventArgs labEv)
        {
            if (!ChangedGroup.HasSubscribers)
                return;

            ChangedGroup.InvokeSafely(new ChangedGroupEventArgs(labEv.Player, labEv.Group));
        }

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> interacts with an elevator.
        /// </summary>
        /// <param name="ev">The <see cref="InteractingElevatorEventArgs"/> instance.</param>
        public static void OnInteractingElevator(InteractingElevatorEventArgs ev) => InteractingElevator.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> interacts with an elevator.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerInteractingElevatorEventArgs"/> instance.</param>
        public static void OnInteractingElevator(PlayerInteractingElevatorEventArgs labEv)
        {
            if (!InteractingElevator.HasSubscribers)
                return;

            InteractingElevatorEventArgs exiledEv = new(labEv.Player, labEv.Elevator.Base, true, labEv.IsAllowed);
            InteractingElevator.InvokeSafely(exiledEv);

            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> interacts with a locker.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerInteractingLockerEventArgs"/> instance.</param>
        public static void OnInteractingLocker(PlayerInteractingLockerEventArgs labEv)
        {
            if (!InteractingLocker.HasSubscribers)
                return;

            InteractingLockerEventArgs exiledEv = new(labEv.Player, labEv.Locker.Base, labEv.Chamber.Base, labEv.CanOpen, labEv.IsAllowed);
            InteractingLocker.InvokeSafely(exiledEv);

            labEv.CanOpen = exiledEv.CanOpen;
            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> triggers a tesla.
        /// </summary>
        /// <param name="ev">The <see cref="TriggeringTeslaEventArgs"/> instance.</param>
        public static void OnTriggeringTesla(TriggeringTeslaEventArgs ev) => TriggeringTesla.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> receives a status effect.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerEffectUpdatingEventArgs"/> instance.</param>
        public static void OnReceivingEffect(PlayerEffectUpdatingEventArgs labEv)
        {
            if (!ReceivingEffect.HasSubscribers)
                return;

            ReceivingEffectEventArgs exiledEv = new(labEv.Player, labEv.Effect, labEv.Intensity, labEv.Effect.Intensity, labEv.Duration, labEv.IsAllowed);
            ReceivingEffect.InvokeSafely(exiledEv);

            labEv.Duration = exiledEv.Duration;
            labEv.Intensity = exiledEv.Intensity;
            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> received a status effect.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerEffectUpdatingEventArgs"/> instance.</param>
        public static void OnReceivedEffect(PlayerEffectUpdatedEventArgs labEv)
        {
            if (!ReceivedEffect.HasSubscribers)
                return;

            ReceivedEffect.InvokeSafely(new ReceivedEffectEventArgs(labEv.Player, labEv.Effect, labEv.Intensity, labEv.Duration));
        }

        /// <summary>
        /// Called before a player using radio.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerUsingRadioEventArgs"/> instance.</param>
        public static void OnUsingRadio(PlayerUsingRadioEventArgs labEv)
        {
            if (!UsingRadio.HasSubscribers)
                return;

            UsingRadioEventArgs exiledEv = new(labEv.Player, labEv.RadioItem.Base, labEv.Drain, labEv.IsAllowed);
            UsingRadio.InvokeSafely(exiledEv);

            labEv.Drain = exiledEv.Drain;
            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called after a player`s radio battery charge is changed.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerUsedRadioEventArgs"/> instance.</param>
        public static void OnUsedRadio(PlayerUsedRadioEventArgs labEv)
        {
            if (!UsedRadio.HasSubscribers)
                return;

            UsedRadio.InvokeSafely(new UsedRadioEventArgs(labEv.Player, labEv.RadioItem.Base, labEv.Drain));
        }

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> MicroHID state is changed.
        /// </summary>
        /// <param name="ev">The <see cref="ChangingMicroHIDStateEventArgs"/> instance.</param>
        public static void OnChangingMicroHIDState(ChangingMicroHIDStateEventArgs ev) => ChangingMicroHIDState.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> MicroHID energy is changed.
        /// </summary>
        /// <param name="ev">The <see cref="UsingMicroHIDEnergyEventArgs"/> instance.</param>
        public static void OnUsingMicroHIDEnergy(UsingMicroHIDEnergyEventArgs ev) => UsingMicroHIDEnergy.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> interacts with a shooting target.
        /// </summary>
        /// <param name="ev">The <see cref="InteractingShootingTargetEventArgs"/> instance.</param>
        public static void OnInteractingShootingTarget(InteractingShootingTargetEventArgs ev) => InteractingShootingTarget.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> damages a shooting target.
        /// </summary>
        /// <param name="ev">The <see cref="DamagingShootingTargetEventArgs"/> instance.</param>
        public static void OnDamagingShootingTarget(DamagingShootingTargetEventArgs ev) => DamagingShootingTarget.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> flips a coin.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerFlippingCoinEventArgs"/> instance.</param>
        public static void OnFlippingCoin(PlayerFlippingCoinEventArgs labEv)
        {
            if (!FlippingCoin.HasSubscribers)
                return;

            FlippingCoinEventArgs exiledEv = new(labEv.Player, labEv.CoinItem.Base, labEv.IsTails, labEv.IsAllowed);
            FlippingCoin.InvokeSafely(exiledEv);

            labEv.IsTails = exiledEv.IsTails;
            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> flips a coin.
        /// </summary>
        /// <param name="labEv">The <see cref="LabApi.Events.Arguments.PlayerEvents.PlayerFlippedCoinEventArgs"/> instance.</param>
        public static void OnFlippedCoin(PlayerFlippedCoinEventArgs labEv)
        {
            if (!FlippedCoin.HasSubscribers)
                return;

            FlippedCoin.InvokeSafely(new FlippedCoinEventArgs(labEv.Player, labEv.CoinItem.Base, labEv.IsTails));
        }

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> toggles the flashlight.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerTogglingFlashlightEventArgs"/> instance.</param>
        public static void OnTogglingFlashlight(PlayerTogglingFlashlightEventArgs labEv)
        {
            if (!TogglingFlashlight.HasSubscribers)
                return;

            TogglingFlashlightEventArgs exiledEv = new(labEv.Player, labEv.LightItem.Base, labEv.NewState, labEv.IsAllowed);
            TogglingFlashlight.InvokeSafely(exiledEv);

            labEv.NewState = exiledEv.NewState;
            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> unloads a weapon.
        /// </summary>
        /// <param name="labEv">The <see cref="UnloadingWeaponEventArgs"/> instance.</param>
        public static void OnUnloadingWeapon(PlayerUnloadingWeaponEventArgs labEv)
        {
            if (!UnloadingWeapon.HasSubscribers)
                return;

            UnloadingWeaponEventArgs exiledEv = new(labEv.Player, labEv.FirearmItem.Base, labEv.IsAllowed);
            UnloadingWeapon.InvokeSafely(exiledEv);

            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> triggers an aim action.
        /// </summary>
        /// <param name="ev">The <see cref="AimingDownSightEventArgs"/> instance.</param>
        public static void OnAimingDownSight(AimingDownSightEventArgs ev) => AimingDownSight.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> toggles the weapon's flashlight.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerTogglingWeaponFlashlightEventArgs"/> instance.</param>
        public static void OnTogglingWeaponFlashlight(PlayerTogglingWeaponFlashlightEventArgs labEv)
        {
            if (!TogglingWeaponFlashlight.HasSubscribers)
                return;

            TogglingWeaponFlashlightEventArgs exiledEv = new(labEv.Player, labEv.FirearmItem.Base, labEv.NewState, labEv.IsAllowed);
            TogglingWeaponFlashlight.InvokeSafely(exiledEv);

            labEv.NewState = exiledEv.NewState;
            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> toggles the weapon's flashlight.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerToggledWeaponFlashlightEventArgs"/> instance.</param>
        public static void OnToggledWeaponFlashlight(PlayerToggledWeaponFlashlightEventArgs labEv)
        {
            if (!ToggledWeaponFlashlight.HasSubscribers)
                return;

            ToggledWeaponFlashlight.InvokeSafely(new ToggledWeaponFlashlightEventArgs(labEv.Player, labEv.FirearmItem.Base, labEv.NewState));
        }

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> dryfires a weapon.
        /// </summary>
        /// <param name="ev">The <see cref="DryfiringWeaponEventArgs"/> instance.</param>
        public static void OnDryfiringWeapon(DryfiringWeaponEventArgs ev) => DryfiringWeapon.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> dryfires a weapon.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerDryFiringWeaponEventArgs"/> instance.</param>
        public static void OnDryfiringWeapon(PlayerDryFiringWeaponEventArgs labEv)
        {
            if (!DryfiringWeapon.HasSubscribers)
                return;

            DryfiringWeaponEventArgs exiledEv = new(labEv.Player, labEv.FirearmItem.Base, labEv.IsAllowed);
            DryfiringWeapon.InvokeSafely(exiledEv);

            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Invoked after a <see cref="API.Features.Player"/> presses the voicechat key.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerSendingVoiceMessageEventArgs"/> instance.</param>
        public static void OnVoiceChatting(PlayerSendingVoiceMessageEventArgs labEv)
        {
            bool voiceChattingFlag = VoiceChatting.HasSubscribers;
            bool transmittingFlag = Transmitting.HasSubscribers;

            if (!voiceChattingFlag && !transmittingFlag)
                return;

            API.Features.Player player = labEv.Player;
            if (player == null || player.Role is not API.Features.Roles.IVoiceRole voiceRole)
                return;

            if (voiceChattingFlag)
            {
                VoiceChattingEventArgs voiceEv = new(player, voiceRole.VoiceModule, labEv.Message, labEv.IsAllowed);
                VoiceChatting.InvokeSafely(voiceEv);

                labEv.Message = voiceEv.VoiceMessage;
                labEv.IsAllowed = voiceEv.IsAllowed;
            }

            if (transmittingFlag && voiceRole.VoiceModule.CurrentChannel == VoiceChat.VoiceChatChannel.Radio)
            {
                TransmittingEventArgs transmittingEv = new(player, labEv.Message, voiceRole.VoiceModule, labEv.IsAllowed);
                Transmitting.InvokeSafely(transmittingEv);

                labEv.Message = transmittingEv.VoiceMessage;
                labEv.IsAllowed = transmittingEv.IsAllowed;
            }
        }

        /// <summary>
        /// Invoked before a <see cref="API.Features.Player"/> receives a voice message.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerReceivingVoiceMessageEventArgs"/> instance.</param>
        public static void OnReceivingVoiceMessage(PlayerReceivingVoiceMessageEventArgs labEv)
        {
            if (!ReceivingVoiceMessage.HasSubscribers)
                return;

            API.Features.Player sender = labEv.Sender;
            API.Features.Player receiver = labEv.Player;
            if (sender.Role is not IVoiceRole voiceRole)
                return;

            ReceivingVoiceMessageEventArgs exiledEv = new(receiver, sender, voiceRole.VoiceModule, labEv.Message, labEv.IsAllowed);
            ReceivingVoiceMessage.InvokeSafely(exiledEv);

            labEv.Message = exiledEv.VoiceMessage;
            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> makes noise.
        /// </summary>
        /// <param name="ev">The <see cref="MakingNoiseEventArgs"/> instance.</param>
        public static void OnMakingNoise(MakingNoiseEventArgs ev) => MakingNoise.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> jumps.
        /// </summary>
        /// <param name="ev">The <see cref="JumpingEventArgs"/> instance.</param>
        public static void OnJumping(JumpingEventArgs ev) => Jumping.InvokeSafely(ev);

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> lands.
        /// </summary>
        /// <param name="ev">The <see cref="LandingEventArgs"/> instance.</param>
        public static void OnLanding(LandingEventArgs ev) => Landing.InvokeSafely(ev);

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> presses the transmission key.
        /// </summary>
        /// <param name="ev">The <see cref="TransmittingEventArgs"/> instance.</param>
        public static void OnTransmitting(TransmittingEventArgs ev) => Transmitting.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> changes move state.
        /// </summary>
        /// <param name="ev">The <see cref="ChangingMoveStateEventArgs"/> instance.</param>
        public static void OnChangingMoveState(ChangingMoveStateEventArgs ev) => ChangingMoveState.InvokeSafely(ev);

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> changes spectated player.
        /// </summary>
        /// <param name="ev">The <see cref="ChangingSpectatedPlayerEventArgs"/> instance.</param>
        public static void OnChangingSpectatedPlayer(ChangingSpectatedPlayerEventArgs ev) => ChangingSpectatedPlayer.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> toggles the NoClip mode.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerTogglingNoclipEventArgs"/> instance.</param>
        public static void OnTogglingNoClip(PlayerTogglingNoclipEventArgs labEv)
        {
            if (!TogglingNoClip.HasSubscribers)
                return;

            TogglingNoClipEventArgs exiledEv = new(labEv.Player, labEv.NewNoclipState, labEv.IsAllowed);
            TogglingNoClip.InvokeSafely(exiledEv);

            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> toggles the NoClip mode.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerToggledNoclipEventArgs"/> instance.</param>
        public static void OnToggledNoClip(PlayerToggledNoclipEventArgs labEv)
        {
            if (!ToggledNoClip.HasSubscribers)
                return;

            ToggledNoClip.InvokeSafely(new ToggledNoClipEventArgs(labEv.Player, labEv.IsNoclipping));
        }

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> toggles overwatch.
        /// </summary>
        /// <param name="ev">The <see cref="TogglingOverwatchEventArgs"/> instance.</param>
        public static void OnTogglingOverwatch(TogglingOverwatchEventArgs ev) => TogglingOverwatch.InvokeSafely(ev);

        /// <summary>
        /// Called before turning the radio on/off.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerTogglingRadioEventArgs"/> instance.</param>
        public static void OnTogglingRadio(PlayerTogglingRadioEventArgs labEv)
        {
            if (!TogglingRadio.HasSubscribers)
                return;

            TogglingRadioEventArgs exiledEv = new(labEv.Player, labEv.RadioItem.Base, labEv.NewState, labEv.IsAllowed);
            TogglingRadio.InvokeSafely(exiledEv);

            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called after turning the <see cref="Radio" /> on/off.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerToggledRadioEventArgs"/> instance.</param>
        public static void OnToggledRadio(PlayerToggledRadioEventArgs labEv)
        {
            if (!ToggledRadio.HasSubscribers)
                return;

            ToggledRadio.InvokeSafely(new ToggledRadioEventArgs(labEv.Player, labEv.RadioItem.Base, labEv.NewState));
        }

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> searches a Pickup.
        /// </summary>
        /// <param name="ev">The <see cref="SearchingPickupEventArgs"/> instance.</param>
        public static void OnSearchPickupRequest(SearchingPickupEventArgs ev) => SearchingPickup.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> searches a Pickup.
        /// </summary>
        /// <param name="labEv">The <see cref="SendingAdminChatEventArgs"/> instance.</param>
        public static void OnSendingAdminChatMessage(SendingAdminChatEventArgs labEv)
        {
            if (!SendingAdminChatMessage.HasSubscribers)
                return;

            API.Features.Player player = API.Features.Player.Get(labEv.Sender);

            SendingAdminChatMessageEventsArgs exiledEv = new(player, labEv.Message, labEv.IsAllowed);
            SendingAdminChatMessage.InvokeSafely(exiledEv);

            labEv.Message = exiledEv.Message;
            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called after a <see cref="Exiled.API.Features.Player" /> has an item added to their inventory.
        /// </summary>
        /// <param name="referenceHub">The <see cref="ReferenceHub"/> the item was added to.</param>
        /// <param name="itemBase">The added <see cref="ItemBase"/>.</param>
        /// <param name="pickupBase">The <see cref="ItemPickupBase"/> the <see cref="ItemBase"/> originated from, or <see langword="null"/> if the item was not picked up.</param>
        public static void OnItemAdded(ReferenceHub referenceHub, ItemBase itemBase, ItemPickupBase pickupBase)
        {
            ItemAddedEventArgs ev = new(referenceHub, itemBase, pickupBase);

            ev.Item.ReadPickupInfoAfter(ev.Pickup);

            ev.Player.ItemsValue.Add(ev.Item);

            ItemAdded.InvokeSafely(ev);
        }

        /// <summary>
        /// Called after a <see cref="Exiled.API.Features.Player" /> has an item removed from their inventory.
        /// </summary>
        /// <param name="referenceHub">The <see cref="ReferenceHub"/> the item was removed from.</param>
        /// <param name="itemBase">The removed <see cref="ItemBase"/>.</param>
        /// <param name="pickupBase">The <see cref="ItemPickupBase"/> the <see cref="ItemBase"/> originated from, or <see langword="null"/> if the item was not picked up.</param>
        public static void OnItemRemoved(ReferenceHub referenceHub, ItemBase itemBase, ItemPickupBase pickupBase)
        {
            ItemRemovedEventArgs ev = new(referenceHub, itemBase, pickupBase);

            ev.Player.ItemsValue.Remove(ev.Item);

            API.Features.Items.Item.BaseToItem.Remove(itemBase);

            ItemRemoved.InvokeSafely(ev);
        }

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> enters in an environmental hazard.
        /// </summary>
        /// <param name="ev">The <see cref="EnteringEnvironmentalHazardEventArgs"/> instance. </param>
        public static void OnEnteringEnvironmentalHazard(EnteringEnvironmentalHazardEventArgs ev) => EnteringEnvironmentalHazard.InvokeSafely(ev);

        /// <summary>
        /// Called when a <see cref="API.Features.Player"/> stays on an environmental hazard.
        /// </summary>
        /// <param name="ev">The <see cref="StayingOnEnvironmentalHazardEventArgs"/> instance. </param>
        public static void OnStayingOnEnvironmentalHazard(StayingOnEnvironmentalHazardEventArgs ev) => StayingOnEnvironmentalHazard.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> exits from an environmental hazard.
        /// </summary>
        /// <param name="ev">The <see cref="ExitingEnvironmentalHazardEventArgs"/> instance. </param>
        public static void OnExitingEnvironmentalHazard(ExitingEnvironmentalHazardEventArgs ev) => ExitingEnvironmentalHazard.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> damage a window.
        /// </summary>
        /// <param name="ev">The <see cref="DamagingWindowEventArgs"/> instance. </param>
        public static void OnDamagingWindow(DamagingWindowEventArgs ev) => DamagingWindow.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> damage a window.
        /// </summary>
        /// <param name="ev">The <see cref="DamagingDoorEventArgs"/> instance. </param>
        public static void OnDamagingDoor(DamagingDoorEventArgs ev) => DamagingDoor.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> unlocks a generator.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerUnlockingGeneratorEventArgs"/> instance.</param>
        public static void OnUnlockingGenerator(PlayerUnlockingGeneratorEventArgs labEv)
        {
            if (!UnlockingGenerator.HasSubscribers)
                return;

            UnlockingGeneratorEventArgs exiledEv = new(labEv.Player, labEv.Generator.Base, labEv.IsAllowed);
            UnlockingGenerator.InvokeSafely(exiledEv);

            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> opens a generator.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerOpeningGeneratorEventArgs"/> instance.</param>
        public static void OnOpeningGenerator(PlayerOpeningGeneratorEventArgs labEv)
        {
            if (!OpeningGenerator.HasSubscribers)
                return;

            OpeningGeneratorEventArgs exiledEv = new(labEv.Player, labEv.Generator.Base, labEv.IsAllowed);
            OpeningGenerator.InvokeSafely(exiledEv);

            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> closes a generator.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerClosingGeneratorEventArgs"/> instance.</param>
        public static void OnClosingGenerator(PlayerClosingGeneratorEventArgs labEv)
        {
            if (!ClosingGenerator.HasSubscribers)
                return;

            ClosingGeneratorEventArgs exiledEv = new(labEv.Player, labEv.Generator.Base, labEv.IsAllowed);
            ClosingGenerator.InvokeSafely(exiledEv);

            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> turns on the generator by switching lever.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerActivatingGeneratorEventArgs"/> instance.</param>
        public static void OnActivatingGenerator(PlayerActivatingGeneratorEventArgs labEv)
        {
            if (!ActivatingGenerator.HasSubscribers)
                return;

            ActivatingGeneratorEventArgs exiledEv = new(labEv.Player, labEv.Generator.Base, labEv.IsAllowed);
            ActivatingGenerator.InvokeSafely(exiledEv);

            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> turns off the generator by switching lever.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerDeactivatingGeneratorEventArgs"/> instance.</param>
        public static void OnStoppingGenerator(PlayerDeactivatingGeneratorEventArgs labEv)
        {
            if (!StoppingGenerator.HasSubscribers)
                return;

            StoppingGeneratorEventArgs exiledEv = new(labEv.Player, labEv.Generator.Base, labEv.IsAllowed);
            StoppingGenerator.InvokeSafely(exiledEv);

            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> interacts with a door.
        /// </summary>
        /// <param name="ev">The <see cref="InteractingDoorEventArgs"/> instance. </param>
        public static void OnInteractingDoor(InteractingDoorEventArgs ev) => InteractingDoor.InvokeSafely(ev);

        /// <summary>
        /// Called before dropping ammo.
        /// </summary>
        /// <param name="ev">The <see cref="DroppingAmmoEventArgs"/> instance. </param>
        public static void OnDroppingAmmo(DroppingAmmoEventArgs ev) => DroppingAmmo.InvokeSafely(ev);

        /// <summary>
        /// Called after dropping ammo.
        /// </summary>
        /// <param name="ev">The <see cref="DroppedAmmoEventArgs"/> instance. </param>
        public static void OnDroppedAmmo(DroppedAmmoEventArgs ev) => DroppedAmmo.InvokeSafely(ev);

        /// <summary>
        /// Called before muting a user.
        /// </summary>
        /// <param name="ev">The <see cref="IssuingMuteEventArgs"/> instance. </param>
        public static void OnIssuingMute(IssuingMuteEventArgs ev) => IssuingMute.InvokeSafely(ev);

        /// <summary>
        /// Called before unmuting a user.
        /// </summary>
        /// <param name="ev">The <see cref="RevokingMuteEventArgs"/> instance. </param>
        public static void OnRevokingMute(RevokingMuteEventArgs ev) => RevokingMute.InvokeSafely(ev);

        /// <summary>
        /// Called before a user's radio preset is changed.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerChangingRadioRangeEventArgs"/> instance.</param>
        public static void OnChangingRadioPreset(PlayerChangingRadioRangeEventArgs labEv)
        {
            if (!ChangingRadioPreset.HasSubscribers)
                return;

            ChangingRadioPresetEventArgs exiledEv = new(labEv.Player, labEv.RadioItem.Base, labEv.RadioItem.RangeLevel, labEv.Range, labEv.IsAllowed);
            ChangingRadioPreset.InvokeSafely(exiledEv);

            labEv.Range = (RadioRangeLevel)exiledEv.NewValue;
            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called after a user's radio preset is changed.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerChangedRadioRangeEventArgs"/> instance.</param>
        public static void OnChangedRadioPreset(PlayerChangedRadioRangeEventArgs labEv)
        {
            if (!ChangedRadioPreset.HasSubscribers)
                return;

            ChangedRadioPreset.InvokeSafely(new ChangedRadioPresetEventArgs(labEv.Player, labEv.RadioItem.Base, labEv.Range));
        }

        /// <summary>
        /// Called before hurting a player.
        /// </summary>
        /// <param name="ev">The <see cref="HurtingEventArgs"/> instance. </param>
        public static void OnHurting(HurtingEventArgs ev) => Hurting.InvokeSafely(ev);

        /// <summary>
        /// Called ater a <see cref="API.Features.Player"/> being hurt.
        /// </summary>
        /// <param name="ev">The <see cref="HurtingEventArgs"/> instance. </param>
        public static void OnHurt(HurtEventArgs ev) => Hurt.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> is healed.
        /// </summary>
        /// <param name="ev">The <see cref="HealingEventArgs"/> instance. </param>
        public static void OnHealing(HealingEventArgs ev) => Healing.InvokeSafely(ev);

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> is healed.
        /// </summary>
        /// <param name="ev">The <see cref="HealedEventArgs"/> instance. </param>
        public static void OnHealed(HealedEventArgs ev) => Healed.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> is saved from death by the Anti-SCP-207 effect.
        /// </summary>
        /// <param name="ev">The <see cref="SavingByAntiScp207EventArgs"/> instance.</param>
        public static void OnSavingByAntiScp207(SavingByAntiScp207EventArgs ev) => SavingByAntiScp207.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/> dies.
        /// </summary>
        /// <param name="labEv">The <see cref="PlayerDyingEventArgs"/> instance. </param>
        public static void OnDying(PlayerDyingEventArgs labEv)
        {
            if (!Dying.HasSubscribers)
                return;

            DyingEventArgs exiledEv = new(labEv.Player, labEv.DamageHandler, labEv.IsAllowed);
            Dying.InvokeSafely(exiledEv);

            labEv.DamageHandler = exiledEv.DamageHandler;
            labEv.IsAllowed = exiledEv.IsAllowed;
        }

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> has joined the server.
        /// </summary>
        /// <param name="ev">The <see cref="JoinedEventArgs"/> instance. </param>
        public static void OnJoined(JoinedEventArgs ev) => Joined.InvokeSafely(ev);

        /// <summary>
        /// Called after a <see cref="API.Features.Player"/> has been verified.
        /// </summary>
        /// <param name="ev">The <see cref="VerifiedEventArgs"/> instance. </param>
        public static void OnVerified(VerifiedEventArgs ev) => Verified.InvokeSafely(ev);

        /// <summary>
        /// Called before destroying a <see cref="API.Features.Player"/>.
        /// </summary>
        /// <param name="ev">The <see cref="DestroyingEventArgs"/> instance. </param>
        public static void OnDestroying(DestroyingEventArgs ev) => Destroying.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="Player"/>'s custom display name is changed.
        /// </summary>
        /// <param name="ev">The <see cref="ChangingNicknameEventArgs"/> instance.</param>
        public static void OnChangingNickname(ChangingNicknameEventArgs ev) => ChangingNickname.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="Player"/> sends valid command.
        /// </summary>
        /// <param name="ev">The <see cref="SendingValidCommandEventArgs"/> instance.</param>
        public static void OnSendingValidCommand(SendingValidCommandEventArgs ev) => SendingValidCommand.InvokeSafely(ev);

        /// <summary>
        /// Called after a <see cref="Player"/> sends valid command.
        /// </summary>
        /// <param name="ev">The <see cref="SentValidCommandEventArgs"/> instance.</param>
        public static void OnSentValidCommand(SentValidCommandEventArgs ev) => SentValidCommand.InvokeSafely(ev);

        /// <summary>
        /// Called before a <see cref="API.Features.Player"/>'s rotates the revolver.
        /// </summary>
        /// <param name="ev">The <see cref="RotatingRevolverEventArgs"/> instance.</param>
        public static void OnRotatingRevolver(RotatingRevolverEventArgs ev) => RotatingRevolver.InvokeSafely(ev);

        /// <summary>
        /// Called before disruptor's mode is changed.
        /// </summary>
        /// <param name="ev">The <see cref="ChangingDisruptorModeEventArgs"/> instance.</param>
        public static void OnChangingDisruptorMode(ChangingDisruptorModeEventArgs ev) => ChangingDisruptorMode.InvokeSafely(ev);

        /// <summary>
        /// Called before disruptor's mode is changed.
        /// </summary>
        /// <param name="ev">The <see cref="ExplodingMicroHIDEventArgs"/> instance.</param>
        public static void OnExplodingMicroHID(ExplodingMicroHIDEventArgs ev) => ExplodingMicroHID.InvokeSafely(ev);

        /// <summary>
        /// Called before the micro HID opens a door.
        /// </summary>
        /// <param name="ev">The <see cref="ChangingDisruptorModeEventArgs"/> instance.</param>
        public static void OnMicroHIDOpeningDoor(MicroHIDOpeningDoorEventArgs ev) => MicroHIDOpeningDoor.InvokeSafely(ev);

        /// <summary>
        /// Called before player interacts with coffee cup.
        /// </summary>
        /// <param name="ev">The <see cref="DrinkingCoffeeEventArgs"/> instance.</param>
        [Obsolete("Never available (for now).")]
        public static void OnDrinkingCoffee(DrinkingCoffeeEventArgs ev) => DrinkingCoffee.InvokeSafely(ev);

        /// <summary>
        /// Called before pre-authenticating a <see cref="API.Features.Player"/>.
        /// </summary>
        /// <param name="ev"><The cref="PreAuthenticatingEventArgs"/> instance.</param>
        public static void OnPreAuthenticating(PreAuthenticatingEventArgs ev) => PreAuthenticating.InvokeSafely(ev);

        /// <summary>
        /// Called after a player triggers the melee attack as an SCP.
        /// </summary>
        /// <param name="ev">The <see cref="HitEventArgs"/> instance.</param>
        public static void OnHit(HitEventArgs ev) => Hit.InvokeSafely(ev);

        /// <summary>
        /// Called before Emergency Release Button is pressed.
        /// </summary>
        /// <param name="ev">The <see cref="InteractingEmergencyButtonEventArgs"/> instance.</param>
        public static void OnInteractingEmergencyButton(InteractingEmergencyButtonEventArgs ev) => InteractingEmergencyButton.InvokeSafely(ev);

        /// <summary>
        /// Called after a 1576 transmission has ended.
        /// </summary>
        /// <param name="ev">The <see cref="Scp1576TransmissionEndedEventArgs"/> instance.</param>
        public static void OnScp1576TransmissionEnded(Scp1576TransmissionEndedEventArgs ev) => Scp1576TransmissionEnded.InvokeSafely(ev);

        /// <summary>
        /// Called before new information about wearables is sent to clients.
        /// </summary>
        /// <param name="ev">The <see cref="ChangingWearablesEventArgs"/> instance.</param>
        public static void OnChangingWearables(ChangingWearablesEventArgs ev) => ChangingWearables.InvokeSafely(ev);
    }
}