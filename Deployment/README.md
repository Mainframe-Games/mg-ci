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