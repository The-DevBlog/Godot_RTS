using Godot;

public partial class RootContainer : Container
{

	[Export] public Container StructuresContainer { get; set; }
	[Export] public Container UnitsContainer { get; set; }
	[Export] public Container UpgradesContainer { get; set; }
	[Export] public Button[] StructuresBtns { get; set; }
	[Export] public Button[] UnitsBtns { get; set; }
	[Export] public Button[] UpgradesBtns { get; set; }

	private MarginContainer _miniMapContainer;
	public override void _Ready()
	{
		_miniMapContainer = GetNode<MarginContainer>("VBoxContainer/MiniMapContainer");

		if (_miniMapContainer == null)
			Utils.PrintErr("MiniMapContainer node not found.");

		foreach (var btn in StructuresBtns)
		{
			btn.SelfModulate = new Color("#c8c8c8");
			btn.MouseEntered += () => btn.SelfModulate = new Color("#ffffff");
			btn.MouseExited += () => btn.SelfModulate = new Color("#c8c8c8");
		}

		foreach (var btn in UnitsBtns)
		{
			btn.SelfModulate = new Color("#c8c8c8");
			btn.MouseEntered += () => btn.SelfModulate = new Color("#ffffff");
			btn.MouseExited += () => btn.SelfModulate = new Color("#c8c8c8");
		}

		// foreach (var btn in UpgradesBtns)
		// {
		// 	btn.MouseEntered += () => btn.SelfModulate = new Color("#ffffff");
		// 	btn.MouseExited += () => btn.SelfModulate = new Color("#c8c8c8");
		// }

		GetTree().Root.SizeChanged += OnWindowResize;
		CallDeferred(nameof(OnWindowResize));
	}

	private void OnWindowResize()
	{
		if (_miniMapContainer == null)
			return;

		float miniMapHeight = _miniMapContainer.Size.Y;
		float windowWidth = GetViewport().GetVisibleRect().Size.X;
		float clampedWidth = Mathf.Min(miniMapHeight, windowWidth);
		float newAnchorLeft = 1.0f - (clampedWidth / windowWidth);

		AnchorLeft = newAnchorLeft;
		AnchorRight = 1.0f;

		Size = new Vector2(miniMapHeight, Size.Y);
	}

	private void ToggleVisibility(Container menu)
	{
		menu.Visible = !menu.Visible;
	}

	private void ShowOnly(Container toShow)
	{
		StructuresContainer.Visible = toShow == StructuresContainer;
		UnitsContainer.Visible = toShow == UnitsContainer;
		UpgradesContainer.Visible = toShow == UpgradesContainer;
	}

	private void OnStructuresBtnPressed() => ShowOnly(StructuresContainer);

	private void OnUnitsBtnPressed() => ShowOnly(UnitsContainer);

	private void OnUpgradesBtnPressed() => ShowOnly(UpgradesContainer);
}
