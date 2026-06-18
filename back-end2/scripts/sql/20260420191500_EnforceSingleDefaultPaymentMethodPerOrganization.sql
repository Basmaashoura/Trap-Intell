START TRANSACTION;

WITH ranked_defaults AS (
    SELECT id,
           ROW_NUMBER() OVER (
               PARTITION BY organization_id
               ORDER BY updated_at DESC, created_at DESC, id DESC
           ) AS rn
    FROM trapintel.payment_methods
    WHERE is_default = true
)
UPDATE trapintel.payment_methods AS pm
SET is_default = false,
    updated_at = NOW()
FROM ranked_defaults AS rd
WHERE pm.id = rd.id
  AND rd.rn > 1;

DROP INDEX IF EXISTS trapintel.ix_payment_methods_org_default;
CREATE UNIQUE INDEX IF NOT EXISTS ux_payment_methods_org_default
    ON trapintel.payment_methods (organization_id)
    WHERE is_default = true;

INSERT INTO trapintel.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20260420191500_EnforceSingleDefaultPaymentMethodPerOrganization', '9.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;
