using System.Collections.Generic;
using Godot;

public partial class StructureBasePlaceholder : StructureBase
{
	[Export] MeshInstance3D ValidityRing { get; set; }
	public readonly HashSet<Area3D> Overlaps = new();
	public bool ValidPlacement => Overlaps.Count == 0;
	private bool _lastValidState = true;
	private ShaderMaterial _validityShader;

	public override void _Ready()
	{
		base._Ready();

		Utils.NullExportCheck(ValidityRing);

		Area.AreaEntered += OnAreaEntered;
		Area.AreaExited += OnAreaExited;

		_validityShader = ValidityRing.GetSurfaceOverrideMaterial(0) as ShaderMaterial;
	}

	public void OnAreaEntered(Area3D other)
	{
		if (other == Area)
			return;   // ignore self-entering

		Overlaps.Add(other);

		UpdateValidityShader();
		// optional: update visuals here
	}

	public void OnAreaExited(Area3D other)
	{
		Overlaps.Remove(other);

		UpdateValidityShader();
		// optional: update visuals here
	}

	private void UpdateValidityShader()
	{
		bool nowValid = ValidPlacement;
		// only flip if it really changed
		if (nowValid == _lastValidState)
			return;
		_lastValidState = nowValid;

		// this assumes your shader has a `uniform bool valid_placement;`
		_validityShader.SetShaderParameter("valid_placement", nowValid);
	}
}
