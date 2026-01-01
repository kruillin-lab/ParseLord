using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.Enums;
using System.Linq;
using System.Numerics;
using System;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace AutoRotationPlugin.Managers
{
    public class TargetingManager
    {
        private readonly Configuration _config;

        public TargetingManager(Configuration config)
        {
            _config = config;
        }

        public void UpdateTargeting()
        {
            if (_config.TargetPriority == TargetPriority.None) return;

            var player = GameState.LocalPlayer;
            if (player == null) return;

            var currentTarget = GameState.TargetAsBattleChara;

            if (IsValidEnemy(currentTarget)) return;

            if (_config.InCombatOnly && !GameState.InCombat) return;

            IBattleChara? bestCandidate = null;

            switch (_config.TargetPriority)
            {
                case TargetPriority.TankAssist:
                    bestCandidate = GetTankTarget();
                    break;
                default:
                    bestCandidate = ScanObjectTable(player);
                    break;
            }

            if (bestCandidate != null && bestCandidate.GameObjectId != (currentTarget?.GameObjectId ?? 0))
            {
                // FIX: API 14 uses Property Setter
                Plugin.TargetManager.Target = bestCandidate;
            }
        }

        private IBattleChara? ScanObjectTable(IPlayerCharacter player)
        {
            var candidates = Plugin.ObjectTable
                .OfType<IBattleChara>()
                .Where(obj => IsPotentialTarget(obj, player));

            switch (_config.TargetPriority)
            {
                case TargetPriority.Nearest:
                    return candidates
                        .OrderBy(obj => Vector3.Distance(player.Position, obj.Position))
                        .FirstOrDefault();

                case TargetPriority.LowestHP:
                    return candidates
                        .OrderBy(obj => obj.CurrentHp)
                        .FirstOrDefault();

                case TargetPriority.HighestMaxHP:
                    return candidates
                        .OrderByDescending(obj => obj.MaxHp)
                        .ThenBy(obj => Vector3.Distance(player.Position, obj.Position))
                        .FirstOrDefault();

                default:
                    return null;
            }
        }

        private IBattleChara? GetTankTarget()
        {
            foreach (var member in Plugin.PartyList)
            {
                if (member.GameObject is IBattleChara partyMember)
                {
                    uint jobId = partyMember.ClassJob.RowId;
                    if (jobId == 19 || jobId == 21 || jobId == 32 || jobId == 37)
                    {
                        var tankTarget = partyMember.TargetObject as IBattleChara;
                        if (IsValidEnemy(tankTarget))
                        {
                            return tankTarget;
                        }
                    }
                }
            }
            return null;
        }

        private bool IsPotentialTarget(IBattleChara obj, IPlayerCharacter player)
        {
            if (!obj.IsTargetable || obj.IsDead || obj.CurrentHp == 0) return false;

            if (obj.ObjectKind != ObjectKind.BattleNpc) return false;
            if (!obj.StatusFlags.HasFlag(StatusFlags.Hostile)) return false;

            float dist = Vector3.Distance(player.Position, obj.Position);
            if (dist > _config.AutoTargetRange) return false;

            if (!GameState.InCombat && !obj.StatusFlags.HasFlag(StatusFlags.InCombat)) return false;

            return true;
        }

        private bool IsValidEnemy(IBattleChara? obj)
        {
            if (obj == null) return false;
            if (!obj.IsValid()) return false;
            if (obj.IsDead || obj.CurrentHp == 0) return false;
            if (!obj.IsTargetable) return false;
            if (!obj.StatusFlags.HasFlag(StatusFlags.Hostile)) return false;
            return true;
        }
    }
}