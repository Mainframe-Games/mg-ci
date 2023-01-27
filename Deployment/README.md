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
- [x] Itchio (Stopped support, but its still in there just needs the class to be hooked up)
- [ ] Apple Store
- [ ] Android Store

# Server Config

```json
{
  "runServer": true,
  "ip": "127.0.0.1",
  "port": 8080,
  "offloadServerUrl": "192.168.x.xxx",
  "steam":{
    "path": "~/Documents/ContentBuilder/Steam",
    "username": "",
    "password": ""
  }
}
```


# Project Config

Build configs should be located in Unity project root folder `./BuilldScripts/buildconfig.json`


```json
{
  "preBuildScript": "None",
  "builds":[
    {
      "target": "Win64",
      "settings": "BuildSettings_Win64",
      "buildPath": "Builds/win64"
    },
    {
      "target": "OSXUniversal",
      "settings": "BuildSettings_MacOS",
      "buildPath": "Builds/macos"
    },
    {
      "target": "Linux64",
      "settings": "BuildSettings_Linux64Server",
      "buildPath": "Builds/linux64_server",
      "versionExtension": "-x86_64"
    }
  ],
  "deploy": {
    "steam": {
      "vdfPath": "path/to/build.vdf",
      "setLive": "beta"
    }
  },
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
| `preBuildScript`          |          | Uses reflection to load prebuild class |
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
