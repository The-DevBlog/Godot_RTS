using Godot;
using MyEnums;

public partial class Resources : Node3D
{
	public static Resources Instance { get; set; }
	[Export] public Vector2 MapSize { get; set; }
	[Export] public Season Season { get; set; }
	[Export] public TimeOfDay TimeOfDay { get; set; }
	[Export] public Weather Weather { get; set; }
	[Export] public MultiplayerSpawner MultiplayerSpawner { get; set; }
	[Export] public Camera3D Camera { get; private set; }
	[Export] public MultiplayerSpawner PlayerSpawner { get; private set; }
	private PlayerManager _playerManager;

	public override void _EnterTree()
	{
		GD.Print("Resources _EnterTree()");

		Instance = this;

		// Add the local player
		_playerManager = PlayerManager.Instance;
		Utils.NullCheck(_playerManager);
		// _playerManager.AddLocalPlayer();

		if (MapSize == Vector2.Zero) Utils.PrintErr("MapSize is not set");
		if (Weather == Weather.None) Utils.PrintErr("Weather is set to None.");
		if (Season == Season.None) Utils.PrintErr("Season is set to None.");
		if (TimeOfDay == TimeOfDay.None) Utils.PrintErr("TimeOfDay is set to None.");

		Utils.NullExportCheck(Camera);
		Utils.NullExportCheck(MultiplayerSpawner);
		Utils.NullExportCheck(PlayerSpawner);

		PlayerSpawner.SpawnFunction = new Callable(this, nameof(OnPlayerSpawned));
		// PlayerSpawner.Spawned += OnPlayerSpawned;
		PlayerManager.Instance.SpawnPlayers();

		GD.Print("Resources _EnterTree() completed");
	}

	private void OnPlayerSpawned(Node spawnedNode)
	{
		GD.Print("RUN");
		// cast to your actual player scene type
		if (spawnedNode is not Player player)
			return;

		// the server already set authority + initial data on spawn,
		// now each peer can pick out their own local player:
		if (player.Id == Multiplayer.GetUniqueId())
		{
			GD.Print($"Setting local player: {player.Id}");
			PlayerManager.Instance.LocalPlayer = player;
		}

		GD.Print($"Player spawned: {player.Id}");
		// GD.Print($"Player spawned: {node.Name}");
	}
}
