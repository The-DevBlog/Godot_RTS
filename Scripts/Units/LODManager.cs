// using System;
// using Godot;
// using MyEnums;

// public partial class LODManager : Node
// {
// 	[ExportCategory("LOD")]
// 	[Export] public float LodNear = 23f;
// 	[Export] public float LodHysteresis = 3f;
// 	[Export] public float UpdateHz = 8f;
// 	[Export] public LODScenes HighId = LODScenes.AntiInfantryHP;
// 	[Export] public LODScenes LowId = LODScenes.AntiInfantryLP;

// 	[ExportCategory("Sockets (inside Model)")]
// 	[Export] public string TurretPath = "Rig/Turret";
// 	[Export] public string MuzzlePath = "Rig/Turret/Muzzle";
// 	[Export] public string AnimationPlayerPath = "AnimationPlayer";

// 	[ExportCategory("Metric")]
// 	[Export] public bool UseTrue3DDistance = true; // NEW: respond to camera height

// 	private Unit _unit;
// 	private Camera3D _cam;
// 	private Node3D _model;
// 	private double _accum;
// 	private bool _initialized;

// 	private enum LodTier { High, Low }
// 	private LodTier _lodState = LodTier.Low;

// 	// sockets
// 	public Node3D TurretYaw { get; private set; }
// 	public Node3D Muzzle { get; private set; }
// 	public AnimationPlayer AnimationPlayer { get; private set; }

// 	public event Action<Node3D, Node3D, AnimationPlayer> SocketsChanged;  // (turretYaw, muzzle)
// 	public event Action<Node3D> ModelChanged;

// 	private bool _swapScheduled;

// 	public override void _Ready()
// 	{
// 		_cam = GetViewport().GetCamera3D();
// 		_unit = GetNodeOrNull<Unit>("../../");
// 		_model = _unit.GetNodeOrNull<Node3D>("Model"); // may be null (that’s fine)
// 													   // _unit = GetParent<Unit>() ?? GetOwner<Unit>();
// 		Utils.NullCheck(_unit);
// 		Utils.NullCheck(_model);

// 		EvaluateAndMaybeSwap(initial: true);
// 	}

// 	public override void _PhysicsProcess(double delta)
// 	{
// 		_accum += delta;
// 		if (_accum < 1.0 / MathF.Max(1f, UpdateHz)) return;
// 		_accum = 0;
// 		EvaluateAndMaybeSwap();
// 	}

// 	private void EvaluateAndMaybeSwap(bool initial = false)
// 	{
// 		_cam ??= GetViewport().GetCamera3D();            // NEW: reacquire if null
// 		if (_cam == null || _unit == null) return;

// 		// --- choose distance metric ---
// 		float distSq;
// 		if (UseTrue3DDistance)
// 		{
// 			Vector3 d3 = _cam.GlobalPosition - _unit.GlobalPosition;
// 			distSq = d3.LengthSquared();
// 		}
// 		else
// 		{
// 			Vector3 d = _cam.GlobalPosition - _unit.GlobalPosition;
// 			d.Y = 0f;
// 			distSq = d.LengthSquared();
// 		}

// 		float inDist = MathF.Max(0f, LodNear - LodHysteresis);
// 		float outDist = LodNear + LodHysteresis;

// 		float nearSq = LodNear * LodNear;
// 		float inSq = inDist * inDist;
// 		float outSq = outDist * outDist;

// 		var desired = _lodState;

// 		// Decide desired tier from current distance
// 		if (!_initialized)
// 		{
// 			desired = (distSq <= nearSq) ? LodTier.High : LodTier.Low;
// 		}
// 		else if (_lodState == LodTier.High && distSq > outSq)
// 		{
// 			desired = LodTier.Low;
// 		}
// 		else if (_lodState == LodTier.Low && distSq < inSq)
// 		{
// 			desired = LodTier.High;
// 		}

// 		// On first run, FORCE alignment to the desired tier (even if a model exists)
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

// 		// --- save current turret orientation BEFORE replacing the model ---
// 		// (use local yaw since this is a turret yaw node under the model)
// 		float savedTurretYawY = 0f;
// 		if (TurretYaw != null)
// 			savedTurretYawY = TurretYaw.Rotation.Y;   // radians

// 		var tier = (LodTier)tierInt;
// 		var sceneId = (tier == LodTier.High) ? HighId : LowId;
// 		var ps = AssetServer.Instance.Models.LODs[sceneId];
// 		if (ps == null) { GD.PushError($"[LODManager] PackedScene for {sceneId} is null."); return; }

// 		var old = _model;

// 		var next = ps.Instantiate<Node3D>();
// 		next.Name = "Model";
// 		if (old != null) next.Transform = old.Transform;

// 		_unit.AddChild(next);
// 		_model = next;
// 		_lodState = tier;

// 		// (re)bind sockets on the NEW model
// 		BindSockets(_model);
// 		ModelChanged?.Invoke(_model);

// 		// --- restore turret yaw on the new model ---
// 		if (TurretYaw != null)
// 		{
// 			var r = TurretYaw.Rotation;
// 			r.Y = savedTurretYawY;          // keep previous yaw
// 			TurretYaw.Rotation = r;
// 		}

// 		old?.QueueFree();
// 	}

// 	// private void BindSockets(Node3D model)
// 	// {
// 	// 	TurretYaw = model?.GetNodeOrNull<Node3D>(TurretPath);
// 	// 	Muzzle = model?.GetNodeOrNull<Node3D>(MuzzlePath);

// 	// 	Utils.NullCheck(TurretYaw);
// 	// 	Utils.NullCheck(Muzzle);

// 	// 	SocketsChanged?.Invoke(TurretYaw, Muzzle);
// 	// }

// 	private void BindSockets(Node3D model)
// 	{
// 		TurretYaw = model?.GetNodeOrNull<Node3D>(TurretPath);
// 		// Muzzle may not exist if you only have Muzzle1/Muzzle2... — that's OK now.
// 		Muzzle = model?.GetNodeOrNull<Node3D>(MuzzlePath);
// 		AnimationPlayer = model?.GetNodeOrNull<AnimationPlayer>(AnimationPlayerPath);

// 		// Utils.NullCheck(TurretYaw);
// 		// Utils.NullCheck(AnimationPlayer);
// 		// Utils.NullCheck(Muzzle);

// 		// Pass both; second may be null. CombatSystem will scan children of TurretYaw for "Muzzle*".
// 		SocketsChanged?.Invoke(TurretYaw, Muzzle, AnimationPlayer);
// 	}

// }

// using System;
// using System.Collections.Generic;
// using System.Diagnostics; // for [Conditional("DEBUG")]
// using Godot;
// using MyEnums;

// public partial class LODManager : Node
// {
// 	[ExportCategory("LOD")]
// 	[Export] public float LodNear = 23f;
// 	[Export] public float LodHysteresis = 3f;
// 	[Export] public float UpdateHz = 8f;
// 	[Export] public LODScenes HighId = LODScenes.AntiInfantryHP;
// 	[Export] public LODScenes LowId = LODScenes.AntiInfantryLP;

// 	[ExportCategory("Sockets (MULTI ONLY)")]
// 	[Export] public Godot.Collections.Array<NodePath> TurretYawPaths = new();
// 	[Export] public Godot.Collections.Array<NodePath> MuzzlePaths = new();
// 	[Export] public Godot.Collections.Array<NodePath> AnimationPlayerPaths = new();

// 	[ExportCategory("Metric")]
// 	[Export] public bool UseTrue3DDistance = true;

// 	private Unit _unit;
// 	private Camera3D _cam;
// 	private Node3D _model;
// 	private double _accum;
// 	private bool _initialized;

// 	private enum LodTier { High, Low }
// 	private LodTier _lodState = LodTier.Low;

// 	// Sockets (lists only)
// 	public List<Node3D> TurretYaws { get; private set; } = new();
// 	public List<Node3D> Muzzles { get; private set; } = new();
// 	public List<AnimationPlayer> AnimationPlayers { get; private set; } = new();

// 	// Events
// 	public event Action<IReadOnlyList<Node3D>, IReadOnlyList<Node3D>, IReadOnlyList<AnimationPlayer>> SocketsChangedMany;
// 	// Legacy: expose first entries for existing listeners
// 	public Node3D TurretYaw => TurretYaws.Count > 0 ? TurretYaws[0] : null;
// 	public Node3D Muzzle => Muzzles.Count > 0 ? Muzzles[0] : null;
// 	public AnimationPlayer AnimationPlayer => AnimationPlayers.Count > 0 ? AnimationPlayers[0] : null;
// 	public event Action<Node3D, Node3D, AnimationPlayer> SocketsChanged;

// 	public event Action<Node3D> ModelChanged;

// 	private bool _swapScheduled;

// 	// --- debug helpers (compiled only in DEBUG) ---
// 	[Conditional("DEBUG")] private static void DBG(string msg) => GD.Print($"[LODManager] {msg}");
// 	[Conditional("DEBUG")]
// 	private static void DBG_LIST(string label, IEnumerable<Node> nodes)
// 	{
// 		int i = 0;
// 		foreach (var n in nodes)
// 			GD.Print($"[LODManager] {label}[{i++}] name={n?.Name} path={n?.GetPath()}");
// 		if (i == 0) GD.Print($"[LODManager] {label}: <none>");
// 	}

// 	public override void _Ready()
// 	{
// 		// GD.Print("TURRET YAW PATH");
// 		// Utils.PrintTree(GetNode(TurretYawPaths[0]));

// 		// GD.Print("MUZZLE PATH");
// 		// Utils.PrintTree(GetNode(MuzzlePaths[0]));

// 		_cam = GetViewport().GetCamera3D();
// 		_unit = GetNodeOrNull<Unit>("../../");
// 		_model = _unit.GetNodeOrNull<Node3D>("Model"); // may be null
// 		Utils.NullCheck(_unit);
// 		Utils.NullCheck(_model);

// 		EvaluateAndMaybeSwap(initial: true);
// 	}

// 	public override void _PhysicsProcess(double delta)
// 	{
// 		_accum += delta;
// 		if (_accum < 1.0 / MathF.Max(1f, UpdateHz)) return;
// 		_accum = 0;
// 		EvaluateAndMaybeSwap();
// 	}

// 	private void EvaluateAndMaybeSwap(bool initial = false)
// 	{
// 		_cam ??= GetViewport().GetCamera3D();
// 		if (_cam == null || _unit == null) return;

// 		// distance metric
// 		float distSq;
// 		if (UseTrue3DDistance)
// 		{
// 			Vector3 d3 = _cam.GlobalPosition - _unit.GlobalPosition;
// 			distSq = d3.LengthSquared();
// 		}
// 		else
// 		{
// 			Vector3 d = _cam.GlobalPosition - _unit.GlobalPosition;
// 			d.Y = 0f;
// 			distSq = d.LengthSquared();
// 		}

// 		float inDist = MathF.Max(0f, LodNear - LodHysteresis);
// 		float outDist = LodNear + LodHysteresis;
// 		float nearSq = LodNear * LodNear;
// 		float inSq = inDist * inDist;
// 		float outSq = outDist * outDist;

// 		var desired = _lodState;

// 		if (!_initialized)
// 		{
// 			desired = (distSq <= nearSq) ? LodTier.High : LodTier.Low;
// 		}
// 		else if (_lodState == LodTier.High && distSq > outSq)
// 		{
// 			desired = LodTier.Low;
// 		}
// 		else if (_lodState == LodTier.Low && distSq < inSq)
// 		{
// 			desired = LodTier.High;
// 		}

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

// 		// save ALL turret local yaws before replacing the model
// 		var savedTurretYawY = new List<float>(TurretYaws.Count);
// 		foreach (var t in TurretYaws)
// 			savedTurretYawY.Add(t?.Rotation.Y ?? 0f);

// 		var tier = (LodTier)tierInt;
// 		var sceneId = (tier == LodTier.High) ? HighId : LowId;
// 		var ps = AssetServer.Instance.Models.LODs[sceneId];
// 		if (ps == null) { GD.PushError($"[LODManager] PackedScene for {sceneId} is null."); return; }

// 		var old = _model;

// 		var next = ps.Instantiate<Node3D>();
// 		next.Name = "Model";
// 		if (old != null) next.Transform = old.Transform;

// 		_unit.AddChild(next);
// 		_model = next;
// 		_lodState = tier;

// 		// rebind sockets on the NEW model
// 		BindSockets(_model);
// 		ModelChanged?.Invoke(_model);

// 		// restore per-turret yaw by index
// 		var count = Math.Min(savedTurretYawY.Count, TurretYaws.Count);
// 		for (int i = 0; i < count; i++)
// 		{
// 			var t = TurretYaws[i];
// 			if (t == null) continue;
// 			var r = t.Rotation;
// 			r.Y = savedTurretYawY[i];
// 			t.Rotation = r;
// 		}

// 		// DBG($"After BindSockets: turrets={TurretYaws.Count} muzzles={Muzzles.Count} animPlayers={AnimationPlayers.Count}");
// 		// DBG_LIST("Muzzle", Muzzles);

// 		old?.QueueFree();
// 	}

// 	private void BindSockets(Node3D model)
// 	{
// 		// Utils.PrintTree(model);
// 		TurretYaws.Clear();
// 		Muzzles.Clear();
// 		AnimationPlayers.Clear();

// 		// arrays ONLY — no single-path fallback
// 		if (TurretYawPaths != null)
// 		{
// 			foreach (var p in TurretYawPaths)
// 			{
// 				if (p != null && !p.IsEmpty)
// 				{

// 					if (GetNodeOrNull(p) is Node3D n)
// 					{
// 						TurretYaws.Add(n);
// 					}
// 				}
// 			}
// 		}

// 		if (MuzzlePaths != null)
// 		{
// 			foreach (var p in MuzzlePaths)
// 			{
// 				if (p != null && !p.IsEmpty)
// 				{
// 					if (GetNodeOrNull(p) is Node3D n)
// 					{
// 						Muzzles.Add(n);
// 					}
// 				}
// 			}
// 		}

// 		if (AnimationPlayerPaths != null)
// 		{
// 			foreach (var p in AnimationPlayerPaths)
// 			{
// 				if (p != null && !p.IsEmpty)
// 				{
// 					if (model?.GetNodeOrNull(p) is AnimationPlayer ap)
// 					{
// 						AnimationPlayers.Add(ap);
// 					}
// 				}
// 			}
// 		}

// 		GD.Print("Muzzle count after bind: " + Muzzles.Count);
// 		// DBG($"After BindSockets: turrets={TurretYaws.Count} muzzles={Muzzles.Count} animPlayers={AnimationPlayers.Count}");
// 		DBG_LIST("Muzzle", Muzzles);

// 		GD.Print("TUrret count after bind: " + TurretYaws.Count);
// 		DBG_LIST("TurretYaw", TurretYaws);

// 		// notify listeners
// 		SocketsChangedMany?.Invoke(TurretYaws, Muzzles, AnimationPlayers);
// 		SocketsChanged?.Invoke(TurretYaw, Muzzle, AnimationPlayer);
// 	}
// }

using System;
using System.Collections.Generic;
using System.Diagnostics; // [Conditional("DEBUG")]
using Godot;
using MyEnums;

public partial class LODManager : Node
{
	[ExportCategory("LOD")]
	[Export] public float LodNear = 23f;
	[Export] public float LodHysteresis = 3f;
	[Export] public float UpdateHz = 8f;
	[Export] public LODScenes HighId = LODScenes.AntiInfantryHP;
	[Export] public LODScenes LowId = LODScenes.AntiInfantryLP;

	[ExportCategory("Sockets (MULTI ONLY, RELATIVE TO THIS NODE)")]
	// Set paths like: ../../Model/Rig/Turret , ../../Model/Rig/Turret/Muzzle/MuzzleHP1 , etc.
	[Export] public Godot.Collections.Array<NodePath> TurretYawPaths = new();
	[Export] public Godot.Collections.Array<NodePath> MuzzlePaths = new();
	[Export] public Godot.Collections.Array<NodePath> AnimationPlayerPaths = new();

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

	// Sockets (lists only)
	public List<Node3D> TurretYaws { get; private set; } = new();
	public List<Node3D> Muzzles { get; private set; } = new();
	public List<AnimationPlayer> AnimationPlayers { get; private set; } = new();

	// Events
	public event Action<IReadOnlyList<Node3D>, IReadOnlyList<Node3D>, IReadOnlyList<AnimationPlayer>> SocketsChangedMany;
	// Legacy: expose first entries for existing listeners
	public Node3D TurretYaw => TurretYaws.Count > 0 ? TurretYaws[0] : null;
	public Node3D Muzzle => Muzzles.Count > 0 ? Muzzles[0] : null;
	public AnimationPlayer AnimationPlayer => AnimationPlayers.Count > 0 ? AnimationPlayers[0] : null;
	public event Action<Node3D, Node3D, AnimationPlayer> SocketsChanged;
	public event Action<Node3D> ModelChanged;

	private bool _swapScheduled;

	private static bool Alive(Node n) => n != null && GodotObject.IsInstanceValid(n) && n.IsInsideTree();

	[Conditional("DEBUG")] private void DBG(string msg) { if (DebugLOD || DebugSockets) GD.Print($"[LODManager] {msg}"); }
	[Conditional("DEBUG")]
	private void DBG_LIST(string label, IEnumerable<Node> nodes)
	{
		if (!(DebugLOD || DebugSockets)) return;
		int i = 0;
		foreach (var n in nodes)
			GD.Print($"[LODManager] {label}[{i++}] name={n?.Name} path={n?.GetPath()}");
		if (i == 0) GD.Print($"[LODManager] {label}: <none>");
	}

	public override void _Ready()
	{
		_cam = GetViewport().GetCamera3D();
		_unit = GetNodeOrNull<Unit>("../../");         // LODManager typically under Unit/Model/...
		_model = _unit?.GetNodeOrNull<Node3D>("Model"); // may be null initially

		if (_unit == null)
		{
			GD.PushError("[LODManager] Unit not found via '../../'.");
			return;
		}

		EvaluateAndMaybeSwap(initial: true);
	}

	public override void _PhysicsProcess(double delta)
	{
		_accum += delta;
		if (_accum < 1.0 / MathF.Max(1f, UpdateHz)) return;
		_accum = 0;
		EvaluateAndMaybeSwap();
	}

	private void EvaluateAndMaybeSwap(bool initial = false)
	{
		_cam ??= GetViewport().GetCamera3D();
		if (_cam == null || _unit == null) return;

		float distSq;
		if (UseTrue3DDistance)
		{
			Vector3 d3 = _cam.GlobalPosition - _unit.GlobalPosition;
			distSq = d3.LengthSquared();
		}
		else
		{
			Vector3 d = _cam.GlobalPosition - _unit.GlobalPosition; d.Y = 0f; distSq = d.LengthSquared();
		}

		float inDist = MathF.Max(0f, LodNear - LodHysteresis);
		float outDist = LodNear + LodHysteresis;
		float nearSq = LodNear * LodNear;
		float inSq = inDist * inDist;
		float outSq = outDist * outDist;

		var desired = _lodState;
		if (!_initialized)
			desired = (distSq <= nearSq) ? LodTier.High : LodTier.Low;
		else if (_lodState == LodTier.High && distSq > outSq)
			desired = LodTier.Low;
		else if (_lodState == LodTier.Low && distSq < inSq)
			desired = LodTier.High;

		if (DebugLOD)
			GD.Print($"[LODManager] dist={(float)Math.Sqrt(distSq):0.00} near={LodNear} in={inDist} out={outDist} cur={_lodState} want={desired}");

		if (!_initialized)
		{
			_initialized = true;
			SwapModelDeferred(desired);
			return;
		}

		if (desired != _lodState)
			SwapModelDeferred(desired);
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

		// Purge stale refs and snapshot yaw safely
		TurretYaws.RemoveAll(t => !Alive(t));
		var savedYaw = new List<float>(TurretYaws.Count);
		foreach (var t in TurretYaws)
			savedYaw.Add(Alive(t) ? ((Node3D)t).Rotation.Y : 0f);

		var tier = (LodTier)tierInt;
		var sceneId = (tier == LodTier.High) ? HighId : LowId;
		var ps = AssetServer.Instance.Models.LODs[sceneId];
		if (ps == null) { GD.PushError($"[LODManager] PackedScene for {sceneId} is null."); return; }

		var old = _model;
		var oldXf = old?.Transform ?? Transform3D.Identity;

		// *** Avoid name collision so the new node can be named exactly "Model" ***
		if (old != null && old.Name == "Model") old.Name = "Model__OLD";

		var next = ps.Instantiate<Node3D>();
		next.Name = "Model";                 // we rely on this for ../../Model/... paths
		next.Transform = oldXf;

		_unit.AddChild(next);
		_model = next;
		_lodState = tier;

		BindSockets(_model);
		ModelChanged?.Invoke(_model);

		// Restore yaw to matching indices
		var count = Math.Min(savedYaw.Count, TurretYaws.Count);
		for (int i = 0; i < count; i++)
		{
			var t = TurretYaws[i];
			if (!Alive(t)) continue;
			var r = t.Rotation; r.Y = savedYaw[i]; t.Rotation = r;
		}

		old?.QueueFree();

		if (DebugLOD)
			GD.Print($"[LODManager] swapped to {tier} (scene {sceneId}).");
	}

	private void BindSockets(Node3D model)
	{
		TurretYaws.Clear();
		Muzzles.Clear();
		AnimationPlayers.Clear();

		// All NodePaths resolved RELATIVE TO THIS LODManager (so ../../Model/... is valid)
		if (TurretYawPaths != null)
			foreach (var p in TurretYawPaths)
				if (p != null && !p.IsEmpty)
					if (GetNodeOrNull<Node3D>(p) is Node3D t) TurretYaws.Add(t);

		if (MuzzlePaths != null)
			foreach (var p in MuzzlePaths)
				if (p != null && !p.IsEmpty)
					if (GetNodeOrNull<Node3D>(p) is Node3D m) Muzzles.Add(m);

		if (AnimationPlayerPaths != null)
			foreach (var p in AnimationPlayerPaths)
				if (p != null && !p.IsEmpty)
					if (GetNodeOrNull<AnimationPlayer>(p) is AnimationPlayer ap) AnimationPlayers.Add(ap);

		if (DebugSockets)
		{
			GD.Print($"[LODManager] After BindSockets: turrets={TurretYaws.Count} muzzles={Muzzles.Count} anims={AnimationPlayers.Count}");
			DBG_LIST("TurretYaw", TurretYaws);
			DBG_LIST("Muzzle", Muzzles);
		}

		SocketsChangedMany?.Invoke(TurretYaws, Muzzles, AnimationPlayers);
		SocketsChanged?.Invoke(TurretYaw, Muzzle, AnimationPlayer);
	}

	// Optional manual testing helpers
	public void ForceSwapToHigh() => SwapModelDeferred(LodTier.High);
	public void ForceSwapToLow() => SwapModelDeferred(LodTier.Low);
}