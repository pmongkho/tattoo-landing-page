# Angular Client

This directory is the frontend/UI project for the tattoo landing page.

It intentionally remains in-repo at `angular-client/` and contains:
- Angular application source under `src/`
- Tailwind configuration
- Environment-based API configuration for local and production builds

Run locally:

```bash
cd angular-client
npm install
npm start
```


search avail square

curl https://connect.squareup.com/v2/bookings/availability/search \
  -X POST \
  -H 'Square-Version: 2026-01-22' \
  -H 'Authorization: Bearer EAAAlyeq3SqlYqwZkk33rBRXcQ_snlHVPOfrSSvM_7j82eReXr6VIRa-wm5dnkoI' \
  -H 'Content-Type: application/json' \
  -d '{
    "query": {
      "filter": {
        "location_id": "LPS2BMRF76GV9",
        "start_at_range": {
          "end_at": "2026-06-12T20:28:19.776Z",
          "start_at": "2026-05-12T20:28:22.137Z"
        },
        "segment_filters": [
          {
            "service_variation_id": "RVHGBTDGK5RDQPLB7VI4ORIT"
          }
        ],
        "booking_id": ""
      }
    }
  }'

  