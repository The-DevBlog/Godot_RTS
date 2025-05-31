using Godot;

public partial class PopMenuBtnManager : MarginContainer
{

	[Export]
	public Container MenuScreen { get; set; }
	[Export]
	public Container OpenMenuScreen { get; set; }
	[Export]
	public Container HelpMenuScreen { get; set; }

	private void ToggleVisibility(Container menu)
	{
		menu.Visible = !menu.Visible;
	}

	private void OnToggleMenuBtnPressed()
	{
		ToggleVisibility(MenuScreen);
		ToggleVisibility(OpenMenuScreen);
	}

	private void OnToggleHelpMenuBtnPressed()
	{
		ToggleVisibility(HelpMenuScreen);
		ToggleVisibility(MenuScreen);
	}
}
