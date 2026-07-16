<div align="center">

# Lichess API — s&box Edition

A C# wrapper for the [Lichess API](https://lichess.org/api), adapted for use within the **s&box sandbox and whitelist environment**.

Build Lichess-connected s&box games with account integration, online matches, puzzles, analysis, streaming, bots, teams, and more.

</div>

---

## Overview

**Lichess API — s&box Edition** provides a strongly typed C# interface for communicating with Lichess from an s&box project.

The library is based on the structure of LichessNET, with networking, serialization, request handling, and other functionality adapted to work within s&box.

It includes:

* Authenticated Lichess API requests
* Automatic bearer-token authorization
* Internal API rate-limit handling
* Strongly typed response models
* NDJSON streaming support
* s&box-compatible HTTP requests
* s&box-compatible JSON serialization
* Account, game, puzzle, analysis, bot, team, user, and tablebase APIs

> [!IMPORTANT]
> This is an unofficial community library and is not affiliated with, endorsed by, or maintained by Lichess.

---

## Supported APIs

| API           | Description                                                                                     |
| ------------- | ----------------------------------------------------------------------------------------------- |
| **Account**   | Retrieve account information, preferences, email, timeline, relationships, and account settings |
| **Games**     | Create, manage, stream, export, and interact with Lichess games                                 |
| **Puzzles**   | Retrieve and interact with Lichess puzzles                                                      |
| **Analysis**  | Access cloud analysis and evaluation data                                                       |
| **Users**     | Retrieve public user profiles and user-related information                                      |
| **OAuth**     | Test and revoke Lichess access tokens                                                           |
| **Bots**      | Build and manage Lichess bot integrations                                                       |
| **Teams**     | Retrieve and interact with Lichess teams                                                        |
| **Tablebase** | Query endgame tablebase information                                                             |
| **Streams**   | Process streamed and NDJSON API responses                                                       |

---

## Installation

### Clone the repository

```bash
git clone https://github.com/beters02/lichess_api_sbox_edition.git
```

Place the repository in your s&box addons or projects directory.

### Add it to your project

Add **Lichess API — s&box Edition** as an addon or project dependency through the s&box Project Editor.

After the dependency is mounted, import the API namespace:

```csharp
using LichessNET.API;
```

---

## Quick Start

### Create a client

```csharp
using LichessNET.API;

public sealed class LichessService
{
	private readonly LichessApiClient Client = new();

	public async Task InitializeAsync( string accessToken )
	{
		await Client.SetToken( accessToken );
	}
}
```

Logging is enabled by default. It can be disabled when constructing the client:

```csharp
var client = new LichessApiClient( doLogging: false );
```

---

## Authentication

Authenticated endpoints require a Lichess personal access token or an OAuth access token.

Set the token before making authenticated requests:

```csharp
var client = new LichessApiClient();

await client.SetToken( accessToken );
```

The client automatically adds the following header to authenticated requests:

```http
Authorization: Bearer YOUR_ACCESS_TOKEN
```

> [!CAUTION]
> Never commit access tokens to GitHub or include them directly in published source code.

Store tokens using an appropriate local or server-side persistence system for your project.

---

## Get the Authenticated User

```csharp
var client = new LichessApiClient();
await client.SetToken( accessToken );

var user = await client.GetOwnProfile();

Log.Info( $"Connected to Lichess as {user.Username}" );
```

---

## Get Account Preferences

```csharp
var preferences = await client.GetAccountPreferences();

Log.Info( $"Animation preference: {preferences.Animation}" );
```

---

## Get the Account Email

The token must include the required Lichess account email scope.

```csharp
var email = await client.GetAccountEmail();

Log.Info( $"Lichess account email: {email}" );
```

---

## Follow a Player

```csharp
var success = await client.FollowPlayerAsync( "lichess_username" );

if ( success )
{
	Log.Info( "Player followed successfully." );
}
```

Additional relationship methods include:

```csharp
await client.UnfollowPlayerAsync( "lichess_username" );
await client.BlockPlayerAsync( "lichess_username" );
await client.UnblockPlayerAsync( "lichess_username" );
```

---

## Test an Access Token

```csharp
var results = await client.TestTokensAsync(
	new List<string>
	{
		accessToken
	}
);

foreach ( var result in results )
{
	if ( result.Value is null )
	{
		Log.Warning( "The token is invalid or expired." );
		continue;
	}

	Log.Info( $"Valid token for {result.Value.Username}" );
}
```

---

## Revoke an Access Token

```csharp
await client.DeleteTokenAsync( accessToken );
```

Once revoked, the token can no longer be used to access the associated Lichess account.

---

## API Rate Limits

The client contains an internal rate-limit controller that coordinates requests to supported Lichess endpoints.

```csharp
var client = new LichessApiClient();
```

You generally do not need to manually delay standard API calls. The client consumes registered rate-limit buckets before sending requests.

Lichess may still return errors or temporarily restrict requests when an application sends excessive traffic. Applications should catch request failures and avoid immediately retrying requests in a tight loop.

---

## Error Handling

Network requests can fail because of:

* Invalid or expired access tokens
* Missing OAuth scopes
* Lichess rate limits
* Network interruptions
* Invalid request parameters
* Temporary Lichess outages

Wrap API calls in `try`/`catch` when failure needs to be handled gracefully:

```csharp
try
{
	var user = await client.GetOwnProfile();
	Log.Info( $"Logged in as {user.Username}" );
}
catch ( Exception exception )
{
	Log.Error( $"Lichess request failed: {exception.Message}" );
}
```

---

## Project Structure

```text
Code/
├── API/
│   ├── AccountAPI.cs
│   ├── AnalysisAPI.cs
│   ├── BotAPI.cs
│   ├── GamesAPI.cs
│   ├── LichessAPIClient.cs
│   ├── LichessStream.cs
│   ├── OAuthAPI.cs
│   ├── PuzzlesAPI.cs
│   ├── TablebaseAPI.cs
│   ├── TeamAPI.cs
│   └── UsersAPI.cs
├── Converters/
├── Database/
├── Entities/
│   ├── Account/
│   ├── Analysis/
│   ├── Enumerations/
│   ├── Game/
│   ├── Interfaces/
│   ├── OAuth/
│   ├── Puzzle/
│   ├── Social/
│   ├── Stats/
│   ├── Teams/
│   └── Tournament/
├── Extensions/
├── Internal/
└── Test/
```

---

## s&box Compatibility

This edition replaces or adapts functionality that is unavailable under the s&box API whitelist.

Notable adaptations include:

* Requests sent through `Sandbox.Http`
* Whitelist-compatible request construction
* Query-based fallbacks where raw request bodies are unavailable
* Custom JSON converters for Lichess response types
* s&box-compatible logging
* Streaming support designed around Lichess NDJSON responses

---

## Example Service Component

```csharp
using LichessNET.API;

public sealed class LichessManager : Component
{
	private LichessApiClient Client { get; } = new();

	public bool IsAuthenticated =>
		!string.IsNullOrWhiteSpace( Client.GetToken() );

	public async Task ConnectAsync( string accessToken )
	{
		try
		{
			await Client.SetToken( accessToken );

			var profile = await Client.GetOwnProfile();

			Log.Info( $"Connected to Lichess as {profile.Username}" );
		}
		catch ( Exception exception )
		{
			await Client.SetToken( null );

			Log.Error( $"Unable to connect to Lichess: {exception.Message}" );
		}
	}

	public async Task DisconnectAsync()
	{
		await Client.SetToken( null );
	}
}
```

---

## Example Project

This library was created for use with **kachess**, an s&box chess game that connects directly to a player’s Lichess account.

Games played through kachess are synchronized with Lichess, allowing an s&box player to play against users on the Lichess website or app.

---

## Development Status

This project is under active development.

Some Lichess endpoints may be:

* Untested
* Partially implemented
* Subject to API changes
* Limited by the s&box whitelist or networking environment

Please report reproducible problems through the repository’s issue tracker.

---

## Contributing

Contributions are welcome.

1. Fork the repository.
2. Create a feature branch.
3. Make and test your changes.
4. Keep new code compatible with the s&box whitelist.
5. Submit a pull request with a clear description.

When adding an endpoint, include:

* Strongly typed request and response models
* XML documentation
* Appropriate error handling
* Rate-limit consideration
* A test or usage example where practical

---

## Useful Links

* [Lichess API Documentation](https://lichess.org/api)
* [Lichess OAuth Documentation](https://lichess.org/api#tag/OAuth)
* [Create a Lichess Personal Access Token](https://lichess.org/account/oauth/token)
* [s&box Documentation](https://sbox.game/dev/doc/)
* [Repository Issues](https://github.com/beters02/lichess_api_sbox_edition/issues)

---

## Credits

This project is an s&box-compatible adaptation of concepts and code from the LichessNET ecosystem.

* [Lichess](https://lichess.org)
* [Lichess API](https://lichess.org/api)
* [s&box](https://sbox.game)

---

## Disclaimer

Lichess is a trademark of its respective owner.

This project is an independent, unofficial API wrapper. Users of this library are responsible for complying with the Lichess API terms, OAuth requirements, rate limits, and fair-play policies.
