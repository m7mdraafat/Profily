# ADR-008: Payment Integration (Paymob)

**Status:** Accepted  
**Date:** 2026-03-27  

---

## Context

Profily has a Free/Pro pricing model ($10/year). We need to integrate Paymob as the payment provider for v1, supporting Egyptian local payment methods. LemonSqueezy will be added in v2 for global payments.

## Constraints

- Based in Egypt — need local payment methods (cards, Fawry, Vodafone Cash, InstaPay)
- $10/year recurring subscription
- No complex billing (one plan, one price)
- Simple user model: `plan` column on users table (`free`/`pro`) + `plan_expires_at`

---

## Paymob Integration Model

### Subscription Module

Paymob has a built-in Subscription Module that handles recurring billing automatically — no need to manually charge users each year.

**Flow:**
1. Create a Subscription Plan on Paymob (one-time setup, done via API or dashboard)
2. When user upgrades, create a Subscription linking them to the plan
3. First payment collected via hosted checkout
4. Paymob automatically charges yearly — sends webhook on each deduction
5. Our API receives webhook → updates `plan`/`plan_expires_at`

### One-Time Setup: Create Subscription Plan

Done once (via API call or Paymob dashboard). Defines the Pro plan:

```
POST https://accept.paymob.com/api/acceptance/subscription-plans
Authorization: Bearer {auth_token}

{
  "name": "Profily Pro - Yearly",
  "frequency": 360,                    // yearly (360 days)
  "amount_cents": 1000,                // $10 = 1000 cents (or EGP equivalent)
  "use_transaction_amount": false,
  "is_active": true,
  "webhook_url": "https://profily-api.azurewebsites.net/api/payments/webhook",
  "reminder_days": "7",                // remind 7 days before renewal
  "retrial_days": "1,3,7",            // retry failed payments on days 1, 3, 7
  "plan_type": "rent",
  "integration": {MOTO_INTEGRATION_ID}
}
```

Response returns `plan_id` — stored in app config.

---

## Payment Flow

### Upgrade to Pro

```
User clicks "Upgrade to Pro" in dashboard
    │
    ▼
Frontend: POST /api/payments/checkout
    │
    ▼
API:
    1. Create Paymob auth token (POST /api/auth/tokens)
    2. Create payment intention (POST /v1/intention)
        - amount: 1000 cents ($10)
        - currency: "EGP" (or USD)
        - payment_methods: [card, fawry, wallet]
        - subscription_plan_id: {plan_id}
        - billing_data: { user_id, email, name }
        - redirection_url: "https://app.profily.dev/dashboard?payment=success"
    3. Return checkout URL to frontend
    │
    ▼
Frontend: Redirect user to Paymob hosted checkout
    │
    ▼
Paymob hosted checkout:
    - User sees payment options (Card, Fawry, Vodafone Cash, etc.)
    - User completes payment
    - Paymob redirects to: app.profily.dev/dashboard?payment=success
    │
    ▼
Simultaneously:
    Paymob → POST webhook to /api/payments/webhook
    │
    ▼
API: Verify HMAC → Update user plan
```

### Sequence Diagram

```
Browser              Frontend             API                 Paymob
  │                    │                    │                    │
  │── Click Upgrade ──▶│                    │                    │
  │                    │── POST /payments/ ─▶│                    │
  │                    │   checkout          │                    │
  │                    │                    │── Auth token ──────▶│
  │                    │                    │◀── token ──────────│
  │                    │                    │── Create intention ▶│
  │                    │                    │◀── checkout URL ───│
  │                    │◀── checkout URL ──│                    │
  │◀── Redirect ──────│                    │                    │
  │                    │                    │                    │
  │── Complete payment on Paymob hosted checkout ──────────────▶│
  │                    │                    │                    │
  │◀── Redirect back ─┼────────────────────┼── redirect ───────│
  │    (success page)  │                    │                    │
  │                    │                    │◀── Webhook POST ──│
  │                    │                    │── Verify HMAC      │
  │                    │                    │── Update user:     │
  │                    │                    │   plan = "pro"     │
  │                    │                    │   plan_expires_at  │
  │                    │                    │   = now + 360 days │
  │                    │                    │── Return 200 ─────▶│
```

---

## Webhook Handling

### Endpoint

```
POST /api/payments/webhook
```

Public endpoint (no auth cookie). Secured by HMAC verification.

### HMAC Verification

Paymob signs every webhook with HMAC-SHA512. We verify before trusting the data.

```csharp
app.MapPost("/api/payments/webhook", async (
    HttpContext ctx,
    PaymobWebhookService webhookService) =>
{
    // 1. Read raw body
    var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();

    // 2. Get HMAC from query string
    var hmac = ctx.Request.Query["hmac"].ToString();

    // 3. Verify HMAC
    if (!webhookService.VerifyHmac(body, hmac))
        return Results.StatusCode(403);

    // 4. Process payment
    var result = await webhookService.ProcessAsync(body);

    return Results.Ok();
});
```

### HMAC Calculation

```csharp
public bool VerifyHmac(string body, string receivedHmac)
{
    // Paymob HMAC key from environment variable
    var hmacSecret = Environment.GetEnvironmentVariable("PAYMOB_HMAC_SECRET")!;

    // Concatenate specific fields in Paymob's required order
    var payload = ExtractHmacPayload(body);  // ordered concatenation of specific fields

    using var hmacAlg = new HMACSHA512(Encoding.UTF8.GetBytes(hmacSecret));
    var computed = hmacAlg.ComputeHash(Encoding.UTF8.GetBytes(payload));
    var computedHex = Convert.ToHexString(computed).ToLowerInvariant();

    return CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(computedHex),
        Encoding.UTF8.GetBytes(receivedHmac));
}
```

**`FixedTimeEquals`** — constant-time comparison to prevent timing attacks.

### Processing Logic

```csharp
public async Task ProcessAsync(string body)
{
    var webhook = JsonSerializer.Deserialize<PaymobWebhook>(body);
    var transaction = webhook.Obj;

    // Extract user ID from billing data (passed during intention creation)
    var userId = Guid.Parse(transaction.BillingData.Extra["profily_user_id"]);

    switch (transaction)
    {
        // Successful payment (first or renewal)
        case { Success: true, IsRefunded: false }:
            await UpgradeUser(userId, transaction);
            break;

        // Failed renewal
        case { Success: false }:
            // Don't downgrade immediately — retrial_days will retry
            await LogFailedPayment(userId, transaction);
            break;

        // Refund
        case { IsRefunded: true }:
            await DowngradeUser(userId);
            break;
    }
}

private async Task UpgradeUser(Guid userId, PaymobTransaction tx)
{
    var user = await db.Users.FindAsync(userId);
    user.Plan = "pro";
    user.PlanExpiresAt = DateTime.UtcNow.AddDays(360);
    user.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
}

private async Task DowngradeUser(Guid userId)
{
    var user = await db.Users.FindAsync(userId);
    user.Plan = "free";
    user.PlanExpiresAt = null;
    user.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
}
```

---

## Renewal Handling

Paymob's Subscription Module handles renewals automatically:

| Event | Paymob Action | Our Action |
|---|---|---|
| 7 days before renewal | Paymob sends reminder email to user | Nothing (Paymob handles) |
| Renewal day | Paymob charges saved card | Webhook → extend `plan_expires_at` by 360 days |
| Charge fails (day 1) | Paymob retries | Nothing (wait for retry) |
| Charge fails (day 3) | Paymob retries | Nothing (wait for retry) |
| Charge fails (day 7) | Paymob retries (last) | Nothing (wait for retry) |
| All retries fail | Paymob marks subscription inactive | Webhook → we downgrade user |

### Grace Period

Between failed charge and final retry (7 days), user keeps Pro access. We don't downgrade until Paymob confirms the subscription is inactive.

### Expiration Check

Even without a webhook (belt and suspenders), the API checks `plan_expires_at` on every authenticated request:

```csharp
// In auth middleware, after loading session
if (user.Plan == "pro" && user.PlanExpiresAt < DateTime.UtcNow)
{
    user.Plan = "free";
    user.PlanExpiresAt = null;
    await db.SaveChangesAsync();
}
```

This ensures a user can't stay Pro if the webhook was missed for any reason.

---

## Feature Gating

### API-Side Enforcement

```csharp
// Middleware or extension method
public static bool IsPro(this User user)
    => user.Plan == "pro" && (user.PlanExpiresAt == null || user.PlanExpiresAt > DateTime.UtcNow);

// In endpoints
app.MapPost("/api/portfolios/{id}/export-github", async (Guid id, HttpContext ctx) =>
{
    var user = ctx.GetUser();
    if (!user.IsPro())
        return Results.Json(new { error = "Pro plan required" }, statusCode: 403);

    // ... proceed with GitHub Pages export
});
```

### Feature Gating Matrix

| Feature | Free | Pro | Gate Location |
|---|---|---|---|
| Templates | 1 (3D Purple) | All templates | API: `GET /templates` filters by `isPro`. Frontend: shows lock icon. |
| Profily hosting | ✅ | ✅ | No gate |
| GitHub Pages export | ❌ | ✅ | API: `POST /export-github` checks `IsPro()` |
| Branding removal | ❌ | ✅ | Template renderer: checks `meta.isPro` to include/exclude footer |
| Multiple portfolios | ❌ | ✅ (P1) | API: portfolio count check |
| Skills inference | ✅ | ✅ | No gate |
| Experience/Education | ✅ | ✅ | No gate |

### Frontend Gating

API returns `user.plan` in the user profile response. Frontend uses it to:
- Show/hide Pro features
- Show lock icons on Pro templates
- Show "Upgrade to Pro" CTAs

```typescript
// User profile response
{
  "plan": "free",          // or "pro"
  "planExpiresAt": null    // or "2027-03-27T00:00:00Z"
}

// Frontend check
const canExportGitHub = user.plan === 'pro';
const canUseTemplate = (t) => !t.isPro || user.plan === 'pro';
```

---

## Template Gating in Renderer

The "Built with Profily" footer is injected by the template renderer, not hardcoded in templates:

```csharp
public async Task<string> RenderAsync(string templateId, PortfolioData data)
{
    var html = await RenderSections(templateId, data);

    // Inject branding footer for free users
    if (!data.Meta.IsPro)
    {
        html = html.Replace("</body>",
            """
            <div style="text-align:center;padding:20px;opacity:0.6;font-size:12px">
                Built with <a href="https://profily.dev">Profily</a>
            </div>
            </body>
            """);
    }

    return html;
}
```

This way template authors don't need to handle branding — it's automatic.

---

## Payment Logging

Store payment events for debugging and customer support:

```sql
CREATE TABLE payment_events (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID REFERENCES users(id) ON DELETE SET NULL,
    paymob_order_id VARCHAR(100),
    transaction_id  VARCHAR(100),
    event_type      VARCHAR(50) NOT NULL,    -- payment_success | payment_failed | refund | subscription_inactive
    amount_cents    INT,
    currency        VARCHAR(10),
    raw_payload     JSONB,                   -- full webhook payload for debugging
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_payment_events_user_id ON payment_events(user_id);
```

Every webhook is logged with full raw payload before processing. If something goes wrong, we have the evidence.

---

## Security Checklist

| Risk | Mitigation |
|---|---|
| Fake webhooks | HMAC-SHA512 verification on every webhook |
| Timing attacks on HMAC | `CryptographicOperations.FixedTimeEquals` |
| User manipulates plan client-side | API enforces `IsPro()` on every gated endpoint |
| Webhook missed | `plan_expires_at` check in auth middleware (belt and suspenders) |
| Replay attacks | Log `transaction_id`, reject duplicates |
| HMAC secret exposure | Stored as environment variable, never in code/config files |

### Replay Protection

```csharp
// In ProcessAsync, before processing
var exists = await db.PaymentEvents
    .AnyAsync(e => e.TransactionId == transaction.Id.ToString());

if (exists)
    return; // Already processed, skip
```

---

## Configuration

Environment variables required:

```
PAYMOB_API_KEY=<api_key>
PAYMOB_HMAC_SECRET=<hmac_secret>
PAYMOB_SUBSCRIPTION_PLAN_ID=<plan_id>
PAYMOB_INTEGRATION_ID=<moto_integration_id>
```

---

## Future: LemonSqueezy (v2)

When adding global payments:

```
User clicks "Upgrade to Pro"
    → Detect location (IP geolocation or user profile)
    → Egypt/MENA → Paymob checkout
    → Everywhere else → LemonSqueezy checkout
    → Both send webhooks to different endpoints
    → Both update the same user.plan / user.plan_expires_at columns
```

LemonSqueezy endpoint: `POST /api/payments/lemonsqueezy-webhook`  
Same processing logic, different HMAC verification (LemonSqueezy uses signature header).

---

## Key Decisions Summary

| # | Decision | Rationale |
|---|---|---|
| 1 | Paymob Subscription Module (not manual recurring) | Automatic yearly billing, retry on failure, reminder emails — all handled by Paymob |
| 2 | Hosted checkout (redirect) | PCI-compliant out of the box. No card data touches our server. Minimal frontend work. |
| 3 | HMAC-SHA512 webhook verification | Ensures webhook authenticity. Constant-time comparison prevents timing attacks. |
| 4 | `plan` + `plan_expires_at` on users table | Simple. No separate subscription table needed for one plan. |
| 5 | Expiration check in auth middleware | Belt and suspenders — catches missed webhooks. |
| 6 | Payment events log table | Full webhook payload stored for debugging and support. |
| 7 | Replay protection via transaction_id | Prevents duplicate processing of same webhook. |
| 8 | Branding injected by renderer (not in templates) | Template authors don't handle billing logic. Automatic for free users. |
| 9 | Feature gating on API side (not frontend only) | Frontend can be bypassed. API is the source of truth for Pro status. |
