using Godot;
using System.Collections.Generic;

public partial class PlayerContainer : PanelContainer
{
	[Export] public int PlayerId;
	[Export] public Label PlayerIdLabel;
	[Export] public Label _fundsLabel;
	[Export] private OptionButton _colorOptionButton;
	[Export] private OptionButton _teamOptionButton;
	[Export] private Color _selectedColor;
	private Dictionary<int, Color> _colors;

	public override void _Ready()
	{
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

		RemoveOptionCheckbox(_colorOptionButton);
		RemoveOptionCheckbox(_teamOptionButton);

		_colorOptionButton.ItemSelected += idx =>
		{
			// then locally apply it too:
			ChangeColor((int)idx);
		};

		// Disable changeable properties if not server
		if (!Multiplayer.IsServer())
		{
			_colorOptionButton.Disabled = true;
			_teamOptionButton.Disabled = true;
		}

		PlayerIdLabel.Text = $"Player {PlayerId}";
	}

	private void ChangeColor(int idx)
	{
		var shared = _colorOptionButton.GetThemeStylebox("normal") as StyleBoxFlat;
		var style = shared?.Duplicate() as StyleBoxFlat ?? new StyleBoxFlat();

		style.BgColor = _colors[idx];
		_colorOptionButton.AddThemeStyleboxOverride("normal", style);
		_colorOptionButton.AddThemeStyleboxOverride("hover", style);
		_colorOptionButton.AddThemeStyleboxOverride("focus", style);
	}

	private void RemoveOptionCheckbox(OptionButton optionButton)
	{
		// Get the popup menu behind this OptionButton
		var popup = optionButton.GetPopup();

		// Loop over every item and disable its radio check
		int count = popup.GetItemCount();
		for (int i = 0; i < count; i++)
		{
			if (popup.IsItemRadioCheckable(i))
				popup.SetItemAsRadioCheckable(i, false);
		}
	}
}
