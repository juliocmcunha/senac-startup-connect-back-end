# InvestCase API (back-end): histórico de alterações

Este documento registra **tudo o que foi feito no back-end** desde o commit `cb3ebab` ("REFORM: integrate backend with PostgreSQL..."): o que foi criado, o que foi modificado em cada arquivo e por quê.

- O passo a passo de **testes no Swagger** está em [`GUIA-CORRECOES-E-SWAGGER.md`](GUIA-CORRECOES-E-SWAGGER.md).
- As mudanças do front-end estão no documento equivalente do repositório `senac-startup-connect-front-end` (`docs/HISTORICO-DE-ALTERACOES.md`).

---

## Sumário

1. [Visão geral](#1-visão-geral)
2. [Etapa 1: correções e integração da API](#2-etapa-1-correções-e-integração-da-api)
3. [Etapa 2: dados de demonstração e integração com o front-end](#3-etapa-2-dados-de-demonstração-e-integração-com-o-front-end)
4. [Etapa 3: chat após o match](#4-etapa-3-chat-após-o-match)
5. [Alterações arquivo por arquivo](#5-alterações-arquivo-por-arquivo)
6. [Arquitetura final](#6-arquitetura-final)
7. [Lista completa de endpoints](#7-lista-completa-de-endpoints)
8. [Como rodar o projeto](#8-como-rodar-o-projeto)
9. [Limitações conhecidas e próximos passos](#9-limitações-conhecidas-e-próximos-passos)

---

## 1. Visão geral

| Etapa | Objetivo | Resultado |
|---|---|---|
| **1. Correções** | Fazer a API funcionar de verdade: ela compilava, mas quase todos os endpoints falhavam | Banco criado automaticamente, DTOs, validações e regras de negócio |
| **2. Integração** | Atender às telas do front-end | Usuários de demonstração e `profileId` nas respostas |
| **3. Chat** | Nova funcionalidade: conversa após o match | Tabela `Messages`, `MessagesController` e notificações |

**Como tudo foi verificado:** o projeto foi compilado com o .NET 10 e executado contra um PostgreSQL 16 real. Cada erro foi **reproduzido antes** da correção e **testado depois**, por chamadas HTTP e por um navegador automatizado (Playwright) que percorreu as telas do front-end como um usuário.

---

## 2. Etapa 1: correções e integração da API

A explicação didática de cada erro, com o código antes e depois, está na seção 2 do [guia](GUIA-CORRECOES-E-SWAGGER.md#2-erros-encontrados-e-correções-por-prioridade). Resumo:

| # | Problema encontrado | Efeito | Correção |
|---|---|---|---|
| 1 | Não existiam **migrations**, então nada criava as tabelas | Todo endpoint de dados dava **500** (`relation "Users" does not exist`), enquanto o Health dizia "online" | Migration `InitialCreate` aplicada automaticamente ao iniciar. O Health passou a avisar quando há migrations pendentes |
| 2 | Os controllers recebiam a **entidade do banco** como corpo da requisição | POST/PUT de Events, FundingCalls, Startups e Investors sempre davam **400** (`CreatedByUser field is required`) | **DTOs** com apenas os campos permitidos |
| 3 | Mandar o objeto aninhado fazia o EF inserir o grafo inteiro | **Usuário falso criado no banco** (e-mail "a", sem senha) | Mesma correção do item 2: o controller monta a entidade |
| 4 | Datas sem fuso (`2026-12-10T19:00:00`) | **500**: o Npgsql só aceita UTC | `DateTimeHelper.ToUtc` aplicado a toda data recebida |
| 5 | Cadastro sem validação | Aceitava e-mail "x" e senha "1" | `[Required]`, `[EmailAddress]` e `[StringLength]` nos contratos |
| 6 | Perfil duplicado ou com papel errado | **500** ou dados inconsistentes | Checagens que devolvem **400** ou **409** |
| 7 | Startup podia demonstrar interesse em outra startup | Dashboard inflado | Interesse só entre Investor e Startup |
| 8 | Match aparecia duas vezes em Opportunities | Itens duplicados | Agrupamento por contraparte |
| 9 | Filtros diferenciavam maiúsculas | `?industry=fintech` não achava "Fintech" | `EF.Functions.ILike` |

---

## 3. Etapa 2: dados de demonstração e integração com o front-end

| Problema | Correção |
|---|---|
| O login do front já vem preenchido com `rafael@investcase.demo` e `nordestepay@investcase.demo`, mas **esses usuários não existiam** (o login dava 401) | `Data/DbSeeder.cs`: com o banco vazio, cria 3 investidores, 4 startups, 2 eventos, 1 edital, 1 interesse e 1 notificação. Senha de todos: `Demo@123` |
| As telas de Oportunidades e Matches precisavam abrir o perfil da outra pessoa, mas as respostas só traziam o id do *interesse* | `profileId` e `userId` adicionados às respostas de `OpportunitiesController` e `MatchesController` |

**Contas de demonstração** (todas com senha `Demo@123`):

| Investidores | Startups |
|---|---|
| `rafael@investcase.demo` (Recife Angels, Fintech/SaaS) | `nordestepay@investcase.demo` (Fintech, MVP) |
| `camila@investcase.demo` (Vale Ventures, Healthtech/Edtech) | `saudeja@investcase.demo` (Healthtech, Tração) |
| `bruno@investcase.demo` (Semente Capital, Agtech/Cleantech) | `agrosense@investcase.demo` (Agtech, Ideação) |
| | `edumais@investcase.demo` (Edtech, Série A) |

O seeder **só roda em Development e só se a tabela `Users` estiver vazia**. Ele nunca sobrescreve dados existentes.

---

## 4. Etapa 3: chat após o match

**Requisito:** depois do match, investidor e startup precisam conseguir conversar.

**Regras implementadas:**
- **Só conversa quem tem match.** O servidor verifica isso em todas as operações e responde **403** quando não há match. A regra fica no back-end, e não só no front, porque qualquer pessoa pode chamar a API diretamente.
- Mensagem vazia, ou com mais de 2000 caracteres, é recusada com **400**.
- Cada mensagem nova cria uma notificação do tipo **"Mensagens"** para quem recebe. Enquanto essa notificação não for lida, as mensagens seguintes **não** criam outra, para não lotar os Alertas.
- O Matches passou a informar a **última mensagem** e a **quantidade de mensagens não lidas** de cada conversa.

**Como as mensagens chegam ao outro usuário:** o front-end consulta a conversa a cada 3 segundos (*polling*). Para um projeto acadêmico, isso é simples e suficiente. A alternativa em tempo real seria o SignalR (WebSockets).

---

## 5. Alterações arquivo por arquivo

Legenda: 🆕 arquivo novo · ✏️ arquivo modificado

### Configuração e inicialização

| Arquivo | O que foi feito | Por quê |
|---|---|---|
| ✏️ `Program.cs` | Em Development, ao iniciar: aplica as migrations (`Database.Migrate()`) e roda o `DbSeeder`. Tudo dentro de `try/catch`, com log de erro | Antes, nada criava as tabelas. O `try/catch` mantém a API de pé quando o PostgreSQL está fora do ar, e o Health mostra o problema |
| 🆕 `dotnet-tools.json` | Registra a ferramenta `dotnet-ef` (versão 10) no projeto | Qualquer pessoa da equipe roda `dotnet tool restore` e passa a ter os comandos `dotnet ef` |
| ✏️ `InvestCase.API.csproj` | Recebeu `<UserSecretsId>` (commit feito por você, pelo `dotnet user-secrets init`) | Permite guardar a senha do banco fora do repositório |

### Dados (Entity Framework)

| Arquivo | O que foi feito | Por quê |
|---|---|---|
| ✏️ `Data/AppDbContext.cs` | Novo `DbSet<Message> Messages` e configuração da tabela `Messages`: conteúdo de até 2000 caracteres, índice `(SenderId, ReceiverId, SentAt)` e chaves estrangeiras para `Users` com exclusão em cascata | Suporte ao chat. O índice acelera a busca de uma conversa em ordem cronológica |
| 🆕 `Data/DbSeeder.cs` | Insere os dados de demonstração quando o banco está vazio | Os logins pré-preenchidos do front passam a funcionar e as telas já abrem com conteúdo |
| 🆕 `Migrations/..._InitialCreate.cs` (+ `.Designer.cs`) | Cria as 7 tabelas originais (`Users`, `Investors`, `Startups`, `Events`, `FundingCalls`, `Interests`, `Notifications`) com índices e chaves | O esquema do banco nunca tinha sido gerado |
| 🆕 `Migrations/..._AddMessages.cs` (+ `.Designer.cs`) | Cria a tabela `Messages` | Chat. É aplicada automaticamente sobre um banco que já existe, sem apagar dados |
| 🆕 `Migrations/AppDbContextModelSnapshot.cs` | "Foto" atual do modelo, mantida pelo EF | O EF compara com ela para saber o que mudou na próxima migration |

### Models (entidades)

| Arquivo | O que foi feito | Por quê |
|---|---|---|
| 🆕 `Models/Message.cs` | Entidade com `SenderId`, `ReceiverId`, `Content`, `SentAt` e `ReadAt` (nulo enquanto não lida) | Representa uma mensagem do chat |

Os demais Models não foram alterados.

### Contracts (DTOs: o que o cliente envia)

| Arquivo | O que foi feito | Por quê |
|---|---|---|
| ✏️ `Contracts/AuthContracts.cs` | Validações nos registros: `[Required]`, `[EmailAddress]`, senha de 6 a 100 caracteres e `[StringLength]` iguais aos limites do banco | Barrar dados inválidos com 400, em vez de aceitar ou gerar 500 |
| 🆕 `Contracts/ProfileContracts.cs` | `UpdateUserRequest`, `CreateInvestorRequest`/`InvestorRequest`, `CreateStartupRequest`/`StartupRequest` | Substituir as entidades nos POST/PUT de Users, Investors e Startups |
| 🆕 `Contracts/EventContracts.cs` | `Create`/`UpdateEventRequest` e `Create`/`UpdateFundingCallRequest` | Substituir as entidades nos POST/PUT de Events e FundingCalls |
| 🆕 `Contracts/MessageContracts.cs` | `SendMessageRequest(SenderId, ReceiverId, Content)` | Corpo do envio de mensagem |

### Helpers

| Arquivo | O que foi feito | Por quê |
|---|---|---|
| 🆕 `Helpers/DateTimeHelper.cs` | `ToUtc(DateTime)` e `ToUtc(DateTime?)` | O PostgreSQL (`timestamp with time zone`) só aceita datas em UTC |

### Controllers

| Arquivo | O que foi feito | Por quê |
|---|---|---|
| ✏️ `AuthController.cs` | O cabeçalho `Location` do 201 aponta para `/api/Users/{id}` (antes vinha vazio) | Seguir a convenção REST do código 201 |
| ✏️ `EventsController.cs` | POST/PUT recebem DTOs, convertem datas para UTC e validam `EndsAt >= StartsAt` também no PUT | Erros 2, 3 e 4 |
| ✏️ `FundingCallsController.cs` | POST/PUT com DTOs, datas em UTC e `DeadlineAt >= PublishedAt`. Sem `PublishedAt`, usa a data atual | Erros 2, 3 e 4 |
| ✏️ `HealthController.cs` | Verifica migrations pendentes e responde **503** com `schema: outdated` | O Health dizia "online" mesmo sem tabelas |
| ✏️ `InterestsController.cs` | Recusa interesse que não seja entre um Investor e uma Startup (**400**) | Erro 7 |
| ✏️ `InvestorsController.cs` | DTOs. No POST: o usuário precisa existir, ter role Investor e ainda não ter perfil (**409**). Filtros sem diferenciar maiúsculas | Erros 2, 3, 6 e 9 |
| ✏️ `StartupsController.cs` | O mesmo do anterior, para Startup. `FoundedAt` convertido para UTC | Erros 2, 3, 4, 6 e 9 |
| ✏️ `UsersController.cs` | PUT recebe `UpdateUserRequest` em vez da entidade `User` | Swagger limpo, sem campos que o cliente não pode alterar |
| ✏️ `OpportunitiesController.cs` | Remove o item duplicado do match e inclui `profileId` e `userId` em cada item | Erro 8 e abertura de perfil pelo front |
| ✏️ `MatchesController.cs` | Inclui `profileId`, `lastMessage` e `unreadMessages` | Abrir perfil e mostrar o resumo do chat |
| 🆕 `MessagesController.cs` | `GET` da conversa, `POST` de mensagem e `PATCH` de leitura, com a regra de match | Chat |

`DashboardController`, `NotificationsController`, os Models originais e os helpers `PasswordHelper` e `PresentationHelper` **não precisaram de alteração**.

### Documentação

| Arquivo | Conteúdo |
|---|---|
| 🆕 `docs/GUIA-CORRECOES-E-SWAGGER.md` | Erros e correções explicados didaticamente, comunicação entre componentes, roteiro de testes no Swagger (passos 0 a 10, incluindo o chat) e códigos HTTP |
| 🆕 `docs/HISTORICO-DE-ALTERACOES.md` | Este documento |

---

## 6. Arquitetura final

```
 Front-end React (localhost:5173)  /  Swagger (localhost:5042/swagger)
        │  HTTP + JSON  (CORS liberado para localhost:5173)
        ▼
 Program.cs ── registra Controllers, Swagger, CORS e AppDbContext (injeção de dependência)
             └─ em Development: aplica migrations e roda o DbSeeder
        │
        ▼
 Controllers ──recebem──▶ Contracts (DTOs + validação automática pelo [ApiController])
        │
        ├── usam ──▶ Helpers (PasswordHelper, PresentationHelper, DateTimeHelper)
        ▼
 AppDbContext (EF Core) ◀── Models (entidades) ◀── Migrations (esquema)
        │
        ▼
 PostgreSQL ── tabelas: Users, Investors, Startups, Events, FundingCalls,
                        Interests, Notifications, Messages
```

**Fluxo principal da plataforma:**

```
Cadastro/Login ──▶ Busca ──▶ Perfil ──▶ Interesse ──▶ (interesse recíproco) ──▶ MATCH ──▶ Chat
     Auth        Startups/   Startups/   Interests       Interests vira           Matches   Messages
                 Investors   Investors   + Notification  "Match" + notificações
```

**Relacionamentos das tabelas:**
- Cada `User` tem **um** perfil: `Investor` **ou** `Startup` (1:1, de acordo com o `Role`).
- `Interest` liga dois `User` (`FromUserId` → `ToUserId`). Dois interesses recíprocos ficam com `Status = "Match"`.
- `Message` liga dois `User` (`SenderId` → `ReceiverId`) e só pode ser criada entre usuários com match.
- `Notification` pertence a um `User`. Os tipos são `Atividades`, `Matches` e `Mensagens`.
- `Event` e `FundingCall` pertencem ao `User` que os criou.

---

## 7. Lista completa de endpoints

| Recurso | Método e rota | Descrição |
|---|---|---|
| Health | `GET /api/Health` | Status da API, do banco e do esquema |
| Auth | `POST /api/Auth/register/investor` | Cadastro de investidor (cria User e Investor) |
| | `POST /api/Auth/register/startup` | Cadastro de startup (cria User e Startup) |
| | `POST /api/Auth/login` | Login. Devolve `userId`, `profileId` e `role` |
| Users | `GET /api/Users` · `GET /api/Users/{id}` | Lista ou consulta usuários (sem `passwordHash`) |
| | `PUT /api/Users/{id}` · `DELETE /api/Users/{id}` | Atualiza ou exclui (a exclusão apaga tudo em cascata) |
| Investors | `GET /api/Investors?search=&type=&industry=&location=&startupUserId=` | Busca com filtros e % de compatibilidade |
| | `GET /api/Investors/{id}` · `POST` · `PUT /{id}` · `DELETE /{id}` | CRUD (o `id` é o **profileId**) |
| Startups | `GET /api/Startups?search=&industry=&stage=&location=&investorUserId=` | Busca com filtros e % de compatibilidade |
| | `GET /api/Startups/{id}` · `POST` · `PUT /{id}` · `DELETE /{id}` | CRUD (o `id` é o **profileId**) |
| Events | `GET` · `GET /{id}` · `POST` · `PUT /{id}` · `DELETE /{id}` em `/api/Events` | CRUD de eventos |
| FundingCalls | as mesmas rotas em `/api/FundingCalls` | CRUD de editais |
| Interests | `POST /api/Interests` | Demonstra interesse. Se for recíproco, vira Match |
| Matches | `GET /api/Matches/user/{userId}` | Matches do usuário, com resumo do chat |
| Messages | `GET /api/Messages/{userId}/with/{otherUserId}` | Conversa entre os dois usuários |
| | `POST /api/Messages` | Envia uma mensagem (exige match) |
| | `PATCH /api/Messages/{userId}/with/{otherUserId}/read` | Marca como lidas as mensagens recebidas |
| Opportunities | `GET /api/Opportunities/investor/{userId}` · `/startup/{userId}` | Status de cada interesse |
| Dashboard | `GET /api/Dashboard/investor/{userId}` · `/startup/{userId}` | Dados da tela inicial |
| Notifications | `GET /api/Notifications/user/{userId}?type=` | Alertas (`Todas`, `Matches`, `Mensagens`, `Atividades`) |
| | `PATCH /api/Notifications/user/{userId}/read-all` | Marca todas como lidas |

> **Atenção a `id` x `userId`:** as rotas `/api/Startups/{id}` e `/api/Investors/{id}` usam o **profileId**. As demais rotas com `{userId}` usam o id do **usuário**. Confundir os dois é a causa mais comum de 404.

---

## 8. Como rodar o projeto

**Pré-requisitos:** .NET 10 SDK e PostgreSQL. Cada pessoa da equipe tem o **próprio banco local**: a API cria tudo sozinha, então ninguém depende do banco de outra pessoa.

1. Instale o PostgreSQL (Linux Mint/Ubuntu):
   ```bash
   sudo apt install -y postgresql
   ```
2. Defina uma senha para o usuário `postgres`:
   ```bash
   sudo -u postgres psql -c "ALTER USER postgres PASSWORD 'SUA_SENHA';"
   ```
3. Informe essa senha à API sem colocá-la no repositório:
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=investcase;Username=postgres;Password=SUA_SENHA"
   ```
4. Rode a API. Na primeira vez, ela cria o banco, as tabelas e os dados de demonstração:
   ```bash
   dotnet run
   ```
5. Confira em http://localhost:5042/api/Health se a resposta tem `"schema": "up to date"`. O Swagger fica em http://localhost:5042/swagger.

**Para criar uma migration nova** depois de alterar um Model:
```bash
dotnet tool restore
```
```bash
dotnet ef migrations add NomeDaMudanca
```

---

## 9. Limitações conhecidas e próximos passos

| Limitação | Impacto | Sugestão |
|---|---|---|
| **Não há autenticação por token** | O login confere a senha, mas os outros endpoints confiam no `userId` enviado. Qualquer cliente pode agir como qualquer usuário, inclusive ler conversas | Implementar **JWT** e tirar o `userId` das rotas |
| Startup não tem os campos **equipe** e **mercado de atuação** | Item do requisito de cadastro de startups ainda não atendido | Adicionar ao Model, criar migration e atualizar DTOs e telas |
| `ProfileViews30Days` nunca é incrementado | "Visualizações" no dashboard fica fixo | Endpoint que registre a visita ao abrir o perfil |
| Status "Sem Resposta" nunca é gerado | O filtro do front fica sempre vazio | Definir uma regra (por exemplo, interesse sem resposta há mais de X dias) |
| Chat por *polling* (3 s) | Pequeno atraso e requisições repetidas | SignalR, se for preciso tempo real |
| Excluir um perfil mantém o `User` | O login desse usuário passa a dar 500 ("profile incomplete") | Excluir pelo `DELETE /api/Users/{id}` ou impedir a exclusão isolada do perfil |
| Migrations e seed automáticos só em Development | Em produção, nada é criado sozinho | Rodar `dotnet ef database update` no deploy |
| `GET /api/Users` é público e lista e-mails | Ponto de atenção para a **LGPD** | Proteger com autenticação ou remover o e-mail da listagem |
