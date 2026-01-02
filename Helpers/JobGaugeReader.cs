using Dalamud.Game.ClientState.JobGauge.Types;

namespace AutoRotationPlugin
{
    public static class JobGaugeReader
    {
        // === WHITE MAGE ===
        public static bool WHM_HasLily => GetWHMGauge()?.Lily > 0;
        public static bool WHM_HasMaxLilies => GetWHMGauge()?.Lily == 3;
        public static bool WHM_BloodLilyReady => GetWHMGauge()?.BloodLily == 3;

        // FIX: Use Svc.Gauges
        private static WHMGauge? GetWHMGauge() => Svc.Gauges.Get<WHMGauge>();

        // === DRAGOON ===
        private const uint LOTD_STATUS_ID = 116;

        public static float DRG_LOTDTimer => GameState.GetStatusDuration(LOTD_STATUS_ID);
        public static bool DRG_IsLOTDActive => GameState.HasStatus(LOTD_STATUS_ID);

        public static int DRG_EyeCount => GetDRGGauge()?.FirstmindsFocusCount ?? 0;
        public static bool DRG_HasMaxFocus => GetDRGGauge()?.FirstmindsFocusCount == 2;

        // FIX: Use Svc.Gauges
        private static DRGGauge? GetDRGGauge() => Svc.Gauges.Get<DRGGauge>();

        // === PALADIN ===
        public static byte PLD_OathGauge => GetPLDGauge()?.OathGauge ?? 0;

        // FIX: Use Svc.Gauges
        private static PLDGauge? GetPLDGauge() => Svc.Gauges.Get<PLDGauge>();
    }
}