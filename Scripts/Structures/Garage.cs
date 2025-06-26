using Godot;
using MyEnums;

public partial class Garage : StructureBase
{
	public int Id { get; private set; }
	private SceneResources _sceneResources = SceneResources.Instance;
	private Signals _signals = Signals.Instance;

	public override void _Ready()
	{
		base._Ready();
		Id = _sceneResources.StructureCount[StructureType.Garage];
		// _signals.BuildVehicle += BuildVehicle;
	}

	public void Activate()
	{
		_signals.BuildVehicle += BuildVehicle;
	}

	public void Deactivate()
	{
		_signals.BuildVehicle -= BuildVehicle;
	}


	private void BuildVehicle(Vehicle vehicle)
	{
		GD.Print($"Building {vehicle.Name} in Garage {Id}");
	}
}
