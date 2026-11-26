<p align="center">
  <a href="https://archnet-plateform.fr"><img src="https://archnet-plateform.fr/ArchNetLogoWithBck.png" alt="ArchNet" width="240"></a>
</p>

# ArchNet · API Service

Talk to your web API in one line, with JSON in and out, one base URL per environment, a timeout, retries and clear errors.

- Unity **6000.0** or newer · Needs [ArchNet Core](https://github.com/ArchNet-1/ArchNetCore)
- Part of [ArchNet](https://archnet-plateform.fr), free and open-source Unity packages

## Add it to your game (one click)

*ArchNet > Hub > Services > API > Add to game* creates, in `Assets/ArchNet/Api/`:

| File | What it is |
|---|---|
| `Scripts/GameApi.cs` | Your endpoints, one line each (an example `Status()`). |
| `Data/ApiSettings.asset` | Base URL per environment (Development used in the Editor and development builds, Production in release builds), timeout, retries. |
| `Prefabs/Api.prefab` | Under the ArchNet Bootstrap. |

```csharp
public Task<Profile> Profile() => Get<Profile>("profile");             // in GameApi
public Task<ScoreResult> SendScore(Score s) => Post<ScoreResult>("scores", s);

try
{
    Profile me = await Services.Get<GameApi>().Profile();
}
catch (ApiException error)                                              // StatusCode (0 = no connection), Body
{
    Services.Get<GameNotifications>().Error(error.Message);
}
```

- Network errors and server errors (5xx, 429) are tried again (2 times by default, waiting 0.5 s then 1 s); other errors throw at once.
- Results are read with `JsonUtility`: `[Serializable]` classes; wrap top-level arrays (`{ "items": [ … ] }`). Ask for `string` to get the raw text.
- **Secrets**: never put a key or password in `ApiSettings` (it ships in the game). Set `AuthToken` at runtime (sent as `Authorization: Bearer …`, kept in memory only); extra headers in `Headers`.
- Override `Transport` to sign requests or to fake the server in tests.

**Open tool**: edit the settings and send a test GET to see what the server answers.

## License

MIT, see [LICENSE.md](LICENSE.md).
