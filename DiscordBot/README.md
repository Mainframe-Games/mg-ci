# Discord Bot

## Config

exe root called `config.json`

```json

{
    "BuildServerUrl": "http://127.0.0.1:8080",
    "Token": "{discord-bot-token}",
    "GuildId": 0,
    "CommandName": "start-build",
    "AuthorisedRoles":[
        "admin",
        "Dev"
    ],
    "Workspaces": [
        {
            "WorkspaceName": "project-name",
            "ChannelId": 0
        }
    ]
}

```