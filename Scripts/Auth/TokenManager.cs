using Godot;

public partial class TokenManager : Node
{
	private string _accessToken = string.Empty;
	private string _refreshToken = string.Empty;
	private bool _isRefreshing = false;

	public string AccessToken => _accessToken;
	public string RefreshToken => _refreshToken;
	public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);

	public override void _Ready()
	{
		GD.Print("TokenManager: _Ready() called");
	}

	public void SetTokens(string accessToken, string refreshToken)
	{
		GD.Print("TokenManager: SetTokens called");
		_accessToken = accessToken;
		_refreshToken = refreshToken;
	}

	public void ClearTokens()
	{
		GD.Print("TokenManager: ClearTokens called");
		_accessToken = string.Empty;
		_refreshToken = string.Empty;
	}

	public bool IsRefreshing()
	{
		return _isRefreshing;
	}

	public void SetRefreshing(bool value)
	{
		_isRefreshing = value;
	}
}
