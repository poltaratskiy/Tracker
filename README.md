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

I left comments across the code to explain some assumptions and decisions.
