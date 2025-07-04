using Godot;
using MyEnums;

public partial class Garage : StructureBase
{
	public int Id { get; private set; }
	private Player _player = PlayerManager.Instance.LocalPlayer;
	private Signals _signals = Signals.Instance;

	public override void _Ready()
	{
		base._Ready();

		int garageCount = _player.StructureCount[StructureType.Garage];
		Id = garageCount;
	}

	public void Activate() => _player.BuildVehicle += BuildVehicle;

	public void Deactivate() => _player.BuildVehicle -= BuildVehicle;

	private void BuildVehicle(Vehicle vehicle)
	{
		GD.Print("BUilding vehcile");
		var sceneRoot = GetTree().CurrentScene;
		sceneRoot.AddChild(vehicle);

		// how far in front of the garage you want to spawn:
		float spawnDistance = 6f;

		// take your garage’s global transform…
		var gtf = this.GlobalTransform;
		// compute forward as –Z:
		Vector3 forward = gtf.Basis.Z.Normalized();

		// build a new transform for the vehicle’s origin:
		var spawnT = gtf;
		spawnT.Origin += forward * spawnDistance;

		// now *rotate* the basis 180° around the Y axis so it points backwards:
		//   Mathf.Pi radians is 180 degrees
		var flippedBasis = spawnT.Basis.Rotated(Vector3.Up, Mathf.Pi);
		spawnT.Basis = flippedBasis;

		vehicle.GlobalTransform = spawnT;

		GD.Print($"Building {vehicle.Name} in Garage {Id}");
	}
}
