# MuuqWearApi

## Product size stock batch update

**Route:** `PATCH api/Product/{productId}/size-stock/batch`  
**Auth:** Admin JWT (`Authorization: Bearer {token}`)

Atomically updates multiple per-size inventory rows for one product. All changes succeed together or none are persisted (rollback on failure).

### Sample request

```http
PATCH /api/Product/{productId}/size-stock/batch
Authorization: Bearer {adminToken}
Content-Type: application/json

{
  "items": [
    { "sizeStockId": "c8c255ef-7275-4941-a9bb-3cccdc8e07ed", "quantity": 10 },
    { "sizeStockId": "5b963079-bc80-4920-b8b4-7ae7badf8894", "quantity": 5 }
  ],
  "upserts": [
    { "size": "XL", "quantity": 2 }
  ]
}
```

At least one of `items` or `upserts` is required.

### Sample response

```json
{
  "success": true,
  "message": "Size stock updated",
  "data": {
    "sizeStock": [
      { "id": "...", "size": "S", "quantity": 10 },
      { "id": "...", "size": "XL", "quantity": 2 }
    ],
    "totalStock": 12
  }
}
```

Existing single-size endpoints (`PATCH api/Product/size-stock/{id}`, `POST api/Product/{id}/size-stock`) are unchanged.

---

## Journal article editorial fields

**Migration:** `docs/scripts/add-journal-article-fields.sql` (run in Supabase before deploy)

Additive JSON fields on journal content items (non-breaking):

| Field | Notes |
|-------|--------|
| `author`, `excerpt`, `slug`, `seoTitle` | Editorial / SEO |
| `tags` | `string[]` |
| `isFeatured` | Only one published hero at a time |
| `scheduledAt` | Required when `status = scheduled` |
| `readTimeMinutes` | Auto-computed from content (~200 wpm) if omitted |

**Public routes:**

- `GET api/Journal` — published list with `excerpt`, tags, slug; includes `featuredArticle` in paginated payload
- `GET api/Journal/featured` — hero article
- `GET api/Journal/{id}` — full published article
- `GET api/Journal/by-slug/{slug}` — full published article by slug
- `POST api/Journal/{id}/view` — increment views

Scheduled articles are promoted to `published` on read when `scheduled_at <= now` (v1).
