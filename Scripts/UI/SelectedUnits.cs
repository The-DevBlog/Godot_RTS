using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using MyEnums;

public partial class SelectedUnits : MarginContainer
{
	[Export] private Container _spacer;
	[Export] private GridContainer _grid;
	[Export] private NinePatchRect _selectedUnitPlaceholder;
	private MyTextures _textures;
	private Player _player;

	public override void _Ready()
	{
		_player = PlayerManager.Instance.LocalPlayer;
		_textures = AssetServer.Instance.Textures;
		_player.SelectUnits += ToggleVisibility;


		Utils.NullExportCheck(_spacer);
		Utils.NullExportCheck(_grid);
		Utils.NullExportCheck(_selectedUnitPlaceholder);
	}

	private void ToggleVisibility(Unit[] units)
	{
		ClearGrid();

		Dictionary<VehicleType, int> vehicleCount = new Dictionary<VehicleType, int>();
		Dictionary<InfantryType, int> infantryCount = new Dictionary<InfantryType, int>();

		foreach (Unit unit in units)
		{
			if (unit is Vehicle vehicle)
			{
				if (!vehicleCount.ContainsKey(vehicle.VehicleType))
					vehicleCount[vehicle.VehicleType] = 0;

				vehicleCount[vehicle.VehicleType]++;
			}
			else if (unit is Infantry infantry)
			{
				if (!infantryCount.ContainsKey(infantry.InfantryType))

					infantryCount[infantry.InfantryType] = 0;
				infantryCount[infantry.InfantryType]++;
			}
		}

		CreateVehicleSlots(vehicleCount);
		CreateVehicleSlots(infantryCount);

		bool visible = units != null && units.Length > 0;

		Visible = visible;
		_spacer.Visible = !visible;


	}

	private void CreateVehicleSlots<T>(Dictionary<T, int> vehicleCount)
	{
		foreach (var kv in vehicleCount)
		{
			var slot = _selectedUnitPlaceholder.Duplicate() as NinePatchRect;
			if (slot == null)
				continue;

			var label = slot.GetNode<Label>("MarginContainer/Label");
			var textureRect = slot.GetNode<TextureRect>("TextureRect");

			Texture2D unitTexture = null;
			if (kv.Key is VehicleType vehicleType)
				unitTexture = _textures.Vehicles[vehicleType];
			else if (kv.Key is InfantryType infantryType)
				unitTexture = _textures.Infantry[infantryType];

			if (unitTexture == null)
			{
				Utils.PrintErr($"Texture for {kv.Key} not found.");
				continue;
			}

			textureRect.Texture = unitTexture;
			label.Text = kv.Value.ToString();
			slot.Visible = true;

			_grid.AddChild(slot);
		}
	}

	private void ClearGrid()
	{
		for (int i = _grid.GetChildCount() - 1; i >= 0; i--)
		{
			Node child = _grid.GetChild(i);

			if (child == _selectedUnitPlaceholder)
				continue;

			_grid.RemoveChild(child);
			child.QueueFree();
		}
	}
}
