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
- [x] iOS
- [x] Android
- [ ] Nintendo Switch
- [ ] Xbox
- [ ] Playstation

Supported deploy targets:

Client Deployments
- [x] Steam
- [x] Apple Store
- [x] Android Store
- [ ] Nintendo Switch Store
- [ ] Xbox Store
- [ ] Playstation Store

Server Deployments:
- [x] Unity Game Server Hosting (Multiplay)
  - [x] Amazing S3 Bucket
  - [ ] Container Image
  - [ ] Unity CCD
- [x] Clanforge

# Server Config

```json
{
  "runServer": true,
  "ip": "192.168.xx.xxx",
  "offloadServerUrl": "http://192.168.xx.xxx:8080",
  "steam": {
    "path": "/full-path-to/steamcmd.sh",
    "username": "",
    "password": ""
  },
  "clanforge": {
    "accessKey": "",
    "secretKey": "",
    "asid": 0,
    "machineId": 0,
    "url": "",
    "imageId": 0
  }
}
```


# Project Config

Build configs should be located in Unity project root folder `./BuilldScripts/buildconfig.json`


```json
{
  "prebuild": {
    "type": "Major_Minor",
    "changeLog": true
  },
  "builds": [
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
    "steam": [
      "path/to/build.vdf"
    ],
    "clanforge": true
  },
  "hooks": [
    {
      "url": "https://hooks.slack.com/services/...",
      "title": "{Game} Deployed"
    },
    {
      "url": "https://discord.com/api/webhooks/...",
      "title": "Change Log Bot"
    }
  ]
}

```


| Key                      | Required | Description                                                                        |
|--------------------------|----------|------------------------------------------------------------------------------------|
| `preBuildScript`         |          | Uses reflection to load prebuild class                                             |
|                          |          |                                                                                    |
| `steam`                  |          |                                                                                    |
| `steam.steamId`          | true     | The main Steam ID                                                                  |
| `steam.location`         | true     |                                                                                    |
| `steam.vdfPath`          | true     |                                                                                    |
| `steam.username`         |          |                                                                                    |
| `steam.password`         |          |                                                                                    |
|                          |          |                                                                                    |
| `executeMethod`          |          | If you want to set up your own build script in unity                               |
|                          |          |                                                                                    |
| `builds`                 |          |                                                                                    |
| `builds.target`          | true     |                                                                                    |
| `builds.settings`        | true     | Name of the settings file in Unity                                                 |
| `builds.buildPath`       | true     | Build target location. TODO: figure out a way to get this from the build settings  |
| `builds.executeMethod`   |          | If you want to set up your own build script in unity. TODO: handle this in unity   |
|                          |          |                                                                                    |
| `hooks`                  |          | Array of hook urls and titles to alert when process is completed                   |
