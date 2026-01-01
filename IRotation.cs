namespace AutoRotationPlugin.Rotations
{
    public interface IRotation
    {
        uint JobId { get; }
        ActionInfo? GetNextAction(Configuration config);
    }
}