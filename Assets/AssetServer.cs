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
        public StandardMaterial3D Selected { get; }
        public StandardMaterial3D Unselected { get; }

        public MyMaterials()
        {
            Selected = GD.Load<StandardMaterial3D>("res://Assets/Materials/Selected.tres");
            Unselected = GD.Load<StandardMaterial3D>("res://Assets/Materials/Unselected.tres");
        }
    }
}