// -----------------------------------------------------------------------
// <copyright file="IPoolableEvent.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.EventArgs.Interfaces
{
    /// <summary>
    /// Represents an event argument that supports object pooling.
    /// </summary>
    public interface IPoolableEvent
    {
        /// <summary>
        /// Returns this event instance back to its pool.
        /// </summary>
        void Return();
    }
}
