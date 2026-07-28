# Before reading the source code

This short note is for reviewers. It explains **why** the solution looks the way it does before you dive into the code.

## Assignment goals I optimized for

The brief asked me to:

- **Prioritize architecture** and apply **best code practices**
- Keep **code and software architecture** as the main focus
- Feel free to extend the project to showcase skills -- **without over-engineering**

I treated those goals as the primary success criteria, not “smallest possible demo that passes the happy path.”

## What I tried to demonstrate

I aimed to show the craft of building a **nearly real-world microservice**: Clean Architecture boundaries, a DDD-inspired domain model, the Result pattern instead of exception-driven control flow, Postgres for durable config, Redis for the hot path, a dedicated Worker for match formation, and solid unit plus Testcontainers-backed integration tests.

The point was not to invent complexity for its own sake, but to simulate how a production-shaped matchmaking service is structured when architecture and maintainability matter.

## Why reading everything takes time

Because of that choice, walking the **entire** codebase takes longer than a minimal sample. That cost is intentional: depth in domain rules, ports and adapters, Redis atomicity, and tests is where I tried to do my best work.

## Experience and honesty

This submission reflects roughly **15 years** of professional coding. There is always room to grow - I am not claiming perfection - but I put real effort into making the architecture, practices, and readability of decisions as strong as I could within a take-home scope. Of course some of the exceellent features like **observability** and **security** are out of scope.

## Small extensions, not scope creep

I added a few **small, deliberate extensions** so the system feels closer to **production-ready** (clear failure model, projection/bootstrap paths, documentation, and test coverage) rather than a throwaway prototype. I tried to stop short of over-engineering.

## Scale without changing application code

One document walks through how this design can grow toward on the order of **~10 million concurrent players** through partitioning, replicas, and ops - **without rewriting the application line by line**:

- [Scaling to ~10M players](./scaling-10m-players.md)
- [kubernetes deployment for ~10M players](./kubernetes-deployment.md)

## Suggested reading order

1. **This note** - intent and expectations
2. [Root README](../README.md) - architecture, decisions, API overview
3. [Scaling to ~10M players](./scaling-10m-players.md) - how the system grows
4. Layer READMEs under `src/` - Domain → Application → Infrastructure → API → Worker
4. Meanwhile if you're interested in [unit testing philosophy of this project](./prompts/coding_standards/unit-testing-standards.md)
4. How [integration tests were written](./prompts/coding_standards/integration-testing-standards.md)

Thank you for the time you spend reviewing it.
