# Tracker
Pet project to demonstrate programming and architecture skills.

## Project quality

[![Build](https://github.com/poltaratskiy/Tracker/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/poltaratskiy/Tracker/actions)
[![Coverage](https://codecov.io/gh/poltaratskiy/Tracker/branch/main/graph/badge.svg)](https://codecov.io/gh/poltaratskiy/Tracker)
[![Sonar Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=poltaratskiy_Tracker&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=poltaratskiy_Tracker)

🔍 [Watch full report at SonarCloud](https://sonarcloud.io/project/overview?id=poltaratskiy_Tracker)

## Goals
The goals were to create microservice-like environment deployed in Docker compose.

## Get started

### Prerequisites
Make sure you have the following installed:
- Docker
- Docker Compose

### Setup

1. Clone the repository:
```bash
git clone
```
2. Start the application:
```bash
docker compose up
```

## Access
- tracker.react.users: http://localhost:5000/
- tracker.dotnet.users: http://localhost:5011/swagger/
- Grafana: http://localhost:3000/
- Kafka console (Redpanda): http://localhost:9090/
- SSO: http://localhost:9011/

## Users
| Login         | Role        | Password   | Notes                  |
|---------------|-------------|------------|------------------------|
| admin         | Admin       | 12345678   | Only SSO Access        |
| admin1        | Admin       | 123        |                        |
| admin2        | Admin       | 123        |                        |
| manager1      | Manager     | 123        |                        |
| manager2      | Manager     | 123        |                        |
| user1         | User        | 123        |                        |
| user2         | User        | 123        |                        |
| user3         | User        | 123        |                        |
| accountant1   | Accountant  | 123        |                        |

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

The project follows an **event-driven architecture**, where services communicate via events published to a message broker, reducing coupling and improving scalability.

## Assumptions
This pet project makes certain assumptions and simplifications to keep the setup lightweight and runnable via Docker Compose. Some of them like storing secrets in docker-compose.yml was intentionally done to be possible to run on a local machine without complex set up, using http instead of https is also intentionally done not to have any issues with certificates on a local machine. It is required to store secrets in a special storage and they must not be in a code, and it is nessesary to use https for external connections.

This project may have some overhead so I left comments across the code to explain some assumptions and decisions.

## Messaging

The project uses **Redpanda** (Kafka-compatible streaming platform) for event-driven communication.

Why Redpanda:
- fully Kafka API compatible
- simpler local setup (no Zookeeper)
- production-grade

All messaging is implemented using Confluent.Kafka client.

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
> Lost messages == producer failures (no hidden loss)

***

3. `acks=all`, `idempotency=true`

**Result:**
- Producer success = 29997
- Producer failed = 3
- Messages delivered = 29998
- Loss = 2
- p50: `00:00:00.0155881`, p95: `00:00:00.0165451`, p99: `00:00:00.0215552`

**Key observation:**
- Lost messages == producer failures (no hidden loss) but faced the problem when message successfully written in Kafka but producer did not receive an acknowledgement due a network faulure.

**Conclusion:**
> With idempotency enabled, system becomes **fully observable:**
> if producer reports success → message is delivered.

### Key insights
> The biggest improvement in reliability comes from `acks=all`,
> not from idempotency itself.
> Idempotency introduces a small overhead, mainly visible in tail latency (p95/p99), 
> but does not significantly affect average performance.
> This is likely because all replicas are running locally, so network latency is minimal.
>
> Finally faced the situation indicates a well-known distributed systems issue:
> **A message may be successfully written to Kafka, but the producer does not receive an acknowledgment due to a network failure or broker restart.**

### To handle this safely:
- retry on producer side
- use Inbox (idempotency) on consumer side

## Kafka latency testing
Latency tests measure the time it takes for a message to travel from the producer to the consumer handler.
Each message is timestamped at the moment of production, and latency is calculated when the consumer receives and begins processing the message.

> ⚠️ Latency **does not include database write time** — only delivery + handler entry.


### Setup
- 3 brokers (Redpanda)
- Replication Factor = 3
- 1000 messages per test
- 1 producer / 3 consumers
- Message timeout was set to 5 seconds

### Scenarios Tested
1. `acks=none`, `useInbox=false`

**Result:**
- p50: `00:00:00.0224200`, p95: `00:00:00.1441100`, p99: `00:00:00.7182900`
- Total time: `00:00:15.4629270`

***

2. `acks=all`, `useInbox=false`

**Result:**
- p50: `00:00:00.0221260`, p95: `00:00:00.0745040`, p99: `00:00:00.4532060`
- Total time: `00:00:23.0142000`

***

3. `acks=all`, `idempotency=true`, `useInbox=false`

**Result:**
- p50: `00:00:00.0289370`, p95: `00:00:00.0795240`, p99: `00:00:00.3920880`
- Total time: `00:00:37.3528700`

### Comparison

| Metric        | acks=none | acks=all | acks=all + idempotency |
|---------------|----------|----------|------------------------|
| Total time    | 15.46s   | 23.01s   | 37.35s                |
| p50           | 22 ms    | 22 ms    | 29 ms                 |
| p95           | 144 ms   | 74 ms    | 79 ms                 |
| p99           | 718 ms   | 453 ms   | 392 ms                |
| Reliability   | low      | high     | very high             |


## Kafka latency testing with Inbox on consumer side
The same latency tests like above but with additional check if message was processed.

### Setup
- 3 brokers (Redpanda)
- Replication Factor = 3
- 1000 messages per test
- 1 producer / 3 consumers
- Message timeout was set to 5 seconds

### Scenarios Tested
1. `acks=none`, `useInbox=true`

**Result:**
- p50: `00:00:00.0270000`, p95: `00:00:01.0961650`, p99: `00:00:01.5050700`
- Total time: `00:00:15.6419260`

***

2. `acks=all`, `useInbox=true`

**Result:**
- p50: `00:00:00.0259800`, p95: `00:00:00.0984220`, p99: `00:00:01.1118020`
- Total time: `00:00:29.0040500`

***

3. `acks=all`, `idempotency=true`, `useInbox=true`

**Result:**
- p50: `00:00:00.0620090`, p95: `00:00:00.1138780`, p99: `00:00:01.0623930`
- Total time: `00:00:39.6342060`

### Comparison

| Metric        | acks=none + inbox | acks=all + inbox | acks=all + idempotency + inbox |
|---------------|------------------:|-----------------:|--------------------------------:|
| Total time    | 15.64s            | 29.00s           | 39.63s                          |
| p50           | 27 ms             | 26 ms            | 62 ms                           |
| p95           |  1096 ms           | 98 ms            | 114 ms                         |
| p99           |  1505 ms           | 1111 ms           | 1062 ms                         |


### Key insights
> Inbox does not significantly affect average latency or throughput,
> but it increases tail latency due to additional persistence operations.
> 
> The actual impact depends heavily on database performance and contention.
