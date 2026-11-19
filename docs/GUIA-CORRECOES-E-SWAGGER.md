# InvestCase API: correções, arquitetura e roteiro de testes no Swagger

Este documento explica o que foi encontrado na análise do back-end, o que foi corrigido e por quê, como as partes do projeto se comunicam e como testar tudo pelo Swagger.

> **Como foi verificado:** o projeto foi compilado com o .NET 10 SDK e executado contra um PostgreSQL 16 real. Todos os cenários do roteiro (seção 4) foram rodados antes e depois das correções. Os erros descritos aqui foram **reproduzidos**, não deduzidos.

---

## 1. Como rodar o projeto

**Pré-requisitos:** .NET 10 SDK e PostgreSQL rodando na porta 5432.

1. **Ajuste a senha do banco.** Em `appsettings.json` está `Password=CHANGE_ME`. Não coloque sua senha real nesse arquivo (ele vai para o GitHub). Use os *user-secrets*, que ficam só na sua máquina:

   ```bash
   dotnet user-secrets init
   ```

   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=investcase;Username=postgres;Password=SUA_SENHA"
   ```

2. **Rode a API:**

   ```bash
   dotnet run
   ```

   Em ambiente de desenvolvimento, a API **cria o banco `investcase` e todas as tabelas sozinha** na inicialização (veja o erro 1).

3. Abra **http://localhost:5042/swagger**.

**Dados de demonstração:** com o banco vazio, a API cria automaticamente (só em Development) usuários, startups, eventos e um edital de exemplo (`Data/DbSeeder.cs`). Todas as contas usam a senha `Demo@123`:

| Perfil | E-mails |
|---|---|
| Investidor | `rafael@investcase.demo` (login padrão do front), `camila@investcase.demo`, `bruno@investcase.demo` |
| Startup | `nordestepay@investcase.demo` (login padrão do front), `saudeja@investcase.demo`, `agrosense@investcase.demo`, `edumais@investcase.demo` |

**Rodando junto com o front-end:** no repositório `senac-startup-connect-front-end`, copie `.env.example` para `.env`, rode `npm install` e `npm run dev`, e abra http://localhost:5173. O CORS da API já libera essa origem.

**Ferramenta de migrations (opcional):** o projeto agora tem `dotnet-tools.json`. Para usar comandos `dotnet ef`:

```bash
dotnet tool restore
```

```bash
dotnet ef migrations add NomeDaMudanca
```

> **Se você já criou as tabelas manualmente** (pelo pgAdmin, por exemplo), a migration vai falhar com "relation already exists". Nesse caso, apague o banco `investcase` e deixe a API recriá-lo.

---

## 2. Erros encontrados e correções (por prioridade)

### Erro 1 (crítico): o banco não tinha tabelas, e todos os endpoints de dados retornavam 500

**Sintoma:** `GET /api/Health` respondia `"database": "online"`, mas qualquer outro endpoint (`/api/Users`, cadastro, etc.) retornava **500** com:

```
Npgsql.PostgresException: 42P01: relation "Users" does not exist
```

**Causa:** o projeto tinha o `AppDbContext` (que **descreve** as tabelas em C#), mas nada que **criasse** essas tabelas no PostgreSQL. Não existia pasta `Migrations` nem chamada a `Migrate()`/`EnsureCreated()`. O Health enganava porque só testava se *conseguia conectar*, não se as tabelas existiam.

**Conceito: migrations.** O Entity Framework (EF) Core compara suas classes (`Models` + `AppDbContext`) e gera um arquivo C# com os comandos `CREATE TABLE`, `CREATE INDEX` etc. Esse arquivo é uma *migration*. Cada mudança futura nos Models gera uma nova migration, e o histórico fica registrado na tabela `__EFMigrationsHistory`.

**Correção:**

1. Foi gerada a migration `Migrations/..._InitialCreate.cs` com `dotnet ef migrations add InitialCreate`.
2. Em `Program.cs`, no ambiente de desenvolvimento, a API aplica as migrations ao iniciar:

```csharp
using (var scope = app.Services.CreateScope())
{
    try
    {
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Could not apply database migrations. ...");
    }
}
```

*Por que `CreateScope()`?* O `AppDbContext` é registrado como **Scoped** (uma instância por requisição HTTP). Fora de uma requisição, precisamos criar um escopo manualmente para pedir uma instância ao contêiner de injeção de dependência.

*Por que o `try/catch`?* Se o PostgreSQL estiver desligado, a API continua de pé e o Health mostra o problema, em vez de a aplicação simplesmente fechar.

3. O `HealthController` agora também verifica migrations pendentes e responde **503** com `"schema": "outdated"` se as tabelas não estiverem atualizadas.

---

### Erro 2 (crítico): POST/PUT de Events, FundingCalls, Startups e Investors sempre retornavam 400

**Sintoma:** ao enviar o JSON pelo Swagger:

```json
{"errors":{"CreatedByUser":["The CreatedByUser field is required."]}}
```

**Causa:** os controllers recebiam a **entidade do banco** diretamente como corpo da requisição:

```csharp
// ANTES
public async Task<ActionResult<Event>> Create(Event item)
```

A entidade `Event` tem a propriedade de navegação `public User CreatedByUser { get; set; } = null!;`. Como o projeto usa `<Nullable>enable</Nullable>`, o ASP.NET Core trata toda propriedade de referência **não anulável** como **obrigatória** na validação automática do `[ApiController]`. Resultado: a API exigia que o cliente mandasse um objeto `User` inteiro dentro do evento. O mesmo valia para `Startup.User` e `Investor.User`.

### Erro 3 (crítico, segurança e integridade): enviar o objeto aninhado criava um usuário falso no banco

**Sintoma:** ao tentar contornar o erro 2 mandando `"user": {...}` no `POST /api/Startups`, a API respondia 201, mas **inseria um novo usuário** com e-mail `"a"`, sem senha, com data `0001-01-01`, e vinculava a startup a ele.

**Causa:** quando você faz `_context.Startups.Add(startup)`, o EF adiciona **todo o grafo de objetos**: a startup *e* o `User` que veio dentro dela. Isso é um caso de *over-posting*: o cliente consegue gravar campos e objetos que nunca deveria controlar.

**Correção dos erros 2 e 3: DTOs (Data Transfer Objects).** Foram criadas classes que representam **apenas o que o cliente pode enviar**:

- `Contracts/EventContracts.cs`: `CreateEventRequest`, `UpdateEventRequest`, `CreateFundingCallRequest`, `UpdateFundingCallRequest`
- `Contracts/ProfileContracts.cs`: `CreateStartupRequest`, `StartupRequest`, `CreateInvestorRequest`, `InvestorRequest`, `UpdateUserRequest`

```csharp
// DEPOIS
public record CreateEventRequest(
    [Required, StringLength(180, MinimumLength = 3)] string Title,
    string? Description,
    [Required] DateTime StartsAt,
    DateTime? EndsAt,
    ...
    [Required] Guid CreatedByUserId);

[HttpPost]
public async Task<ActionResult<Event>> Create(CreateEventRequest request)
{
    ...
    var item = new Event
    {
        Id = Guid.NewGuid(), Title = request.Title.Trim(), ...
        CreatedByUserId = request.CreatedByUserId
    };
```

O controller recebe o DTO, valida e **monta a entidade ele mesmo**. O cliente só informa o *Id* do usuário (`CreatedByUserId`), nunca o objeto. Bônus: o Swagger agora mostra um formulário limpo, sem aquele objeto `createdByUser` gigante com `investor`, `startup`, `createdEvents`...

**Conceito:** *entidade* é o formato do banco; *DTO* é o formato da conversa com o cliente. Separar os dois protege o banco e deixa a API mais clara. O projeto já fazia isso no `AuthController` (`InvestorRegisterRequest` etc.). A correção estendeu o mesmo padrão para o resto.

---

### Erro 4 (alto): datas sem "Z" derrubavam a API com 500

**Sintoma:** `"startsAt": "2026-12-10T19:00:00"` (sem `Z` no final) gerava:

```
Cannot write DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone', only UTC is supported.
```

**Causa:** o Npgsql (driver do PostgreSQL) só aceita datas marcadas como UTC em colunas `timestamp with time zone`. Uma data sem fuso no JSON chega ao C# como `DateTimeKind.Unspecified`.

**Correção:** novo `Helpers/DateTimeHelper.cs`, com `ToUtc(...)` aplicado a toda data recebida (`StartsAt`, `EndsAt`, `PublishedAt`, `DeadlineAt`, `FoundedAt`). Datas sem fuso passam a ser tratadas como UTC.

---

### Erro 5 (médio): cadastros sem validação

**Sintoma:** `POST /api/Auth/register/startup` aceitava nome vazio, e-mail `"x"` e senha `"1"` (retornava 201).

**Correção:** atributos de validação (*Data Annotations*) nos records de `AuthContracts.cs`:

```csharp
[Required, EmailAddress, StringLength(180)] string Email,
[Required, StringLength(100, MinimumLength = 6)] string Password,
```

Com `[ApiController]`, o ASP.NET valida **antes** de executar o método e responde **400** listando cada campo inválido. Os `StringLength` seguem os `HasMaxLength` do `AppDbContext`, então um texto grande demais vira 400 (erro do cliente) em vez de 500 (erro do banco).

---

### Erro 6 (médio): perfis duplicados e perfis com papel (role) errado

**Sintoma:** `POST /api/Startups` para um usuário que já tinha startup, ou `POST /api/Investors` para um usuário do tipo Startup.

**Causa:** só se verificava se o usuário existia. A duplicata batia no índice único `UserId` e virava 500. O papel errado criava um investidor ligado a uma conta "Startup".

**Correção:** antes de criar, `InvestorsController` e `StartupsController` verificam se o usuário existe (**400**), se o `Role` combina (**400**) e se já existe perfil (**409 Conflict**).

---

### Erro 7 (médio, regra de negócio): Startup podia "demonstrar interesse" em outra Startup

**Sintoma:** interesse Startup → Startup era aceito e inflava o `interestsReceived` do dashboard.

**Correção:** em `InterestsController`, um interesse só é aceito entre um **Investor** e uma **Startup**:

```csharp
var roles = new[] { fromUser.Role, toUser.Role };
if (!roles.Contains("Investor") || !roles.Contains("Startup"))
    return BadRequest(new { message = "Interests can only be sent between an Investor and a Startup." });
```

---

### Erro 8 (baixo): match aparecia duplicado em Opportunities

**Causa:** um match é gravado como **dois** registros de `Interest` (A→B e B→A), e a listagem mostrava os dois.

**Correção:** `OpportunitiesController` agrupa por "outra pessoa" e mantém só o registro mais recente.

### Erro 9 (baixo): filtros diferenciavam maiúsculas de minúsculas

**Sintoma:** `GET /api/Investors?industry=fintech` não encontrava `"Fintech, Saúde"`.

**Correção:** os filtros usam `EF.Functions.ILike(...)`, o `LIKE` sem diferenciar maiúsculas do PostgreSQL.

### Ajustes menores

- `PUT /api/Events/{id}` passou a validar `EndsAt >= StartsAt`, como o `POST` já fazia. Funding calls validam `DeadlineAt >= PublishedAt`.
- O cadastro agora devolve o cabeçalho `Location: /api/Users/{id}` (antes vinha vazio).

---

## 3. Como os componentes se comunicam

```
 Swagger / Front-end (React em localhost:5173)
        │  HTTP + JSON
        ▼
 Program.cs ── configura: Controllers, Swagger, CORS, AppDbContext (injeção de dependência), migrations
        │
        ▼
 Controllers  ──recebem──▶  Contracts (DTOs + validação)
        │
        ├── usam ──▶ Helpers (PasswordHelper, PresentationHelper, DateTimeHelper)
        │
        ▼
 AppDbContext (EF Core)  ◀── Models (entidades)  ◀── Migrations (esquema)
        │
        ▼
 PostgreSQL
```

- **Injeção de dependência:** `builder.Services.AddDbContext<AppDbContext>(...)` registra o contexto. Cada controller declara `AppDbContext context` no construtor e o ASP.NET entrega uma instância nova a cada requisição.
- **Fluxo principal da plataforma:**
  1. `Auth/register/*` cria um **User** e o perfil correspondente (**Investor** ou **Startup**) e devolve `userId` e `profileId`.
  2. `Auth/login` devolve os mesmos ids. O front guarda o `userId`.
  3. Quase todas as telas usam o **`userId`** (não o `profileId`): `Dashboard`, `Opportunities`, `Matches`, `Notifications`, `Interests`.
  4. `Interests`: um investidor demonstra interesse numa startup, o que gera uma **Notification** para ela. Se a startup retribuir, os dois interesses viram **Match** e ambos são notificados.
  5. `PresentationHelper.CalculateMatch` calcula o percentual de compatibilidade (área de interesse x setor, mesmo estado).
- **Atenção a `id` x `userId`:** `GET/PUT/DELETE /api/Startups/{id}` e `/api/Investors/{id}` usam o **`profileId`**. Já as rotas `.../user/{userId}` e `.../investor/{userId}` usam o **`userId`**. Confundir os dois é a causa mais comum de 404.

---

## 4. Roteiro de testes no Swagger

Em cada endpoint: clique em **Try it out**, preencha os dados, clique em **Execute** e confira o **Code** e o **Response body**. Anote os ids retornados, porque eles alimentam os passos seguintes.

### Passo 0: saúde da API
| | |
|---|---|
| Endpoint | `GET /api/Health` |
| Esperado | **200** `{"api":"online","database":"online","schema":"up to date"}` |
| Outros | **503** `database: offline` (PostgreSQL desligado ou senha errada) ou `schema: outdated` (migrations não aplicadas) |

### Passo 1: cadastrar um investidor
`POST /api/Auth/register/investor`
```json
{
  "fullName": "Ana Souza",
  "email": "ana@teste.com",
  "password": "Senha123",
  "investorType": "Anjo",
  "interestArea": "Fintech, Saúde",
  "investmentRange": "R$ 50k - 200k",
  "investmentThesis": "Startups early stage com impacto social",
  "location": "Recife, PE"
}
```
- **201**: anote `userId` (chamaremos de **INV_USER**) e `profileId` (**INV_PROFILE**).
- **409**: e-mail já cadastrado (vale também para `ANA@teste.com`, porque o e-mail é normalizado).
- **400**: e-mail inválido, senha com menos de 6 caracteres ou campo obrigatório vazio. Teste de propósito.

### Passo 2: cadastrar uma startup
`POST /api/Auth/register/startup`
```json
{
  "startupName": "PayFlow",
  "email": "pay@teste.com",
  "password": "Senha123",
  "industry": "Fintech",
  "stage": "MVP",
  "investmentNeededRange": "R$ 100k",
  "description": "Pagamentos para pequenos negócios",
  "location": "Olinda, PE"
}
```
- **201**: anote **ST_USER** e **ST_PROFILE**.

### Passo 3: login
`POST /api/Auth/login`
```json
{ "email": "ana@teste.com", "password": "Senha123", "expectedRole": "Investor" }
```
- **200**: os mesmos ids do cadastro.
- **401**: senha errada.
- **400**: `expectedRole` diferente do tipo da conta (ex.: `"Startup"`).

### Passo 4: listar e consultar perfis
- `GET /api/Users`: **200**. Confira que só aparecem os usuários que você cadastrou e que **não** aparece `passwordHash`.
- `GET /api/Startups?investorUserId=INV_USER`: **200**. A PayFlow aparece com `"95% compatível"` (mesmo setor + mesmo estado PE).
- `GET /api/Startups?industry=fintech`: deve encontrar a PayFlow mesmo com letra minúscula.
- `GET /api/Investors?industry=fintech&location=recife`: deve encontrar a Ana.
- `GET /api/Startups/ST_PROFILE`: **200**. Usando ST_USER no lugar, dá **404** (lembre de `id` x `userId`).

### Passo 5: atualizar perfis
`PUT /api/Startups/ST_PROFILE`
```json
{
  "name": "PayFlow",
  "description": "Pagamentos para pequenos negócios",
  "stage": "Tração",
  "industry": "Fintech",
  "investmentNeededRange": "R$ 300k",
  "websiteUrl": "https://payflow.exemplo.com",
  "location": "Olinda, PE",
  "foundedAt": "2024-03-15"
}
```
- **204** (sucesso, sem corpo). Confirme com `GET /api/Startups/ST_PROFILE`.
- **Atenção:** `PUT` **substitui** o recurso. Campos omitidos ficam `null`. Envie sempre o objeto completo.

`PUT /api/Investors/INV_PROFILE` (mesma ideia, campos do investidor) e `PUT /api/Users/INV_USER`:
```json
{ "fullName": "Ana Souza", "bio": "Investidora anjo em Recife", "profilePictureUrl": null, "isActive": true }
```

### Passo 6: validações dos perfis
- `POST /api/Startups` com `{"userId": "ST_USER", "name": "Duplicada"}`: **409** (já tem perfil).
- `POST /api/Investors` com `{"userId": "ST_USER"}`: **400** (usuário é Startup).

### Passo 7: eventos e editais
`POST /api/Events`
```json
{
  "title": "Demo Day Recife",
  "description": "Apresentação de startups",
  "startsAt": "2026-12-10T19:00:00",
  "endsAt": "2026-12-10T22:00:00",
  "location": "Recife, PE",
  "type": "Pitch",
  "registrationUrl": "https://exemplo.com/demoday",
  "createdByUserId": "INV_USER"
}
```
- **201**: repare que `startsAt` volta com `Z` (convertido para UTC). Anote o `id`.
- Teste `endsAt` antes de `startsAt`: **400**.
- `createdByUserId` inexistente: **400**.

`POST /api/FundingCalls`
```json
{
  "title": "Edital Inova PE",
  "description": "Subvenção para startups",
  "industry": "Fintech",
  "totalAmount": 500000,
  "deadlineAt": "2027-01-31",
  "callUrl": "https://exemplo.com/edital",
  "createdByUserId": "INV_USER"
}
```
- **201**. Sem `publishedAt`, a API usa a data atual.

Depois: `GET /api/Events`, `PUT /api/Events/{id}` (**204**), `DELETE /api/Events/{id}` (**204**, e um segundo DELETE retorna **404**).

### Passo 8: interesse e match (o fluxo principal)
1. `POST /api/Interests` `{"fromUserId": "INV_USER", "toUserId": "ST_USER"}`: **201**, `"status": "Interest Sent"`.
2. Repita o mesmo: **409** `Interest already registered`.
3. `GET /api/Notifications/user/ST_USER`: aparece *"Ana Souza demonstrou interesse no seu perfil"*.
4. `GET /api/Dashboard/startup/ST_USER`: `interestsReceived: 1`, a Ana em `investors` e o evento do passo 7 em `events`.
5. `POST /api/Interests` `{"fromUserId": "ST_USER", "toUserId": "INV_USER"}`: **201**, `"status": "Match"`.
6. `GET /api/Matches/user/INV_USER`: a PayFlow aparece **uma vez**.
7. `GET /api/Opportunities/investor/INV_USER` e `GET /api/Opportunities/startup/ST_USER`: **um** item cada, com `status: "Match"`.
8. `GET /api/Notifications/user/INV_USER?type=Matches`: *"Novo match com PayFlow"*.
9. `PATCH /api/Notifications/user/INV_USER/read-all`: **204**. Liste de novo e confira `unread: false`.

**Teste negativo:** cadastre uma segunda startup e tente `POST /api/Interests` de uma startup para a outra: **400**.

### Passo 9: dashboard do investidor
`GET /api/Dashboard/investor/INV_USER`: **200**, `recommendedStartups` ordenado por `matchScore`. Usando um ST_USER: **404** `Investor profile not found`.

### Resumo dos códigos HTTP
| Código | Significado nesta API |
|---|---|
| 200 OK | Consulta bem-sucedida |
| 201 Created | Registro criado (corpo traz o registro) |
| 204 No Content | PUT/PATCH/DELETE bem-sucedido (sem corpo) |
| 400 Bad Request | Dados inválidos (validação ou regra de negócio) |
| 401 Unauthorized | Login com e-mail/senha incorretos |
| 404 Not Found | Id não existe (confira `id` x `userId`) |
| 409 Conflict | Duplicidade (e-mail, perfil, interesse) |
| 500 | Erro não previsto. Copie o corpo da resposta e investigue |
| 503 | Banco offline ou esquema desatualizado (Health) |

---

## 5. Limitações conhecidas (não são bugs, mas vale saber)

- **Não há autenticação por token.** O login só confere a senha. Os outros endpoints confiam no `userId` enviado, então qualquer cliente pode agir como qualquer usuário. Para um projeto acadêmico isso é aceitável, mas o próximo passo natural seria JWT.
- **Excluir um perfil** (`DELETE /api/Investors/{id}`) mantém o `User`, e o login dele passa a responder 500 "profile incomplete". Para remover a conta inteira, use `DELETE /api/Users/{id}`, que apaga tudo em cascata.
- `ProfileViews30Days` nunca é incrementado. Não há endpoint que registre visualizações.
- A migration automática roda só em **Development**. Em produção, aplique com `dotnet ef database update`.
