using System;
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

	[ExportCategory("Sockets (inside Model)")]
	[Export] public string TurretPath = "Rig/Turret";
	[Export] public string MuzzlePath = "Rig/Turret/Muzzle";
	[Export] public string AnimationPlayerPath = "AnimationPlayer";

	[ExportCategory("Metric")]
	[Export] public bool UseTrue3DDistance = true; // NEW: respond to camera height

	private Unit _unit;
	private Camera3D _cam;
	private Node3D _model;
	private double _accum;
	private bool _initialized;

	private enum LodTier { High, Low }
	private LodTier _lodState = LodTier.Low;

	// sockets
	public Node3D TurretYaw { get; private set; }
	public Node3D Muzzle { get; private set; }
	public AnimationPlayer AnimationPlayer { get; private set; }

	public event Action<Node3D, Node3D, AnimationPlayer> SocketsChanged;  // (turretYaw, muzzle)
	public event Action<Node3D> ModelChanged;

	private bool _swapScheduled;

	public override void _Ready()
	{
		_cam = GetViewport().GetCamera3D();
		_unit = GetNodeOrNull<Unit>("../../");
		_model = _unit.GetNodeOrNull<Node3D>("Model"); // may be null (that’s fine)
													   // _unit = GetParent<Unit>() ?? GetOwner<Unit>();
		Utils.NullCheck(_unit);
		Utils.NullCheck(_model);

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
		_cam ??= GetViewport().GetCamera3D();            // NEW: reacquire if null
		if (_cam == null || _unit == null) return;

		// --- choose distance metric ---
		float distSq;
		if (UseTrue3DDistance)
		{
			Vector3 d3 = _cam.GlobalPosition - _unit.GlobalPosition;
			distSq = d3.LengthSquared();
		}
		else
		{
			Vector3 d = _cam.GlobalPosition - _unit.GlobalPosition;
			d.Y = 0f;
			distSq = d.LengthSquared();
		}

		float inDist = MathF.Max(0f, LodNear - LodHysteresis);
		float outDist = LodNear + LodHysteresis;

		float nearSq = LodNear * LodNear;
		float inSq = inDist * inDist;
		float outSq = outDist * outDist;

		var desired = _lodState;

		// Decide desired tier from current distance
		if (!_initialized)
		{
			desired = (distSq <= nearSq) ? LodTier.High : LodTier.Low;
		}
		else if (_lodState == LodTier.High && distSq > outSq)
		{
			desired = LodTier.Low;
		}
		else if (_lodState == LodTier.Low && distSq < inSq)
		{
			desired = LodTier.High;
		}

		// On first run, FORCE alignment to the desired tier (even if a model exists)
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

		// --- save current turret orientation BEFORE replacing the model ---
		// (use local yaw since this is a turret yaw node under the model)
		float savedTurretYawY = 0f;
		if (TurretYaw != null)
			savedTurretYawY = TurretYaw.Rotation.Y;   // radians

		var tier = (LodTier)tierInt;
		var sceneId = (tier == LodTier.High) ? HighId : LowId;
		var ps = AssetServer.Instance.Models.LODs[sceneId];
		if (ps == null) { GD.PushError($"[LODManager] PackedScene for {sceneId} is null."); return; }

		var old = _model;

		var next = ps.Instantiate<Node3D>();
		next.Name = "Model";
		if (old != null) next.Transform = old.Transform;

		_unit.AddChild(next);
		_model = next;
		_lodState = tier;

		// (re)bind sockets on the NEW model
		BindSockets(_model);
		ModelChanged?.Invoke(_model);

		// --- restore turret yaw on the new model ---
		if (TurretYaw != null)
		{
			var r = TurretYaw.Rotation;
			r.Y = savedTurretYawY;          // keep previous yaw
			TurretYaw.Rotation = r;
		}

		old?.QueueFree();
	}

	// private void BindSockets(Node3D model)
	// {
	// 	TurretYaw = model?.GetNodeOrNull<Node3D>(TurretPath);
	// 	Muzzle = model?.GetNodeOrNull<Node3D>(MuzzlePath);

	// 	Utils.NullCheck(TurretYaw);
	// 	Utils.NullCheck(Muzzle);

	// 	SocketsChanged?.Invoke(TurretYaw, Muzzle);
	// }

	private void BindSockets(Node3D model)
	{
		TurretYaw = model?.GetNodeOrNull<Node3D>(TurretPath);
		// Muzzle may not exist if you only have Muzzle1/Muzzle2... — that's OK now.
		Muzzle = model?.GetNodeOrNull<Node3D>(MuzzlePath);
		AnimationPlayer = model?.GetNodeOrNull<AnimationPlayer>(AnimationPlayerPath);

		// Utils.NullCheck(TurretYaw);
		// Utils.NullCheck(AnimationPlayer);
		// Utils.NullCheck(Muzzle);

		// Pass both; second may be null. CombatSystem will scan children of TurretYaw for "Muzzle*".
		SocketsChanged?.Invoke(TurretYaw, Muzzle, AnimationPlayer);
	}

}
