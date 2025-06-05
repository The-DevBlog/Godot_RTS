using System.Text.RegularExpressions;
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

		var structureModel = _models.GetModel(Structure);
		var structure = structureModel.Instantiate() as Node3D;
		if (structure == null)
		{
			Utils.PrintErr("Failed to instantiate structure for " + Structure);
			return;
		}

		var currentScene = GetTree().CurrentScene as Node3D;
		if (currentScene == null)
		{
			Utils.PrintErr("Current scene is not a Node3D.");
			return;
		}

		currentScene.AddChild(structure);
	}
}
