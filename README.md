# JellyDefaults

A Jellyfin plugin that gives users a default home screen library order instead of the alphabetical one.

## Features

- Define the library order (and which views are hidden from "My Media") once, in the plugin settings
- Every newly created user gets it automatically
- Apply it to existing users: only those still on the alphabetical default, or everyone (opt-in checkbox), after a confirmation listing the affected users
- Check out my other plugins: [JellyRate](https://github.com/trecoool/jellyrate), [JellyRequest](https://github.com/trecoool/jellyrequest) and [JellyPrecache](https://github.com/trecoool/jellyprecache).

## Requirements

- Jellyfin 12.3.x
- .NET 10.0

## API Endpoints (admin only)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/JellyDefaults/Views` | Top-level views (libraries, Playlists, Collections) |
| POST | `/JellyDefaults/Apply?includeCustomized=&dryRun=` | Apply the template to existing users, returns affected user names |

## Installation

### Via plugin repository (recommended)

1. In Jellyfin, go to **Dashboard → Plugins → Repositories**.
2. Click **+** and add:
   - **Name**: `JellyDefaults`
   - **URL**: `https://raw.githubusercontent.com/trecoool/jellydefaults/master/manifest.json`
3. Open the **Catalog** tab, find **JellyDefaults**, and install it.
4. Restart Jellyfin.

### Manual install

1. Download the latest `jellydefaults_<version>.zip` from the [Releases](https://github.com/trecoool/jellydefaults/releases) page.
2. Extract `JellyDefaults.dll` to `<jellyfin-config>/plugins/JellyDefaults/`.
3. Restart Jellyfin.

### Build from source

```bash
dotnet build -c Release
```

Copy `bin/Release/net10.0/JellyDefaults.dll` to `<jellyfin-config>/plugins/JellyDefaults/`, then restart Jellyfin.
