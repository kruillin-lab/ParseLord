using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using ActionType = FFXIVClientStructs.FFXIV.Client.Game.ActionType;

namespace AutoRotationPlugin;

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

    #region Properties
    public float AnimationLock => _am != null ? _am->AnimationLock : 0f;
    public bool IsAnimationLocked => AnimationLock > 0.1f;
    public uint ComboAction => _am != null ? _am->Combo.Action : 0;
    #endregion

    #region Execution
    public bool ExecuteAction(uint actionId, bool targetsSelf = false, ulong targetOverrideId = 0)
    {
        if (_am == null) return false;

        ulong targetId = targetOverrideId;
        if (targetId == 0)
        {
            if (targetsSelf)
            {
                // WRATH ARCHITECTURE: Use Svc.ClientState
                targetId = Svc.ClientState.LocalPlayer?.GameObjectId ?? 0;
            }
            else
            {
                // WRATH ARCHITECTURE: Use Svc.Targets
                targetId = Svc.Targets.Target?.GameObjectId ?? 0;
            }
        }
        if (targetId == 0) return false;

        uint adjustedId = GetAdjustedActionId(actionId);

        if (!CanUseAction(adjustedId, targetId)) return false;

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

    #region Weaving & CD
    public bool CanWeave() => GetGCDRemaining() > AnimationLockThreshold;

    public float GetGCDRemaining()
    {
        if (_am == null) return 0f;
        var detail = _am->GetRecastGroupDetail(57); // GCD Group
        if (detail == null) return 0f;
        return detail->Total - detail->Elapsed;
    }

    public int GetActionCharges(uint actionId)
    {
        if (_am == null) return 0;
        var group = _am->GetRecastGroup((int)ActionType.Action, actionId);
        var detail = _am->GetRecastGroupDetail(group);
        if (detail == null) return 0;
        return !detail->IsActive ? 2 : 0;
    }

    public bool HasStatus(IGameObject? obj, uint statusId)
    {
        // Simple helper if you want to check generic objects
        // For player/target, prefer GameState.HasStatus
        if (obj is not Dalamud.Game.ClientState.Objects.Types.IBattleChara chara) return false;
        foreach (var status in chara.StatusList)
            if (status.StatusId == statusId) return true;
        return false;
    }
    #endregion
}