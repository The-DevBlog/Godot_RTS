using Godot;
using MyEnums;

public partial class TabsBtn : Button
{
	[Export] public StructureType StructureType { get; set; }
	private Signals _signals;
	private GlobalResources _globalResources;

	public override void _Ready()
	{
		_signals = Signals.Instance;
		_globalResources = GlobalResources.Instance;

		_signals.AddStructure += EnableDisableBtn;
	}

	private void EnableDisableBtn(int structureId)
	{
		StructureType structureType = (StructureType)structureId;

		if (StructureType != structureType)
			return;

		bool enableBtn = _globalResources.StructureCount[structureType] > 0;
		if (enableBtn)
		{
			// Enable the button if the structure count is greater than 0
			Modulate = new Color(1, 1, 1, 1); // Set to fully opaque
			Disabled = false;
		}
		else
		{
			// Disable the button if the structure count is 0
			Modulate = new Color(1, 1, 1, 0.5f); // Set to semi-transparent
			Disabled = true;
		}
		// enableBtn = structureType switch
		// {
		//     StructureType.Garage => _globalResources,
		//     // StructureType.Factory => true,
		//     // StructureType.PowerPlant => true,
		//     // StructureType.CommandCenter => true,
		//     _ => false
		// };
	}
}
