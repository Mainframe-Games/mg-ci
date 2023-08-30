## ⚠️ This project is under active development. Use with care. ⚠️

# Unity Deployment

Builds Unity projects and deploys various platforms.

## Supports

### Version Control Support
- Plastic SCM

#### Supported build targets
- Standalone (Windows/Mac/Linux)
- Dedicated Server (Windows/Mac/Linux)
- iOS
- Android

### Supported deploy targets

#### Client Deployments
- Steam
- Apple Store
- Android Store

#### Server Deployments:
- Clanforge
- Unity Game Server Hosting (Multiplay)
  - Amazing S3 Bucket

# Server Config

```json
{
    "ip": "192.168.x.xxx",
    "offloadServerUrl": "http://192.168.x.xxx:8080",
    "offload": {
        "targets": [
            "Win64"
        ]
    },
    "hooks": [
        {
            "url": "",
            "title": ""
        }
    ],
    "ugs": {
        "keyId": "",
        "secretKey": "",
        "projects": [
            {
                "name": "",
                "projectId": ""
            }
        ],
        "remoteConfig": {
            "configId": "",
            "valueKey": ""
        }
    },
    "appleStore": {
        "appleId": "",
        "appSpecificPassword": ""
    },
    "steam": {
        "path": "/Users/.../steamcmd.sh",
        "username": "",
        "password": "",
        "defaultSetLive": ""
    },
    "clanforge": {
        "accessKey": "",
        "secretKey": "",
        "asid": 0,
        "machineId": 0,
        "url": "steam://<user>:<password>@install/<app-id>?beta=<branch>&betapassword=<password>&guardcode=<code>",
        "defaultProfile": "deva",
        "profiles": {
            "proda": {
                "name": "Production A",
                "id": 0
            },
            "prodb": {
                "name": "Production B",
                "id": 0
            },
            "deva": {
                "name": "Development A",
                "id": 0
            },
            "devb": {
                "name": "Development B",
                "id": 0
            }
        }
    }
}
```
