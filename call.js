const http = require('http');

const options = {
  hostname: 'localhost',
  port: 5190,
  path: '/value-slow/',
  method: 'GET',
  headers: {
    'Accept': 'application/json'
  }
};

function makeRequest(id) {
  return new Promise((resolve, reject) => {
    const startTime = Date.now();
    const req = http.request(options, (res) => {
      let data = '';
      res.on('data', (chunk) => { data += chunk; });
      res.on('end', () => {
        const duration = Date.now() - startTime;
        let stale = 'unknown';
        let timestamp = 'unknown';
        try {
          const json = JSON.parse(data);
          stale = json.stale;
          timestamp = json.result.value.timestamp;
        } catch (e) {
          // ignore
        }
        console.log(`Request ${id < 10 ? '0' + id : id}: Status ${res.statusCode}, Duration ${duration}ms, Stale: ${stale}, Timestamp: ${timestamp}`);
        resolve();
      });
    });

    req.on('error', (e) => {
      console.error(`Request ${id}: Failed - ${e.message}`);
      resolve();
    });

    req.end();
  });
}

async function run() {
  console.log(`Starting 10 concurrent requests to http://${options.hostname}:${options.port}${options.path}`);
  const promises = [];
  for (let i = 1; i <= 10; i++) {
    promises.push(makeRequest(i));
  }
  await Promise.all(promises);
  console.log('All requests completed');
}

run();
