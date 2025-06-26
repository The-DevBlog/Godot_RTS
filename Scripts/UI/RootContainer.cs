using System;
using Godot;
using MyEnums;

public partial class RootContainer : Control
{
	[Export] public Container UIContainer { get; set; }
	[Export] public Container MiniMapContainer { get; set; }
	[Export] public Container ConstructionOptionsContainer { get; set; }
	[Export] public Container UnitOptionsContainer { get; set; }
	[Export] public Container VehicleOptionsContainer { get; set; }
	[Export] public Container UpgradeOptionsContainer { get; set; }
	[Export] public Container BarracksCountContainer { get; set; }
	[Export] public Container GarageCountContainer { get; set; }
	[Export] public Container InfoPopupContainer { get; set; }
	[Export] public Container UpgradeInfoPopupContainer { get; set; }
	[Export] public Label InfoPopupLabelName { get; set; }
	[Export] public Label InfoPopupLabelCost { get; set; }
	[Export] public Label InfoPopupLabelBuildTime { get; set; }
	[Export] public Label InfoPopupLabelEnergy { get; set; }
	[Export] public Label InfoPopupLabelHP { get; set; }
	[Export] public Label InfoPopupLabelDPS { get; set; }
	[Export] public Label InfoPopupLabelSpeed { get; set; }
	[Export] public Label UpgradeInfoPopupLabelName { get; set; }
	[Export] public Label UpgradeInfoPopupLabelDescription { get; set; }
	[Export] public Label UpgradeInfoPopupLabelCost { get; set; }
	[Export] public Label UpgradeInfoPopupLabelBuildTime { get; set; }

	private Container _structureCountContainer;
	private GlobalResources _globalResources;
	private SceneResources _sceneResources;
	private Signals _signals;
	private Color normalColor = new Color("#c8c8c8");
	private Color hoverColor = new Color("#ffffff");
	public override void _Ready()
	{
		_globalResources = GlobalResources.Instance;
		_sceneResources = SceneResources.Instance;
		_signals = Signals.Instance;
		_signals.AddStructure += AddGarageOrBarracksInstanceBtn;
		_signals.OnBuildOptionsBtnHover += ShowInfoPopup;
		_signals.OnUpgradeBtnHover += ShowUpgradeInfoPopup;
		_signals.UpdateEnergyColor += UpdateEnergyColor;

		Utils.NullExportCheck(UIContainer);
		Utils.NullExportCheck(MiniMapContainer);
		Utils.NullExportCheck(ConstructionOptionsContainer);
		Utils.NullExportCheck(BarracksCountContainer);
		Utils.NullExportCheck(GarageCountContainer);
		Utils.NullExportCheck(UnitOptionsContainer);
		Utils.NullExportCheck(VehicleOptionsContainer);
		Utils.NullExportCheck(UpgradeOptionsContainer);
		Utils.NullExportCheck(InfoPopupContainer);
		Utils.NullExportCheck(InfoPopupLabelName);
		Utils.NullExportCheck(InfoPopupLabelCost);
		Utils.NullExportCheck(InfoPopupLabelBuildTime);
		Utils.NullExportCheck(InfoPopupLabelEnergy);
		Utils.NullExportCheck(InfoPopupLabelHP);
		Utils.NullExportCheck(InfoPopupLabelDPS);
		Utils.NullExportCheck(InfoPopupLabelSpeed);
		Utils.NullExportCheck(UpgradeInfoPopupContainer);
		Utils.NullExportCheck(UpgradeInfoPopupLabelName);
		Utils.NullExportCheck(UpgradeInfoPopupLabelDescription);
		Utils.NullExportCheck(UpgradeInfoPopupLabelCost);
		Utils.NullExportCheck(UpgradeInfoPopupLabelBuildTime);

		_structureCountContainer = BarracksCountContainer.GetParent<Container>();

		SetupButtons(Group.StructureBtns);

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
		_globalResources.IsHoveringUI = mousePosition.X >= UIContainer.GlobalPosition.X;
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

		// how tall your minimap is
		float miniMapHeight = MiniMapContainer.Size.Y;

		// window width
		float windowWidth = GetViewport().GetVisibleRect().Size.X;

		// clamp so you never ask for more than the screen
		float clampedWidth = Mathf.Min(miniMapHeight, windowWidth);
		float newAnchorLeft = 1f - (clampedWidth / windowWidth);

		AnchorLeft = newAnchorLeft;
		AnchorRight = 1f;

		// 1) set the minimum-x to your minimap height
		CustomMinimumSize = new Vector2(miniMapHeight, CustomMinimumSize.Y);

		// 2) still tweak the *actual* size if you want:
		SetDeferred("size", new Vector2(
			// make sure you never shrink below miniMapHeight
			Mathf.Max(miniMapHeight, Size.X),
			Size.Y
		));
	}

	private void ToggleVisibility(Container menu) => menu.Visible = !menu.Visible;

	private void AddGarageOrBarracksInstanceBtn(int structureId)
	{
		StructureType structureType = (StructureType)structureId;

		if (structureType != StructureType.Garage && structureType != StructureType.Barracks)
			return;

		int structureCount = _sceneResources.StructureCount[structureType];

		Container structureCountContainer = _structureCountContainer.Duplicate() as Container;
		structureCountContainer.Visible = true;

		// get the placeholder that is currently in the scene
		NinePatchRect btnContainer = BarracksCountContainer.GetNode("BtnContainer").Duplicate() as NinePatchRect;
		btnContainer.Visible = true;
		if (btnContainer == null)
		{
			Utils.PrintErr($"BtnContainer not found in {structureCountContainer.Name}. Is the name correct?");
			return;
		}

		Button btn = btnContainer.GetNode<Button>("Btn");
		btn.Text = structureCount.ToString();

		Control parent = null;
		if (structureType == StructureType.Garage)
		{
			btn.Pressed += () => SetActiveGarage(btn.Text.ToInt()); ;
			parent = GarageCountContainer;
		}
		else
		{
			// btn.Pressed += OnBarracksBtnPressed;
			parent = BarracksCountContainer;
		}

		parent.AddChild(btnContainer);
	}

	private void ShowOnly(Container toShow)
	{
		ConstructionOptionsContainer.Visible = toShow == ConstructionOptionsContainer;
		UnitOptionsContainer.Visible = toShow == UnitOptionsContainer;
		VehicleOptionsContainer.Visible = toShow == VehicleOptionsContainer;
		UpgradeOptionsContainer.Visible = toShow == UpgradeOptionsContainer;
	}

	private void SetActiveGarage(int id)
	{
		_sceneResources.ActiveGarageId = --id;
		foreach (var garage in _sceneResources.GaragesMap)
		{
			if (garage.Id == id)
				garage.Activate();
			else
				garage.Deactivate();
		}
	}

	private void OnConstructionBtnPressed()
	{
		_structureCountContainer.Visible = false;
		BarracksCountContainer.Visible = false;
		GarageCountContainer.Visible = false;
		ShowOnly(ConstructionOptionsContainer);
	}

	private void OnBarracksBtnPressed()
	{
		bool isVisible = _sceneResources.StructureCount[StructureType.Barracks] > 1;
		_structureCountContainer.Visible = isVisible;
		BarracksCountContainer.Visible = isVisible;
		GarageCountContainer.Visible = false;
		ShowOnly(UnitOptionsContainer);
	}

	private void OnGarageBtnPressed()
	{
		bool isVisible = _sceneResources.StructureCount[StructureType.Garage] > 1;
		_structureCountContainer.Visible = isVisible;
		GarageCountContainer.Visible = isVisible;
		BarracksCountContainer.Visible = false;
		ShowOnly(VehicleOptionsContainer);
	}

	private void OnUpgradesBtnPressed()
	{
		_structureCountContainer.Visible = false;
		BarracksCountContainer.Visible = false;
		GarageCountContainer.Visible = false;
		ShowOnly(UpgradeOptionsContainer);
	}

	// TODO: DOes not work
	private void UpdateEnergyColor()
	{
		if (_sceneResources.EnergyConsumed > _sceneResources.Energy)
		{

		}
		else
		{
			// GD.Print("Energy is sufficient, changing color to white");
			// InfoPopupLabelEnergy.Modulate = Colors.White;
			// InfoPopupLabelEnergy.GetParent<Container>().Modulate = Colors.White;
		}
	}

	private void ShowUpgradeInfoPopup(UpgradeType upgrade)
	{
		if (upgrade == UpgradeType.None)
		{
			UpgradeInfoPopupContainer.Visible = false;
			return;
		}

		UpgradeInfoPopupContainer.Visible = true;

		UpgradeInfo upgradeInfo = UpgradeInfoData.UpgradeDataMap[upgrade];

		if (upgradeInfo == null)
		{
			Utils.PrintErr($"Upgrade data not found for: {upgrade}");
			return;
		}

		UpgradeInfoPopupLabelName.Text = upgradeInfo.Name;
		UpgradeInfoPopupLabelDescription.Text = upgradeInfo.Description;
		UpgradeInfoPopupLabelCost.Text = $"${upgradeInfo.Cost}";
		UpgradeInfoPopupLabelBuildTime.Text = $"{upgradeInfo.BuildTime}s";
	}

	private void ShowInfoPopup(StructureBase structure, Unit unit)
	{
		if (structure == null && unit == null)
		{
			InfoPopupContainer.Visible = false;
			return;
		}

		InfoPopupContainer.Visible = true;

		if (structure != null)
		{
			InfoPopupLabelEnergy.GetParent<Container>().Visible = true;
			InfoPopupLabelDPS.GetParent<Container>().Visible = false;
			InfoPopupLabelSpeed.GetParent<Container>().Visible = false;

			InfoPopupLabelName.Text = structure.Name;
			InfoPopupLabelCost.Text = $"${structure.Cost}";
			InfoPopupLabelHP.Text = $"{structure.HP}";
			InfoPopupLabelEnergy.Text = $"{(structure.Energy > 0 ? "+" : "")}{structure.Energy}";
			InfoPopupLabelBuildTime.Text = $"{structure.BuildTime}s";
			return;
		}

		if (unit != null)
		{
			bool unitUnlocked = false;
			if (unit is Vehicle vehicle)
				unitUnlocked = _sceneResources.VehicleAvailability[vehicle.VehicleType];
			else if (unit is Infantry infantry)
				unitUnlocked = _sceneResources.InfantryAvailability[infantry.InfantryType];

			if (!unitUnlocked)
			{
				InfoPopupContainer.Visible = false;
				return;
			}

			InfoPopupLabelDPS.GetParent<Container>().Visible = true;
			InfoPopupLabelSpeed.GetParent<Container>().Visible = true;
			InfoPopupLabelEnergy.GetParent<Container>().Visible = false;

			InfoPopupLabelName.Text = unit.Name;
			InfoPopupLabelCost.Text = $"${unit.Cost}";
			InfoPopupLabelHP.Text = $"{unit.HP}";
			InfoPopupLabelDPS.Text = $"{unit.DPS}";
			InfoPopupLabelSpeed.Text = $"{unit.Speed}";
			InfoPopupLabelBuildTime.Text = $"{unit.BuildTime}s";
		}
	}
}
