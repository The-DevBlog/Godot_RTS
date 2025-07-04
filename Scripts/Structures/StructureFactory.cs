using Godot;
using MyEnums;

public partial class StructureFactory : Node
{
	public static StructureFactory Instance { get; private set; }
	[Signal] public delegate StructureBasePlaceholder SelectStructureEventHandler(StructureType structureType);
	private Player _player;
	private MyModels _models;
	public override void _Ready()
	{
		Instance = this;
		_models = AssetServer.Instance.Models;
		_player = PlayerManager.Instance.LocalPlayer;

		SelectStructure += BuildPlaceholder;
	}

	public StructureBasePlaceholder BuildPlaceholder(StructureType structureType)
	{
		// check if enough funds
		StructureBase structure = _models.Structures[structureType].Instantiate<StructureBase>();
		if (_player.Funds < structure.Cost)
		{
			GD.Print("Not enough funds to place " + structure);
			structure.QueueFree();
			return null;
		}
		structure.QueueFree();

		StructureBasePlaceholder placeholder = _models.StructurePlaceholders[structureType].Instantiate<StructureBasePlaceholder>();
		placeholder.Player = _player;

		return placeholder;
	}
}
