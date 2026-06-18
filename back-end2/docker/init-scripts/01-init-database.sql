-- PostgreSQL Initialization Script for Trap-Intel
-- This script runs automatically when the PostgreSQL container starts for the first time

-- Create the trapintel schema if it doesn't exist
CREATE SCHEMA IF NOT EXISTS trapintel;

-- Grant all privileges on the schema to the application user
GRANT ALL PRIVILEGES ON SCHEMA trapintel TO trapintel_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA trapintel TO trapintel_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA trapintel TO trapintel_user;

-- Set default privileges for future tables
ALTER DEFAULT PRIVILEGES IN SCHEMA trapintel 
GRANT ALL PRIVILEGES ON TABLES TO trapintel_user;

ALTER DEFAULT PRIVILEGES IN SCHEMA trapintel 
GRANT ALL PRIVILEGES ON SEQUENCES TO trapintel_user;

-- Enable useful extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";    -- For UUID generation
CREATE EXTENSION IF NOT EXISTS "pg_trgm";      -- For text search optimization
CREATE EXTENSION IF NOT EXISTS "btree_gist";   -- For exclusion constraints

-- Log successful initialization
DO $$
BEGIN
    RAISE NOTICE 'Trap-Intel database initialized successfully!';
    RAISE NOTICE 'Schema: trapintel';
    RAISE NOTICE 'Extensions enabled: uuid-ossp, pg_trgm, btree_gist';
END $$;
