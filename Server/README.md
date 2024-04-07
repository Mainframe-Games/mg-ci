# Info

### Prebuild

Ran on the server before the build starts. This is where the game project version bumping is done and committed back to repo


### Build

Ran on the build runners. This can be on the same machine as the main server but can also be offloaded to other machines. 

Uses port `8081`

This is where the game is built, the build is then uploaded to the main server.


### Deploy

Ran on the main server after all the builds are completed. 

This is where the build is deployed to the game servers.


### Hooks

There are hooks for each of the stages. These are ran before and after each stage.
