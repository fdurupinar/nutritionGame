const path = require('path');
module.exports = {
  apps: [{
    name: 'food-for-thought',
    cwd: path.resolve(__dirname, '..'),
    script: 'start.mjs',
    interpreter: process.env.NUTRITION_NODE || '/home/fundad/nutrition-game/runtime/node-v22.23.3-linux-x64/bin/node',
    instances: 1,
    exec_mode: 'fork',
    autorestart: true,
    max_memory_restart: '256M',
    env: { NODE_ENV: 'production', HOST: '0.0.0.0', PORT: '3000', BASE_PATH: '/' }
  }]
};
