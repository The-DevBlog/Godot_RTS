using Godot;
using MyEnums;

public partial class Barracks : StructureBase
{
	public int Id { get; private set; }
	private SceneResources _sceneResources = SceneResources.Instance;
	private Signals _signals = Signals.Instance;

	public override void _Ready()
	{
		base._Ready();

		int barracksCount = _sceneResources.StructureCount[StructureType.Barracks];
		Id = barracksCount;

		if (Id == 0)
			Activate();
	}

	public void Activate() => _signals.BuildInfantry += BuildInfantry;

	public void Deactivate() => _signals.BuildInfantry -= BuildInfantry;

	private void BuildInfantry(Infantry infantry)
	{
		var sceneRoot = GetTree().CurrentScene;
		sceneRoot.AddChild(infantry);

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

		infantry.GlobalTransform = spawnT;

		GD.Print($"Building {infantry.Name} in Barracks {Id}");
	}
}
