using System.Collections.Generic;
using Godot;

public partial class StructureBasePlaceholder : StructureBase
{
	[Export] MeshInstance3D ValidityRing { get; set; }
	public readonly HashSet<Area3D> Overlaps = new();
	public bool ValidPlacement => Overlaps.Count == 0;

	private ShaderMaterial _validityShader;

	public override void _Ready()
	{
		base._Ready();

		Utils.NullExportCheck(ValidityRing);

		_validityShader = ValidityRing.GetSurfaceOverrideMaterial(0) as ShaderMaterial;
	}

	public void OnAreaEntered(Area3D other)
	{
		if (other == Area)
			return;   // ignore self-entering

		Overlaps.Add(other);
		// optional: update visuals here
	}

	public void OnAreaExited(Area3D other)
	{
		Overlaps.Remove(other);
		// optional: update visuals here
	}
}
