using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Numerics;

namespace AutoRotationPlugin
{
    public static class Safety
    {
        // 0.5f = ~120 degree cone (Standard "In Front" detection)
        private const float FACE_THRESHOLD = 0.5f;

        // UPDATED: Now accepts 'IGameObject' instead of the low-level struct
        public static bool IsFacingTarget(IGameObject? player, IGameObject? target)
        {
            if (player == null || target == null) return false;

            // 1. Calculate the direction from Player to Target
            Vector2 toTarget = new Vector2(
                target.Position.X - player.Position.X,
                target.Position.Z - player.Position.Z
            );

            // 2. Normalize length to 1.0
            toTarget = Vector2.Normalize(toTarget);

            // 3. Calculate Player's forward direction
            // Rotation is stored in radians
            Vector2 playerForward = new Vector2(
                (float)Math.Sin(player.Rotation),
                (float)Math.Cos(player.Rotation)
            );

            // 4. Dot Product checks alignment (1.0 = Facing, -1.0 = Away)
            return Vector2.Dot(toTarget, playerForward) > FACE_THRESHOLD;
        }
    }
}