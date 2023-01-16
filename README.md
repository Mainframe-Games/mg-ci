# Unity Build and Deployment Server

## Projects

- [Deployment](Deployment/README.md): Builds and deploys Unity projects.
- [DiscordBot](DiscordBot/README.md): For triggering builds from discord.
- BuildSystem: Unity Package for build script and platform configs


### TODO
-[ ] Separate Build and Deployment processes into different projects as deployment will get more bulky 
-[ ] Make this into a public package


# Setup Guide

1. Copy and paste the `Unity/BuildSystem` folder into the `/Packages` folder.
2. Build `DiscordBot` and host it on a server somewhere or locally. Can be shared CPU.
3. Build `Deployment` and host it on fast machine, don't use shared CPU for this.
4. Download [Steamworks SDK](https://partner.steamgames.com/doc/sdk). Unzip to a location on same server as `Deployment`
5. Create configs and run `DiscordBot` and `Deployment` (see respective readmes)
