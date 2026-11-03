# User Manager

Painel administrativo full-stack para gestão de usuários e auditoria de ações.

Projeto pessoal focado em arquitetura de microsserviços na prática: autenticação JWT, CRUD de usuários, Domain Events, padrão Outbox, mensageria com RabbitMQ e trilha de auditoria em MongoDB.

## Visão geral

| Serviço | Porta | Responsabilidade |
|---------|-------|------------------|
| **Keycloak** | `8080` | Identidade: login, cadastro, confirmação de e-mail, papéis (`viewer`, `operator`) |
| **BFF** | `5080` | Login OIDC no Keycloak, sessão no Redis, cookie `HttpOnly`, repassa `/api/*` com o token |
| **UserManager** | `5255` | Ficha da pessoa (JIT), agenda, Domain Events, Outbox, publicação no RabbitMQ |
| **AuditService API** | `7175` | Consulta dos logs de auditoria (MongoDB) |
| **Audit Worker** | — | Consome eventos do RabbitMQ e grava no MongoDB |
| **Frontend** | `5173` | React + Vite (minha conta, usuários, auditoria) |
| **AuthService** | `5149` | Legado (login de admin e API Keys); sai na fatia B5 |

## Arquitetura

```text
Navegador ── cookie HttpOnly ──> Vite (5173) ──> BFF (5080) ── login OIDC ──> Keycloak (8080)
                                                   |   sessão (tokens) no Redis
                                                   |  /api/me, /api/users ── Bearer ──> UserManager
                                                   |  /api/audit          ── Bearer ──> AuditService API
                                                   v
                                              UserManager
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
3. Um handler traduz o Domain Event num **evento de integração** (`UserCreated`, `ContactDeleted`... em `UserManager.Contracts`) e publica pelo **MassTransit**, que grava a mensagem na **Outbox** (tabela `OutboxMessage`) na mesma transação. O UserManager não sabe quem escuta  
4. O serviço de entrega do MassTransit lê a Outbox e publica no **RabbitMQ**  
5. **Audit Worker** consome (com retry) e salva o log no **MongoDB**  
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
- RabbitMQ + MassTransit 8 (Outbox do EF Core, retry, fila de erro)
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
- Keycloak (`8080`) com banco próprio (`keycloak-db`) e a realm `user-manager` importada no boot
- Mailpit (`8025` tela / `1025` SMTP): captura os e-mails do Keycloak, nada sai para a internet

Credenciais padrão do Compose:
- Postgres: `postgres` / `postgres123`
- Redis: sem senha
- RabbitMQ: `dev` / `dev123`
- MongoDB: `dev` / `dev123`
- Keycloak (console de admin, realm `master`): `admin` / `admin`

### Identidade (Keycloak)

A realm `user-manager` é configuração como código: `docker/keycloak/user-manager-realm.json`.

| URL | Para quê |
|-----|----------|
| http://localhost:8080/admin | console de admin (`admin` / `admin`) |
| http://localhost:8080/realms/user-manager/account | conta da pessoa (cadastro, troca de e-mail e senha) |
| http://localhost:8025 | caixa de entrada do Mailpit (confirmação de e-mail, "esqueci a senha") |

Pessoas de teste (e-mail já verificado):

| Usuário | Senha | Papel |
|---------|-------|-------|
| `ana` | `ana123` | nenhum (pessoa comum) |
| `vera` | `vera123` | `viewer` (só lê usuários e auditoria) |
| `otto` | `otto123` | `operator` (lê e altera usuários) |

O Keycloak **só importa a realm se ela ainda não existe**. Depois de mudar o arquivo, recrie o banco do Keycloak:

```bash
docker compose rm -sf keycloak keycloak-db
docker volume rm user-manager_keycloakdata
docker compose up -d keycloak
```

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

### Mensagens de auditoria com falha (reprocesso manual)

O Audit Worker tenta gravar cada evento até 5 vezes, esperando 1s, 5s, 15s e 30s entre as tentativas (a mensagem fica retida em memória nesse meio-tempo). Com o MongoDB fora do ar, cada tentativa ainda leva ~30s até o driver desistir, então o ciclo todo dura uns 3–4 minutos. Se todas falharem, a mensagem vai para a fila **`audit_events_error`** com o motivo nos cabeçalhos `MT-Fault-*` (tipo da exceção, mensagem, stack trace, número de retentativas). Evento inválido (id vazio, nome em branco, campo obrigatório ausente, JSON fora do contrato) vai direto, sem retry. Uma mensagem de um tipo que nenhum consumidor assina vai para outra fila, **`audit_events_skipped`**.

Os intervalos podem ser trocados em `RabbitMq:RetryIntervals` (formato `"00:00:05"`); a lista configurada substitui a padrão.

Para reprocessar depois de corrigir a causa:

1. Abra o painel do RabbitMQ (http://localhost:15672, `dev` / `dev123`) → **Queues** → `audit_events_error`
2. Use **Get messages** para ler o motivo
3. Em **Move messages**, informe `audit_events` como destino e clique em **Move messages**

O **Move messages** move a fila inteira: um evento inválido volta para `audit_events_error` na hora, porque a causa dele está na mensagem, não no ambiente.

O consumidor é idempotente (índice único em `eventId` no MongoDB), então reprocessar uma mensagem que já foi gravada não duplica o log.

### Login

Abra http://localhost:5173 e clique em **Entrar**: o login acontece na tela do Keycloak (pessoas de teste na seção "Identidade", ou crie uma conta e confirme o e-mail no Mailpit). O navegador nunca recebe token: só o cookie `__Host-user-manager` (`HttpOnly`, `Secure`, `SameSite=Strict`), e F5 não desloga.

## Frontend

Organização feature-first:

- `features/auth` — sessão do BFF (`/bff/user`), login e logout por redirecionamento, rotas protegidas  
- `features/users` — minha conta (`/api/me`) e lista de usuários (`viewer`/`operator`; editar nome e excluir só `operator`)  
- `features/audit` — listagem da trilha de auditoria  
- `shared` — layout, sidebar, api client (manda `X-CSRF: 1` em toda chamada)  

Proxy do Vite: `/api`, `/bff`, `/signin-oidc` e `/signout-callback-oidc` → BFF (`5080`), preservando o `Host` para o navegador ver uma origem só.

## Backend (pontos importantes)

### AuthService
- Login de administrador
- Emissão de JWT
- Hash de senha com **Argon2id** + salt
- API Keys (com cache no Redis)
- Bootstrap automático do admin inicial

### UserManager
- Aceita só tokens do Keycloak (realm `user-manager`, audience `user-manager-api`), conferidos com a chave pública
- O `User` nasce no primeiro acesso (**JIT provisioning**), ligado à pessoa pelo `sub` do token (`IdentityProviderId`); duas requisições simultâneas criam um só `User`
- `/api/me` e `/api/me/contacts`: a pessoa logada vê e altera a própria ficha e a própria agenda; o dono nunca vem da URL
- `/api/users`: `viewer` e `operator` leem; só `operator` troca o nome de exibição ou exclui. E-mail é do Keycloak (atualizado a cada login)
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
