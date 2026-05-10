// -----------------------------------------------------------------------
// <copyright file="WearableElementType.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.API.Enums
{
    using System;

    /// <summary>
    /// An enum containing all types of Wearable elements.
    /// </summary>
    [Flags]
    public enum WearableElementType
    {
        /// <summary>
        /// No wearable elements.
        /// </summary>
        None = 0,

        /// <summary>
        /// SCP-268 wearable element.
        /// </summary>
        Scp268Hat = 1,

        /// <summary>
        /// SCP-1344 wearable element.
        /// </summary>
        Scp1344Goggles = 2,

        /// <summary>
        /// Light armor wearable element.
        /// </summary>
        ArmorLight = 4,

        /// <summary>
        /// Combat armor wearable element.
        /// </summary>
        ArmorCombat = 8,

        /// <summary>
        /// Heavy armor wearable element.
        /// </summary>
        ArmorHeavy = 16,
    }
}