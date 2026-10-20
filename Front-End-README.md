# StartupInvestor API — Guia para o Front-end

Este documento explica como executar a API localmente, usar o Swagger para conhecer os endpoints e fazer requisições de teste.

> **Estado atual do projeto:** a API ainda está em desenvolvimento e não possui banco de dados conectado. Os Controllers têm respostas provisórias para permitir conhecer e testar as rotas. Portanto, os testes atuais verificam principalmente se os endpoints estão acessíveis e como respondem — eles **não** confirmam que os dados foram salvos, atualizados ou excluídos.

## 1. O que é o Swagger?

O **Swagger UI** é uma página web que apresenta os endpoints da API e permite enviar requisições manualmente pelo navegador, sem precisar criar uma tela de front-end primeiro. Nele, você pode consultar os métodos HTTP, visualizar os parâmetros e o formato esperado dos dados e usar **Try it out** para testar uma operação.

A documentação segue a especificação **OpenAPI**, que descreve os recursos e endpoints da API em um formato padronizado. Com a configuração atual baseada em Swashbuckle, o documento JSON pode ser acessado em `/swagger/v1/swagger.json`.

## 2. Pré-requisitos

- Ter o **.NET SDK** instalado, em uma versão compatível com o `TargetFramework` do arquivo `.csproj` da API.
- Ter o repositório do projeto clonado ou baixado no computador.
- Ter acesso ao projeto `StartupInvestor.API`.

Para conferir se o SDK está instalado, abra um terminal e execute:

```bash
dotnet --version
```

## 3. Como executar a API

1. Abra o projeto no Visual Studio ou abra um terminal na pasta que contém o arquivo `StartupInvestor.API.csproj`.
2. Restaure as dependências:

   ```bash
   dotnet restore
   ```

3. Execute a API:

   ```bash
   dotnet run
   ```

   Se estiver executando a partir da pasta raiz da solução, informe o caminho do projeto:

   ```bash
   dotnet run --project ./StartupInvestor.API/StartupInvestor.API.csproj
   ```

   Ajuste o caminho caso a estrutura do repositório seja diferente.

4. Observe o terminal. O ASP.NET Core mostrará os endereços locais em que a aplicação está ouvindo, por exemplo `https://localhost:7001` ou `http://localhost:5001`. **Use a porta exibida no seu terminal**, pois ela pode ser diferente desses exemplos.

5. Abra no navegador o endereço base da API seguido de `/swagger`. Por exemplo:

   ```text
   https://localhost:7001/swagger
   ```

   Se o endereço HTTPS não funcionar, confira também o endereço HTTP exibido pelo terminal. A interface Swagger está habilitada no ambiente de desenvolvimento pela configuração atual do projeto.

## 4. Como testar uma rota no Swagger

1. Abra a página `/swagger`.
2. Expanda o grupo do recurso que deseja testar, como **Startups**, **Investors** ou **Users**.
3. Selecione o endpoint desejado.
4. Clique em **Try it out**.
5. Quando houver um parâmetro de rota, informe o valor solicitado. Para `{id}`, use um identificador no formato GUID, por exemplo `b7d5a8e3-1c24-4f60-9a31-6e2d8c0f5417`.
6. Para operações `POST` ou `PUT`, preencha o corpo JSON quando ele for solicitado.
7. Clique em **Execute**.
8. Confira o código HTTP, os cabeçalhos e o corpo da resposta apresentados pelo Swagger.

## 5. Endpoints disponíveis

Os Controllers seguem o padrão CRUD inicial e expõem estas rotas:

| Recurso | Listar | Consultar por ID | Criar | Atualizar | Excluir |
|---|---|---|---|---|---|
| Users | `GET /api/Users` | `GET /api/Users/{id}` | `POST /api/Users` | `PUT /api/Users/{id}` | `DELETE /api/Users/{id}` |
| Investors | `GET /api/Investors` | `GET /api/Investors/{id}` | `POST /api/Investors` | `PUT /api/Investors/{id}` | `DELETE /api/Investors/{id}` |
| Startups | `GET /api/Startups` | `GET /api/Startups/{id}` | `POST /api/Startups` | `PUT /api/Startups/{id}` | `DELETE /api/Startups/{id}` |
| Events | `GET /api/Events` | `GET /api/Events/{id}` | `POST /api/Events` | `PUT /api/Events/{id}` | `DELETE /api/Events/{id}` |
| Funding Calls | `GET /api/FundingCalls` | `GET /api/FundingCalls/{id}` | `POST /api/FundingCalls` | `PUT /api/FundingCalls/{id}` | `DELETE /api/FundingCalls/{id}` |

O Swagger é a referência mais atual para consultar os endpoints expostos pela versão da API que está em execução.

## 6. Exemplo de teste: criar uma Startup

No Swagger, abra `POST /api/Startups`, clique em **Try it out** e use um JSON como este:

```json
{
  "id": "b7d5a8e3-1c24-4f60-9a31-6e2d8c0f5417",
  "userId": "3d2bc7a1-0b8f-4a67-90c1-2d5c6f7a8b9c",
  "name": "TechFlow",
  "description": "A platform for business process automation.",
  "stage": "Traction",
  "industry": "Software",
  "websiteUrl": "https://example.com",
  "location": "Recife, Brazil",
  "foundedAt": "2024-03-15T00:00:00"
}
```

Os GUIDs acima são exemplos para o teste, não identificadores de registros reais. Os nomes das propriedades seguem os Models atuais e devem ser conferidos no esquema que o Swagger apresentar.

**Resposta esperada no estado atual:** o método `Create` retorna `201 Created` e devolve o objeto recebido. Isso é apenas uma resposta provisória do Controller: o objeto não é salvo em banco de dados. Se o `id` não for informado, o Model pode permanecer com o GUID padrão (`00000000-0000-0000-0000-000000000000`), pois a implementação atual não gera automaticamente um identificador.

## 7. O que significam as respostas atuais?

As respostas dependem da implementação provisória dos Controllers:

| Operação | Resposta HTTP atual | O que significa neste estágio |
|---|---|---|
| `GET` de listagem | `200 OK` | A rota é executada, mas o Controller usa `Ok()` sem devolver uma lista de dados. |
| `GET` por ID | `200 OK` | A rota é executada, mas ainda não consulta um banco nem retorna um registro real. |
| `POST` de criação | `201 Created` | O Controller devolve o objeto recebido; isso **não** significa que foi persistido. |
| `PUT` de atualização | `204 No Content` | O Controller devolve o status de sucesso, mas ainda não altera um registro real. |
| `DELETE` de exclusão | `204 No Content` | O Controller devolve o status de sucesso, mas ainda não exclui um registro real. |

Por isso, atualmente:

- Uma requisição pode retornar um status de sucesso sem realizar uma operação real sobre os dados.
- Um `GET` não deve ser considerado uma fonte de dados reais ou persistentes.
- Fazer `POST` e depois `GET` não garante que o objeto criado apareça na consulta.
- Os testes ainda não validam regras de negócio, autenticação, relacionamentos ou integridade de dados.

## 8. Como o front-end pode usar a API agora?

Neste estágio, o Swagger serve para conhecer as rotas, verificar parâmetros e conferir os códigos HTTP provisórios. O front-end pode desenvolver a estrutura de integração usando a URL base mostrada no terminal, mas deve tratar as respostas como temporárias e evitar depender de listas ou registros reais até a persistência ser implementada.

Quando a API estiver conectada ao banco de dados, os Controllers serão atualizados para consultar e modificar os registros. Nesse momento, será necessário repetir os testes para confirmar os formatos reais das respostas.

> **Atenção à execução em outro computador:** `localhost` se refere ao próprio computador onde o endereço é aberto. Para o front-end em outro computador acessar a API, será necessário usar um endereço de rede acessível e configurar a API para ouvir nesse endereço. Se o navegador bloquear chamadas entre origens diferentes, a configuração de CORS também precisará ser habilitada no back-end.

## 9. Endereços úteis

Substitua a base pela URL e pela porta que aparecerem no terminal:

- **Swagger UI:** `https://localhost:<porta>/swagger`
- **Documento OpenAPI JSON:** `https://localhost:<porta>/swagger/v1/swagger.json`
- **Exemplo de listagem de startups:** `https://localhost:<porta>/api/Startups`

## Referências oficiais

- [Microsoft Learn — Documentação de APIs Web ASP.NET Core com Swagger/OpenAPI](https://learn.microsoft.com/en-us/aspnet/core/tutorials/web-api-help-pages-using-swagger)
- [Microsoft Learn — Comando `dotnet run`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-run)
