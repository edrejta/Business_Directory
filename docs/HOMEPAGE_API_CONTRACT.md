# Homepage Backend Contract (Feature/Home)

## Auth Matrix (Frozen)
- Public: `GET /api/promotions`, `GET /api/reviews`, `GET /api/opendays`, `POST /api/subscribe`
- Owner-only: `GET /api/owner/opendays`, `POST /api/owner/opendays`, `POST /api/promotions`
- Admin-only: not in scope for this phase

## Enum Style
- `category` for promotions is serialized as **string** (`Discounts | FlashSales | EarlyAccess`)

## GET /api/promotions
- Query params: `category?: string`, `businessId?: guid`, `onlyActive?: bool=true`
- Response `200`:
```json
[
  {
    "id": "guid",
    "businessId": "guid",
    "businessName": "string",
    "title": "string",
    "description": "string",
    "category": "Discounts",
    "originalPrice": 100.0,
    "discountedPrice": 80.0,
    "discountPercent": 20,
    "expiresAt": "2026-03-01T00:00:00Z",
    "isActive": true,
    "createdAt": "2026-02-23T21:00:00Z"
  }
]
```
- Errors: `400` (invalid query value)

## POST /api/promotions
- Auth: `BusinessOwner`
- Body:
```json
{
  "businessId": "guid",
  "title": "string",
  "description": "string",
  "category": "Discounts",
  "originalPrice": 100.0,
  "discountedPrice": 80.0,
  "expiresAt": "2026-03-01T00:00:00Z"
}
```
- Response `201`: same shape as `GET /api/promotions` item
- Errors: `400` (validation/business rule), `401`, `403`, `404`

## GET /api/opendays
- Query params: `businessId: guid` (required)
- Response `200`:
```json
{
  "businessId": "guid",
  "mondayOpen": true,
  "tuesdayOpen": true,
  "wednesdayOpen": true,
  "thursdayOpen": true,
  "fridayOpen": true,
  "saturdayOpen": false,
  "sundayOpen": false
}
```
- Errors: `400` (invalid/missing query), `404`

## GET /api/owner/opendays
- Auth: `BusinessOwner`
- Query params: `businessId: guid` (required)
- Response `200`: same as `GET /api/opendays`
- Errors: `400`, `401`, `403`, `404`

## POST /api/owner/opendays
- Auth: `BusinessOwner`
- Body:
```json
{
  "businessId": "guid",
  "mondayOpen": true,
  "tuesdayOpen": true,
  "wednesdayOpen": true,
  "thursdayOpen": true,
  "fridayOpen": true,
  "saturdayOpen": false,
  "sundayOpen": false
}
```
- Response `200`: same as `GET /api/opendays`
- Errors: `400`, `401`, `403`, `404`

## GET /api/reviews
- Query params: `businessId?: guid`, `limit?: int=20` (clamped `1..100`)
- Response `200`:
```json
[
  {
    "id": "guid",
    "businessId": "guid",
    "reviewerName": "string",
    "rating": 5,
    "comment": "string",
    "createdAt": "2026-02-23T21:00:00Z"
  }
]
```
- Errors: `400` (invalid query)

## POST /api/subscribe
- Body:
```json
{
  "email": "user@example.com"
}
```
- Response `200`:
```json
{
  "message": "Success"
}
```
- Errors: `400` (invalid email)
