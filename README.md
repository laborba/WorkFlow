# WorkFlow

Plataforma web multiempresa para gerenciamento de projetos, tarefas, equipes, comunicação interna, notificações e acompanhamento de atividades.

O **WorkFlow** está sendo desenvolvido como um projeto profissional em **.NET 10**, com foco em boas práticas de arquitetura, regras de domínio, segurança, testes automatizados e isolamento multi-tenant.

O projeto possui três objetivos principais:

- servir como projeto de aprendizagem e portfólio profissional;
- ser utilizado futuramente por empresas reais;
- evoluir para um produto SaaS comercial de autoatendimento.

> O projeto está atualmente em desenvolvimento ativo.

---

## Estado atual

O backend já possui uma base funcional para gerenciamento de empresas e usuários.

Atualmente estão implementados:

### Empresas — Tenants

- cadastro de empresas;
- consulta por `PublicId`;
- atualização de dados;
- ativação e desativação;
- listagem paginada;
- busca por nome, registro ou e-mail;
- filtro por status;
- validações de domínio e aplicação.

### Usuários

- cadastro de usuários vinculados a uma empresa;
- consulta individual por `PublicId`;
- isolamento do usuário pelo Tenant;
- perfis de usuário;
- validação de empresa existente;
- bloqueio de cadastro em empresa inativa;
- bloqueio de `SystemAdmin` vinculado a uma empresa;
- senha armazenada somente como hash;
- política de senha;
- normalização de e-mail;
- unicidade de e-mail case-insensitive dentro da mesma empresa;
- possibilidade de reutilizar o mesmo e-mail em empresas diferentes.
- listagem paginada de usuários por empresa;
- busca de usuários por nome ou e-mail;
- filtro por perfil;
- filtro por status ativo/inativo;

### Persistência

- Entity Framework Core;
- PostgreSQL;
- migrations;
- índices únicos;
- relacionamentos configurados explicitamente;
- testes reais de persistência contra PostgreSQL.

### Testes

Última validação local:

```text
530 testes automatizados aprovados
0 falhas
```

A solução possui testes unitários e testes de integração.

---

# Tecnologias

## Backend

- C#
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- PostgreSQL
- Npgsql

## Segurança

- ASP.NET Core PasswordHasher
- User Secrets para credenciais locais
- hash de senha com salt
- normalização de e-mail
- isolamento multi-tenant no backend

## Testes

- xUnit
- testes unitários
- testes de integração
- PostgreSQL real nos testes de persistência

## Frontend planejado

- React
- TypeScript
- Vite

## Tecnologias futuras

Serão adicionadas somente quando houver necessidade real:

- SignalR
- Docker
- Docker Compose
- GitHub Actions
- CI/CD
- serviços em nuvem
- Redis, caso exista justificativa futura
- MongoDB, caso exista justificativa futura

O projeto não utiliza Dapper. O acesso a dados está sendo desenvolvido com Entity Framework Core.

---

# Arquitetura

A solução está dividida em camadas:

```text
WorkFlow
│
├── src
│   ├── WorkFlow.API
│   ├── WorkFlow.Application
│   ├── WorkFlow.Domain
│   └── WorkFlow.Infrastructure
│
├── tests
│   ├── WorkFlow.UnitTests
│   └── WorkFlow.IntegrationTests
│
├── frontend
├── docs
└── WorkFlow.sln
```

## WorkFlow.Domain

Contém o núcleo do negócio:

- entidades;
- enums;
- regras de domínio;
- validações;
- comportamentos das entidades;
- componentes independentes de infraestrutura.

O domínio não conhece:

- Entity Framework Core;
- PostgreSQL;
- HTTP;
- ASP.NET Core;
- React.

---

## WorkFlow.Application

Contém os casos de uso da aplicação.

Responsabilidades:

- commands;
- queries;
- handlers;
- resultados;
- erros da aplicação;
- contratos de repositórios;
- abstrações de serviços.

Exemplo de fluxo:

```text
HTTP Request
    ↓
Controller
    ↓
Handler
    ↓
Domain
    ↓
Repository
    ↓
PostgreSQL
```

---

## WorkFlow.Infrastructure

Contém implementações relacionadas à infraestrutura:

- `WorkFlowDbContext`;
- configurações do Entity Framework Core;
- repositórios;
- PostgreSQL;
- migrations;
- implementação do serviço de hash de senha.

---

## WorkFlow.API

É a camada de entrada HTTP da aplicação.

Responsável por:

- controllers;
- contratos HTTP;
- mapeamento de requests;
- mapeamento de responses;
- status HTTP;
- configuração da aplicação.

Autenticação e autorização ainda serão implementadas.

---

# Multi-tenancy

O WorkFlow é uma aplicação multiempresa.

Cada empresa é representada por um:

```text
Tenant
```

Exemplo:

```text
WorkFlow
│
├── Empresa A
│   ├── Usuários
│   ├── Projetos
│   └── Tarefas
│
└── Empresa B
    ├── Usuários
    ├── Projetos
    └── Tarefas
```

O isolamento entre empresas deve sempre ser garantido pelo backend.

Um recurso pertencente a um Tenant não pode ser acessado utilizando o identificador de outro Tenant.

Por exemplo:

```text
Tenant A
└── Usuário X

Tenant B
└── Usuário Y
```

Mesmo conhecendo o `PublicId` do Usuário Y, uma consulta realizada através do Tenant A não retorna esse usuário.

---

# Identificadores

O projeto diferencia identificadores internos e externos.

```text
Id
→ chave interna e relacionamentos no banco

PublicId
→ identificador externo exposto pela API
```

Os IDs internos não precisam ser expostos para os clientes da API.

O conhecimento de um `PublicId` nunca deve representar autorização para acessar um recurso.

---

# Perfis de usuário

Atualmente existem quatro perfis globais:

```text
SystemAdmin
TenantAdmin
ProjectManager
Member
```

## SystemAdmin

Administrador da plataforma WorkFlow.

Normalmente não pertence a um Tenant.

---

## TenantAdmin

Administrador de uma empresa específica.

Futuramente poderá administrar:

- usuários;
- projetos;
- configurações da empresa;
- permissões administrativas.

---

## ProjectManager

Usuário voltado ao gerenciamento de projetos.

As permissões efetivas também dependerão da participação e das permissões específicas dentro de cada projeto.

---

## Member

Usuário comum de uma empresa.

Poderá participar de projetos, executar tarefas, comentar e receber permissões adicionais.

---

# E-mails de usuários

O WorkFlow preserva o e-mail informado para apresentação:

```text
Lucas.Teste@Empresa.com
```

e mantém internamente uma versão normalizada:

```text
LUCAS.TESTE@EMPRESA.COM
```

Essa normalização é utilizada para comparação e unicidade.

Dentro do mesmo Tenant:

```text
usuario@test.com
Usuario@Test.com
USUARIO@TEST.COM
```

são considerados o mesmo e-mail.

Portanto:

```text
Tenant A + usuario@test.com
Tenant A + Usuario@Test.com
→ não permitido
```

Por outro lado:

```text
Tenant A + usuario@test.com
Tenant B + Usuario@Test.com
→ permitido
```

A regra poderá ser revisada futuramente caso o modelo de identidade evolua para permitir uma única conta global participar de vários Tenants.

---

# Senhas

Senhas nunca são armazenadas em texto puro.

O fluxo atual é:

```text
Senha
↓
PasswordHasher
↓
PasswordHash
↓
PostgreSQL
```

Somente o hash é persistido.

A política atual de senha exige:

- mínimo de 15 caracteres;
- máximo de 128 caracteres;
- suporte a Unicode;
- ausência de exigência arbitrária de maiúsculas, números ou símbolos.

Evoluções futuras previstas:

- bloqueio de senhas comuns ou comprometidas;
- rate limiting;
- MFA;
- recuperação segura de senha.

---

# Endpoints implementados

## Tenants

### Criar empresa

```http
POST /api/tenants
```

---

### Consultar empresa

```http
GET /api/tenants/{publicId}
```

---

### Atualizar empresa

```http
PUT /api/tenants/{publicId}
```

---

### Alterar status

```http
PATCH /api/tenants/{publicId}/status
```

---

### Listar empresas

```http
GET /api/tenants
```

Parâmetros disponíveis:

```text
pageNumber
pageSize
search
isActive
```

Exemplo:

```http
GET /api/tenants?pageNumber=1&pageSize=20&search=empresa&isActive=true
```

---

## Usuários

### Criar usuário dentro de uma empresa

```http
POST /api/tenants/{tenantPublicId}/users
```

Exemplo:

```json
{
  "name": "Usuário Teste",
  "email": "usuario@empresa.com",
  "password": "uma senha longa e segura",
  "role": 4
}
```

---

### Consultar usuário dentro de uma empresa

```http
GET /api/tenants/{tenantPublicId}/users/{userPublicId}
```

Exemplo de resposta:

```json
{
  "publicId": "00000000-0000-0000-0000-000000000000",
  "tenantPublicId": "00000000-0000-0000-0000-000000000000",
  "name": "Usuário Teste",
  "email": "usuario@empresa.com",
  "role": 4,
  "isActive": true,
  "createdAt": "2026-01-01T12:00:00Z",
  "updatedAt": null
}
```

Informações internas como estas não são retornadas:

```text
Id
TenantId
PasswordHash
NormalizedEmail
```

---

### Listar usuários de uma empresa

```http
GET /api/tenants/{tenantPublicId}/users
```

Parâmetros disponíveis:

```text
pageNumber
pageSize
role
isActive
search
```

Exemplo:

```http
GET /api/tenants/{tenantPublicId}/users?pageNumber=1&pageSize=20&role=4&isActive=true&search=lucas
```

A listagem é sempre limitada ao Tenant informado na rota.

Os filtros são opcionais:

- `pageNumber`: número da página, começando em `1`;
- `pageSize`: quantidade de itens por página, entre `1` e `100`;
- `role`: perfil do usuário;
- `isActive`: filtra usuários ativos ou inativos;
- `search`: pesquisa por nome ou e-mail, sem diferenciação entre maiúsculas e minúsculas.

Exemplo de resposta:

```json
{
  "items": [
    {
      "publicId": "00000000-0000-0000-0000-000000000000",
      "name": "Usuário Teste",
      "email": "usuario@empresa.com",
      "role": 4,
      "isActive": true,
      "createdAt": "2026-01-01T12:00:00Z",
      "updatedAt": null
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1
}
```
---

# Tratamento de erros

A API utiliza códigos de erro estáveis.

Exemplos:

```text
Tenants.NotFound
Tenants.Inactive

Users.NotFound
Users.EmailAlreadyExists
Users.SystemAdminCannotBelongToTenant

Validation.InvalidArgument
```

Exemplo de resposta:

```json
{
  "code": "Users.NotFound",
  "message": "O usuário informado não foi encontrado."
}
```

Os códigos são independentes do idioma da interface.

Isso permitirá que o frontend futuramente apresente mensagens localizadas sem depender do texto retornado pela API.

---

# Internacionalização

O WorkFlow está sendo preparado para suportar inicialmente:

```text
English
Português do Brasil
Español
```

Códigos:

```text
en
pt-BR
es
```

A estratégia planejada é:

```text
1. preferência do usuário autenticado
2. preferência salva no navegador
3. inglês como padrão
```

O idioma não será escolhido automaticamente por IP ou localização geográfica.

A localização será responsabilidade principalmente do frontend.

---

# Banco de dados

O projeto utiliza PostgreSQL.

A persistência é feita através do Entity Framework Core.

São utilizados:

- migrations;
- índices;
- Foreign Keys;
- restrições de unicidade;
- configuração explícita de entidades;
- timestamps em UTC.

---

# Migrations

Para aplicar migrations pelo CLI:

```bash
dotnet ef database update \
  --project src/WorkFlow.Infrastructure \
  --startup-project src/WorkFlow.API
```

Para criar uma nova migration:

```bash
dotnet ef migrations add NomeDaMigration \
  --project src/WorkFlow.Infrastructure \
  --startup-project src/WorkFlow.API
```

Também é possível utilizar o Console do Gerenciador de Pacotes do Visual Studio.

---

# Configuração local

## Requisitos

Antes de executar o projeto, é necessário possuir:

- .NET 10 SDK;
- PostgreSQL;
- Visual Studio, Rider ou VS Code;
- ferramenta `dotnet-ef`, caso migrations sejam executadas pelo terminal.

---

## Restaurar dependências

Na raiz da solução:

```bash
dotnet restore
```

---

## Compilar

```bash
dotnet build
```

---

## Banco de dados

Crie um banco PostgreSQL para desenvolvimento.

A string de conexão deve ser configurada fora do código-fonte.

Durante o desenvolvimento local são utilizados:

```text
User Secrets
```

Não adicione ao Git:

- senhas;
- connection strings reais;
- tokens;
- chaves de API;
- credenciais de serviços externos.

Depois de configurar o banco, aplique as migrations:

```bash
dotnet ef database update \
  --project src/WorkFlow.Infrastructure \
  --startup-project src/WorkFlow.API
```

---

# Executando a API

Pelo terminal:

```bash
dotnet run --project src/WorkFlow.API
```

Durante o desenvolvimento, a documentação OpenAPI/Swagger pode ser utilizada para explorar os endpoints disponíveis.

Também existe o arquivo:

```text
WorkFlow.API.http
```

utilizado para executar requisições HTTP manualmente durante o desenvolvimento.

---

# Testes

Para executar todos os testes:

```bash
dotnet test
```

Os testes estão divididos em:

```text
WorkFlow.UnitTests
WorkFlow.IntegrationTests
```

## Testes unitários

Validam principalmente:

- entidades;
- regras de domínio;
- handlers;
- políticas;
- validações;
- casos de sucesso;
- casos de erro.

---

## Testes de integração

Validam principalmente:

- Entity Framework Core;
- PostgreSQL;
- migrations;
- índices;
- relacionamentos;
- repositórios;
- isolamento entre Tenants;
- comportamento real de persistência.

Exemplo de regra protegida por testes:

```text
Um usuário pertencente ao Tenant B
não pode ser retornado por uma consulta realizada
dentro do Tenant A.
```

Os testes permanecerão no projeto durante toda sua evolução.

Futuramente serão executados automaticamente através de CI/CD antes de novas versões serem publicadas.

---

# Segurança

Alguns princípios adotados no projeto:

- senhas nunca são armazenadas em texto puro;
- secrets não são versionados;
- isolamento de Tenant ocorre no backend;
- identificadores públicos não são usados como autorização;
- respostas da API evitam exposição de informações internas;
- histórico importante deverá ser preservado;
- operações críticas deverão possuir testes;
- logs não deverão armazenar senhas, tokens ou secrets.

Autenticação e autorização ainda estão em desenvolvimento e serão adicionadas antes da utilização pública do sistema.

---

# Domínio planejado

As principais entidades identificadas são:

```text
Tenant
User
Project
ProjectMember
ProjectMemberPermission
ProjectTask
TaskCollaborator
TaskComment
ChatMessage
Notification
TaskHistory
ProjectHistory
```

---

# Projetos

O módulo de projetos deverá contemplar:

- criação;
- membros;
- responsável;
- permissões;
- planejamento;
- execução;
- pausa;
- conclusão;
- reabertura;
- arquivamento;
- histórico.

Status planejados incluem:

```text
Planning
InProgress
Paused
Validation
Completed
Cancelled
Archived
```

---

# Tarefas

O módulo de tarefas deverá contemplar:

- título;
- descrição;
- prioridade;
- responsável;
- colaboradores;
- prazo;
- comentários;
- histórico;
- validação;
- pausa;
- reabertura.

Fluxo principal planejado:

```text
Backlog
↓
Todo
↓
InProgress
↓
Validation
↓
Done
```

Outros estados poderão incluir:

```text
Paused
Cancelled
```

---

# Permissões por projeto

Além do papel global do usuário, projetos poderão possuir permissões específicas.

Exemplos:

```text
EDIT_PROJECT
MANAGE_PROJECT_MEMBERS
MANAGE_PROJECT_PERMISSIONS

CREATE_TASK
EDIT_TASK
CLAIM_TASK
ASSIGN_TASK

VALIDATE_TASK
SELF_VALIDATE_TASK
```

Isso permite que um usuário possua capacidades diferentes dependendo do projeto no qual participa.

---

# Notificações

As notificações serão persistentes.

Fluxo planejado:

```text
Evento
↓
Notification salva no banco
↓
tentativa de entrega em tempo real
```

SignalR será utilizado como mecanismo de entrega em tempo real, mas não como armazenamento.

Usuários offline deverão conseguir visualizar suas notificações quando retornarem ao sistema.

---

# Chat

O WorkFlow deverá possuir inicialmente comunicação rápida entre usuários.

A comunicação em tempo real utilizará SignalR.

O chat será considerado comunicação temporária e não substituirá:

- histórico;
- comentários de tarefas;
- documentação;
- registros auditáveis.

---

# SaaS

O projeto deverá futuramente evoluir para um SaaS comercial.

A camada comercial será separada conceitualmente do núcleo operacional.

Domínio operacional:

```text
Projetos
Tarefas
Usuários
Permissões
Comentários
Notificações
Chat
```

Domínio comercial futuro:

```text
Plan
Subscription
Payment
Invoice
BillingEvent
```

O objetivo futuro é permitir:

```text
Conhecer o produto
↓
Cadastrar empresa
↓
Criar conta proprietária
↓
Selecionar plano
↓
Realizar pagamento
↓
Ativar assinatura
↓
Convidar profissionais
↓
Utilizar o WorkFlow
```

Essa camada ainda não está implementada.

---

# Roadmap

## Base da aplicação

- [x] Arquitetura inicial
- [x] Domain
- [x] Application
- [x] Infrastructure
- [x] API
- [x] PostgreSQL
- [x] Entity Framework Core
- [x] Migrations
- [x] Testes unitários
- [x] Testes de integração

## Tenants

- [x] Criar empresa
- [x] Consultar empresa
- [x] Atualizar empresa
- [x] Ativar/desativar empresa
- [x] Listagem paginada
- [x] Busca
- [x] Filtro por status

## Usuários

- [x] Entidade de usuário
- [x] Perfis
- [x] Cadastro por Tenant
- [x] Hash de senha
- [x] Política de senha
- [x] Normalização de e-mail
- [x] Unicidade case-insensitive
- [x] Consulta individual por Tenant
- [x] Listagem de usuários
- [ ] Atualização de usuário
- [ ] Ativação/desativação de usuário
- [ ] Alteração de perfil
- [ ] Recuperação de senha

## Segurança

- [ ] Autenticação
- [ ] Login
- [ ] Tokens
- [ ] Autorização
- [ ] Policies
- [ ] Rate limiting
- [ ] MFA
- [ ] Recuperação de conta

## Projetos

- [x] Entidade e regras centrais de domínio
- [x] Membros de projeto no domínio
- [x] Permissões de projeto no domínio
- [ ] Casos de uso
- [ ] Endpoints
- [ ] Persistência completa dos fluxos
- [ ] Histórico
- [ ] Kanban

## Tarefas

- [x] Entidade e regras centrais de domínio
- [x] Colaboradores no domínio
- [x] Comentários no domínio
- [ ] Casos de uso
- [ ] Endpoints
- [ ] Fluxo operacional completo
- [ ] Validação
- [ ] Histórico

## Comunicação

- [ ] Notificações
- [ ] SignalR
- [ ] Chat
- [ ] Menções
- [ ] Feed de atividades

## Frontend

- [ ] React
- [ ] TypeScript
- [ ] Vite
- [ ] Design system
- [ ] Internacionalização
- [ ] Dashboard
- [ ] Kanban
- [ ] Área administrativa

## Infraestrutura

- [ ] Docker
- [ ] Docker Compose
- [ ] CI/CD
- [ ] GitHub Actions
- [ ] Deploy em cloud
- [ ] Health checks
- [ ] Logs estruturados
- [ ] Monitoramento
- [ ] Backup automatizado

## SaaS

- [ ] Site público
- [ ] Onboarding
- [ ] Proprietário do Tenant
- [ ] Planos
- [ ] Licenças
- [ ] Assinaturas
- [ ] Pagamentos
- [ ] Pix
- [ ] Cartão
- [ ] Renovação
- [ ] Cobrança
- [ ] Gestão da assinatura

---

# Princípios de desenvolvimento

O projeto segue alguns princípios:

1. Não adicionar tecnologias sem necessidade real.
2. Não armazenar secrets no código.
3. Não confiar no frontend como mecanismo de segurança.
4. Garantir isolamento entre Tenants no backend.
5. Separar regras de negócio da infraestrutura.
6. Manter regras importantes cobertas por testes.
7. Evitar exclusão destrutiva de dados que precisam de histórico.
8. Priorizar rastreabilidade.
9. Utilizar identificadores externos sem tratá-los como autorização.
10. Construir primeiro um sistema correto antes de aumentar a complexidade.

---

# Documentação

As decisões de produto e arquitetura são registradas na documentação do projeto.

Entre os assuntos documentados estão:

- modelo de domínio;
- requisitos;
- arquitetura;
- regras de negócio;
- multi-tenancy;
- permissões;
- estratégia de idiomas;
- evolução futura para SaaS.

---

# Status do projeto

```text
Em desenvolvimento
```

O núcleo backend está sendo construído de forma incremental.

Cada nova vertical normalmente passa por:

```text
Definição da regra
↓
Implementação
↓
Testes unitários
↓
Testes de integração
↓
Teste manual da API
↓
Revisão
↓
Commit
```

Último estado validado:

```text
.NET 10
Entity Framework Core 10
PostgreSQL
530 testes automatizados aprovados
```

---

# Licença

A licença definitiva do projeto ainda será definida.