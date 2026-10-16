# Roadmap — User-Manager

> Documento vivo. Atualizado a cada passo concluído.

## Objetivo

Não é um CRUD bonito. É um sistema distribuído que demonstra conhecimento real de **mensageria confiável, DDD, Clean/Hexagonal Architecture e identidade**, de forma que cada decisão possa ser explicada numa entrevista.

### Perguntas que o projeto precisa responder

| Pergunta | Onde é respondida | Status |
|---|---|---|
| Por que existe Outbox? | Outbox original | ✅ |
| O que acontece se o processo morrer depois de publicar no RabbitMQ? | A1 | ✅ |
| Por que não usamos exactly-once? | A1 | ✅ |
| Como evitamos duplicação? | índice único em `eventId` (AuditService), A3 | 🟡 |
| Uma falha no worker derruba a API? | A2 | ✅ |
| O que acontece com uma mensagem inválida? | A3 | ⏳ |
| Dois workers pegam a mesma mensagem? Como escalar os workers? | A4 | ⏳ |
| Como evitar retry infinito? | A5 | ⏳ |
| Por que o domínio não conhece o RabbitMQ? | A6 | 🟡 |
| Como rastrear uma requisição entre vários microsserviços? | D1, D2 | ⏳ |
| Por que o UserManager não usa o Keycloak diretamente? | B | ⏳ |
| Como funciona o JIT provisioning idempotente? | B | ⏳ |

## Frentes

| Frente | Tema |
|---|---|
| **A** | Confiabilidade da mensageria (Outbox, confirms, DLQ, concorrência, retry) |
| **B** | Identidade (Keycloak, JIT provisioning, `ICurrentUser`, BFF, cookies HttpOnly, Redis) |
| **C** | Domínio: agenda pessoal (`User` → owns → `Contact`); roles só quando houver necessidade |
| **D** | Observabilidade e testes (logs estruturados, correlation ID, OpenTelemetry, testes de falha) |
| **E** | Frontend ("Operations Ledger", perfil, avatar; token perdido no F5 se resolve com B) |

## Ordem escolhida (intercalada)

```text
A1 ✅ → A2 ✅ → A3 → A4 → D1 → C → A5 → A6 → B → D2 → E
```

Por quê:
- **A3 e A4 primeiro**: fecham os buracos que perdem mensagens ou entram em loop (consumidor) e o que impede escalar (concorrência).
- **D1 antes do domínio**: depurar mensageria sem correlation ID é sofrido.
- **C antes de B**: o domínio de contatos pode ser construído com a autenticação atual; trocar a identidade depois é uma mudança de borda (isolada por `ICurrentUser`).
- **A5 e A6 depois de C**: o domínio novo vai gerar eventos novos (`ContactCreated`...), que justificam backoff e a separação entre integration events e auditoria.

## Passos

### Frente A — Mensageria

- [x] **A1 — Publicação confiável** (`4a20dfe`)
  Publisher confirms com tracking, mensagem persistente e `mandatory`. A Outbox só marca `ProcessedAt` após o ack do broker; sem fila ligada, o evento fica retido em vez de ser descartado.
- [x] **A2 — Worker resiliente** (`e585e59`)
  Falha num ciclo da Outbox é logada e tentada de novo; não derruba mais o host da API. Cancelamento por shutdown é tratado como desligamento gracioso.
- [ ] **A3 — Consumidor: erro transitório vs. permanente**
  - [x] **A3.1 — Classificação da mensagem**: `AuditEventMessageParser` devolve `Result<AuditLog>` (sem exceção para entrada inválida); `EventId` obrigatório (`required`); inválida → `nack(requeue: false)`.
  - [ ] **A3.2 — Dead Letter Queue** via policy (`dead-letter-exchange`), para a mensagem inválida ir para quarentena em vez de ser descartada.
- [ ] **A4 — Concorrência na Outbox**
  Claim com `FOR UPDATE SKIP LOCKED` para vários workers não publicarem a mesma mensagem.
- [ ] **A5 — Retry com backoff e estado `Failed`**
  `Attempts`, `NextAttemptAt`, `LastError`; resolve o head-of-line blocking do lote de 20.
- [ ] **A6 — Integration events vs. auditoria**
  O produtor publica `user.created` num exchange próprio; o AuditService assina. Remove o acoplamento `IAuditEventPublisher` / `audit_exchange`.

### Frente D — Observabilidade

- [ ] **D1 — Correlation ID e logs estruturados** (HTTP → Outbox → RabbitMQ → AuditService)
- [ ] **D2 — OpenTelemetry** (traces e métricas)

### Frente C — Domínio

- [ ] **C — Contatos** (`Contact` pertencente ao `User`; regras e eventos de domínio)

### Frente B — Identidade

- [ ] **B — Keycloak + JIT provisioning + `ICurrentUser` + BFF**

### Frente E — Frontend

- [ ] **E — Perfil, avatar, estilização** (com dados reais)

## Débitos técnicos conhecidos

Coisas encontradas no caminho, fora do escopo do passo em que apareceram.

| Item | Onde | Quando tratar |
|---|---|---|
| Uma conexão RabbitMQ nova por mensagem publicada | `RabbitMqAuditEventPublisher` | junto de A5 |
| Sem timeout para o ack do broker | `RabbitMqAuditEventPublisher` | junto de A5 |
| Worker depende da classe concreta `OutboxRepository` (dificulta teste unitário) | `OutboxProcessorBackgroundService` | A4 |
| `EventId` do Domain Event descartado; Outbox gera outro | `OutboxRepository.Add` | A6 |
| `RetryPolicies.CreateMongoWritePolicy` sem uso; erro transitório (Mongo fora) ainda reentrega na hora, sem backoff | `AuditService.Infrastructure` | A5 |
| Pacotes com vulnerabilidade conhecida: `SharpCompress`/`Snappier` (via MongoDB.Driver), `Microsoft.OpenApi` (Api) | AuditService | manutenção |
| Warning CS8604 (`_rabbitChannel` possivelmente nulo) | `AuditEventConsumer` | A3.2 |
| Classes `UserCreatedEvent`, `AdminLoggedInEvent` etc. sem uso | `AuditEventBase.cs` | A6 |
| Warnings CS8618 (construtor do EF deixa `Name`/`Email` nulos) | `User.cs` | C |
| Vulnerabilidades em dependências de dev do frontend (`npm audit`) | `frontend` | E |
| Tabela da Outbox cresce sem limpeza; índice em `ProcessedAt` poderia ser parcial | `OutboxMessages` | A5 |
| RabbitMQ local é 3.13 (`rabbitmq:3-management`); 4.x muda padrões de quorum queues (delivery-limit 20) | `docker-compose.yml` | avaliar em A5 |
| Seção "Rodar" do CLAUDE.md e passo a passo de ambiente zerado | docs | quando conveniente |

## Decisões registradas

| Decisão | Motivo |
|---|---|
| At-least-once + consumidor idempotente, nunca exactly-once | publicar e marcar como publicado são dois sistemas sem transação comum; duplicata é detectável, perda não |
| Outbox implementada à mão, não MassTransit/NServiceBus | objetivo de aprendizado; MassTransit v9 é comercial. Comparar com um framework ao final é um estudo opcional |
| Worker da Outbox continua no processo da API | isolar em processo próprio é válido, mas grande demais por enquanto |
| Testes de infraestrutura com Testcontainers (RabbitMQ real) | mock não reproduz `basic.return` nem ack de broker |
