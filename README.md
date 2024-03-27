## ⚠️ This project is under active development. Use with care. ⚠️

# Build and Deployment Server

![](img/gui-client.png)

# Project Contents

This project is a collection of tools for managing builds and deployments of Unity and Godot projects. It is designed to
be used with Unity and Godot projects that are built for multiple platforms and require automated deployment to
storefronts.

- [AvaloniaAppMVVM](AvaloniaAppMVVM/README.md): Gui app for managing builds and deployments.
- [Deployment](Deployment/README.md): Deploys artifacts to storefronts (Steam, Itch.io, Google Play Store, Apple Store)
- [Server](Server/README.md): Web socket server for conducting builds and deployments.
- [DiscordBot](DiscordBot/README.md): Discord bot for starting builds and deployments also for notifications.
- [Unity Package](UnityPackage/BuildSystem/README.md): Unity Package for build script and platform configs

# Setup Guide

1. Install Unity Package `https://github.com/Mainframe-Games/UnityDeployment.git?path=Unity/BuildSystem`
2. Build `DiscordBot` and host it on a server somewhere or locally. Can be shared CPU.
3. Build `Server` and host it on fast machine, don't use shared CPU for this.
4. Download [Steamworks SDK](https://partner.steamgames.com/doc/sdk). Unzip to a location on same server as `Deployment`
5. Create configs and run `DiscordBot` and `Deployment` (see respective readmes)

# Test Projects

- Unity (Mono/IL2CPP) - https://github.com/Mainframe-Games/mg-ci-test-unity
- Godot (Mono) - TODO