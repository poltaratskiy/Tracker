#!/bin/sh
set -e
pg_restore --username "$POSTGRES_USER" \
  --dbname "fusionauth" \
  --no-owner --no-privileges --clean --if-exists \
  /docker-entrypoint-initdb.d/fusionauth.dump