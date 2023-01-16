# Unity Build and Deployment Server

## Projects

- [Deployment](Deployment/README.md): Builds and deploys Unity projects.
- [DiscordBot](DiscordBot/README.md): For triggering builds from discord.
- [BuildSystem](Unity/BuildSystem/README.md): Unity Package for build script and platform configs

# Setup Guide

1. Install Unity Package `https://github.com/Mainframe-Games/UnityDeployment.git?path=Unity/BuildSystem`
2. Build `DiscordBot` and host it on a server somewhere or locally. Can be shared CPU.
3. Build `Deployment` and host it on fast machine, don't use shared CPU for this.
4. Download [Steamworks SDK](https://partner.steamgames.com/doc/sdk). Unzip to a location on same server as `Deployment`
5. Create configs and run `DiscordBot` and `Deployment` (see respective readmes)
