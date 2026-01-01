namespace AutoRotationPlugin
{
    public class ActionInfo
    {
        public uint ActionId { get; set; }
        public string Name { get; set; }
        public bool TargetsSelf { get; set; }
        public ulong TargetOverrideId { get; set; }

        public ActionInfo(uint actionId, string name, bool targetsSelf = false)
        {
            ActionId = actionId;
            Name = name;
            TargetsSelf = targetsSelf;
            TargetOverrideId = 0;
        }
    }
}