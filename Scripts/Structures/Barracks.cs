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
		GD.Print($"Building {infantry.Name} in Barracks {Id}");
	}
}
