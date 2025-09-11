using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using MyEnums;

public partial class Unit : CharacterBody3D, ICostProvider, IDamageable
{
	[Export] public int Team { get; set; }
	[Export] public int Speed { get; set; }
	[Export] public int HP { get; set; }

	[Export] public int Cost { get; set; }
	[Export] public int BuildTime { get; set; }
	[Export] public int Acceleration { get; set; }
	[Export] public bool DebugEnabled { get; set; }
	[Export] public Node3D Death;
	[Export] public float MiniMapRadius { get; set; }
	[Export] private float _rotationSpeed = 220f;

	[ExportCategory("Weapons")]
	[Export] public WeaponSystem PrimaryWeaponSystem { get; set; }

	[ExportCategory("Unit Systems")]
	// [Export] public LODManager LODManager;
	[Export] private HealthSystem _healthSystem;

	[ExportCategory("LOD")]
	[Export] private float _lodNear = 23f;
	[Export] private float _lodHysteresis = 3f;
	[Export] private float _updateHz = 8f;
	[Export] private LODScenes _highId = LODScenes.AntiInfantryHP;
	[Export] private LODScenes _lowId = LODScenes.AntiInfantryLP;

	[ExportCategory("LOD Sockets (RELATIVE TO THIS NODE)")]
	[Export] public NodePath PrimaryTurretPath;
	[Export] public NodePath PrimaryMuzzleContainerPath;
	[Export] public NodePath SecondaryTurretPath;
	[Export] public NodePath SecondaryMuzzleContainerPath;
	[Export] public NodePath AnimationPlayerPath;

	[ExportCategory("Metric")]
	[Export] public bool UseTrue3DDistance = true;

	[ExportCategory("Debug")]
	[Export] public bool DebugLOD = false;
	[Export] public bool DebugSockets = false;

	public int CurrentHP { get; set; }

	// LOD Exposed Sockets
	public Node3D PrimaryTurretYaw { get; private set; }
	public List<Node3D> PrimaryMuzzles { get; private set; } = new();
	public Node3D SecondaryTurretYaw { get; private set; }
	public List<Node3D> SecondaryMuzzles { get; private set; } = new();
	public AnimationPlayer AnimationPlayer { get; private set; }
	// Events (muzzles are arrays)
	public event Action<Node3D, IReadOnlyList<Node3D>, AnimationPlayer> SocketsChangedPrimary;
	public event Action<Node3D, IReadOnlyList<Node3D>, AnimationPlayer> SocketsChangedSecondary;

	// LOD
	private double _accum;
	private bool _initialized;
	private enum LodTier { High, Low }
	private LodTier _lodState = LodTier.Low;
	public event Action<Node3D> ModelChanged;
	private static bool Alive(Node n) => n != null && GodotObject.IsInstanceValid(n) && n.IsInsideTree();
	private bool _swapScheduled;
	[Conditional("DEBUG")] private void DBG(string msg) { if (DebugLOD || DebugSockets) GD.Print($"[LODManager] {msg}"); }


	// Movement
	private float _facingWindowDeg = 10f; // start moving when |diff| <= this
	private float _stopWindowDeg = 18f;   // stop moving when |diff| > this (hysteresis)
	private bool _meshFacesPlusZ = false; // set false if your mesh faces -Z, eg: travel backwards or forwards
	private bool _moving = false; // hysteresis state
	private float _movementDelta;
	private Vector3 _targetPosition;
	private NavigationAgent3D _navigationAgent;

	// Selection
	private Sprite3D _selectBorder;
	private bool _selected = false;
	public bool Selected
	{
		get => _selected;
		set
		{
			if (_selected == value)
				return;

			_selected = value;
			ToggleSelectBorder();
		}
	}

	private Camera3D _cam;
	private Node3D _model;

	public override void _Ready()
	{
		AddToGroup(Group.units.ToString());

		_navigationAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		Utils.NullExportCheck(_navigationAgent);
		_navigationAgent.AvoidanceEnabled = true;
		_navigationAgent.DebugEnabled = DebugEnabled;
		_navigationAgent.VelocityComputed += OnVelocityComputed;

		_selectBorder = GetNode<Sprite3D>("SelectBorder");
		_selectBorder.Visible = false;

		_model = GetNode<Node3D>("Model");
		_targetPosition = Vector3.Zero;
		_cam = GetViewport().GetCamera3D();

		if (HP == 0) Utils.PrintErr("No HP Assigned to unit");
		if (Speed == 0) Utils.PrintErr("No Speed Assigned to unit");
		if (Cost == 0) Utils.PrintErr("No Cost Assigned to unit");
		if (BuildTime == 0) Utils.PrintErr("No BuildTime Assigned to unit");
		if (Acceleration == 0) Utils.PrintErr("No Acceleration Assigned to unit");
		if (Team == 0) Utils.PrintErr("No Team Assigned to unit");
		if (MiniMapRadius == 0) Utils.PrintErr("No MiniMapRadius Assigned to unit");

		Utils.NullCheck(_model);
		Utils.NullExportCheck(PrimaryWeaponSystem);
		Utils.NullExportCheck(_healthSystem);
		// Utils.NullExportCheck(LODManager);
		Utils.NullExportCheck(Death);

		Utils.NullExportCheck(PrimaryTurretPath);
		Utils.NullExportCheck(PrimaryMuzzleContainerPath);
		Utils.NullExportCheck(AnimationPlayerPath);

		CurrentHP = HP;

		// SetTeamColor(_model, PlayerManager.Instance.HumanPlayer.Color);
		EvaluateAndMaybeSwap(initial: true);

	}

	public override void _PhysicsProcess(double delta)
	{
		MoveUnit(delta);

		_accum += delta;
		if (_accum < 1.0 / MathF.Max(1f, _updateHz)) return;
		_accum = 0;
		EvaluateAndMaybeSwap();
	}

	public void ApplyDamage(int amount, Vector3 hitPos, Vector3 hitNormal)
	{
		_healthSystem.ApplyDamage(amount, hitPos, hitNormal);
	}

	public void SetMoveTarget(Vector3 worldPos)
	{
		_targetPosition = worldPos;
		_navigationAgent.TargetPosition = worldPos;
	}

	private protected virtual void PlayAnimation()
	{
		GD.Print("Running from Unit.cs");
	}

	private void MoveUnit(double delta)
	{
		if (NavigationServer3D.MapGetIterationId(_navigationAgent.GetNavigationMap()) == 0) return;
		if (_navigationAgent.IsNavigationFinished()) return;

		// 1) Next waypoint & flat dir
		Vector3 next = _navigationAgent.GetNextPathPosition();
		Vector3 to = next - GlobalPosition;
		to.Y = 0f;
		if (to.LengthSquared() < 0.0001f) return;

		Vector3 desiredDir = to.Normalized();

		// 2) Current & target yaw
		float currentYaw = Rotation.Y; // node's yaw
		float targetYaw = Mathf.Atan2(desiredDir.X, desiredDir.Z);
		if (!_meshFacesPlusZ) targetYaw += Mathf.Pi; // if your model faces -Z

		float maxStep = Mathf.DegToRad(_rotationSpeed) * (float)delta;
		float windowIn = Mathf.DegToRad(_facingWindowDeg);
		float windowOut = Mathf.DegToRad(Mathf.Max(_stopWindowDeg, _facingWindowDeg + 0.1f)); // ensure > windowIn

		// Shortest signed delta in (-PI, PI]
		float diff = Mathf.AngleDifference(currentYaw, targetYaw);

		// 3) Hysteresis state update
		if (_moving && Mathf.Abs(diff) > windowOut) _moving = false;
		else if (!_moving && Mathf.Abs(diff) <= windowIn) _moving = true;

		// 4) Act on state
		if (!_moving)
		{
			// Rotate-in-place using short-arc step
			float step = Mathf.Clamp(diff, -maxStep, maxStep);
			float newYaw = currentYaw + step;
			Rotation = new Vector3(0f, newYaw, 0f);

			if (_navigationAgent.AvoidanceEnabled) _navigationAgent.Velocity = Vector3.Zero;
			else OnVelocityComputed(Vector3.Zero);
			return;
		}
		else
		{
			PlayAnimation();

			// Move forward
			Vector3 vel = desiredDir * Speed;
			if (_navigationAgent.AvoidanceEnabled) _navigationAgent.Velocity = vel;
			else OnVelocityComputed(vel);

			// Keep steering toward target while moving (short-arc)
			float step = Mathf.Clamp(diff, -maxStep, maxStep);
			float newYaw = currentYaw + step;
			Rotation = new Vector3(0f, newYaw, 0f);
		}
	}

	private void OnVelocityComputed(Vector3 safeVelocity)
	{
		Velocity = safeVelocity;
		MoveAndSlide();
	}

	private void ToggleSelectBorder() => _selectBorder.Visible = _selected;

	private void SetTeamColor(Node node, Color color)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is MeshInstance3D mi && mi.Mesh != null)
			{
				var mesh = mi.Mesh;
				int sc = mesh.GetSurfaceCount();
				if (sc == 0)
				{
					// No surfaces to edit; try material_override as a fallback
					if (mi.MaterialOverride is ShaderMaterial smo)
					{
						var dup = (ShaderMaterial)smo.Duplicate(true);
						dup.SetShaderParameter("team_color", color);
						mi.MaterialOverride = dup;
					}
				}
				else
				{
					for (int s = 0; s < sc; s++)
					{
						// Priority: per-surface override → mesh surface material → node material_override
						Material m =
							mi.GetSurfaceOverrideMaterial(s) ??
							mesh.SurfaceGetMaterial(s) ??
							mi.MaterialOverride;

						if (m is ShaderMaterial sm)
						{
							var dup = (ShaderMaterial)sm.Duplicate(true);
							dup.SetShaderParameter("team_color", color);
							// Put it back as a per-surface override so we don't mutate shared assets
							mi.SetSurfaceOverrideMaterial(s, dup);
						}
						else
						{
							// If you *know* everything should use your team shader, you can force it:
							// var forced = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/team_outline.shader") };
							// forced.SetShaderParameter("team_color", color);
							// mi.SetSurfaceOverrideMaterial(s, forced);
							// Otherwise just skip non-shader materials.
						}
					}
				}
			}

			// Recurse
			SetTeamColor(child, color);
		}
	}

	private void EvaluateAndMaybeSwap(bool initial = false)
	{
		_cam ??= GetViewport().GetCamera3D();
		if (_cam == null)
			return;

		float distSq;
		if (UseTrue3DDistance)
			distSq = (_cam.GlobalPosition - GlobalPosition).LengthSquared();
		else
		{
			Vector3 d = _cam.GlobalPosition - GlobalPosition; d.Y = 0f;
			distSq = d.LengthSquared();
		}

		float inDist = MathF.Max(0f, _lodNear - _lodHysteresis);
		float outDist = _lodNear + _lodHysteresis;
		float nearSq = _lodNear * _lodNear;
		float inSq = inDist * inDist;
		float outSq = outDist * outDist;

		var desired = _lodState;
		if (!_initialized) desired = (distSq <= nearSq) ? LodTier.High : LodTier.Low;
		else if (_lodState == LodTier.High && distSq > outSq) desired = LodTier.Low;
		else if (_lodState == LodTier.Low && distSq < inSq) desired = LodTier.High;

		if (DebugLOD) GD.Print($"[LODManager] dist={(float)Math.Sqrt(distSq):0.00} near={_lodNear} in={inDist} out={outDist} cur={_lodState} want={desired}");

		if (!_initialized) { _initialized = true; SwapModelDeferred(desired); return; }
		if (desired != _lodState) SwapModelDeferred(desired);
	}

	private void SwapModelDeferred(LodTier tier)
	{
		if (_swapScheduled) return;
		_swapScheduled = true;
		CallDeferred(nameof(DoSwapModel), (int)tier);
	}

	private void DoSwapModel(int tierInt)
	{
		_swapScheduled = false;

		// snapshot turret yaws safely
		float yawPrimary = Alive(PrimaryTurretYaw) ? PrimaryTurretYaw.Rotation.Y : 0f;
		float yawSecondary = Alive(SecondaryTurretYaw) ? SecondaryTurretYaw.Rotation.Y : 0f;

		var tier = (LodTier)tierInt;
		var sceneId = (tier == LodTier.High) ? _highId : _lowId;
		var ps = AssetServer.Instance.Models.LODs[sceneId];
		if (ps == null) { GD.PushError($"[LODManager] PackedScene for {sceneId} is null."); return; }

		var old = _model;
		var oldXf = old?.Transform ?? Transform3D.Identity;

		if (old != null && old.Name == "Model") old.Name = "Model__OLD";

		var next = ps.Instantiate<Node3D>();
		next.Name = "Model";
		next.Transform = oldXf;

		AddChild(next);
		_model = next;
		_lodState = tier;

		BindSockets();
		ModelChanged?.Invoke(_model);

		if (Alive(PrimaryTurretYaw)) { var r = PrimaryTurretYaw.Rotation; r.Y = yawPrimary; PrimaryTurretYaw.Rotation = r; }
		if (Alive(SecondaryTurretYaw)) { var r = SecondaryTurretYaw.Rotation; r.Y = yawSecondary; SecondaryTurretYaw.Rotation = r; }

		old?.QueueFree();
		if (DebugLOD) GD.Print($"[LODManager] swapped to {tier} (scene {sceneId}).");
	}

	private void BindSockets()
	{
		PrimaryTurretYaw = GetNodeOrNull<Node3D>(PrimaryTurretPath);
		PrimaryMuzzles = CollectMuzzlesFrom(GetNodeOrNull<Node3D>(PrimaryMuzzleContainerPath));

		SecondaryTurretYaw = (SecondaryTurretPath != null && !SecondaryTurretPath.IsEmpty)
			? GetNodeOrNull<Node3D>(SecondaryTurretPath) : null;

		SecondaryMuzzles = (SecondaryMuzzleContainerPath != null && !SecondaryMuzzleContainerPath.IsEmpty)
			? CollectMuzzlesFrom(GetNodeOrNull<Node3D>(SecondaryMuzzleContainerPath))
			: new List<Node3D>();

		AnimationPlayer = GetNodeOrNull<AnimationPlayer>(AnimationPlayerPath);

		if (DebugSockets)
		{
			GD.Print($"[LODManager] PrimaryYaw={PrimaryTurretYaw?.GetPath()}  PrimaryMuzzles={PrimaryMuzzles.Count}");
			GD.Print($"[LODManager] SecondaryYaw={SecondaryTurretYaw?.GetPath()}  SecondaryMuzzles={SecondaryMuzzles.Count}");
		}

		// Defer emits so every listener sees the final, stable lists
		CallDeferred(nameof(EmitPrimary));
		CallDeferred(nameof(EmitSecondary));
	}

	private void EmitPrimary()
	{
		// pass a clone so listeners can’t be affected by future swaps
		SocketsChangedPrimary?.Invoke(PrimaryTurretYaw, new List<Node3D>(PrimaryMuzzles), AnimationPlayer);
	}

	private void EmitSecondary()
	{
		if (SecondaryTurretPath == null && SecondaryMuzzleContainerPath == null) return;
		SocketsChangedSecondary?.Invoke(SecondaryTurretYaw, new List<Node3D>(SecondaryMuzzles), null);
	}

	private static List<Node3D> CollectMuzzlesFrom(Node3D container)
	{
		var list = new List<Node3D>();
		if (container == null) return list;

		bool addedChild = false;
		foreach (var child in container.GetChildren())
		{
			if (child is Node3D n3 && n3.Name.ToString().StartsWith("Muzzle"))
			{
				list.Add(n3);
				addedChild = true;
			}
		}
		if (!addedChild && container.Name.ToString().StartsWith("Muzzle"))
			list.Add(container);

		list.RemoveAll(n => n == null || !GodotObject.IsInstanceValid(n) || !n.IsInsideTree());
		return list;
	}

	public void ForceSwapToHigh() => SwapModelDeferred(LodTier.High);
	public void ForceSwapToLow() => SwapModelDeferred(LodTier.Low);
}
