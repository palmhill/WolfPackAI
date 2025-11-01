-- Initialize databases for WolfPackAI services

-- Create databases if they don't exist
SELECT 'CREATE DATABASE openwebuidb' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'openwebuidb')\gexec
SELECT 'CREATE DATABASE litellmdb' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'litellmdb')\gexec
SELECT 'CREATE DATABASE n8ndb' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'n8ndb')\gexec

-- Grant privileges (optional, user already has superuser access)
GRANT ALL PRIVILEGES ON DATABASE openwebuidb TO postgres;
GRANT ALL PRIVILEGES ON DATABASE litellmdb TO postgres;
GRANT ALL PRIVILEGES ON DATABASE n8ndb TO postgres;
