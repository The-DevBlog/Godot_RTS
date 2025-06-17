using Godot;

public partial class StructureBase : StaticBody3D
{
	[Export] public int Energy { get; set; }
	[Export] public int Cost { get; set; }
	[Export] public int BuildTime { get; set; }
	[Export] public Node3D Model;
	private Resources _resources;

	public override void _Ready()
	{
		_resources = Resources.Instance;

		if (Energy == 0)
			Utils.PrintErr("No Energy Assigned to structure");

		if (Cost == 0)
			Utils.PrintErr("No Cost Assigned to structure");

		if (BuildTime == 0)
			Utils.PrintErr("No BuildTime Assigned to structure");

		if (Model == null)
			Utils.PrintErr("Model is not set!");

		SetTeamColor(_resources.TeamColor);
	}

	public void SetTeamColor(Color color)
	{
		foreach (Node child in Model.GetChildren())
		{
			if (child is MeshInstance3D mesh)
			{
				// Get the current material on surface 1
				var originalMaterial = mesh.Mesh.SurfaceGetMaterial(0) as ShaderMaterial;
				if (originalMaterial == null)
				{
					Utils.PrintErr("Material on surface 1 is not a ShaderMaterial.");
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
