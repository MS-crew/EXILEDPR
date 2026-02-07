// -----------------------------------------------------------------------
// <copyright file="Draw.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.API.Features
{
    using System;
    using System.Collections.Generic;

    using DrawableLine;

    using Mirror;

    using UnityEngine;

    using Utils;

    /// <summary>
    /// A utility class for drawing debug lines, shapes, and bounds for players or globally.
    /// </summary>
    public static class Draw
    {
        /// <summary>
        /// Draws a line between two specified points.
        /// </summary>
        /// <param name="start">The starting position of the line.</param>
        /// <param name="end">The ending position of the line.</param>
        /// <param name="color">The color of the lines.</param>
        /// <param name="duration"> How long the line should remain visible.<para><warning><b>Warning:</b> Avoid using <see cref="float.PositiveInfinity"/> or extremely large values, as these lines cannot be removed from the client once sent.</warning></para></param>
        /// <param name="players">A collection of <see cref="Player"/>s to show the line to.</param>
        public static void Line(Vector3 start, Vector3 end, Color color, float duration, IEnumerable<Player> players = null)
        {
            Send(players, duration, color, start, end);
        }

        /// <summary>
        /// Draws a connected path through a series of points.
        /// </summary>
        /// <param name="points">An array of <see cref="Vector3"/> points to connect sequentially.</param>
        /// <param name="color">The color of the lines.</param>
        /// <param name="duration"> How long the line should remain visible.<para><warning><b>Warning:</b> Avoid using <see cref="float.PositiveInfinity"/> or extremely large values, as these lines cannot be removed from the client once sent.</warning></para></param>
        /// <param name="players">A collection of <see cref="Player"/>s to show the path to.</param>
        public static void Path(Vector3[] points, Color color, float duration, IEnumerable<Player> players = null)
        {
            Send(players, duration, color, points);
        }

        /// <summary>
        /// Draws a circle at a specific position.
        /// </summary>
        /// <param name="origin">The center point of the circle.</param>
        /// <param name="rotation">The rotation of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="color">The color of the lines.</param>
        /// <param name="duration"> How long the line should remain visible.<para><warning><b>Warning:</b> Avoid using <see cref="float.PositiveInfinity"/> or extremely large values, as these lines cannot be removed from the client once sent.</warning></para></param>
        /// <param name="players">A collection of <see cref="Player"/>s to show the circle to.</param>
        /// <param name="horizontal">Indicates whether the circle should be drawn on the horizontal plane (XZ) or vertical plane (XY).</param>
        /// <param name="segments">The number of line segments used to draw the circle. Higher values result in a smoother circle.</param>
        public static void Circle(Vector3 origin, Quaternion rotation, float radius, Color color, float duration, IEnumerable<Player> players = null, bool horizontal = true, int segments = 16)
        {
            Vector3[] circlePoints = GetCirclePoints(origin, rotation, radius, segments, horizontal);
            Send(players, duration, color, circlePoints);
        }

        /// <summary>
        /// Draws a wireframe sphere composed of two circles (horizontal and vertical).
        /// </summary>
        /// <param name="origin">The center point of the sphere.</param>
        /// <param name="rotation">The rotation of the sphere.</param>
        /// <param name="radius">The radius of the sphere.</param>
        /// <param name="color">The color of the lines.</param>
        /// <param name="duration"> How long the line should remain visible.<para><warning><b>Warning:</b> Avoid using <see cref="float.PositiveInfinity"/> or extremely large values, as these lines cannot be removed from the client once sent.</warning></para></param>
        /// <param name="players">A collection of <see cref="Player"/>s to show the sphere to.</param>
        /// <param name="segments">The number of segments for the circles. Higher values result in a smoother sphere.</param>
        public static void Sphere(Vector3 origin, Quaternion rotation, float radius, Color color, float duration, IEnumerable<Player> players = null, int segments = 16)
        {
            Vector3[] horizontal = GetCirclePoints(origin, rotation, radius, segments, true);
            Send(players, duration, color, horizontal);

            Vector3[] vertical = GetCirclePoints(origin, rotation, radius, segments, false);
            Send(players, duration, color, vertical);
        }

        /// <summary>
        /// Draws the edges of a <see cref="Bounds"/> object.
        /// </summary>
        /// <param name="bounds">The <see cref="Bounds"/> object to visualize.</param>
        /// <param name="color">The color of the lines.</param>
        /// <param name="duration"> How long the line should remain visible.<para><warning><b>Warning:</b> Avoid using <see cref="float.PositiveInfinity"/> or extremely large values, as these lines cannot be removed from the client once sent.</warning></para></param>
        /// <param name="players">A collection of <see cref="Player"/>s to show the bounds to.</param>
        public static void Bounds(Bounds bounds, Color color, float duration, IEnumerable<Player> players = null)
        {
            Box(bounds.center, bounds.size, Quaternion.identity, color, duration, players);
        }

        /// <summary>
        /// Draws the edges of a <see cref="RelativeBounds"/> object.
        /// </summary>
        /// <param name="relativeBounds">The <see cref="RelativeBounds"/> object to visualize.</param>
        /// <param name="color">The color of the lines.</param>
        /// <param name="duration"> How long the line should remain visible.<para><warning><b>Warning:</b> Avoid using <see cref="float.PositiveInfinity"/> or extremely large values, as these lines cannot be removed from the client once sent.</warning></para></param>
        /// <param name="players">A collection of <see cref="Player"/>s to show the bounds to.</param>
        public static void RelativeBounds(RelativeBounds relativeBounds, Color color, float duration, IEnumerable<Player> players = null)
        {
            Box(relativeBounds.Origin, relativeBounds.Bounds.size, relativeBounds.Rotation, color, duration, players);
        }

        /// <summary>
        /// Draws a collider.
        /// </summary>
        /// <param name="collider">The <see cref="BoxCollider"/> object to visualize.</param>
        /// <param name="color">The color of the lines.</param>
        /// <param name="duration"> How long the line should remain visible.<para><warning><b>Warning:</b> Avoid using <see cref="float.PositiveInfinity"/> or extremely large values, as these lines cannot be removed from the client once sent.</warning></para></param>
        /// <param name="players">A collection of <see cref="Player"/>s to show the bounds to.</param>
        public static void Collider(Collider collider, Color color, float duration, IEnumerable<Player> players = null)
        {
            if (collider is not BoxCollider box)
                return;

            Vector3 worldSize = Vector3.Scale(box.size, box.transform.lossyScale);
            Vector3 worldCenter = box.transform.TransformPoint(box.center);
            Box(worldCenter, worldSize, box.transform.rotation, color, duration, players);
        }

        /// <summary>
        /// Draws a box using exact dimensions.
        /// </summary>
        /// <param name="center">The center point of the box.</param>
        /// <param name="size">The size of the box.</param>
        /// <param name="rotation">The rotation of the box.</param>
        /// <param name="color">The color of the lines.</param>
        /// <param name="duration"> How long the line should remain visible.<para><warning><b>Warning:</b> Avoid using <see cref="float.PositiveInfinity"/> or extremely large values, as these lines cannot be removed from the client once sent.</warning></para></param>
        /// <param name="players">A collection of <see cref="Player"/>s to show the sphere to.</param>
        public static void Box(Vector3 center, Vector3 size, Quaternion rotation, Color color, float duration, IEnumerable<Player> players = null)
        {
            Vector3 extents = size * 0.5f;

            float width = extents.x;
            float length = extents.z;
            float height = extents.y;

            Vector3[] bottomRect = new Vector3[5];
            Vector3[] topRect = new Vector3[5];

            bottomRect[0] = center + (rotation * new Vector3(-width, -height, -length));
            bottomRect[1] = center + (rotation * new Vector3(width, -height, -length));
            bottomRect[2] = center + (rotation * new Vector3(width, -height, length));
            bottomRect[3] = center + (rotation * new Vector3(-width, -height, length));
            bottomRect[4] = bottomRect[0];

            topRect[0] = center + (rotation * new Vector3(-width, height, -length));
            topRect[1] = center + (rotation * new Vector3(width, height, -length));
            topRect[2] = center + (rotation * new Vector3(width, height, length));
            topRect[3] = center + (rotation * new Vector3(-width, height, length));
            topRect[4] = topRect[0];

            Send(players, duration, color, bottomRect);
            Send(players, duration, color, topRect);

            for (int i = 0; i < 4; i++)
            {
                Send(players, duration, color, bottomRect[i], topRect[i]);
            }
        }

        private static Vector3[] GetCirclePoints(Vector3 origin, Quaternion rotation, float radius, int segments, bool horizontal)
        {
            if (segments <= 0)
                segments = 8;

            if (segments % 2 != 0)
                segments++;

            Vector3[] array = new Vector3[segments + 1];
            float num = MathF.PI * 2f / (float)segments;

            for (int i = 0; i < segments; i++)
            {
                float f = (float)i * num;
                float num2 = Mathf.Cos(f) * radius;
                float num3 = Mathf.Sin(f) * radius;
                Vector3 offset = horizontal ? new Vector3(num2, 0f, num3) : new Vector3(num2, num3, 0f);
                array[i] = origin + (rotation * offset);
            }

            array[segments] = array[0];
            return array;
        }

        private static void Send(IEnumerable<Player> players, float duration, Color color, params Vector3[] points)
        {
            if (points == null || points.Length < 2)
                return;

            DrawableLineMessage msg = new(duration, color, points);

            if (players != null)
            {
                using NetworkWriterPooled writer = NetworkWriterPool.Get();
                NetworkMessages.Pack(msg, writer);
                ArraySegment<byte> segment = writer.ToArraySegment();

                foreach (Player ply in players)
                {
                    ply?.Connection.Send(segment);
                }
            }
            else
            {
                NetworkServer.SendToReady(msg);
            }
        }
    }
}