# Food for Thought — run on vm86:3000

The department already forwards https://vrlab.cs.umb.edu/ to vm86 port 3000. No Nginx or proxy changes are needed.

## Upload and run

1. Upload the deployment ZIP using your usual SFTP/SSH connection through `umb-cs` to `vm86`.
2. Extract it into a folder of your choice on vm86. Keep `web-server/` and `Builds/WebGL/` beside each other.
3. Inside the extracted folder, run:

```sh
sh web-server/start-vm86.sh
```

Then open **https://vrlab.cs.umb.edu/**. This command runs in the foreground; stopping it or closing the SSH session stops the server. To keep it running with the PM2 already installed on your VM:

```sh
pm2 start web-server/deploy/ecosystem.config.cjs
pm2 save
```

The PM2 app name is `food-for-thought`. Stop only this game with `pm2 stop food-for-thought`. The existing app named `server` is not modified.

No `npm install` is needed. The start script uses the separate Node 22.23.3 runtime already staged at `~/nutrition-game/runtime/node-v22.23.3-linux-x64/bin/node`. To use another Node 22+ executable, set `NODE_BIN` for the start script or `NUTRITION_NODE` for PM2.

## Contents

- `Builds/WebGL/`: compiled Unity game, including all three enabled scenes.
- `web-server/server.mjs`: Node static-file server, including Unity compression headers.
- `web-server/start-vm86.sh`: starts the game on all VM interfaces, port 3000.
- `web-server/deploy/ecosystem.config.cjs`: optional PM2 configuration.
- `web-server/server.test.mjs`: hosting tests (`node --test web-server/server.test.mjs`).

The server provides `/healthz`. Player progress is saved in each browser; there is no server-side collection of gameplay results. Right-click hints work without opening the browser context menu.

## Local preview and future builds

With Node 22 or newer, run `node web-server/server.mjs`, then open http://127.0.0.1:3000/.

In Unity 6000.6.0f1 with Web Build Support installed, stop Play mode and choose **Tools → Food for Thought → Build Web Game**. This writes a fresh build to `Builds/WebGL/`. For command-line builds, close the interactive editor first:

```sh
Unity -batchmode -quit -projectPath /path/to/nutritionGame -buildTarget WebGL -executeMethod BuildWebGame.Build -logFile web-build.log
```

Optional server settings: `PORT`, `HOST`, `BASE_PATH`, `BUILD_DIR`. By default it serves `Builds/WebGL/` at `/`, on localhost port 3000. The VM start script changes the host to `0.0.0.0` so the department proxy can reach it.

## Current deployment

Game files are deployed at `/home/fundad/fft` on vm86. The live address is **https://vrlab.cs.umb.edu/** and the PM2 app is `food-for-thought`, listening on `0.0.0.0:3000`.

For routine management on vm86:

```sh
pm2 status food-for-thought
pm2 restart food-for-thought
pm2 logs food-for-thought --lines 30
```

Do not run `npm start` alongside PM2: only one process can use port 3000. For a manual run, first stop the PM2 app, then use `sh ~/fft/web-server/start-vm86.sh`.
