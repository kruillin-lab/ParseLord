namespace AutoRotationPlugin.Rotations;

public class RotationAction
{
    public string Name { get; set; } = string.Empty;
    public uint ActionId { get; set; }
    public bool TargetsSelf { get; set; } = false;
    public uint? TargetOverrideId { get; set; } = null;

    public RotationAction(string name, uint id, bool self = false)
    {
        Name = name;
        ActionId = id;
        TargetsSelf = self;
    }
}