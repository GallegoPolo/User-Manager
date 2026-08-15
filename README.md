# User Manager

Painel administrativo full-stack para gestão de usuários e auditoria de ações.

Projeto pessoal focado em arquitetura de microsserviços na prática: autenticação JWT, CRUD de usuários, Domain Events, padrão Outbox, mensageria com RabbitMQ e trilha de auditoria em MongoDB.

## Visão geral

| Serviço | Porta | Responsabilidade |
|---------|-------|------------------|
| **AuthService** | `5149` | Login de admin, JWT, API Keys, hash Argon2id, cache Redis |
| **UserManager** | `5255` | CRUD de usuários, Domain Events, Outbox, publicação no RabbitMQ |
| **AuditService API** | `7175` | Consulta dos logs de auditoria (MongoDB) |
| **Audit Worker** | — | Consome eventos do RabbitMQ e grava no MongoDB |
| **Frontend** | `5173` | React + Vite (login, usuários, auditoria) |

> Admin (AuthService) ≠ User (UserManager). O admin autentica no Auth; os usuários de negócio vivem no UserManager.

## Arquitetura

```text
Frontend (React)
    |  /api/auth  -> AuthService
    |  /api/users -> UserManager
    |  /api/Audit -> AuditService
    v
AuthService ── JWT ──> UserManager
                 |
                 | Domain Events + Outbox (mesma transação)
                 v
              RabbitMQ
                 |
                 v
           Audit Worker ──> MongoDB
                 ^
                 |
           AuditService API (leitura)
```

Fluxo resumido do Create/Update/Delete de usuário:

1. UserManager persiste o usuário no PostgreSQL  
2. Dispara Domain Event (MediatR)  
3. Handler de auditoria grava mensagem na tabela **Outbox** na mesma transação  
4. Background service lê o Outbox e publica no **RabbitMQ**  
5. **Audit Worker** consome e salva o log no **MongoDB**  
6. Frontend consulta a trilha pela **AuditService API**

## Stack

**Backend**
- .NET 10 / C#
- ASP.NET Core
- EF Core + PostgreSQL
- MediatR
- FluentValidation
- JWT + Argon2id
- Redis
- RabbitMQ
- MongoDB
- Polly

**Frontend**
- React 19
- TypeScript
- Vite
- React Router
- TanStack Query

**Infra local**
- Docker Compose (PostgreSQL, Redis, RabbitMQ, MongoDB)

## Pré-requisitos

- .NET 10 SDK
- Node.js 20+
- Docker Desktop
- npm

## Como rodar

### 1. Subir a infraestrutura

Na raiz do repositório:

```bash
docker compose up -d
```

Sobe:
- PostgreSQL (`5432`) — databases `AuthService_Dev` e `UserManager_Dev`
- Redis (`6379`)
- RabbitMQ (`5672` / painel `15672`)
- MongoDB (`27017`)

Credenciais padrão do Compose:
- Postgres: `postgres` / `postgres123`
- Redis: sem senha
- RabbitMQ: `dev` / `dev123`
- MongoDB: `dev` / `dev123`

### 2. Migrations

```bash
npm run db:migrate
```

### 3. Backend + Frontend

Na raiz:

```bash
npm install
npm run dev
```

Isso:
1. Faz build dos serviços .NET  
2. Sobe Auth, UserManager, Audit API, Audit Worker e o frontend juntos  

Frontend: http://localhost:5173

### Login padrão (bootstrap)

- **E-mail:** `admin@authservice.com`  
- **Senha:** `Admin@123456`

## Frontend

Organização feature-first:

- `features/auth` — login, JWT em memória, rotas protegidas  
- `features/users` — CRUD de usuários  
- `features/audit` — listagem da trilha de auditoria  
- `shared` — layout, sidebar, api client  

Proxy do Vite:
- `/api/auth` → AuthService (`5149`)
- `/api/users` → UserManager (`5255`)
- `/api/Audit` → AuditService (`7175`)

## Backend (pontos importantes)

### AuthService
- Login de administrador
- Emissão de JWT
- Hash de senha com **Argon2id** + salt
- API Keys (com cache no Redis)
- Bootstrap automático do admin inicial

### UserManager
- CRUD de usuários
- Clean Architecture (Domain / Application / Infrastructure / Api)
- Domain Events via MediatR
- **Outbox pattern** para publicação confiável no RabbitMQ
- Validação com FluentValidation

### AuditService
- Worker consome eventos de criação/atualização/exclusão
- Persistência em MongoDB
- API para consulta dos logs

## Swagger

Com os serviços no ar:
- Auth: http://localhost:5149/swagger  
- Users: http://localhost:5255/swagger  
- Audit: http://localhost:7175/swagger  

## RabbitMQ Management

http://localhost:15672  
Usuário: `dev` / Senha: `dev123`

## Screenshots

> Substitua pelos prints reais quando tiver.

### Login
![Login](docs/screenshots/login.png)

### Usuários
![Users](docs/screenshots/users.png)

### Auditoria
![Audit](docs/screenshots/audit.png)

## Estrutura do repositório

```text
User-Manager/
├── docker-compose.yml
├── docker/
├── frontend/
├── Services/
│   ├── AuthService/
│   ├── UserManager/
│   └── AuditService/
└── package.json
```

## Roadmap (próximos passos)

- [ ] README com diagramas e screenshots finais
- [ ] JWT obrigatório de ponta a ponta no UserManager (se ainda houver gap)
- [ ] Pepper no Argon2
- [ ] Logout + blacklist de JWT no Redis
- [ ] Polimento visual do frontend
- [ ] API Gateway (YARP)

## Autor

**João Mauricio Gallego Polo**  
GitHub: [joaogallego](https://github.com/joaogallego)  
LinkedIn: [joaogallego](https://linkedin.com/in/joaogallego)
