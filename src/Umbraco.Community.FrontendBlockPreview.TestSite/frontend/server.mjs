// A tiny headless frontend for trying the package: plain HTML and JavaScript, no framework, no dependencies.
// Run the test site with the "Headless (Delivery API)" profile, then: node frontend/server.mjs
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { extname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('.', import.meta.url));
const port = 5173;
const types = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css' };

createServer(async (request, response) => {
  const path = new URL(request.url, `http://localhost:${port}`).pathname;

  // Static files, otherwise every route (including /block-preview/{token}/) gets the app: the catch-all route
  const file = path === '/app.js' ? 'app.js' : 'index.html';
  const body = await readFile(join(root, file));

  response.writeHead(200, { 'Content-Type': types[extname(file)] });
  response.end(body);
}).listen(port, () => console.log(`Demo frontend on http://localhost:${port}`));
