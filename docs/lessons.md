# What I learned

Notes from building and breaking this cluster. Short on purpose.

## Setting up the cluster

- Starting the nodes is not enough. They wait until `cockroach init` is run **once** against any node. Before that, the console returns `404` and `/health?ready=1` is not ready. This is on purpose: no node is allowed to create a cluster on its own, otherwise three nodes starting at once could create three clusters.
- Each node uses three ports: **26357** for node-to-node traffic (Raft, gossip), **26257** for SQL clients, **8080** for the console and health checks. Only SQL goes through HAProxy.
- `--listen-addr` is where a node binds. `--advertise-addr` is the address it tells the others. In Docker it must be the container name (`roach1:26357`), never `localhost`.
- `--join` is just a list of nodes to contact first. After that, gossip finds everyone else.
- Node IDs are given in the order nodes join, not by container name. In my cluster `roach3` became `n2` and `roach2` became `n3`. Check with:
  ```sql
  SELECT node_id, address, is_live FROM crdb_internal.gossip_nodes ORDER BY node_id;
  ```
- A multi-node cluster needs a license key after 7 days, otherwise it is throttled to 5 concurrent transactions.

## Ranges and replication

- All data is one big sorted key space (`/Table/<id>/<index>/<primary key>`), cut into **ranges**.
- Every range has **3 replicas**. With 3 nodes, every node holds a copy of every range.
- One replica per range is the **leaseholder**: it serves reads and coordinates writes. The node you connect to (the gateway) forwards the request there, so the app never needs to know where data lives.
- With 3 nodes and 3 replicas, more ranges doesn't spread the data (each node already has everything). It spreads the **work**, because leaseholders end up on different nodes.
- Small tables share a range. My three tables all lived in range 96. The range ID is just a counter.
- A range splits when it grows past `range_max_bytes` (512 MiB by default), when it gets too much load, when part of it needs different settings, or when you run `ALTER TABLE ... SPLIT AT`. Small neighbouring ranges are merged back automatically (I watched range 96's end key change to `/Max`).
- Range size and replica count are **cluster settings stored in the database** (`SHOW ZONE CONFIGURATION ...`), changed with SQL. They have nothing to do with `docker-compose.yml`, which only configures each node's process.
- Useful queries:
  ```sql
  SHOW RANGE FROM TABLE customers FOR ROW ('<uuid>'::UUID);
  SHOW RANGES FROM TABLE customers WITH DETAILS;
  ```

## Reading the console

- "Ranges" for a table = how many ranges its keys overlap, not how many it owns. An empty table can show 2.
- "Replication Size" is a rough disk estimate across all copies. For tiny tables it's meaningless. "Live data" is the real size of the rows.
- Updates and deletes keep old versions for a while (MVCC, `gc.ttlseconds`, 4 hours by default), so "total" can be bigger than "live".

## Majority rule and failover

- **CockroachDB only accepts a read or write when a majority of the copies agree. With 3 copies, the majority is 2.**
- One node down: everything keeps working. The console shows all ranges as under-replicated, but 0 unavailable.
- Two nodes down: everything stops. The last node can't renew its own liveness, answers `503` on `/health?ready=1`, and HAProxy takes it out too. This is correct: if it kept going alone, a network split could produce two different versions of the data.
- A node missing for 5 minutes is marked **dead**. With only 3 nodes there's nowhere to rebuild the missing copies, so ranges stay under-replicated until it comes back.
- 3 nodes survive 1 failure, 5 survive 2, 7 survive 3.

## HAProxy

- It balances **TCP connections, not queries**. Every query on a connection goes to the same node.
- Health check: `GET /health?ready=1` on port 8080.
- By default HAProxy resolves hostnames once at startup. After the containers restarted they got different IPs, and HAProxy marked the wrong node as down. Fix: a `resolvers` section pointing at Docker's DNS (`127.0.0.11`).
- Because of connection pooling, `GET /db/node` returns the same node almost every time. With `Pooling=false` you can watch the round-robin (1, 2, 3). In production, set `Connection Lifetime` so pooled connections are recycled and a node that comes back gets traffic again.

## EF Core and Npgsql

- There's no official CockroachDB provider. Npgsql works because CockroachDB speaks the PostgreSQL protocol, but it isn't PostgreSQL inside.
- `dotnet ef database update` failed twice:
  1. `LOCK TABLE ... IN ACCESS EXCLUSIVE MODE` isn't supported. Fixed with a custom history repository.
  2. "This NpgsqlTransaction has completed". CockroachDB's `autocommit_before_ddl` (on by default) commits the open transaction before any schema change. Fixed by running migration statements without a transaction.
- Use random UUIDs from the database (`gen_random_uuid()`). Npgsql generates time-ordered UUIDv7 by default, which sends every insert to the same range (a hotspot).

## Tools

- pgAdmin only lists databases with an ID above 16383 (a PostgreSQL convention). CockroachDB IDs are small (`bank` was 102), so the database is hidden. The Query Tool still works. DataGrip and DBeaver have a proper CockroachDB connection type.
- `docker exec` into a container gives you a shell, not SQL. Use `./cockroach sql --insecure --host=...`.

## What would change in production

- Real TLS certificates and SQL users with passwords instead of `--insecure` and `root`.
- One node per machine, spread across availability zones. Three containers on one laptop share one disk, one network and one power supply, so this setup has no real hardware-level high availability.
- Persistent disks sized and monitored per node, and regular backups (`BACKUP` to cloud storage).
- 5 nodes (and replication factor 5) if two simultaneous failures must be survived.
- Retry logic for serialization errors (`40001`), which CockroachDB can return under concurrency.
