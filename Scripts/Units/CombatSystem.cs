using Godot;

public partial class CombatSystem : Node
{
	[Export] private Unit _unit;
	[Export] private float AcquireHz = 5f;          // how often to re-acquire a target (times/sec)
	[Export] private float TurnSpeedDeg = 180f;     // tank yaw speed (deg/sec)
	private int _hp;
	private int _dps;
	private int _range;
	private Unit _currentTarget;
	private float _fireRateTimer;
	private string UnitsGroup = MyEnums.Group.Units.ToString();
	private AudioStreamPlayer3D _attackSound;
	[Signal] public delegate void OnAttackEventHandler(Unit target, int dps);

	public override void _Ready()
	{
		Utils.NullExportCheck(_unit);

		_hp = _unit.CurrentHP;
		_dps = _unit.DPS;
		_range = _unit.Range;

		_attackSound = GetNode<AudioStreamPlayer3D>("../../Audio/Attack");
		Utils.NullCheck(_attackSound);
	}

	public override void _PhysicsProcess(double delta)
	{
		// TODO: Remove. For debugging only.
		if (_unit.Team == 2)
			return;

		FaceTowardsTarget((float)delta);
		TryAttack(delta);
	}

	private void TryAttack(double delta)
	{
		if (!IsInstanceValid(_currentTarget))
		{
			_currentTarget = null;
			return;
		}

		// Ensure target it still in range
		Vector3 distance = _currentTarget.GlobalPosition - _unit.GlobalPosition;
		distance.Y = 0;

		if (distance.LengthSquared() > (_range * _range))
			return;

		// cooldown tick
		_fireRateTimer = Mathf.Max(0f, _fireRateTimer - (float)delta);

		if (_fireRateTimer > 0f)
			return;

		_fireRateTimer = _unit.FireRate;

		EmitSignal(SignalName.OnAttack, _currentTarget, _dps);
		_attackSound.Play();
	}

	private Unit GetNearestEnemyInRange()
	{
		if (_unit == null) return null;

		Vector3 myPos = _unit.GlobalPosition;
		int myTeam = _unit.Team;
		float rangeSq = _range * _range;

		float bestDistSq = float.MaxValue;
		Unit best = null;

		foreach (Node n in GetTree().GetNodesInGroup(UnitsGroup))
		{
			if (n == _unit || n == null) continue;
			if (n is not Unit other) continue;
			if (other.CurrentHP <= 0) continue;           // skip dead
			if (other.Team == myTeam) continue;    // skip friendlies

			Vector3 d = other.GlobalPosition - myPos;
			d.Y = 0; // horizontal distance
			float dsq = d.LengthSquared();
			if (dsq <= rangeSq && dsq < bestDistSq)
			{
				bestDistSq = dsq;
				best = other;
			}
		}

		return best;
	}

	private void FaceTowardsTarget(float dt)
	{
		_currentTarget = GetNearestEnemyInRange();
		if (!IsInstanceValid(_currentTarget))
			return;

		Vector3 myPos = _unit.GlobalPosition;
		Vector3 dir = _currentTarget.GlobalPosition - myPos;
		dir.Y = 0;

		if (dir.LengthSquared() < 1e-6f) return;

		// Desired yaw (Godot: yaw around +Y; forward is -Z)
		float targetYaw = Mathf.Atan2(-dir.X, -dir.Z);   // <- note the minus signs
		float currentYaw = _unit.Rotation.Y;

		// Step-limited turn toward the target yaw
		float maxStep = Mathf.DegToRad(TurnSpeedDeg) * dt;
		float deltaYaw = targetYaw - currentYaw;
		while (deltaYaw > Mathf.Pi) deltaYaw -= Mathf.Tau;
		while (deltaYaw < -Mathf.Pi) deltaYaw += Mathf.Tau;
		float step = Mathf.Clamp(deltaYaw, -maxStep, maxStep);

		_unit.Rotation = new Vector3(0f, currentYaw + step, 0f);
	}
}
