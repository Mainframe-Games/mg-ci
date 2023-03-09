# Build Server Set Up Guide

## Gotchas
- Windows you may need to allow the port in the firewall
- Windows you MUST run exe as Administrator
- Mac exe needs to be run in terminal, doesn't register config file when double clicking on app for some reason
- Mac once the `Deployment` server is terminated another can't be started without restarting machine. So need to figure out why thats happening. Seems like the server isn't killed properly.
- Mac you may need to run `sudo chmod +X Development` to allow it run on machine.

## Master Server (Mac)

1. Set up `config-server.json` at location of exe.
2. Set `runServer: true`
3. Set `ip` to local ip of the machine `192.168.xx.xxx`
4. Set `offloadServerUrl` to where the offload server is on network. Can be remote or local. Local is easier. `http://192.168.50.205:8080`
5. Open terminal in folder of app
5. Run `./Deployment`


## Offload Server (Windows)

1. Set up `config-server.json` at location of exe.
2. Set `runServer: true`
3. Set `ip` to local ip of the machine `192.168.xx.xxx`
4. Right click `Deployment.exe` and Run as Administrator
