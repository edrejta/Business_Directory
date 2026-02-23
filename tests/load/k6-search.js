import http from 'k6/http';
import { check, sleep } from 'k6';

const baseUrl = __ENV.BASE_URL || 'http://127.0.0.1:5003';

export const options = {
  scenarios: {
    aggressive_search: {
      executor: 'ramping-vus',
      startVUs: 20,
      stages: [
        { duration: '20s', target: 80 },
        { duration: '40s', target: 120 },
        { duration: '20s', target: 0 }
      ],
      gracefulRampDown: '10s'
    }
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<1200', 'p(99)<2000']
  }
};

export default function () {
  const url = `${baseUrl}/search?onlyWithCoordinates=true&lat=42.66&lng=21.16&radiusKm=20&limit=20&page=1&sortBy=distance`;
  const res = http.get(url, { headers: { Accept: 'application/json' } });

  check(res, {
    'status is 200': (r) => r.status === 200
  });

  sleep(0.1);
}
