using Godot;

public partial class Signals : Node
{
    public static Signals Instance { get; private set; }

    [Signal]
    public delegate void SetUIMaxSizeEventHandler(float maxSize);

    public override void _Ready()
    {
        Instance = this;
    }
}
