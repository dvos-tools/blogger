# Mirror / Networking Integration

## ?? Server-Controlled Session IDs

### SERVER: Generate and send session ID
```csharp
void OnPlayerConnected(NetworkConnectionToClient conn) {
    string sessionId = Guid.NewGuid().ToString("N").Substring(0, 16);
    conn.identity.GetComponent<PlayerScript>().RpcSetSessionId(sessionId);
}
```

### CLIENT: Receive and apply session ID
```csharp
[ClientRpc]
void RpcSetSessionId(string sessionId) {
    LoggingContext.SetSessionId(sessionId);  // ? All logs now include this sessionId!
    Debug.Log("Connected to server");
}
```

### Query logs by session in Grafana:
```logql
{app="unity-client"} |= "sessionId=abc123"
```

## ?? Match-Based Sessions

Use matchId as sessionId to group all logs from a match:

```csharp
void OnMatchStart(string matchId, string playerId) {
    LoggingContext.SetSessionId(matchId);     // ? Track by match
    LoggingContext.SetUserId(playerId);       // ? Track by player
    Debug.Log("Match started");
    // Output: "[userId=player123 sessionId=match-xyz ...] Match started"
}
```

Query all logs from a specific match:
```logql
{app="unity-client"} |= "sessionId=match-xyz789"
```
