using Godot;
using MyEnums;
using System;

public partial class Garage : StructureBase
{
	private int _id;
	private SceneResources _sceneResources = SceneResources.Instance;

	public override void _Ready()
	{
		base._Ready();
		_id = _sceneResources.StructureCount[StructureType.Garage] + 1;
	}

	private void BuildVehicle()
	{

	}
}
