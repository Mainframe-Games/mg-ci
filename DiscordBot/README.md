# Discord Bot

## Config

By default the program with look for the config file `config-discord.json` at the root of the exe location.

If one doesn't exist one will be created.

It can be a good idea to run the exe first to get a template of the config file.

Additionally you can use `-config` flag to any location on disk.


```json

{
    "buildServerUrl": "http://127.0.0.1:8080",
    "token": "string",
    "guildId": 0,
    "commandName": "start-build",
    "authorisedRoles": [
        "admin",
        "Dev"
    ]
}

```

# Slash Commands

For a general overview of Discord slash commands see:

- [Discord.NET](https://discordnet.dev/guides/int_basics/application-commands/intro.html)
- [Discord Web API](https://discord.com/developers/docs/interactions/application-commands#slash-commands)

## Guild Commands

### /docs

Opens this documents page

### /start-build (or`commandName`)
Starts a build on the master server at url `buildServerUrl`.

I most cases you can simply choose the workspace you wish to build and thats it.

Additionally you can send a string of arguments to the build server

Notable args:

`-branch`: Builds workspace from non-default branch

`-setlive`: Deploys to non-default Steam channel

`-clanforge`: Deploys server to non-default profile

Example:

`-branch main/dev-branch -setlive beta -clanforge devb`
Builds from specific branch and deploys to non-default server environment

For more arguments see [Builds README](../Builds/README.md#arguments)

### /refresh-workspaces

Refreshes the workspaces on the build server. 
This is so if you add a new workspace you don't need to restart server application.

### /server-update-clanforge

### /server-update-ugs (coming soon)

### /production