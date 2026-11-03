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
| Como evitar retry infinito? | A5 | 🟡 (consumidor sim; produtor sem rota e mensagem que derruba o processo, ver débitos) |
| Por que o domínio não conhece o RabbitMQ? | A6 | ✅ |
| Qual a diferença entre evento de domínio e evento de integração? | A6 | ✅ |
| Como rastrear uma requisição entre vários microsserviços? | D | ✅ |
| Por que o UserManager não usa o Keycloak diretamente? | B2 (ficha própria `User` ligada pelo `sub`; `ICurrentUser` esconde o Keycloak) | ✅ |
| Como funciona o JIT provisioning idempotente? | B2 (índice único em `IdentityProviderId` + `23505` traduzido e releitura; teste com barreira e Postgres real) | ✅ |

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
A1 ✅ → A2 ✅ → A3 ✅ → A4 ✅ → D ✅ → C ✅ → A5 ✅ → A6 ✅ → B → E
```

Por quê:
- **A3 e A4 primeiro**: fecham os buracos que perdem mensagens ou entram em loop (consumidor) e o que impede escalar (concorrência).
- **D antes do domínio**: depurar mensageria sem correlation ID é sofrido. D1 (correlation ID) e D2 (OpenTelemetry) foram feitos juntos, por escolha, porque o OpenTelemetry já usa o mesmo mecanismo (W3C `traceparent`).
- **C antes de B**: o domínio de contatos pode ser construído com a autenticação atual; trocar a identidade depois é uma mudança de borda (isolada por `ICurrentUser`).
- **A5 e A6 depois de C**: o domínio novo vai gerar eventos novos (`ContactCreated`...), que justificam backoff e a separação entre integration events e auditoria.

## Passos

### Frente A — Mensageria

> A1–A4 foram construídos à mão para aprender o mecanismo. Em A5 a Outbox, o publicador e o consumidor manuais foram trocados pelo MassTransit; os itens abaixo ficam como histórico.

- [x] **A1 — Publicação confiável** (`4a20dfe`)
  Publisher confirms com tracking, mensagem persistente e `mandatory`. A Outbox só marca `ProcessedAt` após o ack do broker; sem fila ligada, o evento fica retido em vez de ser descartado.
- [x] **A2 — Worker resiliente** (`e585e59`)
  Falha num ciclo da Outbox é logada e tentada de novo; não derruba mais o host da API. Cancelamento por shutdown é tratado como desligamento gracioso.
- [x] **A3 — Consumidor: erro transitório vs. permanente**
  - [x] **A3.1 — Classificação da mensagem** (`2ec3ceb`): `AuditEventMessageParser` devolve `Result<AuditLog>` (sem exceção para entrada inválida); `EventId` obrigatório (`required`); inválida → `nack(requeue: false)`.
  - [x] **A3.2 — Dead Letter Queue**: o consumidor declara `audit_exchange.dlx` (fanout) e `audit_events.dead-letter`; a policy `audit-events-dlx` (`^audit_events$` → `dead-letter-exchange`) liga a fila principal à DLX. Localmente a policy vem de `docker/rabbitmq/definitions.json`, importado no boot. Inválida → quarentena com `x-death` (`reason: rejected`); erro transitório continua voltando para a fila. Testes de integração com Testcontainers.
- [x] **A4 — Concorrência na Outbox**
  Reserva por tempo (lease): `ClaimPendingAsync` grava `LockedUntil = now() + 30 s` num único `UPDATE ... WHERE Id IN (SELECT ... FOR UPDATE SKIP LOCKED) RETURNING *`; a publicação acontece fora da transação. Worker que morre libera a carta quando o lease vence. Testes com Postgres real (Testcontainers); ponta a ponta com 2 instâncias da API: antes 40 duplicatas em 40 cartas, depois 20/20 sem sobreposição.
- [x] **A5 — MassTransit, retry com backoff e estado `Failed`**
  - **Troca para MassTransit 8.5** (`d5e6141`): contrato compartilhado `Audit.Contracts.AuditEventOccurred`; o UserManager publica por `IAuditEventPublisher` → `IPublishEndpoint` com a **bus outbox do EF Core** (tabelas `OutboxMessage`/`OutboxState`/`InboxState`, gravadas na mesma transação do agregado); `eventId` = `EventId` do evento de domínio (no A5 também era o `MessageId`; deixou de ser no A6). Publish com `mandatory`: sem fila ligada a mensagem fica na Outbox e é reenviada (provado por teste). Migration troca a tabela antiga e **aborta se houver cartas pendentes**. AuditService com `IConsumer<AuditEventOccurred>`. Trace ponta a ponta mantido (`outbox send` → `outbox process` → `send` → `receive`/`process`).
  - **Resiliência do consumidor** (`66c338e`): retry em memória (1s, 5s, 15s, 30s); evento inválido ou fora do contrato vai direto, sem retry. Esgotou → fila `audit_events_error` com o motivo nos cabeçalhos `MT-Fault-*` — é o estado **`Failed`** escolhido (opção (a): fica guardado com o motivo e é reprocessado à mão). Reprocesso pelo **Move messages** do painel (plugin shovel no compose; passo a passo no README). Worker sobe antes do RabbitMQ e conecta quando ele aparece (teste com container na porta fixa). Ponta a ponta: Mongo parado → `TimeoutException` → `_error` com `RetryCount = 4` após ~3,5 min → Mongo de volta → Move → gravado.
  - **Limpeza**: remove `TestEventPublisher` (publicava JSON cru no `audit_exchange`, que não existe mais) a política de escrita do Polly e as classes `AuditEventBase`/`UserCreatedEvent`... sem uso.
- [x] **A6 — Integration events vs. auditoria** (migração expand/contract em dois commits)
  - **A6.1 — Expand** (`5b2e76d`): novo projeto `UserManager.Contracts` com `UserCreated`/`UserUpdated`/`UserDeleted` e `ContactCreated`/`ContactUpdated`/`ContactDeleted` (o contrato é de quem publica). Os handlers da Application traduzem evento de domínio → evento de integração e publicam por `IIntegrationEventPublisher`; o UserManager não conhece mais a auditoria. O AuditService assina os 6 eventos (`UserEventsConsumer`, `ContactEventsConsumer`) e decide sozinho o formato do log (`IntegrationEventMapper`, mesmo formato no Mongo de antes). O consumidor antigo de `AuditEventOccurred` ficou ligado para as mensagens em trânsito. `OccurredAt` virou `DateTimeOffset`.
  - **A6.2 — Contract**: depois de confirmar o dreno (Outbox e filas vazias), remove `Audit.Contracts`, o consumidor e o mapeador antigos, e apaga do RabbitMQ local o exchange `Audit.Contracts:AuditEventOccurred` (o MassTransit não remove bindings sozinho). Testes de retry/erro/trace migrados para os eventos novos; novo teste trava que tipo não assinado vai para `audit_events_skipped`. Ponta a ponta: criar usuário → criar, alterar e excluir contato → excluir usuário gera 5 logs no mesmo formato de antes.

### Frente D — Observabilidade

- [x] **D — Observabilidade com OpenTelemetry** (D1 + D2)
  - **Base** (`5e468a6`): OpenTelemetry na `UserManager.Api` e no `AuditService.Worker` (traces, logs estruturados e métricas HTTP) exportando OTLP para `grafana/otel-lgtm` (Grafana, Tempo, Loki, Prometheus) no compose. Npgsql só rastreia comandos dentro de um trace (sem ruído do poller da Outbox).
  - **Propagação**: `TraceParent` gravado na Outbox durante a requisição; o Worker abre `outbox publish` como filho dela; o `RabbitMQ.Client` injeta `traceparent` no cabeçalho e o consumidor continua o mesmo trace. Ponta a ponta: um `POST /api/users` vira um único trace até o `deliver` no AuditService, com logs dos dois serviços ligados a ele. Desde A5 quem propaga é o MassTransit (cabeçalho `MT-Activity-Id`, guardado junto da mensagem na Outbox).

### Frente C — Domínio

- [x] **C — Contatos** (`Contact` pertencente ao `User`; regras e eventos de domínio)
  - [x] **C1 — AuditService conhece `contact.*`** (consumidor primeiro): `EEventType` e `KnownEventTypes` ganham `contact.created/updated/deleted`, para os eventos novos não caírem na DLQ.
  - [x] **C2 — Domínio**: Value Objects `Email` e `PhoneNumber`, agregado `Contact` (independente, com `OwnerId`) e eventos.
  - [x] **C3 — Criar e listar** (`b36aed5`): `Result<T>` com `ErrorType` (`Validation`/`NotFound`/`Conflict` → 400/404/409); `POST`/`GET /api/users/{userId}/contacts`; tabela `Contacts` (conversores EF para os VOs, índice único `(OwnerId, Email)`, FK com cascade); `contact.created` auditado pela Outbox. Corrida de duplicado testada ponta a ponta (200 POSTs simultâneos: nenhum 500; o que escapa da checagem vira `23505` → 409). Novo projeto `UserManager.Application.UnitTests` (fakes escritos à mão).
  - [x] **C4 — Detalhar, alterar, excluir** (`374c51f`): `GET`/`PUT`/`DELETE /api/users/{userId}/contacts/{contactId}`. Busca sempre por `(dono, contato)`: contato de outro dono → 404 (nunca 403, que revelaria a existência). `PUT` é substituição completa; manter o próprio e-mail não é conflito (`exceptContactId`). Regras de Create/Update num validador só (`IContactDetails` + `Include`); `contact.updated`/`contact.deleted` auditados com um payload único (`ContactAuditPayload`). `Location` do `POST` aponta para o `GET` por id.
  - [x] **C5 — `User` usa o Value Object `Email`**: `User.Create`/`Update` recebem `Email`; conversor EF na coluna existente (sem migration: `has-pending-model-changes` limpo); validadores de User usam `Email.IsValid`. Corrige dois bugs reais: e-mail com caixa diferente criava um segundo usuário (o índice único compara texto cru) e `ana@localhost` era aceito. Resolve os avisos `CS8618` do `User.cs`.

### Frente B — Identidade

- [ ] **B — Keycloak + JIT provisioning + `ICurrentUser` + BFF** (spec local: `docs/superpowers/specs/2026-11-02-b-identidade-design.md`)
  Cada pessoa loga pelo Keycloak e cuida da própria agenda; operadores com papéis leem/alteram usuários e leem a auditoria; o navegador nunca vê token. Papéis: nenhum (pessoa comum), `viewer` (só lê usuários e auditoria), `operator` (lê e altera usuários); ninguém vê a agenda de outro.
  - [x] **B1 — Keycloak + Mailpit + realm como código** (`ce0dede`): `keycloak` 26.8.1 com banco próprio (`keycloak-db`) e Mailpit no compose; realm `user-manager` em `docker/keycloak/user-manager-realm.json`, importada no boot (cadastro, verificação de e-mail, "esqueci a senha", papéis `viewer`/`operator`, client `user-manager-bff` confidencial com Authorization Code + PKCE e sem password grant, claim plano `roles`, `aud` = `user-manager-api` e `audit-api`, pessoas de teste `ana`/`vera`/`otto`). Teste de contrato com Keycloak real (`Testcontainers.Keycloak`) trava o formato do token que o B2 consome (`sub`, `email`, `name`, `roles`, `aud`); mutação provou que tirar o mapper ou o audience derruba os testes certos. SMTP provado ponta a ponta (Admin API → Mailpit).
  - [x] **B2 — UserManager com identidade**: aceita só tokens do Keycloak (Authority + audience `user-manager-api`, claim `roles`, `ClockSkew` 30 s); policies por permissão (`users:read`, `users:manage`) e política padrão "logado" para todo endpoint. `User` ganha `IdentityProviderId` (único) e `Provision`/`Rename`/`SyncEmail`; migration apaga os dados de teste e tira o índice único de `Email` (a cópia pode estar desatualizada). `ICurrentUser` na Application; middleware da Api roda o JIT (`ProvisionCurrentUserCommand`) a cada requisição autenticada; token sem `sub`/e-mail válido → 403. Corrida do primeiro acesso: índice único → `UnitOfWork` traduz `23505` em `UniqueConstraintViolationException` (descartando as mudanças pendentes, menos o `OutboxState` do escopo do MassTransit — achado do revisor: com `ChangeTracker.Clear()` a escrita seguinte na mesma requisição quebrava com FK `23503`; travado por teste) → o caso de uso relê a ficha vencedora; teste com barreira e Postgres real prova 1 `User` e 1 `UserCreated` na Outbox (mutação: tirar a captura derruba o teste com `IX_Users_IdentityProviderId`). Rotas `/api/me`, `/api/me/contacts` (dono do `ICurrentUser`), `/api/users` sem `POST`; status pelo `ErrorType`. Novo projeto `UserManager.Api.IntegrationTests` (`WebApplicationFactory` + JWT RSA de teste) com a matriz de permissões. Ponta a ponta com tokens reais do Keycloak. Frontend antigo fora do ar até o B3 (aceito)
  - [x] **B3 — BFF** (`Services/Bff/UserManager.Bff`, porta 5080): cookie `__Host-user-manager` (`HttpOnly`, `Secure`, `SameSite=Strict`) como esquema padrão que responde 401/403 em vez de redirecionar; OpenIdConnect (code + PKCE, `ResponseMode=query`, `SaveTokens`) só em `/bff/login` — o .NET 10 usa **PAR** automaticamente com o Keycloak. Sessão no Redis (`ITicketStore`, ticket protegido por Data Protection: o cookie leva só um id e nenhum token fica em texto puro). Renovação do access token 60 s antes de vencer no `OnValidatePrincipal` (timeout de 10 s): o Keycloak recusou (400/401) → sessão encerrada; Keycloak fora do ar ou 5xx → a sessão segue enquanto o token atual vale (achado do revisor: antes, 500 em todo pedido e logout geral num 5xx). YARP repassa `/api/me`, `/api/users`, `/api/audit` com `Bearer` e **sem o `Cookie`** do navegador. Anti-CSRF: `/api/*` e `/bff/user` exigem `X-CSRF: 1`; `returnUrl` do login só aceita caminho local sem caracteres de controle (achado do revisor: `/%09/evil.example` virava open redirect depois de um login real); logout é `GET /bff/logout?sid=` (navegação não leva cabeçalho; `sid` da sessão barra logout forjado), com `id_token_hint` (Keycloak encerra sem confirmação). Frontend sem token no JS: login/logout por redirecionamento, estado em `/bff/user`, links por papel, "Minha conta". Testes: 26 (Redis real, `WebApplicationFactory` com esquema de teste, downstream de eco), mutações nas defesas. Ponta a ponta simulando o navegador com `curl`: login da `vera` pelo formulário do Keycloak, F5, `/api/users` 200/400/401, logout
  - [x] **B4 — Auditoria**: os 6 eventos de `UserManager.Contracts` ganham `Guid? PerformedBy` (opcional: JSON antigo sem o campo continua válido e vira `"system"`, travado por teste; tornar `required` quebra até a compilação dos produtores). Os handlers de integração preenchem com `ICurrentUser.UserId`; eventos de `User` gerados no próprio JIT (sem `UserId` ainda) levam o `UserId` do evento; ação do `operator` sobre outra pessoa registra o operator. AuditService.Api com JWT Bearer do Keycloak (audience `audit-api`), policy `audit:read` (`viewer`|`operator`), política padrão "logado", health checks anônimos e OpenTelemetry (`audit-service-api`); novo `AuditService.Api.IntegrationTests` (401/403/200). Frontend mostra "Quem fez". Ponta a ponta: contato criado pela `ana` pelo BFF → log no Mongo com `performedBy` = id do `User` dela; `ana` recebe 403 na auditoria, `vera` lê
  - [ ] **B5 — Remoção do AuthService** (incluindo API Keys)
  - [ ] **B6 — Login com GitHub**
  - [ ] **B7 — Login com Google**

### Frente E — Frontend

- [ ] **E — Perfil, avatar, estilização** (com dados reais)

## Débitos técnicos conhecidos

Coisas encontradas no caminho, fora do escopo do passo em que apareceram.

| Item | Onde | Quando tratar |
|---|---|---|
| **MassTransit 8 tem suporte (Apache 2.0) só até o fim de 2026; a v9 é comercial.** Decidir: pagar a v9, ficar na v8 sem correções, ou trocar (Wolverine/CAP, ambos MIT) | UserManager + AuditService | antes de 2027 |
| Retry do consumidor é em memória: numa queda longa do Mongo cada mensagem prende uma vaga do prefetch por ~3,5 min (30 s de timeout do driver por tentativa) e tudo acaba no `_error`, com trabalho manual proporcional à queda. Alternativas: `UseKillSwitch` (para de consumir e volta sozinho) ou redelivery agendado (plugin `rabbitmq_delayed_message_exchange`) | `AuditService` `MessagingExtensions` | quando conveniente |
| `MongoDbContext` cria os índices de forma síncrona no construtor do singleton: com o Mongo fora, cada tentativa bloqueia uma thread ~30 s (e o DI tenta de novo a cada tentativa) | `MongoDbContext` | junto do item acima |
| Desligar o Worker no meio de um retry não foi testado (intervalo + tentativa em andamento passam do `ShutdownTimeout` padrão de 30 s); fila clássica sem `delivery-limit`: mensagem que derruba o processo volta para sempre | AuditService / RabbitMQ | avaliar com quorum queues (o compose usa `rabbitmq:3-management`, 3.13; no 4.x o `delivery-limit` padrão é 20) |
| Produtor: mensagem sem rota (nenhuma fila ligada) fica na Outbox e é retentada sem limite (log de erro a cada ~1 s, `QueryDelay`), sem estado `Failed` nem alerta. Intencional (não perde o evento), mas invisível sem monitorar a tabela `OutboxMessage` | `UserManager` `MessagingExtensions` | quando houver alertas (Grafana) |
| Filas `audit_events_error` e `audit_events_skipped` sem alerta de tamanho | AuditService | quando houver alertas (Grafana) |
| Com várias instâncias da API (cada uma com o serviço de entrega da Outbox), a ordem global de publicação não é garantida | Outbox | quando surgir um consumidor que dependa de ordem (hoje a auditoria grava o horário do evento, não a ordem de chegada) |
| AuditService referencia o projeto `UserManager.Contracts` (acoplamento de compilação entre serviços) | `AuditService.Infrastructure.csproj` | se os serviços forem para repositórios separados (vira pacote NuGet) |
| Pacotes com vulnerabilidade conhecida: `SharpCompress`/`Snappier` (via MongoDB.Driver), `Microsoft.OpenApi` (Api) | AuditService | manutenção |
| Vulnerabilidades em dependências de dev do frontend (`npm audit`) | `frontend` | E |
| Tabela `InboxState` criada sem uso (exigida pela FK do `OutboxMessage` do MassTransit; o produtor não consome) | `UserManagerDbContext` | se um dia o UserManager consumir mensagens |
| Teste de partida do Worker usa porta fixa (`FreeTcpPort`) e `localhost`: corrida rara de porta e não roda com Docker remoto | `AuditConsumersStartupTests` | quando houver CI |
| Seção "Rodar" do CLAUDE.md e passo a passo de ambiente zerado | docs | quando conveniente |
| MongoDB sem spans (driver 3.6 sem suporte nativo a OpenTelemetry) | AuditService | quando o driver suportar |
| `AuthService` sem OpenTelemetry | AuthService | fecha por remoção no B5 |
| `/health/live` da AuditService.Api roda o check do Mongo (sem filtro): com o Mongo fora, a liveness espera o timeout do driver (~30 s) e falha, o que num orquestrador reiniciaria o processo por causa de uma dependência | `AuditService.Api` `Program` | quando for para containers (liveness sem dependências: `Predicate = _ => false`) |
| `performedBy` é um id: a tela de auditoria mostra o `Guid`, não o nome | frontend / AuditService | E (resolver nome pelo `/api/users/{id}`) |
| Realm só é importada se não existe: mudar `user-manager-realm.json` exige recriar o volume `keycloakdata` (passo a passo no README) | `docker/keycloak` | se a realm precisar evoluir sem perder pessoas cadastradas (Admin API ou ferramenta de config-as-code como `keycloak-config-cli`) |
| Data Protection do BFF sem chaves compartilhadas (as chaves também protegem a sessão no Redis: trocar de máquina ou de perfil invalida as sessões) | BFF | quando houver mais de uma instância do BFF (`PersistKeysToStackExchangeRedis`) |
| Renovações simultâneas do token (várias abas no mesmo segundo perto do vencimento) chamam o Keycloak N vezes; ok enquanto a realm não revoga refresh token usado ("Revoke Refresh Token" desligado) | BFF `TokenRefresher` | se ligar a rotação estrita de refresh token |
| `id_token` renovado não é guardado nem revalidado pelo BFF (só o access token é usado, e as APIs o validam; o logout com `id_token_hint` antigo funciona no Keycloak) | BFF `TokenRefresher` | se o BFF passar a ler claims do `id_token` renovado |
| Cookie `Secure` + `__Host-` em `http://localhost` verificado só com `curl` (Chrome e Firefox atuais aceitam; Safari historicamente não) | BFF / frontend | abrir no navegador; se falhar, Vite com HTTPS local |
| Deep link perdido no login: o `ProtectedRoute` já foi para `/login`, então o `returnUrl` vira `/login` → `/me` | frontend `AuthContext`/`ProtectedRoute` | E (passar `state.from`) |
| `logoutUrl` velho (outra aba logou de novo e trocou o `sid`) → "Sair" cai num 400 em branco | BFF `/bff/logout` + frontend | E (recarregar `/bff/user` antes do logout) |
| Sem teste da ligação `OnValidatePrincipal` → falha na renovação → sessão apagada do Redis → 401 (o `TokenRefresher` é testado sozinho) | BFF | quando conveniente |
| Issuer `http://localhost:8080/...` só funciona com os serviços no host; em containers precisa de `KC_HOSTNAME` | Keycloak / hosts | quando os serviços forem para containers |
| Token de serviço (client credentials) não pode disparar JIT | UserManager | fatia do gateway para clientes máquina |
| Worker do AuditService sem métricas (`AddMeter("System.Runtime")`, sem pacote) | `AuditService.Worker` | quando conveniente |
| Portas do Grafana/OTLP abertas em todas as interfaces, com Grafana anônima (admin) | `docker-compose.yml` | quando conveniente (`127.0.0.1:`) |
| Traces de ruído do Swagger e de `OPTIONS` (CORS) | `UserManager.Api` | quando conveniente (filtro na instrumentação) |
| Endpoint OTLP implícito (`localhost:4317`); em containers precisará de `OTEL_EXPORTER_OTLP_ENDPOINT` | hosts | quando os serviços forem para containers |
| Conversor EF revalida o VO na leitura: se a regra de `Email`/`PhoneNumber` endurecer, linhas antigas lançam ao carregar (exige migração de dados junto) | `UserManagerDbContext` | ao mudar regra de VO |
| Corrida que escapa da checagem do caso de uso cai no middleware: duplicado → 409 com corpo `ErrorResponse` (`Database`, inglês) em vez do array `Email` (desde B2 o `23505` já chega como `UniqueConstraintViolationException`, travado por teste da Api; falta o caso de uso de contato convertê-la em `Result.Conflict`); dono excluído no meio do POST → FK `23503` → 400 em vez de 404; PUT×DELETE ou DELETE×DELETE simultâneos do mesmo contato → `UPDATE`/`DELETE` afeta 0 linhas → `DbUpdateConcurrencyException` → **500** (PUT×PUT não falha: vence a última escrita e os dois `contact.updated` são auditados). Corrigir traduzindo a violação de constraint na Infrastructure para algo neutro que o caso de uso converta em `Result.Conflict`/`NotFound` | `GlobalExceptionHandlerMiddleware` / casos de uso de contato | antes do frontend (E) — decidido adiar no C4 |
| Auditoria grava e-mail/telefone de terceiros sem prazo (LGPD: mascarar ou reter por tempo limitado) | `ContactAuditPayload` (os 3 eventos de contato), `UserCreatedAuditPayload` | quando o tema surgir |
| Lista de contatos sem paginação | `GET /api/me/contacts` | quando houver volume |
| Trace não amostrado: comportamento da propagação do MassTransit não verificado (com o `RabbitMQ.Client` o AuditService abria um trace órfão) | Outbox / RabbitMQ | quando houver sampling (sampler consistente nos dois serviços) |
| `SyncEmail` concorrente sem token de concorrência: duas requisições com e-mail novo geram dois `UserUpdated` iguais; durante a troca de e-mail, token antigo e novo ainda válidos alternam o valor (dado final certo, ruído na auditoria) | `User` / JIT | quando incomodar a auditoria (`xmin` como token de concorrência) |
| Swagger UI sem esquema Bearer: com a política padrão "logado" toda chamada dá 401 (o caminho documentado é o `UserManager.Api.http`) | `UserManager.Api`, `AuditService.Api` | quando conveniente |
| `Authentication:Authority`/`Audience` sem validação na partida: configuração faltando só aparece na primeira requisição | `UserManager.Api`, `AuditService.Api`, BFF | quando houver mais de um ambiente (`ValidateOnStart`) |
| Tela de auditoria mostra "Falha ao carregar" também para 403 (pessoa sem papel) | frontend | E |
| `IUserRepository.GetByIdAsync` sem `CancellationToken` (assinatura antiga repetida nos casos de uso novos) | UserManager | quando conveniente |
| `Down` da migration `AddIdentityProviderIdToUsers` recria o índice único de `Email` e falha se já houver e-mails repetidos | migration | só se precisar reverter |
| `sub` com mais de 255 caracteres vira 422 (`DomainException`) em vez de 403 no JIT (teórico: no Keycloak é UUID) | `ProvisionCurrentUserHandler` | quando surgir outro IdP |
| Teste do 409 cria um segundo host (`WithWebHostBuilder`) sem descartar | `MeContactsTests` | quando conveniente |

## Decisões registradas

| Decisão | Motivo |
|---|---|
| At-least-once + consumidor idempotente, nunca exactly-once | publicar e marcar como publicado são dois sistemas sem transação comum; duplicata é detectável, perda não |
| ~~Outbox implementada à mão~~ → **MassTransit 8** (bus outbox do EF Core, retry, fila `_error`) desde A5 | A1–A4 à mão serviram para entender o mecanismo; depois o usuário preferiu menos código próprio e o padrão de mercado (2026-11-01). v8 é Apache 2.0 com suporte até o fim de 2026; a v9 é comercial (ver débito) |
| Entrega da Outbox continua no processo da API | isolar em processo próprio é válido, mas grande demais por enquanto |
| Testes de infraestrutura com Testcontainers (RabbitMQ e Postgres reais) | mock não reproduz `basic.return`, ack de broker nem a transação da Outbox |
| **Evento de domínio ≠ evento de integração** (A6) | o de domínio é interno e pode mudar à vontade; o de integração é um contrato público com outros serviços. O handler traduz um no outro, então o domínio evolui sem quebrar quem escuta |
| Contrato de integração é **de quem publica** (`UserManager.Contracts`), não de quem consome | o UserManager anuncia fatos sem saber quem escuta; um serviço novo (ex.: e-mail) só assina, sem mudar o UserManager. Mesmo modelo do dotnet/eShop. Substituiu o `Audit.Contracts` do A5, que fazia o UserManager "falar auditoria" |
| Regras de evolução do contrato publicado | nunca renomear tipo nem namespace (o MassTransit roteia por `UserManager.Contracts:UserCreated`); campo novo sempre **opcional** (um `required` novo faz o consumidor rejeitar mensagens antigas); remover campo só com expand/contract |
| Trocas de contrato em **expand/contract** | primeiro o consumidor passa a entender os dois formatos e o produtor muda; depois de confirmar o dreno, remove-se o formato antigo. Nenhuma mensagem em trânsito se perde |
| `MessageId` gerado pelo MassTransit, não mais igual ao `EventId` | a idempotência continua pelo índice único em `eventId` no Mongo. Forçar o `MessageId` pediria passar o `eventId` à porta (`PublishAsync(evento, eventId, ct)`, redundante com o corpo) ou uma interface nos contratos marcada com `[ExcludeFromTopology]` (o que puxa o MassTransit para o projeto de contratos). Custo aceito: o Inbox do MassTransit, se um dia ligado, não deduplicaria uma republicação do mesmo evento |
| `OccurredAt` como `DateTimeOffset` nos contratos | instante exato com fuso explícito; `DateTime` sem `Kind` seria lido como horário local pelo driver do Mongo |
| Publish com **`mandatory`** (filtro de publish no RabbitMQ) | sem isso o broker descarta em silêncio a mensagem sem fila ligada e a Outbox a dá por entregue (regressão achada na revisão do A5); com `mandatory` ela volta e fica na Outbox |
| Falha esgotada → fila `audit_events_error` do MassTransit (substitui a DLX por policy de A3.2) | o próprio MassTransit move a mensagem, com motivo, tipo da exceção, stack e número de retentativas nos cabeçalhos `MT-Fault-*`; não há `x-death` |
| Retry **em memória** no consumidor (1s, 5s, 15s, 30s), sem redelivery agendado | cobre quedas curtas sem plugin; redelivery agendado exige `rabbitmq_delayed_message_exchange`. Quedas longas caem no `_error` = estado `Failed` da opção (a), reprocessado à mão |
| Evento inválido lança `InvalidAuditEventException`, ignorada pelo retry | no MassTransit a falha de um consumidor é sinalizada por exceção; aqui não é fluxo de negócio, é violação de contrato vinda de outro serviço |
| Reprocesso manual pelo **Move messages** (plugin shovel) | ferramenta do próprio RabbitMQ, sem código; o consumidor é idempotente (índice único em `eventId`), então mover de novo não duplica |
| Arquivo de definições do RabbitMQ importado no boot (usuário `dev`) | configuração do broker como código; o nó não cria o usuário padrão quando importa definições. A policy de DLX saiu em A5 |
| Porta `IIntegrationEventPublisher.PublishAsync` (antes `IAuditEventPublisher`) se chama "publicar", não "enfileirar" | convenção do MassTransit; que a mensagem só sai depois do `SaveChanges` é garantido pelo `UnitOfWork` e provado por teste (sem `SaveChanges`, nada chega ao broker) |
| Migration de troca da Outbox **aborta se houver cartas pendentes** na tabela antiga | dropar a tabela perderia eventos em trânsito; a regra é esvaziar a Outbox com a versão anterior antes de migrar |
| (até A4) Concorrência da Outbox por **lease** (`LockedUntil`) + `FOR UPDATE SKIP LOCKED`, com `now()` do Postgres | transação curta, sem segurar tranca durante a publicação; escala com N Workers. No MassTransit o equivalente é o lock do `OutboxState` (`UsePostgres`) |
| (até A4/D) Migration aplicada **antes** do código novo (expand → deploy). A troca do A5 é exceção: não é aditiva (dropa `OutboxMessages`), então a ordem é parar a API antiga com a Outbox vazia, migrar e subir a nova | com `LockedUntil` (A4) e `TraceParent` (D) mapeadas, o `INSERT` e o claim (`RETURNING *`) da Outbox incluem as colunas; código novo em banco sem a migration falha ao criar/alterar usuário. A mudança é aditiva e nula, então o código antigo convive com o esquema novo |
| `User.Email` virou Value Object **sem migration de dados** | a coluna não muda; pré-condição verificada no banco: todos os e-mails já normalizados, válidos e sem colisão após `lower(trim())`. Em produção, rodar essa checagem antes do deploy, porque o conversor chama `Email.Create` na leitura. Dois modos de falha: (1) e-mail antigo **inválido** → qualquer leitura daquele usuário (lista, GET, PUT, DELETE) lança `DomainException` → 422, um erro do servidor reportado como do cliente; (2) e-mail válido mas **não normalizado** (`Ana@X.com`) → é lido normalizado, mas `EmailExistsAsync` compara com a coluna crua e o índice (sensível a caixa) aceita o duplicado. Checagem: `SELECT "Email" FROM "Users" WHERE "Email" <> lower(btrim("Email")) OR "Email" !~ '^[^@[:space:]]+@[^@[:space:]]+\.[^@[:space:]]+$'` e `SELECT lower(btrim("Email")) FROM "Users" GROUP BY 1 HAVING count(*) > 1` (ambos devem voltar vazios) |
| Observabilidade com **OpenTelemetry + OTLP** | padrão aberto (CNCF); o backend é trocável sem mudar código (Grafana local hoje; Grafana Cloud, Datadog etc. depois) |
| Backend local `grafana/otel-lgtm` com volume | gratuito, ecossistema usado no mercado (Tempo, Loki, Prometheus, Grafana) e persiste dados; a própria Grafana declara a imagem como de dev/demo |
| Sem pacotes prerelease nem desnecessários para tracing | MassTransit e Npgsql 10 já têm `ActivitySource` embutido; basta `AddSource` (`DiagnosticHeaders.DefaultListenerName` para o MassTransit) |
| Contexto do trace guardado junto da mensagem na Outbox | a publicação acontece fora da requisição; sem isso o trace se perde. Até A4 era a coluna `TraceParent`; desde A5 o MassTransit faz isso (cabeçalho `MT-Activity-Id`), provado por teste |
| `Result` com **tipo de erro** (`Validation`/`NotFound`/`Conflict`); `DomainException` só como última defesa | sem exceção para fluxo esperado; o controller traduz o tipo em status HTTP sem adivinhar |
| Unicidade do e-mail na agenda: checagem no caso de uso **e** índice único `(OwnerId, Email)` | a checagem dá a mensagem amigável; o índice fecha a corrida (`23505` → 409). `NULL` não conflita no Postgres, então vários contatos só com telefone convivem |
| Middleware identifica erro do Postgres por `SqlState` (`PostgresErrorCodes`), não pelo texto | o texto muda com o idioma do servidor (`lc_messages`) |
| Testes de caso de uso com **fakes escritos à mão**, sem biblioteca de mock | poucos colaboradores e interfaces pequenas; nenhum pacote a mais |
| Recurso de outro dono → **404**, nunca 403; o repositório só busca por `(dono, id)` | 403 confirmaria que o recurso existe (OWASP IDOR); filtrar na consulta impede o vazamento por construção |
| `PUT` de contato é **substituição completa** | semântica do HTTP; alteração parcial seria `PATCH`, fora do escopo |
| Consumidor continua o trace do produtor (pai, não só link) | padrão do MassTransit (antes, do `RabbitMQ.Client` 7.2); travado por teste |
| **Keycloak** como provedor de identidade; o UserManager guarda uma ficha própria (`User`) ligada pelo `sub` (B) | o domínio precisa de um dono local para a agenda (FK), o sistema continua de pé sem o Keycloak para quem já tem token, e trocar de IdP só mexe na borda (Anti-Corruption Layer). O UserManager nunca chama o Keycloak |
| `User` nasce só pelo **JIT** no primeiro acesso; `POST /api/users` sai (B) | um `User` criado pelo operador não teria `sub` e nunca conseguiria entrar |
| E-mail é do Keycloak (cópia atualizada a cada login); **nome de exibição é do UserManager** (semeado no JIT) (B) | o que a pessoa **é** fica no IdP; como ela **aparece** no app fica no app (avatar na frente E). Mesmo critério do Slack com SSO, que deixa isso como política |
| Papéis só onde há necessidade: `viewer` e `operator`; policies com nome de **permissão** (`users:read`, `users:manage`, `audit:read`) (B) | o papel some do código: mudar quem pode o quê é mudar um mapeamento só |
| Papéis chegam como claim plano `roles` por **mapper na realm** (B1) | sem código para achatar `realm_access.roles`; a forma do token é configuração versionada e testada |
| BFF próprio (ASP.NET Core + OIDC + YARP) agora; gateway para clientes máquina depois (B) | um gateway atrás do BFF não faria nada que o BFF já não faz; com um cliente máquina (client credentials) ele ganha motivo. Duende.BFF exige licença comercial; oauth2-proxy tiraria o BFF do .NET |
| AuthService sai em etapas (B5), incluindo as API Keys | nenhum serviço usa as API Keys; um emissor de token só; a história "construí para entender, troquei pelo padrão de mercado" é a mesma da Outbox (A1–A4 → A5) |
| "Excluir usuário" apaga os dados do app; bloquear o acesso é desativar a conta no Keycloak (B) | cada sistema cuida do que é seu; o UserManager não escreve no Keycloak |
| Keycloak com **Postgres próprio** (`keycloak-db`) (B1) | banco por serviço; o `init.sql` do Postgres compartilhado só roda em volume vazio; o `sub` sobrevive a reinícios |
| Teste da realm liga o password grant **só no container de teste** (Admin API) para obter tokens sem navegador (B1) | o arquivo da realm continua sem password grant (desaconselhado pelo OAuth 2.1); o teste confere isso antes de ligar |
| JIT num **middleware da Api** a cada requisição autenticada (B2) | todo caso de uso já encontra o `User` pronto em `ICurrentUser.UserId`; custo: um `SELECT` indexado por requisição (cache só se medir necessidade) |
| Corrida do JIT resolvida pelo **banco** (índice único + releitura), não por lock em memória (B2) | só o banco vê as duas requisições, inclusive em instâncias diferentes da Api; o `UnitOfWork` descarta as mudanças pendentes (a mensagem da perdedora não vaza para a Outbox), mas **mantém o `OutboxState`** que o MassTransit cria uma vez por escopo, senão a próxima escrita da mesma requisição quebra com FK |
| `UnitOfWork` traduz `23505` em `UniqueConstraintViolationException` (da Application) (B2) | o caso de uso reage à corrida sem conhecer Npgsql/EF; é a tradução neutra pedida no débito de corrida do C4 |
| Índice único de `Email` em `Users` removido (B2) | a cópia do e-mail pode estar desatualizada (troca cruzada de e-mails no Keycloak barraria o login); unicidade é do Keycloak |
| Política padrão de autorização = "pessoa logada" (B2) | seguro por padrão: endpoint novo sem `[Authorize]` não fica aberto por esquecimento |
| Token válido mas inutilizável (sem `sub` ou sem e-mail válido) → **403** (B2) | a identidade foi aceita, mas não serve para este app; 400 culparia a requisição e 500 esconderia a causa |
| Testes da Api com `WebApplicationFactory` e **JWT RSA gerado no teste** (B2) | prova a matriz de permissões sem Keycloak; o formato real do token já é travado pelo teste da realm (B1) |
| BFF: cookie é o esquema padrão e responde **401/403** em vez de redirecionar; o desafio OIDC só acontece em `/bff/login` (B3) | `fetch` não segue redirecionamento para outra origem; o frontend decide quando mandar a pessoa ao login |
| Sessão do BFF no **Redis** (`ITicketStore`) com ticket **protegido por Data Protection** (B3) | cookie pequeno (só um id), sessão revogável no servidor e nenhum token legível por quem lê o Redis |
| BFF **remove o `Cookie`** ao repassar para as APIs (B3) | o cookie de sessão é do BFF; um serviço interno (ou um log dele) não deve ver a credencial do navegador |
| Anti-CSRF por cabeçalho `X-CSRF: 1` nas chamadas de API (B3) | um site de terceiros não consegue mandar cabeçalho próprio sem preflight CORS, que o BFF não libera; junto com `SameSite=Strict` |
| Logout por `GET /bff/logout?sid=` (B3) | logout é navegação (o Keycloak redireciona o navegador) e navegação não leva cabeçalho; o `sid` da sessão impede um link forjado de deslogar alguém (padrão do Duende.BFF) |
| `ResponseMode=query` e cookies de correlação/nonce `SameSite=Lax` + `Secure` (B3) | o retorno do Keycloak é uma navegação GET de primeiro nível, que leva cookies `Lax` mesmo vindo de outro site; `form_post` exigiria `SameSite=None` |
| `PerformedBy` = id do `User` no UserManager, preenchido nos **handlers de integração** a partir do `ICurrentUser` (B4) | o domínio não precisa saber quem chama; a auditoria não conhece o Keycloak (nunca o `sub`); campo opcional pela regra de evolução do contrato |
| Autor dos eventos de `User` gerados no próprio JIT = a própria pessoa (`UserId` do evento) (B4) | no JIT o `ICurrentUser.UserId` ainda não existe; quem "cria" a ficha no primeiro acesso é a própria pessoa |
