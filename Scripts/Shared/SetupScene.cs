using Godot;

public partial class SetupScene : Node3D
{
	private PlayerManager _playerManager;

	public override void _EnterTree()
	{
		GD.Print("SetupScene _EnterTree()");
		_playerManager = PlayerManager.Instance;
		Utils.NullCheck(_playerManager);
		// if (_playerManager == null)
		// {
		// 	GD.PrintErr("PlayerManager instance not found!");
		// 	return;
		// }

		// Add the local player
		_playerManager.AddLocalPlayer();
		GD.Print("local player: " + _playerManager.LocalPlayer);
	}
}
