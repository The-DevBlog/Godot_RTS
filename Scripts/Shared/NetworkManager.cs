using Godot;

public partial class NetworkManager : Node
{
	public static NetworkManager Instance { get; private set; }
	[Export] public int Port = 7777;
	[Export] public int MaxClients = 4;

	public bool IsServer { get; private set; }

	public override void _Ready()
	{
		Instance = this;

		// Connect to connection signals if you like
		Multiplayer.ConnectedToServer += () => GD.Print("Connected to server");
		Multiplayer.ServerDisconnected += () => GD.Print("Disconnected from server");
		Multiplayer.PeerConnected += id => GD.Print($"Peer {id} connected");
		Multiplayer.PeerDisconnected += id => GD.Print($"Peer {id} disconnected");
	}

	public void StartServer()
	{
		var peer = new ENetMultiplayerPeer();
		peer.CreateServer(Port, MaxClients);
		Multiplayer.MultiplayerPeer = peer;
		IsServer = true;
		GD.Print($"[Network] Server listening on port {Port}");
	}

	public void ConnectToServer(string address)
	{
		var peer = new ENetMultiplayerPeer();
		peer.CreateClient(address, Port);
		Multiplayer.MultiplayerPeer = peer;
		IsServer = false;
		GD.Print($"[Network] Connecting to {address}:{Port}");
	}
}
