# Changelog

## [2.0.0] - 2026-11-26

First version, numbered like the other ArchNet 2.0 packages.

### Added
- `ApiService`: `Get`, `Post`, `Put`, `Delete` with JSON, base URL per environment, timeout, retries on network and server errors, `ApiException` with status and body, `AuthToken` and `Headers`, overridable `Transport`.
- `ApiSettings` asset.
- ArchNet Hub service **API**: `GameApi.cs`, `ApiSettings.asset`, `Api.prefab` under the Bootstrap; API window with a test GET.
- Edit Mode tests.
