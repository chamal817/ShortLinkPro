## Story 1.5: Implement sharded/partitioned PostgreSQL schema

As a developer,  
I want the PostgreSQL schema for links (and click counts) to be sharded or partitioned by short_code so that the database can scale horizontally to handle 10M+ redirects per day.

### Acceptance Criteria

- **Given** the architecture specifies a sharded/partitioned PostgreSQL backend keyed by short_code  
  **When** I design and apply the database schema  
  **Then** the links data is stored in a PostgreSQL schema that uses short_code as the sharding/partitioning key (e.g. hash or range partitioning)  
  **And** a primary key or unique constraint exists on short_code across all partitions/shards  
  **And** application queries that resolve a single short code continue to work without changing the API contract (sharding is transparent to callers)  
  **And** migration/deployment steps for initializing and evolving the sharded/partitioned schema are documented

### Implementation Plan

- **Partitioning strategy**
  - Use PostgreSQL native partitioning on table `links`:
    - Parent table `links`.
    - Partitions `links_p0`, `links_p1`, … using `hash` partitioning on `short_code`.
  - Ensure index/constraint on `short_code` across all partitions.
- **Schema definition**
  - Define parent table with `short_code` as partition key.
  - Create N hash partitions (e.g. 8) to distribute load.
- **EF Core integration**
  - Keep EF Core mapping targeting the parent `links` table.
  - Application code should not be aware of partitions.
- **Migrations and deployment**
  - Create SQL-based migration that:
    - Creates parent and child tables.
    - Adds constraints and indexes.
  - Document how to apply migrations in each environment.

### Sample Implementation Code (SQL for partitioned table)

```sql
CREATE TABLE IF NOT EXISTS links (
    id uuid NOT NULL,
    short_code text NOT NULL,
    long_url text NOT NULL,
    created_at timestamptz NOT NULL,
    CONSTRAINT pk_links PRIMARY KEY (id)
) PARTITION BY HASH (short_code);

-- Example: 4 hash partitions
CREATE TABLE IF NOT EXISTS links_p0 PARTITION OF links
    FOR VALUES WITH (MODULUS 4, REMAINDER 0);

CREATE TABLE IF NOT EXISTS links_p1 PARTITION OF links
    FOR VALUES WITH (MODULUS 4, REMAINDER 1);

CREATE TABLE IF NOT EXISTS links_p2 PARTITION OF links
    FOR VALUES WITH (MODULUS 4, REMAINDER 2);

CREATE TABLE IF NOT EXISTS links_p3 PARTITION OF links
    FOR VALUES WITH (MODULUS 4, REMAINDER 3);

CREATE UNIQUE INDEX IF NOT EXISTS ux_links_short_code ON links (short_code);
```

