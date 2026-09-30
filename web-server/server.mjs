import http from 'node:http';
import { createReadStream } from 'node:fs';
import { realpath, stat } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const types = { '.html': 'text/html; charset=utf-8', '.js': 'application/javascript', '.wasm': 'application/wasm', '.data': 'application/octet-stream', '.json': 'application/json', '.css': 'text/css', '.png': 'image/png', '.jpg': 'image/jpeg', '.svg': 'image/svg+xml', '.ico': 'image/x-icon', '.mp4': 'video/mp4', '.ogg': 'audio/ogg', '.wav': 'audio/wav' };
export function createGameServer({ root = path.resolve(here, '../Builds/WebGL'), basePath = '/' } = {}) {
  root = path.resolve(root);
  basePath = '/' + basePath.split('/').filter(Boolean).join('/');
  if (basePath !== '/') basePath += '/';
  return http.createServer(async (req, res) => {
    const reply = (status, body) => { res.writeHead(status, { 'Content-Type': 'text/plain; charset=utf-8', 'Cache-Control': 'no-store' }); res.end(req.method === 'HEAD' ? undefined : body); };
    if (!['GET', 'HEAD'].includes(req.method)) { res.setHeader('Allow', 'GET, HEAD'); return reply(405, 'Method not allowed'); }
    let pathname;
    try { pathname = decodeURIComponent(req.url.split('?')[0]); } catch { return reply(400, 'Invalid URL'); }
    if (pathname.includes('\0') || pathname.includes('\\') || pathname.split('/').some(p => p === '..' || p.startsWith('.'))) return reply(403, 'Forbidden');
    if (basePath !== '/' && (pathname === '/' || pathname === basePath.slice(0, -1))) {
      res.writeHead(308, { Location: basePath }); return res.end();
    }
    if (!pathname.startsWith(basePath)) return reply(404, 'Not found');
    const relative = pathname.slice(basePath.length) || 'index.html';
    if (relative === 'healthz') {
      try { await stat(path.join(root, 'index.html')); return reply(200, 'ready'); }
      catch { return reply(503, 'WebGL build not installed'); }
    }
    try {
      const [canonicalRoot, filename] = await Promise.all([realpath(root), realpath(path.resolve(root, relative))]);
      if (!filename.startsWith(canonicalRoot + path.sep)) return reply(403, 'Forbidden');
      const info = await stat(filename);
      if (!info.isFile()) return reply(404, 'Not found');
      const encoding = filename.endsWith('.gz') ? 'gzip' : filename.endsWith('.br') ? 'br' : null;
      const uncompressedName = encoding ? filename.slice(0, -3) : filename;
      const headers = { 'Content-Type': types[path.extname(uncompressedName)] || 'application/octet-stream', 'Content-Length': info.size, 'Cache-Control': 'no-cache', 'X-Content-Type-Options': 'nosniff' };
      if (encoding) headers['Content-Encoding'] = encoding;
      res.writeHead(200, headers);
      if (req.method === 'HEAD') return res.end();
      const stream = createReadStream(filename);
      stream.on('error', () => res.destroy());
      res.on('close', () => stream.destroy());
      stream.pipe(res);
    } catch (error) {
      reply(error.code === 'ENOENT' || error.code === 'ENOTDIR' ? 404 : 500, 'File unavailable');
    }
  });
}

export async function startGameServer() {
  const root = path.resolve(process.env.BUILD_DIR || path.join(here, '../Builds/WebGL'));
  await stat(path.join(root, 'index.html')).catch(() => { throw new Error(`No WebGL build at ${root}. Build the game in Unity first.`); });
  const host = process.env.HOST || '127.0.0.1';
  const port = Number(process.env.PORT || 3000);
  const basePath = process.env.BASE_PATH || '/';
  const server = createGameServer({ root, basePath });
  server.listen(port, host, () => console.log(`Food for Thought: http://${host}:${port}${basePath}`));
  for (const signal of ['SIGTERM', 'SIGINT']) process.on(signal, () => server.close(() => process.exit(0)));
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) await startGameServer();
