# Server

This is the main entry point for the build pipeline. 

# Endpoints 

This list will change often so wont be accurate, however at any time you can use `/info` to see the current list of endpoints.

## /info

```json
{
    "version": "1.6.0.0",
    "startTime": "2023-10-04 12:08:17Z",
    "runTime": "0d 0h 0m 3s",
    "endPoints": [
        "UploadBuild: PUT /upload",
        "BuildWorkspace: POST /build",
        "GameServerUpdate: POST /game-server-update",
        "OffloadBuild: POST /offload-build",
        "OffloadBuildResponse: POST /offload-response",
        "ProductionRequest: POST /production",
        "Commits: GET /commits",
        "ServerInfo: GET /info",
        "Workspaces: GET /workspaces",
        "CancelPipeline: DELETE /cancel"
    ]
}
```