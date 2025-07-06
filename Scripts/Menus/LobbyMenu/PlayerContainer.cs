using Godot;
using System.Collections.Generic;

public partial class PlayerContainer : HBoxContainer
{
	[Export] private OptionButton _colorOptionButton;
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
	}

	private void ChangeColor(int idx)
	{
		var baseStyle = _colorOptionButton.GetThemeStylebox("normal") as StyleBoxFlat;
		baseStyle.BgColor = _colors[idx];
	}
}
