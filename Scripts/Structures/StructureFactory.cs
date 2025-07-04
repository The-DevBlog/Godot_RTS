using Godot;
using MyEnums;

public partial class StructureFactory : Node
{
	public static StructureFactory Instance { get; private set; }
	private Player _player;
	private MyModels _models;
	public override void _Ready()
	{
		Instance = this;
		_models = AssetServer.Instance.Models;
		_player = PlayerManager.Instance.LocalPlayer;
	}

	public StructureBasePlaceholder BuildPlaceholder(StructureType structureType)
	{
		// Deselect existing selections
		_player.EmitSignal(nameof(_player.DeselectAllUnits));

		// check if enough funds
		StructureBase structure = _models.Structures[structureType].Instantiate<StructureBase>();
		if (_player.Funds < structure.Cost)
		{
			GD.Print("Not enough funds to place " + structure);
			structure.QueueFree();
			return null;
		}
		structure.QueueFree();

		// Deselect existing selections
		_player.EmitSignal(nameof(_player.DeselectAllUnits));

		// Check max structures before placement
		if (_player.MaxStructureCountReached(structureType))
			return null;

		StructureBasePlaceholder placeholder = _models.StructurePlaceholders[structureType].Instantiate<StructureBasePlaceholder>();
		placeholder.Player = _player;

		GlobalResources.Instance.IsPlacingStructure = true;

		return placeholder;
	}
}
