using System.Collections.Generic;
using Godot;

public partial class ColorOptionButton : OptionButton
{
	private Dictionary<int, Color> _colors = new()
	{
		{ 0, new Color("#d13f4b") }, // Red
		{ 1, new Color("#678746") }, // Green
		{ 2, new Color("#5090b3") }, // Blue
		{ 3, new Color("#a26f2a") }, // Orange
	};

	private void ChangeColor(int idx)
	{
		var baseStyle = GetThemeStylebox("normal") as StyleBoxFlat;
		baseStyle.BgColor = _colors[idx];
	}
}
