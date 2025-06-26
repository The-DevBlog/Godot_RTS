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

		int garageCount = _sceneResources.StructureCount[StructureType.Garage];
		Id = garageCount;

		if (Id == 0)
			Activate();
	}

	public void Activate() => _signals.BuildVehicle += BuildVehicle;

	public void Deactivate() => _signals.BuildVehicle -= BuildVehicle;

	private void BuildVehicle(Vehicle vehicle)
	{
		// Get the root node of the *current* scene:
		//    This returns whatever you called `GetTree().ChangeSceneTo(...)` on
		//    (e.g. your main world node, which might be a Node2D or a Spatial).
		var sceneRoot = GetTree().CurrentScene;
		if (sceneRoot == null)
		{
			GD.PrintErr("No CurrentScene set – are you running from a scene that was loaded via ChangeScene?");
			return;
		}

		sceneRoot.AddChild(vehicle);

		Vector3 position = GlobalPosition;
		position.Z += 5;
		vehicle.GlobalPosition = position;

		GD.Print($"Building {vehicle.Name} in Garage {Id}");
	}

}
