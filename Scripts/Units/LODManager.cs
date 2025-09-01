using System;
using Godot;
using MyEnums;

public partial class LODManager : Node
{
	[ExportCategory("LOD")]
	[Export] public float LodNear = 23f;          // center distance
	[Export] public float LodHysteresis = 3f;     // +/- band to avoid thrash
	[Export] public float UpdateHz = 8f;

	[Export] public LODScenes HighId = LODScenes.AntiInfantryHigh;
	[Export] public LODScenes LowId = LODScenes.AntiInfantryLow;

	private Unit _unit;

	private enum LodTier { High, Low }
	private LodTier _lodState = LodTier.Low;

	private Camera3D _cam;
	private Node3D _model;                       // current child named "Model"
	private double _accum;

	// Systems (e.g., CombatSystem) can rebind model-internal paths when it changes
	public event Action<Node3D> ModelChanged;

	public override void _Ready()
	{
		// Resolve references
		_cam ??= GetViewport().GetCamera3D();
		_unit = GetNode("../").GetParent<Unit>();
		Utils.NullCheck(_unit);

		// Use existing Model if present
		_model = _unit.GetNodeOrNull<Node3D>("Model");
		Utils.NullCheck(_model);

		// Evaluate once on start (may spawn initial LOD)
		EvaluateAndMaybeSwap(initial: true);
	}

	public override void _PhysicsProcess(double delta)
	{
		_accum += delta;
		if (_accum < 1.0 / Math.Max(1.0f, UpdateHz)) return;
		_accum = 0;

		EvaluateAndMaybeSwap();
	}

	private void EvaluateAndMaybeSwap(bool initial = false)
	{
		if (_cam == null || _unit == null) return;

		// Ground distance (XZ)
		Vector3 d = _cam.GlobalPosition - _unit.GlobalPosition;
		d.Y = 0f;
		float distSq = d.LengthSquared();

		float nearIn = MathF.Max(0f, LodNear - LodHysteresis);
		float nearOut = LodNear + LodHysteresis;
		float nearInSq = nearIn * nearIn;
		float nearOutSq = nearOut * nearOut;
		float nearSq = LodNear * LodNear;

		LodTier desired = _lodState;

		if (initial && _model == null)
			desired = (distSq <= nearSq) ? LodTier.High : LodTier.Low;
		else if (_lodState == LodTier.High && distSq > nearOutSq)
			desired = LodTier.Low;
		else if (_lodState == LodTier.Low && distSq < nearInSq)
			desired = LodTier.High;

		if (desired != _lodState || (initial && _model == null))
			SwapModel(desired);
	}

	private void SwapModel(LodTier tier)
	{
		var sceneId = (tier == LodTier.High) ? HighId : LowId;
		var ps = AssetServer.Instance.Models.LODs[sceneId];
		if (ps == null) { GD.PushError($"[LODManager] PackedScene for {sceneId} is null."); return; }

		Node3D newModel = ps.Instantiate<Node3D>();
		newModel.Name = "Model";

		// Preserve transform
		if (_model != null)
			newModel.Transform = _model.Transform;

		// Attach under the Unit so other systems can find "Model" via UnitRef.GetNode("Model")
		_unit.AddChild(newModel);

		// Replace
		_model?.QueueFree();
		_model = newModel;
		_lodState = tier;

		// Let dependents rebind their cached paths (e.g., CombatSystem)
		ModelChanged?.Invoke(_model);
	}
}
