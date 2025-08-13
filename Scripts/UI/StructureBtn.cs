using Godot;
using MyEnums;

public partial class StructureBtn : Button
{
	[Export] public StructureType Structure { get; set; }

	private Player _player;
	private Signals _signals;
	private StructureBasePlaceholder _placeholder;
	private MyModels _models;
	private Camera3D _camera;
	private Node3D _scene;
	private PlayerManager _playerManager;
	private StructureFactory _structureFactory;

	public override void _Ready()
	{
		_structureFactory = StructureFactory.Instance;
		_playerManager = PlayerManager.Instance;

		_playerManager.WhenHumanPlayerReady(player =>
		{
			_player = player;
		});

		_signals = Signals.Instance;
		_models = AssetServer.Instance.Models;
		_camera = GetViewport().GetCamera3D();
		_scene = GetTree().CurrentScene as Node3D;

		Pressed += SelectStructure;
		MouseEntered += OnBtnEnter;
		MouseExited += OnBtnExit;

		if (Structure == StructureType.None)
			Utils.PrintErr($"Structure enum not set on {Name}");

		if (_scene == null)
			Utils.PrintErr("Current scene root is not a Node3D.");
	}

	private void OnHumanPlayerReady(Player player)
	{
		_player = player;
		GD.Print("[StructureBtn] Human player ready:", _player.Name);
	}

	private void SelectStructure()
	{
		// Cancel if already placing
		if (_placeholder != null)
		{
			CancelPlaceholder();
			return;
		}

		_placeholder = _structureFactory.BuildPlaceholder(_player, Structure);

		if (_placeholder == null)
		{
			Utils.PrintErr($"Failed to create placeholder for structure: {Structure}");
			return;
		}

		_scene.AddChild(_placeholder);

		ReleaseFocus();
	}

	public override void _Input(InputEvent @event)
	{
		if (_placeholder == null)
			return;

		if (Input.IsActionJustPressed("mb_secondary"))
		{
			CancelPlaceholder();
			return;
		}

		if (Input.IsActionJustPressed("mb_primary"))
		{
			PlaceStructure();
			return;
		}

		if (@event is InputEventMouseButton mb && mb.Pressed)
		{
			RotatePlaceholder(mb.ButtonIndex == MouseButton.WheelUp ? 90f : -90f);
		}
	}

	private void PlaceStructure()
	{
		if (GlobalResources.Instance.IsHoveringUI)
		{
			CancelPlaceholder();
			return;
		}

		if (_placeholder == null || !_placeholder.ValidPlacement)
			return;

		_structureFactory.PlaceStructure(_placeholder);
		CancelPlaceholder();
	}

	private void CancelPlaceholder()
	{
		GlobalResources.Instance.IsPlacingStructure = false;
		if (_placeholder == null) return;
		_placeholder.Area.AreaEntered -= _placeholder.OnAreaEntered;
		_placeholder.Area.AreaExited -= _placeholder.OnAreaExited;
		_placeholder.Overlaps.Clear();
		_placeholder.QueueFree();
		_placeholder = null;
	}

	private void RotatePlaceholder(float degrees)
	{
		var rot = _placeholder.RotationDegrees;
		rot.Y += degrees;
		_placeholder.RotationDegrees = rot;
	}

	private void OnBtnEnter()
	{
		var preview = _models.Structures[Structure].Instantiate<StructureBase>();
		_signals.EmitBuildOptionsBtnBtnHover(preview, null);
	}

	private void OnBtnExit()
	{
		_signals.EmitBuildOptionsBtnBtnHover(null, null);
	}
}
