using Godot;

public partial class AssetServer : Node
{
    public static AssetServer Instance { get; private set; }
    public MyModels Models { get; set; }
    public override void _Ready()
    {
        Instance = this;
        Models = new MyModels();
    }
}