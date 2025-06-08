using System;
using Godot;
using MyEnums;

public partial class RootContainer : Container
{
	[Export] public Container MiniMapContainer { get; set; }
	[Export] public Container StructuresContainer { get; set; }
	[Export] public Container UnitsContainer { get; set; }
	[Export] public Container VehiclesContainer { get; set; }
	[Export] public Container UpgradesContainer { get; set; }
	[Export] public Container UnitStructureCountContainer { get; set; }
	[Export] public Container VehicleStructureCountContainer { get; set; }
	[Export] public NinePatchRect StructureCountBtn { get; set; }
	private Resources _resources;
	private Signals _signals;
	private Color normalColor = new Color("#c8c8c8");
	private Color hoverColor = new Color("#ffffff");
	public override void _Ready()
	{
		_resources = Resources.Instance;
		_signals = Signals.Instance;
		_signals.AddStructure += OnStructureAdd;

		Utils.NullCheck(MiniMapContainer);
		Utils.NullCheck(StructuresContainer);
		Utils.NullCheck(UnitStructureCountContainer);
		Utils.NullCheck(VehicleStructureCountContainer);
		Utils.NullCheck(UnitsContainer);
		Utils.NullCheck(VehiclesContainer);
		Utils.NullCheck(UpgradesContainer);
		Utils.NullCheck(StructureCountBtn);

		SetupButtons(Group.StructureBtns);
		SetupButtons(Group.UnitBtns);
		SetupButtons(Group.VehicleBtns);

		GetTree().Root.SizeChanged += OnWindowResize;
		CallDeferred(nameof(OnWindowResize));
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
		if (MiniMapContainer == null)
			return;

		float miniMapHeight = MiniMapContainer.Size.Y;
		float windowWidth = GetViewport().GetVisibleRect().Size.X;
		float clampedWidth = Mathf.Min(miniMapHeight, windowWidth);
		float newAnchorLeft = 1.0f - (clampedWidth / windowWidth);

		AnchorLeft = newAnchorLeft;
		AnchorRight = 1.0f;

		SetDeferred("size", new Vector2(miniMapHeight, Size.Y));
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

	private void OnStructureAdd(int structureId)
	{
		StructureType structureType = (StructureType)structureId;

		if (structureType != StructureType.Garage && structureType != StructureType.Barracks)
		{
			GD.Print("Returning");
			return;
		}

		int structureCount = _resources.StructureCount[structureType];

		NinePatchRect btnContainer = StructureCountBtn.Duplicate() as NinePatchRect;
		btnContainer.Visible = true;
		Button btn = btnContainer.GetNode<Button>("Btn");

		if (btn == null)
		{
			Utils.PrintErr("Button not found in StructureCountBtn. Is the name correct?");
			return;
		}

		btn.Text = structureCount.ToString();

		Control parent = structureType == StructureType.Garage ? VehicleStructureCountContainer : UnitStructureCountContainer;
		parent.AddChild(btnContainer);
	}

	private void OnStructuresBtnPressed()
	{
		UnitStructureCountContainer.Visible = false;
		VehicleStructureCountContainer.Visible = false;
		ShowOnly(StructuresContainer);
	}

	private void OnUnitsBtnPressed()
	{
		UnitStructureCountContainer.Visible = true;
		VehicleStructureCountContainer.Visible = false;
		ShowOnly(UnitsContainer);
	}

	private void OnVehiclesBtnPressed()
	{
		UnitStructureCountContainer.Visible = false;
		VehicleStructureCountContainer.Visible = true;
		ShowOnly(VehiclesContainer);
	}

	private void OnUpgradesBtnPressed()
	{
		UnitStructureCountContainer.Visible = false;
		VehicleStructureCountContainer.Visible = false;
		ShowOnly(UpgradesContainer);
	}
}
