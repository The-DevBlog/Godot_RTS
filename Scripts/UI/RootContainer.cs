using System;
using Godot;
using MyEnums;

public partial class RootContainer : Container
{
	[Export] public Container StructuresContainer { get; set; }
	[Export] public Container UnitsContainer { get; set; }
	[Export] public Container VehiclesContainer { get; set; }
	[Export] public Container UpgradesContainer { get; set; }
	private Resources _resources;
	private Color normalColor = new Color("#c8c8c8");
	private Color hoverColor = new Color("#ffffff");
	private MarginContainer _miniMapContainer;
	public override void _Ready()
	{
		_miniMapContainer = GetNode<MarginContainer>("VBoxContainer/MiniMapContainer");
		_resources = Resources.Instance;

		if (_miniMapContainer == null)
			Utils.PrintErr("MiniMapContainer node not found.");

		SetupButtons(Group.StructureBtns);
		SetupButtons(Group.UnitBtns);
		SetupButtons(Group.VehicleBtns);

		GetTree().Root.SizeChanged += OnWindowResize;
		CallDeferred(nameof(OnWindowResize));

		NullCheck();
	}

	public override void _Process(double delta)
	{
		SetIsHoveringUI();
	}

	private void SetIsHoveringUI()
	{
		var mousePosition = GetViewport().GetMousePosition();
		_resources.IsHoveringUI = mousePosition.X >= GlobalPosition.X;
	}

	private void NullCheck()
	{
		if (StructuresContainer == null)
			Utils.PrintErr("StructuresContainer is not set.");

		if (UnitsContainer == null)
			Utils.PrintErr("UnitsContainer is not set.");

		if (VehiclesContainer == null)
			Utils.PrintErr("VehiclesContainer is not set.");

		if (UpgradesContainer == null)
			Utils.PrintErr("UpgradesContainer is not set.");

		if (_miniMapContainer == null)
			Utils.PrintErr("MiniMapContainer is not set.");
	}

	private void SetupButtons(Enum group)
	{
		var rawBtnList = GetTree().GetNodesInGroup(group.ToString());
		foreach (var node in rawBtnList)
		{
			if (node is Button btn)
			{
				btn.Modulate = normalColor;
				btn.MouseEntered += () => btn.Modulate = hoverColor;
				btn.MouseExited += () => btn.Modulate = normalColor;
			}
		}
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
		VehiclesContainer.Visible = toShow == VehiclesContainer;
		UpgradesContainer.Visible = toShow == UpgradesContainer;
	}

	private void OnStructuresBtnPressed() => ShowOnly(StructuresContainer);

	private void OnUnitsBtnPressed() => ShowOnly(UnitsContainer);

	private void OnVehiclesBtnPressed() => ShowOnly(VehiclesContainer);

	private void OnUpgradesBtnPressed() => ShowOnly(UpgradesContainer);
}
