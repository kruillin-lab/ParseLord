using AutoRotationPlugin.Rotations;
using AutoRotationPlugin.Rotations.Jobs;
using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoRotationPlugin.Managers
{
    public class RotationManager
    {
        private readonly Configuration _config;
        private readonly Dictionary<uint, IRotation> _rotations;

        // NEW: Targeting Manager Instance
        private readonly TargetingManager _targetingManager;

        public RotationManager(Configuration config)
        {
            _config = config;
            _rotations = new Dictionary<uint, IRotation>();
            _targetingManager = new TargetingManager(config); // Initialize
            InitializeRotations();
        }

        private void InitializeRotations()
        {
            Register(new DragoonRotation());
            Register(new PaladinRotation());
            Register(new WhiteMageRotation());
        }

        private void Register(IRotation rotation)
        {
            if (_rotations.ContainsKey(rotation.JobId)) return;
            _rotations.Add(rotation.JobId, rotation);
            Plugin.Log.Debug($"[ParseLord] Registered rotation for Job ID {rotation.JobId}");
        }

        public ActionInfo? GetNextAction()
        {
            if (!_config.Enabled) return null;
            if (GameState.LocalPlayer == null) return null;

            // 1. RUN AUTO-TARGETING
            // This ensures we have a valid target before the rotation logic asks for one.
            try
            {
                _targetingManager.UpdateTargeting();
            }
            catch (Exception ex)
            {
                // Don't crash the rotation if targeting fails
                Plugin.Log.Warning(ex, "[ParseLord] Targeting Error");
            }

            // 2. Proceed with Rotation
            uint jobId = GameState.LocalPlayer.ClassJob.RowId;

            if (_rotations.TryGetValue(jobId, out var rotation))
            {
                try
                {
                    return rotation.GetNextAction(_config);
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error(ex, $"[ParseLord] Error in rotation for Job {jobId}");
                }
            }

            return null;
        }

        public static int GetHostileCountAround(IBattleChara player, float range)
        {
            if (player == null) return 0;
            return GameState.HostileCountAroundPosition(player.Position, range);
        }
    }
}