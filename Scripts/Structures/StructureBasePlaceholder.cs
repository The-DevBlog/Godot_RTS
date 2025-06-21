using Godot;

public partial class StructureBasePlaceholder : StructureBase
{
	[Export] MeshInstance3D ValidityRing { get; set; }
	private ShaderMaterial _validityShader;

	public override void _Ready()
	{
		base._Ready();

		HP = -1;
		Energy = -1;
		Cost = -1;
		BuildTime = -1;

		Utils.NullExportCheck(ValidityRing);

		_validityShader = ValidityRing.GetSurfaceOverrideMaterial(0) as ShaderMaterial;
	}
}
