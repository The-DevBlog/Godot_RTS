using System.ComponentModel.Design;
using Godot;
using MyEnums;

public partial class Garage : StructureBase
{
	public int Id { get; private set; }
	private Signals _signals = Signals.Instance;

	public override void _Ready()
	{
		base._Ready();

		int garageCount = Player.StructureCount[StructureType.Garage];
		Id = garageCount;

		// TODO: This will cause bugs. What if a garage gets destroyed?
		if (Player.GaragesMap.Count == Id)
			Activate();
	}

	public void Activate()
	{
		GD.Print($"Activating Garage {Id} for Player {Player.Id}");
		Player.BuildVehicle += BuildVehicle;
	}

	public void Deactivate() => Player.BuildVehicle -= BuildVehicle;

	private void BuildVehicle(Vehicle vehicle)
	{
		var sceneRoot = GetTree().CurrentScene;

		vehicle.Team = Player.Team;
		sceneRoot.AddChild(vehicle);

		// how far in front of the garage you want to spawn:
		float spawnDistance = 6f;

		// take your garage’s global transform…
		var gtf = this.GlobalTransform;
		// compute forward as –Z:
		Vector3 forward = gtf.Basis.Z.Normalized();

		// build a new transform for the vehicle’s origin:
		Transform3D spawnTransform = gtf;
		spawnTransform.Origin += forward * spawnDistance;

		// now *rotate* the basis 180° around the Y axis so it points backwards:
		//   Mathf.Pi radians is 180 degrees
		var flippedBasis = spawnTransform.Basis.Rotated(Vector3.Up, Mathf.Pi);
		spawnTransform.Basis = flippedBasis;

		vehicle.GlobalTransform = spawnTransform;

		GD.Print($"Building {vehicle.Name} in Garage {Id}");
	}
}
