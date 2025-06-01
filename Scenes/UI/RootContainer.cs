using Godot;

public partial class RootContainer : Container
{
    public override void _Ready()
    {
        GetTree().Root.SizeChanged += OnWindowResize;
        OnWindowResize();
    }

    private void OnWindowResize()
    {
        float windowWidth = GetViewport().GetVisibleRect().Size.X;

        var anchorLeft = 0.819f; // <= 2500px
        if (windowWidth >= 2500f)
            anchorLeft = 0.863f; // >= 2500px

        AnchorLeft = anchorLeft;
    }
}
