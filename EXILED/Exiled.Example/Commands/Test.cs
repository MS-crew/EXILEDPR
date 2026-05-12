// -----------------------------------------------------------------------
// <copyright file="Test.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Example.Commands
{
    using System;
    using System.Linq;

    using CommandSystem;

    using Exiled.API.Enums;
    using Exiled.API.Features;

    /// <summary>
    /// This is an example of how commands should be made.
    /// </summary>
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class Test : ICommand
    {
        /// <inheritdoc/>
        public string Command { get; } = "test";

        /// <inheritdoc/>
        public string[] Aliases { get; } = new[] { "t" };

        /// <inheritdoc/>
        public string Description { get; } = "A simple test command.";

        /// <inheritdoc/>
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = $"false!";
            if (!Enum.TryParse(arguments.ElementAtOrDefault(0), out WearableElementType wearables))
                return false;

            foreach (Player player in Player.List)
                player.Wearables = wearables;
            response = $"true !";

            // Return true if the command was executed successfully; otherwise, false.
            return true;
        }
    }
}