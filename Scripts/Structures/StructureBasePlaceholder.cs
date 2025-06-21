using Godot;

public partial class StructureBasePlaceholder : StructureBase
{
	[Export] MeshInstance3D ValidityRing { get; set; }
	private ShaderMaterial _validityShader;

	public override void _Ready()
	{
		base._Ready();

		Utils.NullExportCheck(ValidityRing);

		_validityShader = ValidityRing.GetSurfaceOverrideMaterial(0) as ShaderMaterial;
	}
}
