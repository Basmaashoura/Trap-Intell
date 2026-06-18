-- Deprecated SQL seeding script.
--
-- Seed data is now handled by EF Core startup pipeline in Trap-Intel.Api:
-- 1) Database migrations are applied
-- 2) DatabaseSeederOrchestrator runs all registered seeders
--
-- This file is intentionally kept as a no-op for backward compatibility.
DO $$
BEGIN
    RAISE NOTICE 'Skipping SQL seed script: EF Core orchestrated seeders are enabled.';
END $$;
