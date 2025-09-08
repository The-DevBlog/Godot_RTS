using System;
using System.Collections.Generic;
using System.Diagnostics; // [Conditional("DEBUG")]
using Godot;
using MyEnums;

public partial class LODManager : Node
{
	[ExportCategory("LOD")]
	[Export] private float _lodNear = 23f;
	[Export] private float _lodHysteresis = 3f;
	[Export] private float _updateHz = 8f;
	[Export] private LODScenes _highId = LODScenes.AntiInfantryHP;
	[Export] private LODScenes _lowId = LODScenes.AntiInfantryLP;

	[ExportCategory("Sockets (RELATIVE TO THIS NODE)")]
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

	private Unit _unit;
	private Camera3D _cam;
	private Node3D _model;
	private double _accum;
	private bool _initialized;

	private enum LodTier { High, Low }
	private LodTier _lodState = LodTier.Low;

	// Exposed sockets
	public Node3D PrimaryTurretYaw { get; private set; }
	public List<Node3D> PrimaryMuzzles { get; private set; } = new();
	public Node3D SecondaryTurretYaw { get; private set; }
	public List<Node3D> SecondaryMuzzles { get; private set; } = new();
	public AnimationPlayer AnimationPlayer { get; private set; }

	// Events (muzzles are arrays)
	public event Action<Node3D, IReadOnlyList<Node3D>, AnimationPlayer> SocketsChangedPrimary;
	public event Action<Node3D, IReadOnlyList<Node3D>, AnimationPlayer> SocketsChangedSecondary;

	public event Action<Node3D> ModelChanged;

	private bool _swapScheduled;

	private static bool Alive(Node n) => n != null && GodotObject.IsInstanceValid(n) && n.IsInsideTree();
	[Conditional("DEBUG")] private void DBG(string msg) { if (DebugLOD || DebugSockets) GD.Print($"[LODManager] {msg}"); }

	public override void _Ready()
	{
		Utils.NullExportCheck(PrimaryTurretPath);
		Utils.NullExportCheck(PrimaryMuzzleContainerPath);
		Utils.NullExportCheck(AnimationPlayerPath);

		_cam = GetViewport().GetCamera3D();
		_unit = GetNodeOrNull<Unit>("../../");
		_model = _unit?.GetNodeOrNull<Node3D>("Model");
		if (_unit == null) { GD.PushError("[LODManager] Unit not found via '../../'."); return; }

		EvaluateAndMaybeSwap(initial: true);
	}

	public override void _PhysicsProcess(double delta)
	{
		_accum += delta;
		if (_accum < 1.0 / MathF.Max(1f, _updateHz)) return;
		_accum = 0;
		EvaluateAndMaybeSwap();
	}

	private void EvaluateAndMaybeSwap(bool initial = false)
	{
		_cam ??= GetViewport().GetCamera3D();
		if (_cam == null || _unit == null) return;

		float distSq;
		if (UseTrue3DDistance)
			distSq = (_cam.GlobalPosition - _unit.GlobalPosition).LengthSquared();
		else
		{
			Vector3 d = _cam.GlobalPosition - _unit.GlobalPosition; d.Y = 0f;
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

		_unit.AddChild(next);
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
