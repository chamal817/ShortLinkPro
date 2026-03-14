## Story 4.4: k6 load tests for redirect and create endpoints

As an operator or performance engineer,  
I want k6 load tests for the redirect and create endpoints so that I can validate latency and throughput targets (10M+ redirects/day, P95 < 100ms, cache hit rate > 80%).

### Acceptance Criteria

- **Given** the API and its dependencies (PostgreSQL, Redis) are running in a test or staging environment  
  **When** I run the provided k6 scripts for the redirect path and the create endpoint  
  **Then** there are k6 test files (e.g. `load/k6/redirect.js` and `load/k6/create.js`) that generate representative traffic against `GET /{shortCode}` and `POST /api/links`  
  **And** the scripts define thresholds that reflect PRD NFRs (e.g. P95 redirect latency < 100ms, acceptable error rate)  
  **And** the tests can be parameterized for target RPS and environment URLs  
  **And** documentation explains how to run the k6 tests and interpret results to decide whether NFRs are being met

### Implementation Plan

- **Directory structure**
  - Create `load/k6/redirect.js`.
  - Create `load/k6/create.js`.
- **Configuration**
  - Use environment variables for:
    - `BASE_URL` (e.g. `http://localhost:8080`).
    - `TARGET_RPS` or stages.
  - Define thresholds:
    - `http_req_duration{endpoint="redirect"}: p(95) < 100`.
    - Low error rate (e.g. `< 1%`).
- **Redirect test (redirect.js)**
  - Pre-seed a set of valid short codes (from a file, env, or k6 options).
  - In the `default` function:
    - Make `GET /{shortCode}` requests.
    - Tag requests as `endpoint="redirect"`.
- **Create test (create.js)**
  - Generate random valid long URLs.
  - In the `default` function:
    - Make `POST /api/links` requests with JSON bodies.
    - Tag requests as `endpoint="create"`.
- **Docs**
  - Document how to run:
    - `k6 run load/k6/redirect.js`.
    - `k6 run -e BASE_URL=... load/k6/create.js`.

### Sample Implementation Code (`load/k6/redirect.js`)

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 100 },
    { duration: '1m', target: 100 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    'http_req_duration{endpoint:redirect}': ['p(95)<100'],
    'http_req_failed{endpoint:redirect}': ['rate<0.01'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const SHORT_CODES = (__ENV.SHORT_CODES || 'abc123,xyz789').split(',');

export default function () {
  const shortCode = SHORT_CODES[Math.floor(Math.random() * SHORT_CODES.length)];
  const res = http.get(`${BASE_URL}/${shortCode}`, {
    tags: { endpoint: 'redirect' },
  });

  check(res, {
    'status is 302 or 301': (r) => r.status === 302 || r.status === 301,
  });

  sleep(0.1);
}
```

### Sample Implementation Code (`load/k6/create.js`)

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 20 },
    { duration: '1m', target: 20 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    'http_req_duration{endpoint:create}': ['p(95)<200'],
    'http_req_failed{endpoint:create}': ['rate<0.01'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';

function randomUrl() {
  const id = Math.floor(Math.random() * 1e9);
  return `https://example.com/page/${id}`;
}

export default function () {
  const payload = JSON.stringify({ longUrl: randomUrl() });
  const params = {
    headers: { 'Content-Type': 'application/json' },
    tags: { endpoint: 'create' },
  };

  const res = http.post(`${BASE_URL}/api/links`, payload, params);

  check(res, {
    'status is 201': (r) => r.status === 201,
  });

  sleep(0.1);
}
```

