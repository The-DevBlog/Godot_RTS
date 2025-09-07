// using System;
// using System.Collections.Generic;
// using System.Diagnostics; // [Conditional("DEBUG")]
// using Godot;
// using MyEnums;

// public partial class LODManager : Node
// {
// 	[ExportCategory("LOD")]
// 	[Export] private float _lodNear = 23f;
// 	[Export] private float _lodHysteresis = 3f;
// 	[Export] private float _updateHz = 8f;
// 	[Export] private LODScenes _highId = LODScenes.AntiInfantryHP;
// 	[Export] private LODScenes LowId = LODScenes.AntiInfantryLP;

// 	[ExportCategory("Sockets (MULTI ONLY, RELATIVE TO THIS NODE)")]
// 	// Set paths like: ../../Model/Rig/Turret , ../../Model/Rig/Turret/Muzzle/MuzzleHP1 , etc.
// 	// [Export] public Godot.Collections.Array<NodePath> TurretYawPaths = new();
// 	[Export] public NodePath PrimaryTurretPath;
// 	[Export] public NodePath PrimaryMuzzlePath;
// 	[Export] public NodePath AnimationPlayerPath;
// 	[Export] public NodePath SecondaryTurretPath;
// 	[Export] public NodePath SecondaryMuzzlePath;

// 	[ExportCategory("Metric")]
// 	[Export] public bool UseTrue3DDistance = true;

// 	[ExportCategory("Debug")]
// 	[Export] public bool DebugLOD = false;
// 	[Export] public bool DebugSockets = false;

// 	private Unit _unit;
// 	private Camera3D _cam;
// 	private Node3D _model;
// 	private double _accum;
// 	private bool _initialized;

// 	private enum LodTier { High, Low }
// 	private LodTier _lodState = LodTier.Low;

// 	// Sockets (lists only)
// 	private List<Node3D> _turretYaws = new();
// 	private List<Node3D> _muzzles = new();
// 	private List<AnimationPlayer> _animationPlayers = new();

// 	// Events
// 	public event Action<IReadOnlyList<Node3D>, IReadOnlyList<Node3D>, IReadOnlyList<AnimationPlayer>> SocketsChangedMany;
// 	// Legacy: expose first entries for existing listeners
// 	public Node3D TurretYaw => _turretYaws.Count > 0 ? _turretYaws[0] : null;
// 	public Node3D Muzzle => _muzzles.Count > 0 ? _muzzles[0] : null;
// 	public AnimationPlayer AnimationPlayer => _animationPlayers.Count > 0 ? _animationPlayers[0] : null;
// 	public event Action<Node3D, Node3D, AnimationPlayer> SocketsChanged;
// 	public event Action<Node3D> ModelChanged;

// 	private bool _swapScheduled;

// 	private static bool Alive(Node n) => n != null && GodotObject.IsInstanceValid(n) && n.IsInsideTree();

// 	[Conditional("DEBUG")] private void DBG(string msg) { if (DebugLOD || DebugSockets) GD.Print($"[LODManager] {msg}"); }
// 	[Conditional("DEBUG")]
// 	private void DBG_LIST(string label, IEnumerable<Node> nodes)
// 	{
// 		if (!(DebugLOD || DebugSockets)) return;
// 		int i = 0;
// 		foreach (var n in nodes)
// 			GD.Print($"[LODManager] {label}[{i++}] name={n?.Name} path={n?.GetPath()}");
// 		if (i == 0) GD.Print($"[LODManager] {label}: <none>");
// 	}

// 	public override void _Ready()
// 	{
// 		Utils.NullExportCheck(PrimaryMuzzlePath);
// 		Utils.NullExportCheck(PrimaryTurretPath);
// 		Utils.NullExportCheck(AnimationPlayerPath);

// 		_cam = GetViewport().GetCamera3D();
// 		_unit = GetNodeOrNull<Unit>("../../");         // LODManager typically under Unit/Model/...
// 		_model = _unit?.GetNodeOrNull<Node3D>("Model"); // may be null initially

// 		if (_unit == null)
// 		{
// 			GD.PushError("[LODManager] Unit not found via '../../'.");
// 			return;
// 		}

// 		EvaluateAndMaybeSwap(initial: true);
// 	}

// 	public override void _PhysicsProcess(double delta)
// 	{
// 		_accum += delta;
// 		if (_accum < 1.0 / MathF.Max(1f, _updateHz)) return;
// 		_accum = 0;
// 		EvaluateAndMaybeSwap();
// 	}

// 	private void EvaluateAndMaybeSwap(bool initial = false)
// 	{
// 		_cam ??= GetViewport().GetCamera3D();
// 		if (_cam == null || _unit == null) return;

// 		float distSq;
// 		if (UseTrue3DDistance)
// 		{
// 			Vector3 d3 = _cam.GlobalPosition - _unit.GlobalPosition;
// 			distSq = d3.LengthSquared();
// 		}
// 		else
// 		{
// 			Vector3 d = _cam.GlobalPosition - _unit.GlobalPosition; d.Y = 0f; distSq = d.LengthSquared();
// 		}

// 		float inDist = MathF.Max(0f, _lodNear - _lodHysteresis);
// 		float outDist = _lodNear + _lodHysteresis;
// 		float nearSq = _lodNear * _lodNear;
// 		float inSq = inDist * inDist;
// 		float outSq = outDist * outDist;

// 		var desired = _lodState;
// 		if (!_initialized)
// 			desired = (distSq <= nearSq) ? LodTier.High : LodTier.Low;
// 		else if (_lodState == LodTier.High && distSq > outSq)
// 			desired = LodTier.Low;
// 		else if (_lodState == LodTier.Low && distSq < inSq)
// 			desired = LodTier.High;

// 		if (DebugLOD)
// 			GD.Print($"[LODManager] dist={(float)Math.Sqrt(distSq):0.00} near={_lodNear} in={inDist} out={outDist} cur={_lodState} want={desired}");

// 		if (!_initialized)
// 		{
// 			_initialized = true;
// 			SwapModelDeferred(desired);
// 			return;
// 		}

// 		if (desired != _lodState)
// 			SwapModelDeferred(desired);
// 	}

// 	private void SwapModelDeferred(LodTier tier)
// 	{
// 		if (_swapScheduled) return;
// 		_swapScheduled = true;
// 		CallDeferred(nameof(DoSwapModel), (int)tier);
// 	}

// 	private void DoSwapModel(int tierInt)
// 	{
// 		_swapScheduled = false;

// 		// Purge stale refs and snapshot yaw safely
// 		_turretYaws.RemoveAll(t => !Alive(t));
// 		var savedYaw = new List<float>(_turretYaws.Count);
// 		foreach (var t in _turretYaws)
// 			savedYaw.Add(Alive(t) ? ((Node3D)t).Rotation.Y : 0f);

// 		var tier = (LodTier)tierInt;
// 		var sceneId = (tier == LodTier.High) ? _highId : LowId;
// 		var ps = AssetServer.Instance.Models.LODs[sceneId];
// 		if (ps == null) { GD.PushError($"[LODManager] PackedScene for {sceneId} is null."); return; }

// 		var old = _model;
// 		var oldXf = old?.Transform ?? Transform3D.Identity;

// 		// *** Avoid name collision so the new node can be named exactly "Model" ***
// 		if (old != null && old.Name == "Model") old.Name = "Model__OLD";

// 		var next = ps.Instantiate<Node3D>();
// 		next.Name = "Model";                 // we rely on this for ../../Model/... paths
// 		next.Transform = oldXf;

// 		_unit.AddChild(next);
// 		_model = next;
// 		_lodState = tier;

// 		BindSockets(_model);
// 		ModelChanged?.Invoke(_model);

// 		// Restore yaw to matching indices
// 		var count = Math.Min(savedYaw.Count, _turretYaws.Count);
// 		for (int i = 0; i < count; i++)
// 		{
// 			var t = _turretYaws[i];
// 			if (!Alive(t)) continue;
// 			var r = t.Rotation; r.Y = savedYaw[i]; t.Rotation = r;
// 		}

// 		old?.QueueFree();

// 		if (DebugLOD)
// 			GD.Print($"[LODManager] swapped to {tier} (scene {sceneId}).");
// 	}

// 	private void BindSockets(Node3D model)
// 	{
// 		_turretYaws.Clear();
// 		_muzzles.Clear();
// 		_animationPlayers.Clear();

// 		if (GetNodeOrNull<Node3D>(PrimaryTurretPath) is Node3D t)
// 			_turretYaws.Add(t);

// 		if (GetNodeOrNull<Node3D>(PrimaryMuzzlePath) is Node3D m)
// 			_muzzles.Add(m);

// 		if (GetNodeOrNull<AnimationPlayer>(AnimationPlayerPath) is AnimationPlayer ap)
// 			_animationPlayers.Add(ap);


// 		// if (GetNodeOrNull<Node3D>(SecondaryTurretPath) is Node3D t2)

// 		SocketsChangedMany?.Invoke(_turretYaws, _muzzles, _animationPlayers);
// 		SocketsChanged?.Invoke(TurretYaw, Muzzle, AnimationPlayer);
// 	}


// 	// private void BindSockets(Node3D model)
// 	// {
// 	// 	TurretYaws.Clear();
// 	// 	Muzzles.Clear();
// 	// 	AnimationPlayers.Clear();

// 	// 	// All NodePaths resolved RELATIVE TO THIS LODManager (so ../../Model/... is valid)
// 	// 	if (TurretYawPaths != null)
// 	// 	{
// 	// 		// GD.Print("TurretYawPaths count: " + TurretYawPaths.Count);
// 	// 		foreach (var p in TurretYawPaths)
// 	// 		{
// 	// 			// GD.Print("TurretYaw path: " + p);
// 	// 			if (p != null && !p.IsEmpty)
// 	// 			{
// 	// 				if (GetNodeOrNull<Node3D>(p) is Node3D t)
// 	// 				{
// 	// 					// Utils.PrintTree(t);
// 	// 					TurretYaws.Add(t);
// 	// 				}
// 	// 			}
// 	// 		}
// 	// 	}

// 	// 	if (MuzzlePaths != null)
// 	// 	{
// 	// 		// GD.Print("MuzzlePaths count: " + MuzzlePaths.Count);
// 	// 		foreach (var p in MuzzlePaths)
// 	// 		{
// 	// 			// GD.Print("Muzzle path: " + p);
// 	// 			if (p != null && !p.IsEmpty)
// 	// 			{
// 	// 				if (GetNodeOrNull<Node3D>(p) is Node3D m)
// 	// 				{
// 	// 					// Utils.PrintTree(m);
// 	// 					Muzzles.Add(m);
// 	// 				}
// 	// 			}
// 	// 		}
// 	// 	}

// 	// 	if (AnimationPlayerPaths != null)
// 	// 		foreach (var p in AnimationPlayerPaths)
// 	// 			if (p != null && !p.IsEmpty)
// 	// 				if (GetNodeOrNull<AnimationPlayer>(p) is AnimationPlayer ap) AnimationPlayers.Add(ap);

// 	// 	if (DebugSockets)
// 	// 	{
// 	// 		GD.Print($"[LODManager] After BindSockets: turrets={TurretYaws.Count} muzzles={Muzzles.Count} anims={AnimationPlayers.Count}");
// 	// 		DBG_LIST("TurretYaw", TurretYaws);
// 	// 		DBG_LIST("Muzzle", Muzzles);
// 	// 	}

// 	// 	SocketsChangedMany?.Invoke(TurretYaws, Muzzles, AnimationPlayers);
// 	// 	SocketsChanged?.Invoke(TurretYaw, Muzzle, AnimationPlayer);
// 	// }

// 	// Optional manual testing helpers
// 	public void ForceSwapToHigh() => SwapModelDeferred(LodTier.High);
// 	public void ForceSwapToLow() => SwapModelDeferred(LodTier.Low);
// }

using System;
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
	[Export] public NodePath PrimaryMuzzlePath;      // container or single muzzle node
	[Export] public NodePath AnimationPlayerPath;
	[Export] public NodePath SecondaryTurretPath;    // optional
	[Export] public NodePath SecondaryMuzzlePath;    // optional

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

	// Exposed sockets (no lists)
	public Node3D PrimaryTurretYaw { get; private set; }
	public Node3D PrimaryMuzzle { get; private set; }
	public Node3D SecondaryTurretYaw { get; private set; }
	public Node3D SecondaryMuzzle { get; private set; }
	public AnimationPlayer AnimationPlayer { get; private set; }

	// Events (no “Many”)
	public event Action<Node3D, Node3D, AnimationPlayer> SocketsChangedPrimary;
	public event Action<Node3D, Node3D, AnimationPlayer> SocketsChangedSecondary;
	// Legacy single (mapped to primary)
	public event Action<Node3D, Node3D, AnimationPlayer> SocketsChanged;

	public event Action<Node3D> ModelChanged;

	private bool _swapScheduled;

	private static bool Alive(Node n) => n != null && GodotObject.IsInstanceValid(n) && n.IsInsideTree();
	[Conditional("DEBUG")] private void DBG(string msg) { if (DebugLOD || DebugSockets) GD.Print($"[LODManager] {msg}"); }

	public override void _Ready()
	{
		Utils.NullExportCheck(PrimaryTurretPath);
		Utils.NullExportCheck(PrimaryMuzzlePath);
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

		// snapshot current yaws
		float yawPrimary = Alive(PrimaryTurretYaw) ? PrimaryTurretYaw.Rotation.Y : 0f;
		float yawSecondary = Alive(SecondaryTurretYaw) ? SecondaryTurretYaw.Rotation.Y : 0f;

		var tier = (LodTier)tierInt;
		var sceneId = (tier == LodTier.High) ? _highId : _lowId;
		var ps = AssetServer.Instance.Models.LODs[sceneId];
		if (ps == null) { GD.PushError($"[LODManager] PackedScene for {sceneId} is null."); return; }

		var old = _model;
		var oldXf = old?.Transform ?? Transform3D.Identity;

		// ensure new can be named exactly "Model"
		if (old != null && old.Name == "Model") old.Name = "Model__OLD";

		var next = ps.Instantiate<Node3D>();
		next.Name = "Model";
		next.Transform = oldXf;

		_unit.AddChild(next);
		_model = next;
		_lodState = tier;

		BindSockets(_model);
		ModelChanged?.Invoke(_model);

		// restore yaws
		if (Alive(PrimaryTurretYaw)) { var r = PrimaryTurretYaw.Rotation; r.Y = yawPrimary; PrimaryTurretYaw.Rotation = r; }
		if (Alive(SecondaryTurretYaw)) { var r = SecondaryTurretYaw.Rotation; r.Y = yawSecondary; SecondaryTurretYaw.Rotation = r; }

		old?.QueueFree();
		if (DebugLOD) GD.Print($"[LODManager] swapped to {tier} (scene {sceneId}).");
	}

	private void BindSockets(Node3D model)
	{
		PrimaryTurretYaw = GetNodeOrNull<Node3D>(PrimaryTurretPath);
		PrimaryMuzzle = GetNodeOrNull<Node3D>(PrimaryMuzzlePath);
		SecondaryTurretYaw = (SecondaryTurretPath != null && !SecondaryTurretPath.IsEmpty) ? GetNodeOrNull<Node3D>(SecondaryTurretPath) : null;
		SecondaryMuzzle = (SecondaryMuzzlePath != null && !SecondaryMuzzlePath.IsEmpty) ? GetNodeOrNull<Node3D>(SecondaryMuzzlePath) : null;
		AnimationPlayer = GetNodeOrNull<AnimationPlayer>(AnimationPlayerPath);

		if (DebugSockets)
		{
			GD.Print($"[LODManager] PrimaryYaw={PrimaryTurretYaw?.GetPath()}  PrimaryMuzzle={PrimaryMuzzle?.GetPath()}  Anim={AnimationPlayer?.GetPath()}");
			GD.Print($"[LODManager] SecondaryYaw={SecondaryTurretYaw?.GetPath()}  SecondaryMuzzle={SecondaryMuzzle?.GetPath()}");
		}

		// Broadcast primary + legacy
		SocketsChangedPrimary?.Invoke(PrimaryTurretYaw, PrimaryMuzzle, AnimationPlayer);
		SocketsChanged?.Invoke(PrimaryTurretYaw, PrimaryMuzzle, AnimationPlayer);

		// Broadcast secondary (only if configured)
		if (SecondaryTurretPath != null || SecondaryMuzzlePath != null)
			SocketsChangedSecondary?.Invoke(SecondaryTurretYaw, SecondaryMuzzle, AnimationPlayer);
	}

	public void ForceSwapToHigh() => SwapModelDeferred(LodTier.High);
	public void ForceSwapToLow() => SwapModelDeferred(LodTier.Low);
}
