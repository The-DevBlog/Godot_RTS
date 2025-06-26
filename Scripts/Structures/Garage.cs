using Godot;
using MyEnums;

public partial class Garage : StructureBase
{
	private int _id = 1;
	private SceneResources _sceneResources = SceneResources.Instance;
	private Signals _signals = Signals.Instance;

	public override void _Ready()
	{
		base._Ready();
		_id = _sceneResources.StructureCount[StructureType.Garage] + 1;
		_signals.BuildVehicle += BuildVehicle;
	}

	private void BuildVehicle(int garageId)
	{
		GD.Print("Building vehicle in Garage ID: " + garageId);
	}
}
