using System.Collections.Generic;
using Godot;
using MyEnums;

public class UpgradeInfo
{
	public string Name { get; set; }
	public string Description { get; set; }
	public int Cost { get; set; }
	public int BuildTime { get; set; }
}

public static class UpgradeInfoData
{
	public static readonly Dictionary<UpgradeType, UpgradeInfo> UpgradeDataMap = new Dictionary<UpgradeType, UpgradeInfo>
	{
		{ UpgradeType.None, new UpgradeInfo { Name = UpgradeType.None.ToString(), Description = "No upgrade selected", Cost = 0, BuildTime = 0 } },
		{ UpgradeType.Upgrade1, new UpgradeInfo { Name = UpgradeType.Upgrade1.ToString(), Description = "Upgrade 1 description about some kind of cool upgrade thingy majig.", Cost = 100, BuildTime = 5 } },
		{ UpgradeType.Upgrade2, new UpgradeInfo { Name = UpgradeType.Upgrade2.ToString(), Description = "Upgrade 2 description about some kind of cool upgrade thingy majig.", Cost = 200, BuildTime = 10 } },
		{ UpgradeType.Upgrade3, new UpgradeInfo { Name = UpgradeType.Upgrade3.ToString(), Description = "Upgrade 3 description about some kind of cool upgrade thingy majig.", Cost = 300, BuildTime = 15 } },
		{ UpgradeType.Upgrade4, new UpgradeInfo { Name = UpgradeType.Upgrade4.ToString(), Description = "Upgrade 4 description about some kind of cool upgrade thingy majig.", Cost = 400, BuildTime = 20 } },
		{ UpgradeType.Upgrade5, new UpgradeInfo { Name = UpgradeType.Upgrade5.ToString(), Description = "Upgrade 5 description about some kind of cool upgrade thingy majig.", Cost = 500, BuildTime = 25 } },
		{ UpgradeType.Upgrade6, new UpgradeInfo { Name = UpgradeType.Upgrade6.ToString(), Description = "Upgrade 6 description about some kind of cool upgrade thingy majig.", Cost = 600, BuildTime = 30 } },
		{ UpgradeType.Upgrade7, new UpgradeInfo { Name = UpgradeType.Upgrade7.ToString(), Description = "Upgrade 7 description about some kind of cool upgrade thingy majig.", Cost = 700, BuildTime = 35 } },
		{ UpgradeType.Upgrade8, new UpgradeInfo { Name = UpgradeType.Upgrade8.ToString(), Description = "Upgrade 8 description about some kind of cool upgrade thingy majig.", Cost = 800, BuildTime = 40 } }
	};
}

public partial class UpgradeBtn : Button
{
	[Export] public UpgradeType Upgrade { get; set; }
	private Player _player;
	private Signals _signals;
	private Color _normalModulate = new Color("#c8c8c8");
	private Color _hoverModulate = new Color("#ffffff");
	private Color _disabledModulate = new Color("#262626");
	private TextureRect _lockTexture;

	public override void _Ready()
	{
		if (Upgrade == UpgradeType.None)
			Utils.PrintErr("Upgrade type is set to none");

		_lockTexture = GetNode<TextureRect>("LockTexture");
		_player = PlayerManager.Instance.LocalPlayer;

		if (_lockTexture == null) Utils.PrintErr("LockTexture not found for unit: " + Upgrade.ToString());


		SelfModulate = Disabled ? _disabledModulate : _normalModulate;

		MouseEntered += OnMouseEnter;
		MouseExited += OnMouseExit;
		_signals = Signals.Instance;
		_player.UpdateUpgradesAvailability += EnableDisableBtns;
	}

	private void OnMouseEnter()
	{
		if (Disabled)
			return;

		SelfModulate = _hoverModulate;

		_signals.EmitUpgradeBtnHover(Upgrade);
	}

	private void OnMouseExit()
	{
		SelfModulate = Disabled ? _disabledModulate : _normalModulate;
		_signals.EmitUpgradeBtnHover(UpgradeType.None);
	}

	private void EnableDisableBtns()
	{
		Disabled = !_player.UpgradesAvailable;
		SelfModulate = Disabled ? _disabledModulate : _normalModulate;
		_lockTexture.Visible = Disabled;
	}
}
