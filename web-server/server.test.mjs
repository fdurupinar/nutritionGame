import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, mkdir, writeFile, symlink, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import http from 'node:http';
import { gzipSync, brotliCompressSync } from 'node:zlib';
import { createGameServer } from './server.mjs';

test('Unity hosting: compression, mount path, HEAD, readiness and file isolation', async () => {
  const root = await mkdtemp(path.join(tmpdir(), 'fft-web-'));
  await mkdir(path.join(root, 'Build'));
  await writeFile(path.join(root, 'index.html'), '<canvas></canvas>');
  await writeFile(path.join(root, 'Build/game.wasm.gz'), gzipSync('wasm fixture'));
  await writeFile(path.join(root, 'Build/game.framework.js.br'), brotliCompressSync('JS fixture'));
  await symlink('/etc/hosts', path.join(root, 'outside'));
  const server = createGameServer({ root, basePath: '/nutrition-game/' });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const request = (url, method = 'GET') => new Promise((resolve, reject) => {
    http.request({ host: '127.0.0.1', port: server.address().port, path: url, method }, res => {
      const data = []; res.on('data', c => data.push(c));
      res.on('end', () => resolve({ status: res.statusCode, headers: res.headers, body: Buffer.concat(data) }));
    }).on('error', reject).end();
  });
  try {
    assert.equal((await request('/')).headers.location, '/nutrition-game/');
    assert.equal((await request('/nutrition-game/')).status, 200);
    assert.equal((await request('/nutrition-game/healthz')).status, 200);
    const wasm = await request('/nutrition-game/Build/game.wasm.gz');
    assert.equal(wasm.headers['content-type'], 'application/wasm');
    assert.equal(wasm.headers['content-encoding'], 'gzip');
    const js = await request('/nutrition-game/Build/game.framework.js.br');
    assert.equal(js.headers['content-type'], 'application/javascript');
    assert.equal(js.headers['content-encoding'], 'br');
    const head = await request('/nutrition-game/Build/game.wasm.gz', 'HEAD');
    assert.equal(head.body.length, 0); assert.equal(Number(head.headers['content-length']), wasm.body.length);
    for (const url of ['/nutrition-game/%2e%2e/package.json', '/nutrition-game/.env', '/nutrition-game/outside']) assert.equal((await request(url)).status, 403);
    assert.equal((await request('/nutrition-game/%zz')).status, 400);
    assert.equal((await request('/nutrition-game/missing')).status, 404);
    assert.equal((await request('/nutrition-game/', 'POST')).status, 405);
    await rm(path.join(root, 'index.html'));
    assert.equal((await request('/nutrition-game/healthz')).status, 503);
  } finally { await new Promise(resolve => server.close(resolve)); await rm(root, { recursive: true, force: true }); }
});
