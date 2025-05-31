using Godot;

public partial class StatDisplay : VBoxContainer
{
	private Label _coinLabel;
	private Label _scoreLabel;
	private string _coinTxt = "85";
	private string _scoreTxt = "1250";

	public override void _Ready()
	{
		_coinLabel = GetNode<Label>("CoinDisplay");
		_scoreLabel = GetNode<Label>("ScoreDisplay");

		_coinLabel.Text = "COINS: " + _coinTxt;
		_scoreLabel.Text = "SCORE: " + _scoreTxt;
	}
}
