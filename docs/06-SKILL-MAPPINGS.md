# Profily — Skill Inference Mappings

## Overview

The skill inference engine analyzes GitHub data in 3 stages:
1. **Languages API** — bytes of code per language per repo
2. **Config File Parsing** — dependency files reveal frameworks/tools
3. **Repo Metadata** — topics, forks, recency, stars for confidence weighting

---

## Stage 1: Language → Skill Mapping

Direct mapping from GitHub Languages API. Always applied.

| GitHub Language | Skill Name | Category |
|---|---|---|
| C# | C# | backend |
| Python | Python | backend |
| JavaScript | JavaScript | frontend |
| TypeScript | TypeScript | frontend |
| Java | Java | backend |
| Go | Go | backend |
| Rust | Rust | backend |
| C++ | C++ | backend |
| C | C | backend |
| Ruby | Ruby | backend |
| PHP | PHP | backend |
| Swift | Swift | mobile |
| Kotlin | Kotlin | mobile |
| Dart | Dart | mobile |
| HTML | HTML | frontend |
| CSS | CSS | frontend |
| SCSS | SASS/SCSS | frontend |
| Shell | Shell/Bash | devops |
| PowerShell | PowerShell | devops |
| HCL | Terraform | devops |
| Dockerfile | Docker | devops |
| SQL | SQL | database |

---

## Stage 2: Config File → Skill Mapping

### `.csproj` / `.fsproj` (NuGet Packages)

Parse `<PackageReference Include="..." />` elements.

| Package Pattern | Skill Name | Category |
|---|---|---|
| `Microsoft.AspNetCore.*` | ASP.NET Core | backend |
| `Microsoft.EntityFrameworkCore*` | Entity Framework Core | backend |
| `Npgsql.EntityFrameworkCore*` | PostgreSQL | database |
| `Microsoft.EntityFrameworkCore.SqlServer` | SQL Server | database |
| `MongoDB.Driver` | MongoDB | database |
| `StackExchange.Redis` | Redis | database |
| `MediatR` | MediatR / CQRS | backend |
| `FluentValidation*` | FluentValidation | backend |
| `AutoMapper*` | AutoMapper | backend |
| `Swashbuckle*` | Swagger / OpenAPI | backend |
| `NSwag*` | Swagger / OpenAPI | backend |
| `xunit*` | xUnit Testing | tools |
| `NUnit*` | NUnit Testing | tools |
| `MSTest*` | MSTest Testing | tools |
| `Moq` | Unit Testing | tools |
| `Serilog*` | Serilog Logging | backend |
| `Microsoft.Azure.*` | Azure | devops |
| `Azure.*` | Azure | devops |
| `AWSSDK.*` | AWS | devops |
| `Hangfire*` | Hangfire | backend |
| `SignalR` | SignalR / Real-time | backend |
| `Dapper` | Dapper | backend |
| `Polly` | Polly / Resilience | backend |
| `MassTransit*` | Message Queue | backend |
| `RabbitMQ.Client` | RabbitMQ | backend |
| `Microsoft.ML*` | ML.NET | ai |
| `Microsoft.SemanticKernel*` | Semantic Kernel / AI | ai |

### `package.json` (npm dependencies)

Parse `dependencies` and `devDependencies`.

| Package Pattern | Skill Name | Category |
|---|---|---|
| `react` | React | frontend |
| `react-dom` | React | frontend |
| `next` | Next.js | frontend |
| `vue` | Vue.js | frontend |
| `nuxt` | Nuxt.js | frontend |
| `@angular/core` | Angular | frontend |
| `svelte` | Svelte | frontend |
| `express` | Express.js | backend |
| `fastify` | Fastify | backend |
| `nestjs*` / `@nestjs/*` | NestJS | backend |
| `tailwindcss` | Tailwind CSS | frontend |
| `sass` / `node-sass` | SASS/SCSS | frontend |
| `styled-components` | Styled Components | frontend |
| `three` | Three.js | frontend |
| `gsap` | GSAP Animation | frontend |
| `d3` | D3.js | frontend |
| `mongoose` | MongoDB | database |
| `prisma` / `@prisma/client` | Prisma ORM | backend |
| `sequelize` | Sequelize ORM | backend |
| `typeorm` | TypeORM | backend |
| `redis` / `ioredis` | Redis | database |
| `jest` | Jest Testing | tools |
| `vitest` | Vitest Testing | tools |
| `cypress` | Cypress E2E | tools |
| `playwright` | Playwright E2E | tools |
| `webpack` | Webpack | tools |
| `vite` | Vite | tools |
| `eslint` | ESLint | tools |
| `prettier` | Prettier | tools |
| `graphql` | GraphQL | backend |
| `@apollo/server` | Apollo GraphQL | backend |
| `socket.io` | WebSockets | backend |
| `openai` | OpenAI API | ai |
| `langchain` | LangChain | ai |

### `requirements.txt` / `pyproject.toml` / `Pipfile`

| Package Pattern | Skill Name | Category |
|---|---|---|
| `flask` | Flask | backend |
| `django` | Django | backend |
| `fastapi` | FastAPI | backend |
| `uvicorn` | FastAPI | backend |
| `sqlalchemy` | SQLAlchemy | backend |
| `celery` | Celery / Task Queue | backend |
| `redis` | Redis | database |
| `psycopg2*` | PostgreSQL | database |
| `pymongo` | MongoDB | database |
| `pytest` | Pytest | tools |
| `numpy` | NumPy | ai |
| `pandas` | Pandas / Data Science | ai |
| `scikit-learn` | Scikit-Learn / ML | ai |
| `tensorflow` | TensorFlow | ai |
| `torch` / `pytorch` | PyTorch | ai |
| `keras` | Keras | ai |
| `transformers` | Hugging Face / NLP | ai |
| `langchain*` | LangChain | ai |
| `openai` | OpenAI API | ai |
| `streamlit` | Streamlit | frontend |
| `matplotlib` | Data Visualization | ai |
| `selenium` | Selenium / Automation | tools |
| `beautifulsoup4` | Web Scraping | tools |
| `requests` | HTTP Client | backend |
| `pydantic` | Pydantic | backend |

### `go.mod`

| Module Pattern | Skill Name | Category |
|---|---|---|
| `github.com/gin-gonic/gin` | Gin Framework | backend |
| `github.com/gorilla/mux` | Gorilla Mux | backend |
| `gorm.io/gorm` | GORM ORM | backend |
| `github.com/go-redis/redis` | Redis | database |

### Files (Existence Check)

| File / Pattern | Skill Name | Category |
|---|---|---|
| `Dockerfile` | Docker | devops |
| `docker-compose.yml` / `docker-compose.yaml` | Docker Compose | devops |
| `.github/workflows/*.yml` | GitHub Actions | devops |
| `azure-pipelines.yml` | Azure DevOps | devops |
| `.gitlab-ci.yml` | GitLab CI | devops |
| `Jenkinsfile` | Jenkins | devops |
| `*.tf` | Terraform | devops |
| `kubernetes/*.yml` / `k8s/*.yml` | Kubernetes | devops |
| `nginx.conf` | Nginx | devops |
| `.eslintrc*` | ESLint | tools |
| `.prettierrc*` | Prettier | tools |
| `jest.config.*` | Jest Testing | tools |
| `playwright.config.*` | Playwright | tools |
| `Makefile` | Make / Build Tools | tools |
| `.env.example` | Environment Config | tools |

---

## Stage 3: Repo Metadata

### Topic → Skill Mapping (Supplementary)

| GitHub Repo Topic | Skill Name | Category |
|---|---|---|
| `machine-learning` / `ml` | Machine Learning | ai |
| `deep-learning` | Deep Learning | ai |
| `artificial-intelligence` / `ai` | AI | ai |
| `nlp` / `natural-language-processing` | NLP | ai |
| `computer-vision` | Computer Vision | ai |
| `rest-api` / `api` | REST APIs | backend |
| `graphql` | GraphQL | backend |
| `microservices` | Microservices | backend |
| `clean-architecture` | Clean Architecture | backend |
| `ddd` / `domain-driven-design` | DDD | backend |
| `cqrs` | CQRS | backend |
| `docker` | Docker | devops |
| `kubernetes` / `k8s` | Kubernetes | devops |
| `devops` / `ci-cd` | CI/CD | devops |
| `serverless` | Serverless | devops |
| `blockchain` / `web3` | Blockchain | backend |
| `react-native` | React Native | mobile |
| `flutter` | Flutter | mobile |
| `ios` | iOS Development | mobile |
| `android` | Android Development | mobile |

---

## Confidence Scoring Algorithm

```
For each detected skill:

  baseConfidence = 0.5

  + repoCountBonus:
      1 repo:    +0.05
      2-3 repos: +0.10
      4-6 repos: +0.20
      7+ repos:  +0.30

  + bytesBonus (for language skills):
      top 1 language: +0.15
      top 2-3:        +0.10
      top 4-6:        +0.05
      below top 6:    +0.00

  + recencyBonus:
      used in last 30 days:  +0.10
      used in last 90 days:  +0.05
      used in last 365 days: +0.02
      older:                 +0.00

  + starsBonus:
      any repo with 10+ stars using this: +0.05
      any repo with 50+ stars:            +0.10

  - forkPenalty:
      all repos using this are forks: -0.15
      some repos are forks:           -0.05

  confidence = clamp(baseConfidence + bonuses - penalties, 0.3, 0.99)
```

**Final confidence is displayed as percentage in the portfolio (e.g., 0.92 → 92%).**

---

## Skill Categories

| Category | Display Name | Example Skills |
|---|---|---|
| `frontend` | Frontend | React, Vue, Angular, CSS, Tailwind, Three.js |
| `backend` | Backend | C#, Python, Node.js, ASP.NET, Django, Express |
| `database` | Database | PostgreSQL, SQL Server, MongoDB, Redis |
| `devops` | DevOps & Cloud | Docker, Kubernetes, Azure, AWS, GitHub Actions |
| `ai` | AI & Data | TensorFlow, PyTorch, LangChain, Pandas |
| `mobile` | Mobile | React Native, Flutter, Swift, Kotlin |
| `tools` | Tools | Git, VS Code, Figma, Jest, Playwright |

---

## Abbreviations for Template Display

Used in skill cards/badges where space is limited:

| Skill | Abbreviation |
|---|---|
| JavaScript | JS |
| TypeScript | TS |
| C# / .NET | C# |
| Python | Py |
| ASP.NET Core | .NET |
| React | Re |
| Next.js | Nx |
| Node.js | No |
| PostgreSQL | PG |
| SQL Server | SQ |
| Docker | Dk |
| Kubernetes | K8 |
| Azure | Az |
| REST APIs | API |
| Three.js | 3D |
| Machine Learning | ML |
| GitHub Actions | CI |
