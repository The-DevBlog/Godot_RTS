using Godot;

public partial class FpsCounter : CanvasLayer
{
	private Label _fpsLabel;

	public override void _Ready()
	{
		_fpsLabel = GetNode<Label>("Label");
	}

	public override void _Process(double delta)
	{
		_fpsLabel.Text = $"FPS: {Engine.GetFramesPerSecond()}";
	}
}
