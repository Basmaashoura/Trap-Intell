START TRANSACTION;
CREATE UNIQUE INDEX ux_invoices_subscription_billing_period ON trapintel.invoices (subscription_id, billing_period_start, billing_period_end);

INSERT INTO trapintel.__ef_migrations_history ("MigrationId", "ProductVersion")
VALUES ('20260420124500_AddUniqueInvoicePeriodPerSubscription', '9.0.0');

COMMIT;

