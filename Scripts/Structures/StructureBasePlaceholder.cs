using System.Collections.Generic;
using Godot;
using MyEnums;

public partial class StructureBasePlaceholder : StructureBase
{
	[Export] MeshInstance3D ValidityRing { get; set; }
	public readonly HashSet<Area3D> Overlaps = new();
	public bool ValidPlacement => Overlaps.Count == 0;
	private Vector2 _gridCellSize = new Vector2(2.0f, 2.0f);
	private bool _lastValidState = true;
	private ShaderMaterial _validityShader;
	private Camera3D _camera;

	public override void _Ready()
	{
		base._Ready();

		_camera = GetViewport().GetCamera3D();

		Utils.NullExportCheck(ValidityRing);

		Area.AreaEntered += OnAreaEntered;
		Area.AreaExited += OnAreaExited;

		_validityShader = ValidityRing.GetSurfaceOverrideMaterial(0) as ShaderMaterial;
	}

	public override void _Process(double delta)
	{
		GetHoveredMapBase(out Vector3 hitPos);

		hitPos.X = Mathf.Round(hitPos.X / _gridCellSize.X) * _gridCellSize.X;
		hitPos.Z = Mathf.Round(hitPos.Z / _gridCellSize.Y) * _gridCellSize.Y;

		GlobalPosition = hitPos;
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

	public StaticBody3D GetHoveredMapBase(out Vector3 hitPosition)
	{
		hitPosition = Vector3.Zero;

		// Build the ray under the mouse
		Vector2 mousePos = GetViewport().GetMousePosition();
		Vector3 rayOrigin = _camera.ProjectRayOrigin(mousePos);
		Vector3 rayDirection = _camera.ProjectRayNormal(mousePos);
		Vector3 rayEnd = rayOrigin + rayDirection * 1000.0f;

		var spaceState = _camera.GetWorld3D().DirectSpaceState;
		var query = new PhysicsRayQueryParameters3D
		{
			From = rayOrigin,
			To = rayEnd,
			CollisionMask = 2,
		};

		var result = spaceState.IntersectRay(query);
		if (result.Count == 0)
			return null;

		// Extract the collider and check if it’s a StaticBody3D in MapBase
		CollisionObject3D colObj = (CollisionObject3D)result["collider"];
		if (colObj == null)
			return null;

		StaticBody3D bodyHit = colObj as StaticBody3D;
		if (bodyHit == null || !bodyHit.IsInGroup(Group.MapBase.ToString()))
			return null;

		hitPosition = (Vector3)result["position"];
		return bodyHit;
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
