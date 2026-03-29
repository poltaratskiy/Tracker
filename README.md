# Tracker
Pet project to demonstrate programming and architecture skills.

## Project quality

[![Build](https://github.com/poltaratskiy/Tracker/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/poltaratskiy/Tracker/actions)
[![Coverage](https://codecov.io/gh/poltaratskiy/Tracker/branch/main/graph/badge.svg)](https://codecov.io/gh/poltaratskiy/Tracker)
[![Sonar Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=poltaratskiy_Tracker&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=poltaratskiy_Tracker)

🔍 [Watch full report at SonarCloud](https://sonarcloud.io/project/overview?id=poltaratskiy_Tracker)

## Goals
The goals were to create microservice-like environment deployed in Docker compose.

## Developed
- Logging, using Promtail, Loki, Grafana instead of ELK
- Tracing request Id
- Configured SSO
- Set up authentication and authorization for backend services
- Core libraries for logging, messaging via Kafka, exception handling, authorization
- Backend service framework
- Frontend service using authentication via SSO

## Approaches
I used the Clean Architecture + CQRS approach and layered architecture because it allows to add features without strong modification of existing code and it increases readability and maintainability.

## Assumptions
This pet project makes certain assumptions and simplifications to keep the setup lightweight and runnable via Docker Compose. Some of them like storing secrets in docker-compose.yml was intentionally done to be possible to run on a local machine without complex set up, using http instead of https is also intentionally done not to have any issues with certificates on a local machine. It is required to store secrets in a special storage and they must not be in a code, and it is nessesary to use https for external connections.

This project may have some overhead so I left comments across the code to explain some assumptions and decisions.

# Kafka stress tests
It was interesting for me to make stress test of Kafka and understand in practice how each combination of parameters affect on latency and reliablity.

## Kafka throughput testing
### Setup
- 3 brokers (Redpanda)
- Replication Factor = 3
- 30,000 messages per test
- 3 producers / 3 consumers
- Message timeout was set to 5 seconds
- Simulated failure: one broker down for ~10 seconds

### Scenarios Tested
1. `acks=none`

**Result:**
- Producer success = 30000
- Messages delivered < 30000
- Loss observed: 4–11 messages
- Producer failures: sometimes 0–2
- p50: `00:00:00.0155541`, p95: `00:00:00.0167236`, p99: `00:00:00.0183125`

**Key observation:**
- Messages can be lost without producer knowing

**Conclusion:**
> acks=none leads to silent data loss. Producer metrics do not reflect actual delivery.

***

2. `acks=all`, `idempotency=false`

**Result:**
- Producer success = 30000
- Messages delivered = 30000
- Loss: 0
- p50: `00:00:00.0155374`, p95: `00:00:00.0167078`, p99: `00:00:00.0200694`

**Key observation:**
- No delivery failures

**Conclusion:**
> `acks=all` ensures no message loss under single broker failure.
> Kafka cluster successfully re-elects leaders and continues processing.

***

3. `acks=all`, `idempotency=true`

**Result:**
- Producer success = 29999
- Producer failed = 1
- Messages delivered = 29999
- Loss = 0
- p50: `00:00:00.0155881`, p95: `00:00:00.0205451`, p99: `00:00:00.0355552`

**Key observation:**
- Lost messages == producer failures (no hidden loss)

**Conclusion:**
> With idempotency enabled, system becomes **fully observable:**
> if producer reports success → message is delivered.

### Key insights
> The biggest improvement in reliability comes from `acks=all`,
> not from idempotency itself.
> Idempotency introduces a small overhead, mainly visible in tail latency (p95/p99), 
> but does not significantly affect average performance.
> This is likely because all replicas are running locally, so network latency is minimal.
