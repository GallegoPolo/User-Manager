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
| O que acontece com uma mensagem inválida? | A3 | ✅ |
| Dois workers pegam a mesma mensagem? Como escalar os workers? | A4 | ✅ |
| Como evitar retry infinito? | A5 | ⏳ |
| Por que o domínio não conhece o RabbitMQ? | A6 | 🟡 |
| Como rastrear uma requisição entre vários microsserviços? | D | ✅ |
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
A1 ✅ → A2 ✅ → A3 ✅ → A4 ✅ → D ✅ → C → A5 → A6 → B → E
```

Por quê:
- **A3 e A4 primeiro**: fecham os buracos que perdem mensagens ou entram em loop (consumidor) e o que impede escalar (concorrência).
- **D antes do domínio**: depurar mensageria sem correlation ID é sofrido. D1 (correlation ID) e D2 (OpenTelemetry) foram feitos juntos, por escolha, porque o OpenTelemetry já usa o mesmo mecanismo (W3C `traceparent`).
- **C antes de B**: o domínio de contatos pode ser construído com a autenticação atual; trocar a identidade depois é uma mudança de borda (isolada por `ICurrentUser`).
- **A5 e A6 depois de C**: o domínio novo vai gerar eventos novos (`ContactCreated`...), que justificam backoff e a separação entre integration events e auditoria.

## Passos

### Frente A — Mensageria

- [x] **A1 — Publicação confiável** (`4a20dfe`)
  Publisher confirms com tracking, mensagem persistente e `mandatory`. A Outbox só marca `ProcessedAt` após o ack do broker; sem fila ligada, o evento fica retido em vez de ser descartado.
- [x] **A2 — Worker resiliente** (`e585e59`)
  Falha num ciclo da Outbox é logada e tentada de novo; não derruba mais o host da API. Cancelamento por shutdown é tratado como desligamento gracioso.
- [x] **A3 — Consumidor: erro transitório vs. permanente**
  - [x] **A3.1 — Classificação da mensagem** (`2ec3ceb`): `AuditEventMessageParser` devolve `Result<AuditLog>` (sem exceção para entrada inválida); `EventId` obrigatório (`required`); inválida → `nack(requeue: false)`.
  - [x] **A3.2 — Dead Letter Queue**: o consumidor declara `audit_exchange.dlx` (fanout) e `audit_events.dead-letter`; a policy `audit-events-dlx` (`^audit_events$` → `dead-letter-exchange`) liga a fila principal à DLX. Localmente a policy vem de `docker/rabbitmq/definitions.json`, importado no boot. Inválida → quarentena com `x-death` (`reason: rejected`); erro transitório continua voltando para a fila. Testes de integração com Testcontainers.
- [x] **A4 — Concorrência na Outbox**
  Reserva por tempo (lease): `ClaimPendingAsync` grava `LockedUntil = now() + 30 s` num único `UPDATE ... WHERE Id IN (SELECT ... FOR UPDATE SKIP LOCKED) RETURNING *`; a publicação acontece fora da transação. Worker que morre libera a carta quando o lease vence. Testes com Postgres real (Testcontainers); ponta a ponta com 2 instâncias da API: antes 40 duplicatas em 40 cartas, depois 20/20 sem sobreposição.
- [ ] **A5 — Retry com backoff e estado `Failed`**
  `Attempts`, `NextAttemptAt`, `LastError`; resolve o head-of-line blocking do lote de 20.
- [ ] **A6 — Integration events vs. auditoria**
  O produtor publica `user.created` num exchange próprio; o AuditService assina. Remove o acoplamento `IAuditEventPublisher` / `audit_exchange`.

### Frente D — Observabilidade

- [x] **D — Observabilidade com OpenTelemetry** (D1 + D2)
  - **Base** (`5e468a6`): OpenTelemetry na `UserManager.Api` e no `AuditService.Worker` (traces, logs estruturados e métricas HTTP) exportando OTLP para `grafana/otel-lgtm` (Grafana, Tempo, Loki, Prometheus) no compose. Npgsql só rastreia comandos dentro de um trace (sem ruído do poller da Outbox).
  - **Propagação**: `TraceParent` gravado na Outbox durante a requisição; o Worker abre `outbox publish` como filho dela; o `RabbitMQ.Client` injeta `traceparent` no cabeçalho e o consumidor continua o mesmo trace. Ponta a ponta: um `POST /api/users` vira um único trace até o `deliver` no AuditService, com logs dos dois serviços ligados a ele.

### Frente C — Domínio

- [ ] **C — Contatos** (`Contact` pertencente ao `User`; regras e eventos de domínio)
  - [x] **C1 — AuditService conhece `contact.*`** (consumidor primeiro): `EEventType` e `KnownEventTypes` ganham `contact.created/updated/deleted`, para os eventos novos não caírem na DLQ.
  - [x] **C2 — Domínio**: Value Objects `Email` e `PhoneNumber`, agregado `Contact` (independente, com `OwnerId`) e eventos.
  - [x] **C3 — Criar e listar** (`b36aed5`): `Result<T>` com `ErrorType` (`Validation`/`NotFound`/`Conflict` → 400/404/409); `POST`/`GET /api/users/{userId}/contacts`; tabela `Contacts` (conversores EF para os VOs, índice único `(OwnerId, Email)`, FK com cascade); `contact.created` auditado pela Outbox. Corrida de duplicado testada ponta a ponta (200 POSTs simultâneos: nenhum 500; o que escapa da checagem vira `23505` → 409). Novo projeto `UserManager.Application.UnitTests` (fakes escritos à mão).
  - [x] **C4 — Detalhar, alterar, excluir**: `GET`/`PUT`/`DELETE /api/users/{userId}/contacts/{contactId}`. Busca sempre por `(dono, contato)`: contato de outro dono → 404 (nunca 403, que revelaria a existência). `PUT` é substituição completa; manter o próprio e-mail não é conflito (`exceptContactId`). Regras de Create/Update num validador só (`IContactDetails` + `Include`); `contact.updated`/`contact.deleted` auditados com um payload único (`ContactAuditPayload`). `Location` do `POST` aponta para o `GET` por id.
  - [ ] **C5 — `User` usa o Value Object `Email`**.

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
| Worker depende da classe concreta `OutboxRepository` (dificulta teste unitário) | `OutboxProcessorBackgroundService` | A5 (quando a lógica de tentativas justificar testes de unidade) |
| Com vários Workers, a ordem global de publicação não é garantida | Outbox | A6 |
| Lease de 30 s vale para o lote inteiro, mas o lote é publicado em sequência: se o tempo total do lote (até 20 publicações) passar do lease, a cauda é reservada por outro Worker e publicada em dobro (tolerado pelo at-least-once). Resolver junto com o timeout do ack (parar o lote ao se aproximar do fim do lease) | `OutboxProcessorBackgroundService` | A5 |
| Cartas reservadas por uma instância que desliga (deploy) esperam até 30 s para voltar | `OutboxProcessorBackgroundService` | A5 |
| `EventId` do Domain Event descartado; Outbox gera outro | `OutboxRepository.Add` | A6 |
| `RetryPolicies.CreateMongoWritePolicy` sem uso; erro transitório (Mongo fora) ainda reentrega na hora, sem backoff | `AuditService.Infrastructure` | A5 |
| Pacotes com vulnerabilidade conhecida: `SharpCompress`/`Snappier` (via MongoDB.Driver), `Microsoft.OpenApi` (Api) | AuditService | manutenção |
| Nome da DLX duplicado entre `RabbitMqSettings` e a policy do `definitions.json` (se divergirem, a mensagem volta a ser descartada em silêncio) | AuditService / `docker/rabbitmq` | A6 |
| DLQ sem reprocessamento (replay) e sem alerta de tamanho | AuditService | D2 (alerta); replay quando houver necessidade |
| Classes `UserCreatedEvent`, `AdminLoggedInEvent` etc. sem uso | `AuditEventBase.cs` | A6 |
| Warnings CS8618 (construtor do EF deixa `Name`/`Email` nulos) | `User.cs` | C |
| Vulnerabilidades em dependências de dev do frontend (`npm audit`) | `frontend` | E |
| Tabela da Outbox cresce sem limpeza; índice em `ProcessedAt` poderia ser parcial | `OutboxMessages` | A5 |
| RabbitMQ local é 3.13 (`rabbitmq:3-management`); 4.x muda padrões de quorum queues (delivery-limit 20) | `docker-compose.yml` | avaliar em A5 |
| Seção "Rodar" do CLAUDE.md e passo a passo de ambiente zerado | docs | quando conveniente |
| MongoDB sem spans (driver 3.6 sem suporte nativo a OpenTelemetry) | AuditService | quando o driver suportar |
| `AuditService.Api` e `AuthService` sem OpenTelemetry | AuditService.Api, AuthService | B |
| Worker do AuditService morre se o RabbitMQ não estiver pronto na partida (`BrokerUnreachableException` no `ExecuteAsync` derruba o host; sem retry de conexão) | `AuditEventConsumer` | A5 (retry com backoff) |
| Worker do AuditService sem métricas (`AddMeter("System.Runtime")`, sem pacote) | `AuditService.Worker` | quando conveniente |
| Portas do Grafana/OTLP abertas em todas as interfaces, com Grafana anônima (admin) | `docker-compose.yml` | quando conveniente (`127.0.0.1:`) |
| Traces de ruído do Swagger e de `OPTIONS` (CORS) | `UserManager.Api` | quando conveniente (filtro na instrumentação) |
| Endpoint OTLP implícito (`localhost:4317`); em containers precisará de `OTEL_EXPORTER_OTLP_ENDPOINT` | hosts | quando os serviços forem para containers |
| Rotas de User ainda "adivinham" o status (404/400) em vez de usar `Result.ErrorType` | `UserController` | quando conveniente (depois de C5) |
| Conversor EF revalida o VO na leitura: se a regra de `Email`/`PhoneNumber` endurecer, linhas antigas lançam ao carregar (exige migração de dados junto) | `UserManagerDbContext` | ao mudar regra de VO |
| Corrida que escapa da checagem do caso de uso cai no middleware: duplicado → 409 com corpo `ErrorResponse` (`Database`, inglês) em vez do array `Email`; dono excluído no meio do POST → FK `23503` → 400 em vez de 404; PUT×DELETE ou DELETE×DELETE simultâneos do mesmo contato → `UPDATE`/`DELETE` afeta 0 linhas → `DbUpdateConcurrencyException` → **500** (PUT×PUT não falha: vence a última escrita e os dois `contact.updated` são auditados). Corrigir traduzindo a violação de constraint na Infrastructure para algo neutro que o caso de uso converta em `Result.Conflict`/`NotFound` | `GlobalExceptionHandlerMiddleware` / casos de uso de contato | antes do frontend (E) — decidido adiar no C4 |
| Auditoria grava e-mail/telefone de terceiros sem prazo (LGPD: mascarar ou reter por tempo limitado) | `ContactAuditPayload` (os 3 eventos de contato), `UserCreatedAuditPayload` | quando o tema surgir |
| Lista de contatos sem paginação | `GET /api/users/{userId}/contacts` | quando houver volume |
| Trace não amostrado (`traceparent ...-00`): o RabbitMQ.Client não injeta o cabeçalho e o AuditService abre um trace órfão | Outbox / RabbitMQ | quando houver sampling (sampler consistente nos dois serviços) |
| Carta que falha para sempre acrescenta um span de erro ao trace original a cada 5 s (trace pode ficar grande demais no Tempo) | `OutboxProcessorBackgroundService` | A5 (limitar tentativas resolve; avaliar link em vez de pai nas retentativas) |

## Decisões registradas

| Decisão | Motivo |
|---|---|
| At-least-once + consumidor idempotente, nunca exactly-once | publicar e marcar como publicado são dois sistemas sem transação comum; duplicata é detectável, perda não |
| Outbox implementada à mão, não MassTransit/NServiceBus | objetivo de aprendizado; MassTransit v9 é comercial. Comparar com um framework ao final é um estudo opcional |
| Worker da Outbox continua no processo da API | isolar em processo próprio é válido, mas grande demais por enquanto |
| Testes de infraestrutura com Testcontainers (RabbitMQ real) | mock não reproduz `basic.return` nem ack de broker |
| DLX ligada por **policy**, não por `x-arguments` | policy muda sem redeploy e vale para a fila existente sem recriá-la (recomendação oficial do RabbitMQ) |
| Policy local aplicada por **arquivo de definições** importado no boot | configuração do broker como código, reaplicada a cada boot; equivalente local de Topology Operator / Terraform. Custo: o usuário `dev` vai no arquivo (hash), pois o nó não cria o usuário padrão quando importa definições |
| Testes do consumidor aplicam a policy com `rabbitmqctl set_policy` | o teste prova o comportamento do consumidor sem depender do `docker-compose` |
| Concorrência da Outbox por **lease** (`LockedUntil`) + `FOR UPDATE SKIP LOCKED` | transação curta, sem segurar tranca durante a publicação; escala com N Workers. Rejeitados: tranca durante a publicação (transação longa) e advisory lock (um Worker por vez, não escala) |
| Lease calculado com `now()` do Postgres | um único relógio para todas as instâncias; evita divergência entre servidores |
| Migration aplicada **antes** do código novo (expand → deploy) | com `LockedUntil` (A4) e `TraceParent` (D) mapeadas, o `INSERT` e o claim (`RETURNING *`) da Outbox incluem as colunas; código novo em banco sem a migration falha ao criar/alterar usuário. A mudança é aditiva e nula, então o código antigo convive com o esquema novo |
| Observabilidade com **OpenTelemetry + OTLP** | padrão aberto (CNCF); o backend é trocável sem mudar código (Grafana local hoje; Grafana Cloud, Datadog etc. depois) |
| Backend local `grafana/otel-lgtm` com volume | gratuito, ecossistema usado no mercado (Tempo, Loki, Prometheus, Grafana) e persiste dados; a própria Grafana declara a imagem como de dev/demo |
| Sem pacotes prerelease (`RabbitMQ.Client.OpenTelemetry`) e sem pacotes desnecessários (`Npgsql.OpenTelemetry`) | `RabbitMQ.Client` 7.2 e Npgsql 10 já têm `ActivitySource` embutido; basta `AddSource` |
| `traceparent` persistido na Outbox (`TraceParent`, só formato W3C) | a publicação acontece fora da requisição; sem isso o trace se perde no Worker |
| `Result` com **tipo de erro** (`Validation`/`NotFound`/`Conflict`); `DomainException` só como última defesa | sem exceção para fluxo esperado; o controller traduz o tipo em status HTTP sem adivinhar |
| Unicidade do e-mail na agenda: checagem no caso de uso **e** índice único `(OwnerId, Email)` | a checagem dá a mensagem amigável; o índice fecha a corrida (`23505` → 409). `NULL` não conflita no Postgres, então vários contatos só com telefone convivem |
| Middleware identifica erro do Postgres por `SqlState` (`PostgresErrorCodes`), não pelo texto | o texto muda com o idioma do servidor (`lc_messages`) |
| Testes de caso de uso com **fakes escritos à mão**, sem biblioteca de mock | poucos colaboradores e interfaces pequenas; nenhum pacote a mais |
| Recurso de outro dono → **404**, nunca 403; o repositório só busca por `(dono, id)` | 403 confirmaria que o recurso existe (OWASP IDOR); filtrar na consulta impede o vazamento por construção |
| `PUT` de contato é **substituição completa** | semântica do HTTP; alteração parcial seria `PATCH`, fora do escopo |
| Consumidor continua o trace do produtor (pai, não só link) | é o padrão do `RabbitMQ.Client` 7.2 (`UsePublisherAsParent = true`); travado por teste de caracterização |
