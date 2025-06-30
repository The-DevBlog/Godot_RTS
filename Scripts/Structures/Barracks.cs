using Godot;
using MyEnums;

public partial class Barracks : StructureBase
{
	public int Id { get; private set; }
	private Signals _signals = Signals.Instance;
	private Player _player => PlayerManager.Instance.LocalPlayer;

	public override void _Ready()
	{
		base._Ready();

		int barracksCount = _player.StructureCount[StructureType.Barracks];
		Id = barracksCount;
	}

	public void Activate() => _player.BuildInfantry += BuildInfantry;

	public void Deactivate() => _player.BuildInfantry -= BuildInfantry;

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
