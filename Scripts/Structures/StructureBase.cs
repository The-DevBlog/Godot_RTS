using Godot;
using MyEnums;

public partial class StructureBase : StaticBody3D, ICostProvider
{
	[Export] public Player Player { get; set; }
	[Export] public StructureType StructureType { get; set; }
	[Export] public int HP { get; set; }
	[Export] public int Energy { get; set; }
	[Export] public int Cost { get; set; }
	[Export] public int BuildTime { get; set; }
	[Export] public Node3D Model;
	public Area3D Area { get; private set; }

	public override void _Ready()
	{
		Area = GetNode<Area3D>("Area3D");

		if (HP == 0) Utils.PrintErr("No HP Assigned to structure");
		if (Energy == 0) Utils.PrintErr("No Energy Assigned to structure");
		if (Cost == 0) Utils.PrintErr("No Cost Assigned to structure");
		if (BuildTime == 0) Utils.PrintErr("No BuildTime Assigned to structure");
		if (StructureType == StructureType.None) Utils.PrintErr("StructureType is not set for structure");
		if (Model == null) Utils.PrintErr("Model is not assigned for structure: " + StructureType);

		Player = PlayerManager.Instance.LocalPlayer;

		Utils.NullExportCheck(Player);
		Utils.NullExportCheck(Model);
		Utils.NullExportCheck(Area);

		if (Player == null)
		{
			Player = PlayerManager.Instance.LocalPlayer;
		}

		GD.Print("Structure owner: " + Player.Name + ", Id: " + Player.Id);
		SetTeamColor(Player.Color);
	}

	public void SetTeamColor(Color color)
	{
		foreach (Node child in Model.GetChildren())
		{
			if (child is MeshInstance3D mesh)
			{
				// Get the current material on surface 0
				var originalMaterial = mesh.Mesh.SurfaceGetMaterial(0) as ShaderMaterial;
				if (originalMaterial == null)
				{
					Utils.PrintErr("Material on surface 0 is null.");
					continue;
				}

				// Duplicate the material to avoid affecting all instances
				ShaderMaterial matInstance = (ShaderMaterial)originalMaterial.Duplicate();

				// Set the uniform parameter
				matInstance.SetShaderParameter("team_color", color);

				// Assign it back to the mesh
				mesh.SetSurfaceOverrideMaterial(0, matInstance);
			}
		}
	}
}
