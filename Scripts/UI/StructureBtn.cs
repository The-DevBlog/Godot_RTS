using Godot;
using MyEnums;

public partial class StructureBtn : Button
{
	[Export]
	public Structure Structure { get; set; }
	private MyModels _models => AssetServer.Instance.Models;

	public override void _Ready()
	{
		if (Structure == Structure.None)
			Utils.PrintErr("Structure Enum is not set for " + Name);

		Pressed += OnButtonPressed;
	}

	private void OnButtonPressed()
	{
		if (Structure == Structure.None)
		{
			Utils.PrintErr("Structure is set to  none!");
			return;
		}

		PackedScene structureModel = _models.Structures[Structure];
		Node3D structure = structureModel.Instantiate() as Node3D;
		if (structure == null)
		{
			Utils.PrintErr("Failed to instantiate structure for " + Structure);
			return;
		}

		Node3D currentScene = GetTree().CurrentScene as Node3D;
		if (currentScene == null)
		{
			Utils.PrintErr("Current scene is not a Node3D.");
			return;
		}

		currentScene.AddChild(structure);
	}
}
