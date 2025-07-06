using Godot;
using System.Collections.Generic;

public partial class PlayerContainer : HBoxContainer
{
	[Export] private OptionButton _colorOptionButton;
	[Export] private OptionButton _teamOptionButton;
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

		Utils.NullExportCheck(_colorOptionButton);
		Utils.NullExportCheck(_teamOptionButton);

		RemoveOptionCheckbox(_colorOptionButton);
		RemoveOptionCheckbox(_teamOptionButton);
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

	private void ChangeColor(int idx)
	{
		var shared = _colorOptionButton.GetThemeStylebox("normal") as StyleBoxFlat;

		var style = shared?.Duplicate() as StyleBoxFlat ?? new StyleBoxFlat();
		style.BgColor = _colors[idx];

		_colorOptionButton.AddThemeStyleboxOverride("normal", style);
		_colorOptionButton.AddThemeStyleboxOverride("hover", style);
		_colorOptionButton.AddThemeStyleboxOverride("focus", style);
	}
}
