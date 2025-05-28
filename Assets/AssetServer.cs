using Godot;

public partial class AssetServer : Node
{
    public static AssetServer Instance { get; private set; }
    public MyMaterials Materials { get; private set; }
    public override void _Ready()
    {
        Instance = this;
        Materials = new MyMaterials();
    }

    public class MyMaterials
    {
        public MyMaterials()
        {
        }
    }
}