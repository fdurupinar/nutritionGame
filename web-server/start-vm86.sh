#!/bin/sh
set -eu
cd "$(dirname "$0")"
if [ -n "${NODE_BIN:-}" ]; then
  game_node="$NODE_BIN"
elif [ -x "$HOME/nutrition-game/runtime/node-v22.23.3-linux-x64/bin/node" ]; then
  game_node="$HOME/nutrition-game/runtime/node-v22.23.3-linux-x64/bin/node"
else
  game_node=node
fi
"$game_node" -e 'if (Number(process.versions.node.split(".")[0]) < 22) { console.error("Use Node.js 22 or newer. Set NODE_BIN to its executable path."); process.exit(1); }'
export HOST=0.0.0.0
export PORT=3000
export BASE_PATH=/
exec "$game_node" server.mjs
