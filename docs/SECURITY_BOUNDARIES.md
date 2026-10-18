# Security boundaries

Virtual credits only. No deposits, cashouts, payment providers, crypto, KYC or operator integration. No certification or regulated-money readiness is claimed.

Server owns RNG, feature mode, validation, settlement and wallet. Clients send supported stake/omission and idempotency UUID only; unsupported properties are rejected. CryptoRandomSource is the only production registration. Deterministic replay is CLI/test-only, never an HTTP outcome-control route.

Secrets have 256 random bits, SHA-256 hash storage and constant-time comparison. Cookie carries ID+secret, is HttpOnly/Strict, Secure when HTTPS. Local HTTP is intentional. LocalStorage holds only sound/turbo preferences and a pending command, not wallet authority or the cookie secret.

JSON/custom-header POSTs, strict Origin authority/scheme comparison and cross-site Fetch Metadata rejection form the CSRF boundary. No wildcard CORS. Proxies preserve full public Host including port; Vite changeOrigin=false is intentional. Nginx adds CSP, nosniff, referrer policy and frame-ancestor denial. Public TLS deployment needs explicit trusted forwarding/HTTPS cookie configuration; Compose binds loopback only.

Database row locks serialize across processes; durable unique idempotency mappings prevent duplicate settlement. Checked integers and a nonnegative-balance constraint protect accounting. Work/cancellation/arithmetic failures roll back. Tests cover conflicts/retries/concurrency, cross-session replay access, work exhaustion and real API restart.

The local helper creates a separate password-protected PostgreSQL cluster on 55433, bound to 127.0.0.1. Credentials/data stay in ignored `.local/`. `.env`, research clones and local data are excluded from Git/Docker context. Integration fixtures create/drop random test databases; use a dedicated role with CREATEDB, never production/shared data.

Deployment limitations: no account recovery, distributed rate limiting, retention worker, audit signing, remote observability, production secret manager or regulatory RNG review. These are explicit future tasks, not implied capabilities. Docker execution status is recorded separately.
