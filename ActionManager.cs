using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ActionType = FFXIVClientStructs.FFXIV.Client.Game.ActionType;

namespace AutoRotationPlugin
{
    public unsafe class ActionManager : IDisposable
    {
        private static ActionManager? _instance;
        public static ActionManager Instance => _instance ??= new ActionManager();

        private readonly FFXIVClientStructs.FFXIV.Client.Game.ActionManager* _am;
        private const float AnimationLockThreshold = 0.6f;

        private ActionManager()
        {
            _am = FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
        }

        public static void Dispose() => _instance = null;
        void IDisposable.Dispose() => Dispose();

        #region Core Properties

        public float AnimationLock => _am != null ? _am->AnimationLock : 0f;
        public bool IsAnimationLocked => AnimationLock > 0.1f;

        // FIX: Removed IsActionQueued check as it causes compilation errors with some struct versions
        public bool IsQueued => false;

        public uint ComboAction => _am != null ? _am->Combo.Action : 0;
        public float ComboTime => _am != null ? _am->Combo.Timer : 0f;

        #endregion

        #region Execution

        public bool ExecuteAction(uint actionId, bool targetsSelf = false, ulong targetOverrideId = 0)
        {
            if (_am == null) return false;

            ulong targetId = targetOverrideId;
            if (targetId == 0)
            {
                if (targetsSelf)
                    targetId = Plugin.ObjectTable.Length > 0 ? Plugin.ObjectTable[0]?.GameObjectId ?? 0 : 0;
                else
                    targetId = Plugin.TargetManager.Target?.GameObjectId ?? 0;
            }
            if (targetId == 0) return false;

            uint adjustedId = GetAdjustedActionId(actionId);

            if (!CanUseAction(adjustedId, targetId)) return false;

            // API 14 Signature
            return _am->UseAction(ActionType.Action, adjustedId, targetId, 0, 0, 0, null);
        }

        public bool CanUseAction(uint actionId, ulong targetId = 0)
        {
            if (_am == null) return false;
            return _am->GetActionStatus(ActionType.Action, actionId, targetId) == 0;
        }

        public uint GetAdjustedActionId(uint actionId)
        {
            if (_am == null) return actionId;
            return _am->GetAdjustedActionId(actionId);
        }

        #endregion

        #region Weaving & Cooldowns

        public bool CanWeave() => GetGCDRemaining() > AnimationLockThreshold;

        public float GetGCDRemaining()
        {
            if (_am == null) return 0f;
            var detail = _am->GetRecastGroupDetail(57);
            if (detail == null) return 0f;
            return detail->Total - detail->Elapsed;
        }

        public float GetCooldownRemaining(uint actionId)
        {
            if (_am == null) return 0f;
            var group = _am->GetRecastGroup((int)ActionType.Action, actionId);
            var detail = _am->GetRecastGroupDetail(group);
            if (detail == null) return 0f;
            return detail->Total - detail->Elapsed;
        }

        public int GetActionCharges(uint actionId)
        {
            if (_am == null) return 0;
            var group = _am->GetRecastGroup((int)ActionType.Action, actionId);
            var detail = _am->GetRecastGroupDetail(group);
            if (detail == null) return 0;

            // FIX: IsActive is now a boolean in recent ClientStructs. 
            // If IsActive is TRUE, we are on cooldown (charges < Max).
            // If IsActive is FALSE, we are ready (Full charges).
            return !detail->IsActive ? 2 : 0;
        }

        #endregion

        #region Status Helper
        public bool HasStatus(IGameObject? obj, uint statusId)
        {
            if (obj is not IBattleChara chara) return false;
            foreach (var status in chara.StatusList)
            {
                if (status.StatusId == statusId) return true;
            }
            return false;
        }
        #endregion
    }
}