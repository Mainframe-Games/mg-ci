# Unity Deployment

Builds Unity projects and deploys various platforms.

### Supports

Version Control Support
- [x] Plastic SCM
- [ ] Git

Supported build targets:
- [x] Windows
- [x] Mac
- [x] Linux
- [ ] iOS
- [ ] Android
- [ ] Nintendo Switch
- [ ] Xbox
- [ ] Playstation

Supported deploy targets:
- [x] Steam
- [x] Itchio
- [ ] Apple Store
- [ ] Android Store


# Project Config

Build configs should be located in Unity project root folder `./BuilldScripts/buildconfig.json`


```json
{
  "preBuildScript": "Deployment.PreBuild.PreBuild_Mainframe",
  "builds":[
    {
      "steam": {
        "steamId": 69420,
        "location": "~/Documents/ContentBuilder/Steam",
        "vdfPath": "BuildScripts/Steam/build_official.vdf",
        "setLive": "beta",
        "username": "",
        "password": ""
      },
      "executeMethod": "BuildSystem.BuildScript.BuildPlayer",
      "unityPath": "/Applications/Unity/Hub/Editor/{unityVersion}/Unity.app/Contents/MacOS/Unity",
      "targets": [
        {
          "target": "Win64",
          "settings": "BuildSettings_Win64",
          "os": "windows",
          "buildPath": "Builds/win64"
        },
        {
          "target": "OSXUniversal",
          "settings": "BuildSettings_MacOS",
          "buildPath": "Builds/macos"
        },
        {
          "target": "Linux64",
          "settings": "BuildSettings_Linux64",
          "buildPath": "Builds/linux64"
        }
      ]
    }
  ],
  "hooks": {
    "slack": {
      "url": "https://hooks.slack.com/services/...",
      "title": "{Game} Deployed"
    },
    "discord":{
      "url": "https://discord.com/api/webhooks/...",
      "title": "Change Log Bot"
    }
  }
}

```


| Key                       | Required | Description                            |
|---------------------------|----------|----------------------------------------|
| `preBuildScript`          | true     | Uses reflection to load prebuild class |
|                           |          |                                        |
| `steam`                   |          |                                        |
| `steam.steamId`           | true     | The main Steam ID                      |
| `steam.location`          | true     |                                        |
| `steam.vdfPath`           | true     |                                        |
| `steam.username`          |          |                                        |
| `steam.password`          |          |                                        |
|                           |          |                                        |
| `executeMethod`           |          |                                        |
| `unityPath `              | true     |                                        |
|                           |          |                                        |
| `targets`                 |          |                                        |
| `targets.target`          | true     |                                        |
| `targets.settings`        | true     |                                        |
| `targets.buildPath`       | true     |                                        |
| `targets.os`              |          | windows, osx                           |
| `targets.executeMethod`   |          |                                        |
| `targets.unityPath`       |          |                                        |
| `targets.offloadUrl`      |          |                                        |
|                           |          |                                        |
| `hooks`                   |          |                                        |
| `hooks.slack`             |          |                                        |
| `hooks.discord`           |          |                                        |
