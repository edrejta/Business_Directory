const hooks = require('hooks');
const http = require('http');
const { execSync } = require('child_process');
const UUID_RE = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i;

let adminToken = null;
let userToken = null;
let existingBusinessId = '11111111-1111-1111-1111-111111111111';
let uniqueCounter = 0;

function nextEmail(prefix = 'contract') {
  uniqueCounter += 1;
  return `${prefix}.${Date.now()}.${uniqueCounter}@test.local`;
}

function requestJson(method, path, body, token) {
  return new Promise((resolve, reject) => {
    const payload = body ? JSON.stringify(body) : null;
    const headers = {
      'Content-Type': 'application/json'
    };

    if (payload) headers['Content-Length'] = Buffer.byteLength(payload);
    if (token) headers.Authorization = `Bearer ${token}`;

    const req = http.request(
      {
        hostname: '127.0.0.1',
        port: 5003,
        method,
        path,
        headers
      },
      (res) => {
        let data = '';
        res.on('data', (chunk) => {
          data += chunk;
        });
        res.on('end', () => {
          let parsed = null;
          if (data) {
            try {
              parsed = JSON.parse(data);
            } catch {
              parsed = null;
            }
          }
          resolve({ statusCode: res.statusCode, body: parsed, rawBody: data });
        });
      }
    );

    req.on('error', reject);
    if (payload) req.write(payload);
    req.end();
  });
}

function tryReadTokenViaDocker(email) {
  const query = `SET NOCOUNT ON; SELECT TOP 1 EmailVerificationToken FROM Users WHERE Email='${email.replace(/'/g, "''")}';`;
  const cmd = `docker exec business-directory-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Your_strong_password123!" -d BusinessDirectory -C -h -1 -W -Q "${query}"`;
  const output = execSync(cmd, { stdio: ['ignore', 'pipe', 'ignore'], encoding: 'utf8' });
  return output.trim().split(/\r?\n/).filter(Boolean).pop() || '';
}

function tryReadTokenViaHostSqlcmd(email) {
  const query = `SET NOCOUNT ON; SELECT TOP 1 EmailVerificationToken FROM Users WHERE Email='${email.replace(/'/g, "''")}';`;
  const cmd = `sqlcmd -S localhost,1433 -U sa -P "Your_strong_password123!" -d BusinessDirectory -C -h -1 -W -Q "${query}"`;
  const output = execSync(cmd, { stdio: ['ignore', 'pipe', 'ignore'], encoding: 'utf8' });
  return output.trim().split(/\r?\n/).filter(Boolean).pop() || '';
}

function readVerificationToken(email) {
  for (let i = 0; i < 10; i += 1) {
    try {
      const token = tryReadTokenViaDocker(email);
      if (token) return token;
    } catch {}

    try {
      const token = tryReadTokenViaHostSqlcmd(email);
      if (token) return token;
    } catch {}

    Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, 200);
  }

  return '';
}

function tryReadLatestTokenViaDocker() {
  const query = "SET NOCOUNT ON; SELECT TOP 1 EmailVerificationToken FROM Users WHERE EmailVerified=0 AND EmailVerificationToken IS NOT NULL ORDER BY CreatedAt DESC;";
  const cmd = `docker exec business-directory-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Your_strong_password123!" -d BusinessDirectory -C -h -1 -W -Q "${query}"`;
  const output = execSync(cmd, { stdio: ['ignore', 'pipe', 'ignore'], encoding: 'utf8' });
  return output.trim().split(/\r?\n/).filter(Boolean).pop() || '';
}

function tryReadLatestTokenViaHostSqlcmd() {
  const query = "SET NOCOUNT ON; SELECT TOP 1 EmailVerificationToken FROM Users WHERE EmailVerified=0 AND EmailVerificationToken IS NOT NULL ORDER BY CreatedAt DESC;";
  const cmd = `sqlcmd -S localhost,1433 -U sa -P "Your_strong_password123!" -d BusinessDirectory -C -h -1 -W -Q "${query}"`;
  const output = execSync(cmd, { stdio: ['ignore', 'pipe', 'ignore'], encoding: 'utf8' });
  return output.trim().split(/\r?\n/).filter(Boolean).pop() || '';
}

function readLatestVerificationToken() {
  for (let i = 0; i < 10; i += 1) {
    try {
      const token = tryReadLatestTokenViaDocker();
      if (token) return token;
    } catch {}

    try {
      const token = tryReadLatestTokenViaHostSqlcmd();
      if (token) return token;
    } catch {}

    Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, 200);
  }

  return '';
}

hooks.beforeAll(async (transactions, done) => {
  try {
    const adminLogin = await requestJson('POST', '/api/auth/login', {
      email: 'admin@business.local',
      password: 'Admin12345!'
    });

    if (adminLogin.statusCode === 200 && adminLogin.body && adminLogin.body.token) {
      adminToken = adminLogin.body.token;
    }

    const userLogin = await requestJson('POST', '/api/auth/login', {
      email: 'user@business.local',
      password: 'User12345!'
    });

    if (userLogin.statusCode === 200 && userLogin.body && userLogin.body.token) {
      userToken = userLogin.body.token;
    }

    if (adminToken) {
      const businesses = await requestJson('GET', '/api/admin/businesses?page=1&pageSize=1', null, adminToken);
      if (businesses.statusCode === 200 && Array.isArray(businesses.body) && businesses.body.length > 0) {
        existingBusinessId = businesses.body[0].id || existingBusinessId;
      }
    }
  } catch (err) {
    // keep defaults so contract tests can still run and show clear API errors
  }

  done();
});

hooks.beforeEach(async (transaction, done) => {
  const path = transaction.fullPath || '';
  const method = (transaction.request.method || 'GET').toUpperCase();
  const expectedStatus = Number(transaction.expected && transaction.expected.statusCode);

  try {
    if (path === '/api/auth/register' && method === 'POST') {
      const valid = expectedStatus === 201;
      transaction.request.body = JSON.stringify({
        username: valid ? 'contract.user' : '',
        email: valid ? nextEmail('register') : 'bad-email',
        password: valid ? 'Pass12345!' : '123',
        role: valid ? 0 : 7
      });
    }

    if (path === '/api/auth/login' && method === 'POST') {
      const valid = expectedStatus === 200;
      transaction.request.body = JSON.stringify({
        email: valid ? 'admin@business.local' : 'admin@business.local',
        password: valid ? 'Admin12345!' : 'WrongPassword!'
      });
    }

    if (path === '/api/auth/verify-email' && method === 'POST') {
      if (expectedStatus === 200) {
        let token = readLatestVerificationToken();
        if (!token) {
          const email = nextEmail('verify');
          const registerResponse = await requestJson('POST', '/api/auth/register', {
            username: 'verify.contract',
            email,
            password: 'Pass12345!',
            role: 0
          });

          if (registerResponse.statusCode === 201) {
            token = readVerificationToken(email);
          }
        }

        transaction.request.body = JSON.stringify({ token: token || 'invalid-test-token' });
      } else {
        transaction.request.body = JSON.stringify({ token: 'invalid-test-token' });
      }
    }

    if (path === '/api/auth/resend-verification' && method === 'POST') {
      transaction.request.body = JSON.stringify({ email: nextEmail('resend') });
    }

    if (path === '/subscribe' && method === 'POST') {
      const valid = expectedStatus === 200;
      transaction.request.body = JSON.stringify({
        email: valid ? nextEmail('subscribe') : ''
      });
    }

    if (path.includes('/api/admin/')) {
      delete transaction.request.headers.Authorization;

      if (expectedStatus === 200 || expectedStatus === 404) {
        if (adminToken) transaction.request.headers.Authorization = `Bearer ${adminToken}`;
      } else if (expectedStatus === 403) {
        if (userToken) transaction.request.headers.Authorization = `Bearer ${userToken}`;
      }

      if (path.includes('{id}')) {
        const replacementId = expectedStatus === 404
          ? '00000000-0000-0000-0000-000000000001'
          : existingBusinessId;

        transaction.fullPath = path.replace('{id}', replacementId);
        transaction.request.uri = transaction.request.uri.replace('{id}', replacementId);
      } else if (path.includes('/api/admin/businesses/') && UUID_RE.test(path)) {
        const replacementId = expectedStatus === 404
          ? '00000000-0000-0000-0000-000000000001'
          : existingBusinessId;

        transaction.fullPath = path.replace(UUID_RE, replacementId);
        transaction.request.uri = transaction.request.uri.replace(UUID_RE, replacementId);
      }
    }
  } catch (err) {
    transaction.request.body = transaction.request.body || '{}';
  }

  done();
});

hooks.beforeEachValidation((transaction, done) => {
  const contentType = transaction.real && transaction.real.headers && transaction.real.headers['content-type'];
  if (contentType && contentType.includes('application/json')) {
    transaction.real.headers['content-type'] = 'application/json';
  }
  done();
});
