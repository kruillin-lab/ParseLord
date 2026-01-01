namespace AutoRotationPlugin
{
    public enum RotationPhase
    {
        Idle,
        Opener,
        Burst,
        Filler,
        Downtime
    }

    public class RotationState
    {
        public RotationPhase CurrentPhase { get; private set; } = RotationPhase.Idle;
        public bool IsInBurstWindow { get; private set; }
        public bool IsInCombat { get; private set; }
        public float TimeInCombat { get; private set; }
        
        // Opener State
        public int OpenerStep { get; set; } = 0;
        public bool OpenerFinished { get; set; } = false;

        public void Update(float deltaTime, bool inCombat, bool hasTarget, float playerHP, float lowestPartyHP)
        {
            if (inCombat)
            {
                if (!IsInCombat)
                {
                    // Combat just started
                    IsInCombat = true;
                    TimeInCombat = 0;
                    OpenerStep = 0;
                    OpenerFinished = false;
                    CurrentPhase = RotationPhase.Opener;
                }
                else
                {
                    TimeInCombat += deltaTime;
                }

                // Simple 2-minute burst logic (standard for FFXIV)
                // Burst window is usually 0-20s, 120-140s, etc.
                // We add a slight offset (e.g., starts at 2.5s) to align with raid buffs
                float burstCycle = TimeInCombat % 120f;
                IsInBurstWindow = (burstCycle >= 0 && burstCycle <= 20) || (burstCycle >= 120);

                if (OpenerFinished)
                {
                    CurrentPhase = IsInBurstWindow ? RotationPhase.Burst : RotationPhase.Filler;
                }
            }
            else
            {
                IsInCombat = false;
                TimeInCombat = 0;
                CurrentPhase = RotationPhase.Idle;
                IsInBurstWindow = false;
                OpenerFinished = false;
                OpenerStep = 0;
            }
        }
    }
}