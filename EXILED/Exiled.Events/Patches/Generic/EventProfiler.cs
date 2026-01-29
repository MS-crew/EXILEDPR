// -----------------------------------------------------------------------
// <copyright file="EventProfiler.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.Patches.Generic
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.InteropServices;

    using Exiled.API.Features;
    using Exiled.API.Features.Pools;
    using Exiled.Events.Features;

    using HarmonyLib;

    using static HarmonyLib.AccessTools;

    /// <summary>
    /// Patch for adding profiler to <see cref="Event{T}.BlendedInvoke"/>.
    /// </summary>
    [HarmonyPatch]
    internal static class EventProfiler
    {
        private static float profilerThreshold;

        private static long allocationThreshold;

        private static Dictionary<Type, PropertyInfo> handlerPropCache;

        private static bool Prepare()
        {
            Config config = Exiled.Events.Events.Instance?.Config;

            if (config == null || !config.EventProfiler)
                return false;

            handlerPropCache = new Dictionary<Type, PropertyInfo>();
            profilerThreshold = (float)config.EventProfilerThreshold;
            allocationThreshold = config.EventProfilerAllocationThreshold;

            return true;
        }

        private static IEnumerable<MethodBase> TargetMethods()
        {
            Assembly exiledAssembly = typeof(Exiled.Events.Events).Assembly;

            foreach (Type type in exiledAssembly.GetExportedTypes())
            {
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Static | BindingFlags.Public))
                {
                    Type currentType = property.PropertyType;

                    while (currentType != null && currentType != typeof(object))
                    {
                        // if (currentType == typeof(Event) || (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(Event<>)))
                        if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(Event<>))
                        {
                            MethodInfo method = property.PropertyType.GetMethod("BlendedInvoke", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);

                            if (method != null)
                                yield return method;

                            break;
                        }

                        currentType = currentType.BaseType;
                    }
                }
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase originalMethod)
        {
            List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

            bool isGenericEvent = originalMethod.DeclaringType.IsGenericType;

            List<Label> gotoLogicLabel;
            Label doLogLabel = generator.DefineLabel();
            Label skipLogLabel = generator.DefineLabel();

            LocalBuilder startTick = generator.DeclareLocal(typeof(long));
            LocalBuilder startBytes = generator.DeclareLocal(typeof(long));
            LocalBuilder startGcCount = generator.DeclareLocal(typeof(int));

            LocalBuilder elapsedMs = generator.DeclareLocal(typeof(double));
            LocalBuilder allocatedBytes = generator.DeclareLocal(typeof(long));
            LocalBuilder gcRun = generator.DeclareLocal(typeof(bool));

            int index = newInstructions.FindIndex(i => i.opcode == OpCodes.Callvirt && i.operand is MethodInfo mi && mi.Name == "Invoke");

            newInstructions.InsertRange(
                index,
                [

                    // startTick = Stopwatch.GetTimestamp();
                    new(OpCodes.Call, Method(typeof(Stopwatch), nameof(Stopwatch.GetTimestamp))),
                    new(OpCodes.Stloc_S, startTick.LocalIndex),

                    // startBytes = GC.GetTotalMemory(false);
                    new(OpCodes.Ldc_I4_0),
                    new(OpCodes.Call, Method(typeof(GC), nameof(GC.GetTotalMemory), new[] { typeof(bool) })),
                    new(OpCodes.Stloc_S, startBytes.LocalIndex),

                    // startGcCount = GC.CollectionCount(0);
                    new(OpCodes.Ldc_I4_0),
                    new(OpCodes.Call, Method(typeof(GC), nameof(GC.CollectionCount), new[] { typeof(int) })),
                    new(OpCodes.Stloc_S, startGcCount.LocalIndex),
                ]);

            index += 9;
            gotoLogicLabel = newInstructions[index].ExtractLabels();
            newInstructions[index].WithLabels(skipLogLabel);

            newInstructions.InsertRange(
                index,
                [

                    // elapsedMs = (Stopwatch.GetTimestamp() - startTick) * 1000.0 / Stopwatch.Frequency;
                    new CodeInstruction(OpCodes.Call, Method(typeof(Stopwatch), nameof(Stopwatch.GetTimestamp))).WithLabels(gotoLogicLabel),
                    new(OpCodes.Ldloc_S, startTick.LocalIndex),
                    new(OpCodes.Sub),
                    new(OpCodes.Conv_R8),
                    new(OpCodes.Ldc_R8, 1000.0),
                    new(OpCodes.Mul),
                    new(OpCodes.Ldsfld, Field(typeof(Stopwatch), nameof(Stopwatch.Frequency))),
                    new(OpCodes.Conv_R8),
                    new(OpCodes.Div),
                    new(OpCodes.Stloc_S, elapsedMs.LocalIndex),

                    // allocatedBytes = GC.GetTotalMemory(false) - startBytes;
                    new(OpCodes.Ldc_I4_0),
                    new(OpCodes.Call, Method(typeof(GC), nameof(GC.GetTotalMemory), new[] { typeof(bool) })),
                    new(OpCodes.Ldloc_S, startBytes.LocalIndex),
                    new(OpCodes.Sub),
                    new(OpCodes.Stloc_S, allocatedBytes.LocalIndex),

                    // gcRan = GC.CollectionCount(0) > startGcCount;
                    new(OpCodes.Ldc_I4_0),
                    new(OpCodes.Call, Method(typeof(GC), nameof(GC.CollectionCount), new[] { typeof(int) })),
                    new(OpCodes.Ldloc_S, startGcCount.LocalIndex),
                    new(OpCodes.Cgt),
                    new(OpCodes.Stloc_S, gcRun.LocalIndex),

                    // if (elapsedMs > Events.ProfilerThreshold || allocatedBytes > Events.AllocationThreshold)
                    new(OpCodes.Ldloc_S, elapsedMs.LocalIndex),
                    new(OpCodes.Ldsfld, Field(typeof(EventProfiler), nameof(profilerThreshold))),
                    new(OpCodes.Bgt, doLogLabel),

                    new(OpCodes.Ldloc_S, allocatedBytes.LocalIndex),
                    new(OpCodes.Ldsfld, Field(typeof(EventProfiler), nameof(allocationThreshold))),
                    new(OpCodes.Ble, skipLogLabel),

                    // LogWarning(registrationArray, index, eventArg, ms, bytes, gcRan);
                    new CodeInstruction(OpCodes.Ldloc_0).WithLabels(doLogLabel),
                    new(OpCodes.Ldloc_3),
                    isGenericEvent ? new CodeInstruction(OpCodes.Ldarg_1) : new CodeInstruction(OpCodes.Ldnull),
                    new(OpCodes.Ldloc_S, elapsedMs.LocalIndex),
                    new(OpCodes.Ldloc_S, allocatedBytes.LocalIndex),
                    new(OpCodes.Ldloc_S, gcRun.LocalIndex),
                    new(OpCodes.Call, Method(typeof(EventProfiler), nameof(LogProfil))),
                ]);

            for (int i = 0; i < newInstructions.Count; i++)
                yield return newInstructions[i];

            ListPool<CodeInstruction>.Pool.Return(newInstructions);
        }

        private static void LogProfil(object registrationArray, int index, object eventArg, double ms, long bytes, bool gcRan)
        {
            if (registrationArray is not Array arr)
                return;
            try
            {
                object registrationItem = arr.GetValue(index);
                if (registrationItem == null)
                    return;

                Type regType = registrationItem.GetType();

                if (!handlerPropCache.TryGetValue(regType, out PropertyInfo prop))
                {
                    prop = regType.GetProperty("handler", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    handlerPropCache[regType] = prop;
                }

                if (prop?.GetValue(registrationItem) is not Delegate handler)
                    return;

                MethodInfo method = handler.Method;
                Type targetType = handler.Target?.GetType() ?? method.DeclaringType;

                string pluginName = targetType?.Assembly.GetName().Name ?? "Unknown";
                string className = targetType?.Name ?? "Unknown";
                string eventName = eventArg?.GetType().Name.Replace("EventArgs", string.Empty) ?? "VoidEvent";

                if (bytes < 0)
                    bytes = 0;

                string[] sizes = { "B", "KB", "MB", "GB" };
                int order = 0;
                double len = bytes;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len /= 1024;
                }

                string ramResult = $"{len:0.##} {sizes[order]}";

                string triggerPrefix = string.Empty;
                switch (gcRan, ms > profilerThreshold, bytes > allocationThreshold)
                {
                    case (true, true, true):
                        triggerPrefix = "[GC] [CPU]/[MEMORY]";
                        break;

                    case (true, true, false):
                        triggerPrefix = "[GC] [CPU]";
                        break;

                    case (true, false, true):
                        triggerPrefix = "[GC] [MEMORY]";
                        break;

                    case (false, true, true):
                        triggerPrefix = "[CPU]/[MEMORY]";
                        break;

                    case (false, true, false):
                        triggerPrefix = "[CPU]";
                        break;

                    case (false, false, true):
                        triggerPrefix = "[MEMORY]";
                        break;
                }

                Log.Warn($"[Event Profiler] {triggerPrefix.Trim()} '{eventName}' | Time: {ms:F2}ms | RAM: {ramResult} | Plugin: {pluginName} | Class: {className} | Method: {method.Name}");
            }
            catch (Exception ex)
            {
                Log.Error($"[EventProfiler] Error while profiling: {ex}");
            }
        }
    }
}