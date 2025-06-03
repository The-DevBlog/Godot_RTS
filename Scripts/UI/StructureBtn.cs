using Godot;
using MyEnums;

public partial class StructureBtn : Button
{
    [Export]
    public Structure Structure { get; set; }

    public override void _Ready()
    {
        if (Structure == Structure.None)
            Utils.PrintErr("Structure Enum is not set for " + Name);

        Pressed += OnButtonPressed;
    }

    private void OnButtonPressed()
    {
        GD.Print("Button pressed for structure: " + Structure);
    }
}
