using Godot;

public partial class AssetServer : Node
{
    public static AssetServer Instance { get; private set; }
    public MyModels Models { get; set; }
    public MyTextures Textures { get; set; }
    public MyScenes Scenes { get; set; }
    public override void _Ready()
    {
        Instance = this;
        Models = new MyModels();
        Textures = new MyTextures();
        Scenes = new MyScenes();
    }
}