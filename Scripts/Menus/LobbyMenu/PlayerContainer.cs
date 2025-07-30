using Godot;
using System.Collections.Generic;

public partial class PlayerContainer : PanelContainer
{
	[Export] public int PlayerId { get; set; }
	[Export] public Label PlayerIdLabel;
	[Export] private Label _fundsLabel;
	[Export] private CheckButton _readyupBtn;
	[Export] private OptionButton _colorOptionButton;
	[Export] private OptionButton _teamOptionButton;
	[Export]
	public Color PlayerColor
	{
		get => _playerColor;
		set
		{
			_playerColor = value;
			ApplyColor(_playerColor);
		}
	}

	// Replicated color property
	private Color _playerColor = new Color("#d13f4b");
	private Dictionary<int, Color> _colors;
	private PlayerManager _playerManager;

	public override void _Ready()
	{
		_playerManager = PlayerManager.Instance;

		// Initialize the available colors
		_colors = new()
		{
			{ 0, new Color("#d13f4b") }, // Red
			{ 1, new Color("#678746") }, // Green
			{ 2, new Color("#5090b3") }, // Blue
			{ 3, new Color("#a26f2a") }, // Orange
		};

		Utils.NullExportCheck(PlayerIdLabel);
		Utils.NullExportCheck(_colorOptionButton);
		Utils.NullExportCheck(_fundsLabel);
		Utils.NullExportCheck(_teamOptionButton);
		Utils.NullExportCheck(_readyupBtn);

		RemoveOptionCheckbox(_colorOptionButton);
		RemoveOptionCheckbox(_teamOptionButton);

		// Apply the replicated color on spawn or change
		ApplyColor(_playerColor);

		// Only the server may change color (clients see disabled control)
		_colorOptionButton.ItemSelected += idx =>
		{
			if (!Multiplayer.IsServer())
				return;
			// Set the replicated property (Sync on Change)
			PlayerColor = _colors[(int)idx];
		};

		// Disable interactive controls on clients
		if (!Multiplayer.IsServer())
		{
			_colorOptionButton.Disabled = true;
			_teamOptionButton.Disabled = true;
		}

		// Set the player label
		PlayerIdLabel.Text = $"Player {PlayerId}";
	}

	private void ApplyColor(Color color)
	{
		var shared = _colorOptionButton.GetThemeStylebox("normal") as StyleBoxFlat;
		var style = shared?.Duplicate() as StyleBoxFlat ?? new StyleBoxFlat();
		style.BgColor = color;
		_colorOptionButton.AddThemeStyleboxOverride("normal", style);
		_colorOptionButton.AddThemeStyleboxOverride("hover", style);
		_colorOptionButton.AddThemeStyleboxOverride("focus", style);

		// ResetReadyUp();
	}

	private void RemoveOptionCheckbox(OptionButton optionButton)
	{
		var popup = optionButton.GetPopup();
		int count = popup.GetItemCount();
		for (int i = 0; i < count; i++)
		{
			if (popup.IsItemRadioCheckable(i))
				popup.SetItemAsRadioCheckable(i, false);
		}
	}

	private void OnReadyUpPressed(bool toggledOn)
	{
		if (!Multiplayer.IsServer())
			return;

		if (!toggledOn)
		{
			_playerManager.UnstagePlayer(PlayerId);
			return;
		}

		Player newPlayer = new Player(PlayerId, PlayerColor, 25000, true);
		_playerManager.StagePlayer(newPlayer);
	}

	// Resets the ready-up state for a player if any of his settings are changed
	private void ResetReadyUp(int idx)
	{
		_readyupBtn.SetPressedNoSignal(false);

		if (Multiplayer.IsServer())
			_playerManager.UnstagePlayer(PlayerId);
	}
}
