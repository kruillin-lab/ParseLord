using Dalamud.Game.ClientState.JobGauge.Types;

namespace AutoRotationPlugin
{
    public static class JobGaugeReader
    {
        // === WHITE MAGE ===
        public static bool WHM_HasLily => GetWHMGauge()?.Lily > 0;
        public static bool WHM_HasMaxLilies => GetWHMGauge()?.Lily == 3;
        public static bool WHM_BloodLilyReady => GetWHMGauge()?.BloodLily == 3;

        private static WHMGauge? GetWHMGauge() =>
            Plugin.JobGauges?.Get<WHMGauge>();

        // === DRAGOON (7.0 Architecture Fix) ===
        // Life of the Dragon (LOTD) is no longer on the Gauge struct in API 11.
        // It is now tracked purely as a Status Effect (Buff ID 116).

        private const uint LOTD_STATUS_ID = 116;

        // Redirect to GameState to read the Buff Timer
        public static float DRG_LOTDTimer => GameState.GetStatusDuration(LOTD_STATUS_ID);
        public static bool DRG_IsLOTDActive => GameState.HasStatus(LOTD_STATUS_ID);

        // Eyes were removed. Mapped to Firstminds Focus (0-2 stacks).
        public static int DRG_EyeCount => GetDRGGauge()?.FirstmindsFocusCount ?? 0;
        public static bool DRG_HasMaxFocus => GetDRGGauge()?.FirstmindsFocusCount == 2;

        private static DRGGauge? GetDRGGauge() =>
            Plugin.JobGauges?.Get<DRGGauge>();

        // === PALADIN ===
        public static byte PLD_OathGauge => GetPLDGauge()?.OathGauge ?? 0;

        private static PLDGauge? GetPLDGauge() =>
            Plugin.JobGauges?.Get<PLDGauge>();
    }
}