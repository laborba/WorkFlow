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

O backend já possui uma base funcional para gerenciamento de empresas, usuários e projetos.

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
- atualização de nome e e-mail do usuário;
- ativação e desativação de usuários;
- alteração de perfil do usuário;
- isolamento do usuário pelo Tenant;
- perfis de usuário;
- validação de empresa existente;
- bloqueio de cadastro em empresa inativa;
- bloqueio de `SystemAdmin` vinculado a uma empresa;
- senha armazenada somente como hash;
- política de senha;
- normalização de e-mail;
- unicidade de e-mail case-insensitive dentro da mesma empresa;
- possibilidade de reutilizar o mesmo e-mail em empresas diferentes;
- listagem paginada de usuários por empresa;
- busca de usuários por nome ou e-mail;
- filtro por perfil;
- filtro por status ativo/inativo.

### Projetos

- criação de projetos dentro de um Tenant;
- criação permitida para `TenantAdmin` e `ProjectManager`;
- bloqueio de criação por `Member`;
- isolamento do projeto pelo Tenant autenticado;
- identificação do criador através do JWT;
- o cliente não pode escolher arbitrariamente o usuário criador;
- validação do estado atual do Tenant e do usuário criador;
- bloqueio de criação em Tenant inativo;
- bloqueio de criação por usuário inativo;
- projeto criado inicialmente com status `Planning`;
- nome, descrição e prazo opcional;
- criador adicionado automaticamente como membro ativo do projeto;
- quando o criador é `ProjectManager`, sua participação inicial recebe automaticamente as permissões `EditProject`, `ManageProjectMembers` e `ManageProjectPermissions`;
- `TenantAdmin` não depende de permissões específicas de projeto para as operações administrativas atualmente protegidas por bypass;
- criação de projeto, membro inicial e permissões iniciais protegida por transação;
- rollback em caso de falha durante qualquer etapa da persistência da criação;
- policy `ProjectCreation` para criação por `TenantAdmin` ou `ProjectManager`;
- combinação das policies `TenantAccess` e `ProjectCreation` no endpoint de criação;
- consulta individual de projeto por `PublicId`;
- consulta sempre isolada pelo Tenant informado na rota;
- `TenantAdmin` pode consultar qualquer projeto do próprio Tenant;
- `ProjectManager` pode consultar somente projetos nos quais seja membro ativo;
- `Member` pode consultar somente projetos nos quais seja membro ativo;
- `SystemAdmin` não possui acesso operacional aos projetos de um Tenant;
- projeto arquivado continua consultável pelo `TenantAdmin` e por membros ativos;
- usuário removido do projeto perde o acesso baseado em participação;
- usuário desativado no Tenant não pode acessar o projeto;
- resposta da consulta utiliza `PublicId` para criador e responsável, sem expor identificadores internos;
- listagem paginada de projetos do Tenant;
- `TenantAdmin` pode listar todos os projetos do próprio Tenant;
- `ProjectManager` pode listar somente projetos nos quais possua participação ativa;
- `Member` pode listar somente projetos nos quais possua participação ativa;
- projetos sem autorização simplesmente não aparecem na listagem;
- participação ativa é determinada por `ProjectMember.RemovedAt == null`;
- projetos arquivados ficam fora da listagem normal por padrão;
- projetos arquivados podem ser consultados explicitamente através do filtro de status;
- busca case-insensitive por nome ou descrição do projeto;
- filtro por status;
- filtro por responsável através de `ResponsibleUserPublicId`;
- paginação com tamanho máximo de 100 itens;
- ordenação padrão por projetos mais recentes;
- dados do responsável são resolvidos na própria consulta de persistência, evitando consultas adicionais por projeto;
- contagem e paginação são aplicadas somente após as regras de Tenant, participação e filtros;
- listagem sem resultados retorna `200 OK` com coleção vazia;
- atualização dos dados básicos do projeto;
- atualização de nome, descrição e prazo;
- possibilidade de remover o prazo enviando `DueDate = null`;
- `TenantAdmin` pode atualizar qualquer projeto do próprio Tenant sem depender de permissão específica;
- `ProjectManager` e `Member` podem atualizar projetos quando possuem participação ativa e a permissão `EditProject`;
- usuários sem `EditProject` não podem atualizar o projeto;
- `SystemAdmin` não possui acesso operacional à atualização de projetos de Tenant;
- projetos concluídos (`Completed`) continuam permitindo atualização dos dados básicos;
- projetos arquivados (`Archived`) não podem ser atualizados;
- status e responsável não são alterados pelo endpoint de atualização;
- atualização utiliza entidade rastreada pelo Entity Framework Core e persiste as alterações através do `UnitOfWork`;
- início de projetos através da transição `Planning → InProgress`;
- pausa de projetos através da transição `InProgress → Paused`;
- a pausa exige um motivo válido;
- retomada de projetos através da transição `Paused → InProgress`;
- ao retomar um projeto, o prazo atual pode ser mantido ou substituído por um novo `DueDate`;
- `TenantAdmin` pode iniciar, pausar e retomar qualquer projeto do próprio Tenant sem depender de permissão específica;
- `ProjectManager` e `Member` precisam possuir participação ativa e a permissão `EditProject` para iniciar, pausar ou retomar projetos;
- usuários sem participação ativa ou sem `EditProject` não podem executar essas transições;
- transições incompatíveis com o estado atual do projeto são rejeitadas;
- alterações de status utilizam a entidade rastreada pelo Entity Framework Core e são persistidas através do `UnitOfWork`;
- inclusão de usuários como membros de projetos;
- `TenantAdmin` pode adicionar membros a qualquer projeto do próprio Tenant sem depender de permissão específica;
- `ProjectManager` e `Member` podem adicionar membros quando possuem participação ativa e a permissão `ManageProjectMembers`;
- usuários sem `ManageProjectMembers` não podem adicionar membros;
- `SystemAdmin` não possui acesso operacional ao gerenciamento de membros;
- somente usuários existentes, ativos e pertencentes ao mesmo Tenant podem ser adicionados;
- projetos arquivados não aceitam novos membros;
- não pode existir mais de uma participação ativa do mesmo usuário no mesmo projeto;
- uma participação removida permanece preservada no histórico e não impede uma nova participação futura;
- o usuário autenticado que realizou a inclusão é registrado em `AddedByUserId`;
- a inclusão de um membro passa imediatamente a conceder o acesso baseado em participação ativa;
- listagem paginada dos membros ativos de um projeto;
- `TenantAdmin` pode listar membros de qualquer projeto do próprio Tenant;
- `ProjectManager` e `Member` podem listar membros somente quando possuem participação ativa no projeto;
- somente participações com `ProjectMember.RemovedAt == null` são retornadas;
- participações removidas permanecem preservadas no histórico, mas não aparecem na listagem operacional;
- projetos arquivados continuam permitindo a consulta de membros conforme as regras normais de autorização;
- busca case-insensitive de membros por nome ou e-mail;
- paginação com tamanho máximo de 100 itens;
- ordenação estável por data de inclusão e identificador interno;
- a consulta resolve os dados do membro e do usuário responsável pela inclusão diretamente na persistência;
- a resposta expõe somente identificadores públicos, incluindo `UserPublicId` e `AddedByUserPublicId`;
- remoção lógica de membros através de `ProjectMember.RemovedAt`;
- `TenantAdmin` pode remover membros de qualquer projeto do próprio Tenant sem depender de permissão específica;
- `ProjectManager` e `Member` podem remover membros quando possuem participação ativa e a permissão `ManageProjectMembers`;
- usuários sem `ManageProjectMembers` não podem remover membros;
- projetos arquivados não permitem remoção de membros;
- o usuário alvo pode ser removido mesmo que esteja inativo no Tenant;
- remover um usuário sem participação ativa retorna `ProjectMembers.NotActive`;
- participações removidas permanecem preservadas no histórico;
- um usuário removido perde imediatamente o acesso concedido pela participação;
- um usuário removido pode ser adicionado novamente futuramente, criando uma nova participação.
- concessão de permissões específicas para membros de projetos;
- listagem das permissões ativas de um membro;
- revogação lógica de permissões;
- histórico de concessões e revogações preservado;
- uma permissão revogada pode ser concedida novamente, criando um novo registro histórico;
- apenas uma concessão ativa da mesma permissão pode existir para a mesma participação;
- as permissões são vinculadas ao `ProjectMember`, e não diretamente ao usuário;
- remover e adicionar novamente um usuário ao projeto cria uma nova participação e não reativa permissões antigas;
- `TenantAdmin` pode gerenciar permissões em qualquer projeto do próprio Tenant;
- `ProjectManager` e `Member` precisam possuir participação ativa e a permissão `ManageProjectPermissions` para conceder, listar ou revogar permissões;
- a revogação de `ManageProjectPermissions` produz efeito imediato nas operações protegidas por essa permissão;
- projetos arquivados permitem consulta de permissões, mas não permitem concessão ou revogação;
- o usuário alvo precisa possuir participação ativa no projeto;
- permissões de usuário alvo inativo podem continuar sendo consultadas e revogadas;
- isolamento das operações de permissões pelo Tenant autenticado.


### Autenticação e autorização

- login de usuários vinculados a uma empresa;
- login global de `SystemAdmin` sem vínculo com Tenant;
- autenticação baseada em JWT Bearer;
- geração de access token assinado;
- validação de assinatura, `Issuer`, `Audience` e expiração;
- identificação do Tenant dentro do token para usuários vinculados a uma empresa;
- tokens de `SystemAdmin` não possuem a claim `tenant_public_id`;
- identificação do perfil do usuário através de claims;
- login com e-mail case-insensitive;
- isolamento multi-tenant durante a autenticação;
- bloqueio de login para empresas inativas;
- bloqueio de login para usuários inativos;
- resposta genérica para e-mail inexistente ou senha incorreta;
- endpoint protegido para consulta do usuário autenticado;
- autorização baseada em policies;
- policy `TenantAccess` para garantir acesso somente ao Tenant autenticado;
- policy `TenantAdmin` para operações administrativas da empresa;
- policy `SystemAdmin` para operações administrativas globais da plataforma;
- proteção contra acesso entre Tenants;
- endpoints administrativos de usuários restritos a `TenantAdmin`;
- endpoints de consulta de usuários restritos ao Tenant autenticado;
- endpoints administrativos de Tenant restritos a `SystemAdmin`;
- criação inicial controlada de `SystemAdmin` através de bootstrap configurado por User Secrets;
- bootstrap de `SystemAdmin` idempotente e desabilitável após o provisionamento inicial.

O login possui dois contextos:

```text
Usuário vinculado a Tenant
→ TenantPublicId informado
→ TenantAdmin, ProjectManager ou Member
→ JWT contém tenant_public_id

SystemAdmin
→ TenantPublicId ausente
→ autenticação global
→ JWT não contém tenant_public_id
```

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
969 testes automatizados aprovados
0 falhas
```

A solução possui testes unitários e testes de integração.

A autenticação e autorização possuem testes cobrindo, entre outros cenários:

- login válido de usuários vinculados a Tenant;
- credenciais inválidas;
- usuário inativo;
- empresa inativa;
- isolamento entre Tenants;
- busca de usuário por e-mail normalizado;
- login global de `SystemAdmin`;
- credenciais inválidas de `SystemAdmin`;
- bloqueio de `SystemAdmin` inativo;
- geração de JWT;
- claims do token;
- assinatura do token;
- validação de Issuer e Audience;
- rejeição de token assinado com chave incorreta;
- JWT de usuários de Tenant com claim `tenant_public_id`;
- JWT de `SystemAdmin` sem claim `tenant_public_id`;
- rejeição de JWT inconsistente entre perfil e Tenant;
- consulta de `/api/authentication/me` para usuários de Tenant;
- consulta de `/api/authentication/me` para `SystemAdmin`;
- acesso ao Tenant correto;
- bloqueio de acesso a outro Tenant;
- ausência da claim de Tenant;
- claim de Tenant inválida;
- rota sem Tenant;
- Tenant inválido na rota;
- autorização de `TenantAdmin`;
- autorização de `SystemAdmin`;
- bloqueio de operações administrativas para `Member`;
- bootstrap inicial de `SystemAdmin`;
- bootstrap desabilitado;
- bootstrap idempotente;
- validação das configurações do bootstrap;
- busca case-insensitive de `SystemAdmin` por e-mail;
- unicidade case-insensitive de e-mail de `SystemAdmin`;
- criação de projeto por `TenantAdmin`;
- criação de projeto por `ProjectManager`;
- bloqueio de criação por `Member`;
- validação de Tenant inexistente ou inativo na criação de projeto;
- validação de usuário criador inexistente ou inativo;
- identificação do criador através do usuário autenticado;
- inclusão automática do criador como `ProjectMember`;
- transação durante a criação de projeto e membro inicial;
- rollback quando a persistência do projeto falha;
- rollback quando a persistência do membro inicial falha;
- criação automática das permissões `EditProject`, `ManageProjectMembers` e `ManageProjectPermissions` para o `ProjectManager` criador;
- rollback quando a persistência das permissões iniciais falha;
- persistência real de `Project` e `ProjectMember` no PostgreSQL;
- consulta individual de projeto por `PublicId`;
- isolamento da consulta pelo Tenant;
- consulta de projeto por `TenantAdmin`;
- consulta de projeto por `ProjectManager` membro ativo;
- consulta de projeto por `Member` membro ativo;
- bloqueio de consulta para usuário sem participação ativa;
- consulta de projeto arquivado por membro ativo;
- validação de Tenant inexistente ou inativo na consulta;
- validação de usuário solicitante inexistente ou inativo;
- resolução do criador e do responsável através de identificadores internos sem expô-los pela API;
- consulta de `Project` por `PublicId` limitada ao Tenant no PostgreSQL;
- verificação de participação ativa em projeto no PostgreSQL;
- bloqueio de participação após remoção do `ProjectMember`;
- consulta de usuário por `Id` limitada ao Tenant no PostgreSQL;
- listagem paginada de projetos;
- listagem completa para `TenantAdmin`;
- restrição da listagem de `ProjectManager` e `Member` por participação ativa;
- exclusão de participações removidas da listagem;
- isolamento da listagem entre Tenants;
- exclusão de projetos arquivados da listagem padrão;
- consulta explícita de projetos arquivados por status;
- busca case-insensitive por nome ou descrição;
- filtro de projetos por responsável;
- retorno dos dados públicos do responsável na listagem;
- paginação e contagem total da listagem;
- validação de número e tamanho de página;
- validação de status inválido;
- validação de responsável com identificador público vazio;
- listagem vazia quando nenhum projeto estiver visível;
- testes reais da consulta paginada de projetos contra PostgreSQL;
- atualização de projeto por `TenantAdmin` com bypass administrativo;
- atualização de projeto por `ProjectManager` com participação ativa e `EditProject`;
- atualização de projeto por `Member` com participação ativa e `EditProject`;
- bloqueio de atualização para `ProjectManager` ou `Member` sem participação ativa;
- bloqueio de atualização para `ProjectManager` ou `Member` sem `EditProject`;
- atualização de projetos concluídos;
- bloqueio de atualização de projetos arquivados;
- validação de Tenant inexistente ou inativo durante a atualização;
- validação de usuário solicitante inexistente ou inativo;
- validação de projeto inexistente;
- validação dos identificadores públicos obrigatórios;
- validação de nome vazio;
- atualização e remoção de prazo;
- resolução do criador e do responsável na resposta da atualização;
- isolamento da busca do projeto pelo Tenant;
- persistência real das alterações do projeto no PostgreSQL;
- testes da camada HTTP para sucesso, autenticação, autorização e conflitos da atualização;
- início de projeto através da transição `Planning → InProgress`;
- pausa de projeto através da transição `InProgress → Paused`;
- retomada de projeto através da transição `Paused → InProgress`;
- manutenção do prazo atual durante a retomada quando nenhum novo prazo é informado;
- alteração do prazo durante a retomada quando um novo `DueDate` é informado;
- autorização de `TenantAdmin` para alterações de status através de bypass administrativo;
- autorização de `ProjectManager` e `Member` através de participação ativa e `EditProject`;
- bloqueio de alteração de status sem participação ativa ou sem `EditProject`;
- bloqueio de transições incompatíveis com o estado atual do projeto;
- testes da camada HTTP para início, pausa e retomada de projetos;
- testes de integração exercitando handlers, repositórios reais, Entity Framework Core e PostgreSQL nas alterações de status;
- confirmação da persistência real das transições de status e do novo prazo no PostgreSQL;
- inclusão de membro por `TenantAdmin` com bypass administrativo;
- inclusão de membro por `ProjectManager` e `Member` com participação ativa e `ManageProjectMembers`;
- bloqueio de inclusão para `ProjectManager` ou `Member` sem participação ativa;
- bloqueio de inclusão para `ProjectManager` ou `Member` sem `ManageProjectMembers`;
- bloqueio de inclusão em projeto arquivado;
- validação de usuário inexistente ou inativo durante a inclusão;
- validação de Tenant e projeto durante a inclusão;
- bloqueio de participação ativa duplicada;
- registro do usuário responsável pela inclusão através de `AddedByUserId`;
- persistência real de `ProjectMember` no PostgreSQL;
- restrição única para uma participação ativa por projeto e usuário;
- possibilidade de nova participação depois da remoção da participação anterior;
- testes da camada HTTP para autenticação, autorização, duplicidade e recursos inexistentes na inclusão de membros;
- listagem paginada dos membros ativos de um projeto;
- listagem por `TenantAdmin` sem necessidade de participação ativa;
- listagem por `ProjectManager` e `Member` com participação ativa;
- bloqueio da listagem para `ProjectManager` ou `Member` sem participação ativa;
- consulta de membros de projeto arquivado;
- exclusão de participações removidas da listagem;
- busca case-insensitive de membros por nome ou e-mail;
- paginação e ordenação estável da listagem de membros;
- resolução de `AddedByUserPublicId` diretamente na consulta de persistência;
- validação de Tenant, usuário solicitante e projeto durante a listagem;
- validação de número e tamanho de página;
- testes reais da listagem de membros contra PostgreSQL;
- testes da camada HTTP para sucesso, autenticação, autorização e recursos inexistentes na listagem de membros;
- remoção de membro por `TenantAdmin` com bypass administrativo;
- remoção de membro por `ProjectManager` e `Member` com participação ativa e `ManageProjectMembers`;
- bloqueio de remoção para `ProjectManager` ou `Member` sem participação ativa;
- bloqueio de remoção para `ProjectManager` ou `Member` sem `ManageProjectMembers`;
- bloqueio de remoção em projeto arquivado;
- remoção permitida mesmo quando o usuário alvo está inativo;
- validação de usuário alvo inexistente;
- validação de participação ativa inexistente ou já removida;
- preservação histórica da participação através de `RemovedAt`;
- persistência real da remoção lógica no PostgreSQL;
- confirmação de que membro removido deixa de ser considerado participante ativo;
- possibilidade de reinclusão depois da remoção;
- testes da camada HTTP para sucesso, autenticação, autorização, conflito e recursos inexistentes na remoção de membros.
- concessão de permissões específicas para membros de projetos;
- autorização de concessão por `TenantAdmin`;
- autorização de `ProjectManager` e `Member` através de participação ativa e `ManageProjectPermissions`;
- bloqueio de gerenciamento de permissões sem `ManageProjectPermissions`;
- bloqueio de concessão duplicada da mesma permissão ativa;
- possibilidade de nova concessão depois da revogação;
- vínculo das permissões à participação atual em `ProjectMember`;
- persistência real das permissões no PostgreSQL;
- persistência simultânea das três permissões padrão concedidas ao `ProjectManager` criador;
- consulta real de participação ativa através de `GetActiveAsync` no PostgreSQL;
- exclusão de participações removidas da consulta `GetActiveAsync`;
- restrição única para uma mesma permissão ativa por participação;
- listagem somente das permissões ativas;
- resolução de `GrantedByUserPublicId` na persistência;
- consulta de permissões em projeto arquivado;
- consulta de permissões de usuário alvo inativo;
- revogação lógica através de `RevokedAt` e `RevokedByUserId`;
- revogação permitida quando o usuário alvo está inativo;
- bloqueio de revogação de permissão inexistente ou já revogada;
- bloqueio de concessão e revogação em projeto arquivado;
- confirmação de que uma permissão revogada deixa imediatamente de ser considerada ativa;
- confirmação de que a revogação de `ManageProjectPermissions` remove imediatamente a capacidade de gerenciar permissões;
- testes da camada HTTP para concessão, listagem e revogação de permissões;
- testes de autenticação, autorização, isolamento entre Tenants e recursos inexistentes nas operações de permissões.

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
- autenticação JWT Bearer
- tokens assinados com HMAC SHA-256
- validação de `Issuer`
- validação de `Audience`
- validação de assinatura
- validação de expiração
- User Secrets para credenciais locais e chave JWT
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

A autenticação JWT e o login já estão implementados.

A API também possui uma camada inicial de autorização baseada em policies.

Atualmente estão implementadas:

```text
TenantAccess
TenantAdmin
SystemAdmin
ProjectCreation
```

A policy `TenantAccess` garante que o `TenantPublicId` presente no JWT corresponda ao Tenant informado na rota.

A policy `TenantAdmin` restringe operações administrativas a usuários com perfil `TenantAdmin`.

A policy `SystemAdmin` restringe operações administrativas globais da plataforma a usuários com perfil `SystemAdmin`.

A policy `ProjectCreation` permite a criação de projetos para usuários com perfil `TenantAdmin` ou `ProjectManager`.
Ela é utilizada em conjunto com a policy `TenantAccess`, garantindo que o usuário somente possa criar projetos dentro do próprio Tenant.

Atualmente, os endpoints administrativos de Tenant são protegidos pela policy `SystemAdmin`.

O gerenciamento inicial de permissões específicas por projeto já está implementado.

Membros de projeto podem possuir permissões próprias através de `ProjectMemberPermission`.

As permissões específicas já participam efetivamente da autorização de operações do projeto.

Atualmente:

- `ManageProjectPermissions` protege concessão, consulta e revogação de permissões;
- `EditProject` protege a atualização dos dados básicos e as operações de início, pausa e retomada do projeto;
- `ManageProjectMembers` protege inclusão e remoção de membros;
- `TenantAdmin` possui bypass administrativo nessas operações dentro do próprio Tenant;
- `ProjectManager` e `Member` precisam possuir participação ativa e a permissão exigida pela operação.

As demais permissões existentes no domínio serão integradas progressivamente aos fluxos de conclusão, reabertura, arquivamento e tarefas.

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

### Autorização dos endpoints de usuários

Os endpoints de usuários utilizam autenticação e autorização no backend.

Operações administrativas exigem:

```text
TenantAccess
+
TenantAdmin
```

Atualmente isso inclui:

```http
POST /api/tenants/{tenantPublicId}/users
PUT /api/tenants/{tenantPublicId}/users/{userPublicId}
PATCH /api/tenants/{tenantPublicId}/users/{userPublicId}/status
PATCH /api/tenants/{tenantPublicId}/users/{userPublicId}/role
```

Consultas de usuários exigem autenticação e acesso ao Tenant:

```text
TenantAccess
```

Atualmente isso inclui:

```http
GET /api/tenants/{tenantPublicId}/users
GET /api/tenants/{tenantPublicId}/users/{userPublicId}
```

Uma requisição sem token válido retorna:

```text
401 Unauthorized
```

Um usuário autenticado tentando acessar outro Tenant retorna:

```text
403 Forbidden
```

Um usuário autenticado sem o perfil necessário para uma operação administrativa retorna:

```text
403 Forbidden
```

---

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

Perfis disponíveis:

```text
1 = SystemAdmin
2 = TenantAdmin
3 = ProjectManager
4 = Member
```

Usuários criados dentro de uma empresa não podem utilizar o perfil:

```text
SystemAdmin
```

Nesse caso, a API retorna:

```text
Users.SystemAdminCannotBelongToTenant
```

Não é permitido cadastrar usuários em uma empresa inativa.

Nesse caso, a API retorna:

```text
Tenants.Inactive
```

O e-mail deve ser único dentro da mesma empresa, sem diferenciação entre maiúsculas e minúsculas.

Por exemplo:

```text
usuario@empresa.com
Usuario@Empresa.com
USUARIO@EMPRESA.COM
```

são considerados o mesmo e-mail dentro do mesmo Tenant.

O mesmo endereço de e-mail pode ser utilizado em empresas diferentes.

---

### Consultar usuário dentro de uma empresa

```http
GET /api/tenants/{tenantPublicId}/users/{userPublicId}
```

O usuário é localizado obrigatoriamente dentro do Tenant informado na rota.

Um `userPublicId` pertencente a outro Tenant retorna:

```text
Users.NotFound
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

### Atualizar usuário dentro de uma empresa

```http
PUT /api/tenants/{tenantPublicId}/users/{userPublicId}
```

Exemplo:

```json
{
  "name": "Usuário Atualizado",
  "email": "usuario-atualizado@empresa.com"
}
```

Campos disponíveis para atualização:

- `name`: nome do usuário;
- `email`: e-mail do usuário.

Este endpoint não altera:

- perfil;
- senha;
- status ativo/inativo.

O usuário é localizado obrigatoriamente dentro do Tenant informado na rota.

Um `userPublicId` pertencente a outro Tenant retorna:

```text
Users.NotFound
```

O e-mail deve continuar sendo único dentro da mesma empresa, sem diferenciação entre maiúsculas e minúsculas.

Caso outro usuário do mesmo Tenant já utilize o e-mail informado, a API retorna:

```text
Users.EmailAlreadyExists
```

Alterar apenas a capitalização do próprio e-mail é permitido.

Por exemplo:

```text
usuario@empresa.com
↓
Usuario@Empresa.com
```

Exemplo de resposta:

```json
{
  "publicId": "00000000-0000-0000-0000-000000000000",
  "tenantPublicId": "00000000-0000-0000-0000-000000000000",
  "name": "Usuário Atualizado",
  "email": "usuario-atualizado@empresa.com",
  "role": 4,
  "isActive": true,
  "createdAt": "2026-01-01T12:00:00Z",
  "updatedAt": "2026-01-02T15:30:00Z"
}
```

---

### Alterar status do usuário

```http
PATCH /api/tenants/{tenantPublicId}/users/{userPublicId}/status
```

Exemplo para desativar:

```json
{
  "isActive": false
}
```

Exemplo para ativar:

```json
{
  "isActive": true
}
```

O usuário é localizado obrigatoriamente dentro do Tenant informado na rota.

Um `userPublicId` pertencente a outro Tenant retorna:

```text
Users.NotFound
```

Não é permitido alterar o status de usuários quando a empresa estiver inativa.

Nesse caso, a API retorna:

```text
Tenants.Inactive
```

Exemplo de resposta:

```json
{
  "publicId": "00000000-0000-0000-0000-000000000000",
  "tenantPublicId": "00000000-0000-0000-0000-000000000000",
  "isActive": false,
  "updatedAt": "2026-08-31T12:00:00Z"
}
```

---

### Alterar perfil do usuário

```http
PATCH /api/tenants/{tenantPublicId}/users/{userPublicId}/role
```

Exemplo:

```json
{
  "role": 3
}
```

Perfis disponíveis:

```text
1 = SystemAdmin
2 = TenantAdmin
3 = ProjectManager
4 = Member
```

Usuários vinculados a uma empresa não podem assumir o perfil:

```text
SystemAdmin
```

Nesse caso, a API retorna:

```text
Users.SystemAdminCannotBelongToTenant
```

O usuário é localizado obrigatoriamente dentro do Tenant informado na rota.

Um `userPublicId` pertencente a outro Tenant retorna:

```text
Users.NotFound
```

Não é permitido alterar o perfil de usuários quando a empresa estiver inativa.

Nesse caso, a API retorna:

```text
Tenants.Inactive
```

Valores que não representam um perfil válido também são rejeitados.

Por exemplo:

```json
{
  "role": 99
}
```

Nesse caso, a API retorna:

```text
Validation.InvalidArgument
```

Exemplo de resposta:

```json
{
  "publicId": "00000000-0000-0000-0000-000000000000",
  "tenantPublicId": "00000000-0000-0000-0000-000000000000",
  "role": 3,
  "updatedAt": "2026-08-31T12:00:00Z"
}
```

---

## Projetos

### Autorização dos endpoints de projetos

A criação de projetos exige as policies:

```text
TenantAccess
+
ProjectCreation
```

A policy `TenantAccess` garante que o Tenant informado na rota corresponda ao Tenant presente no JWT.

A policy `ProjectCreation` permite a criação para:

```text
TenantAdmin
ProjectManager
```

e bloqueia:

```text
Member
SystemAdmin
```

Portanto, na criação:

```text
Sem autenticação
→ 401 Unauthorized

Usuário autenticado tentando utilizar outro Tenant
→ 403 Forbidden

Member
→ 403 Forbidden

SystemAdmin
→ 403 Forbidden

TenantAdmin no próprio Tenant
→ permitido

ProjectManager no próprio Tenant
→ permitido
```

A consulta individual e a listagem utilizam:

```text
TenantAccess
```

e aplicam as regras específicas de acesso aos projetos no backend.

As regras atuais são:

```text
TenantAdmin
→ pode consultar qualquer projeto do próprio Tenant

ProjectManager
→ pode consultar somente se for membro ativo do projeto

Member
→ pode consultar somente se for membro ativo do projeto

SystemAdmin
→ não possui acesso operacional a projetos de Tenant
```

Estar no mesmo Tenant não é suficiente para `ProjectManager` ou `Member`.

O usuário precisa possuir uma participação ativa em:

```text
ProjectMember
```

Na consulta individual, caso contrário:

```text
403 Forbidden
Projects.ViewNotAllowed
```

Na listagem, projetos sem participação ativa simplesmente não são retornados.

Projetos arquivados continuam consultáveis individualmente seguindo as mesmas regras de autorização.

A atualização dos dados básicos do projeto também utiliza:

```text
TenantAccess
```

e aplica autorização específica no backend.

As regras atuais de atualização são:

```text
TenantAdmin
→ pode atualizar qualquer projeto do próprio Tenant

ProjectManager
→ precisa possuir participação ativa + EditProject

Member
→ precisa possuir participação ativa + EditProject

SystemAdmin
→ não possui acesso operacional a projetos de Tenant
```

Para `ProjectManager` e `Member`, a autorização exige simultaneamente:

```text
ProjectMember.RemovedAt == null
+
ProjectMemberPermission.EditProject ativa
```

Projetos nos estados:

```text
Planning
InProgress
Paused
Completed
```

podem ter seus dados básicos atualizados.

Projetos com status:

```text
Archived
```

não podem ser atualizados.

Nesse caso, a API retorna:

```text
409 Conflict
Projects.Archived
```

Um `ProjectManager` ou `Member` sem participação ativa ou sem `EditProject` recebe:

```text
403 Forbidden
Projects.UpdateNotAllowed
```

O endpoint de atualização altera somente:

```text
Name
Description
DueDate
```

O status e o responsável do projeto não são alterados por esse endpoint.

O conhecimento do `PublicId` de um projeto não concede acesso ao recurso.

---

### Criar projeto

```http
POST /api/tenants/{tenantPublicId}/projects
```

Exemplo de requisição:

```json
{
  "name": "Novo Projeto",
  "description": "Descrição do projeto",
  "dueDate": "2026-12-31T18:00:00Z"
}
```

Campos disponíveis:

- `name`: nome do projeto;
- `description`: descrição opcional;
- `dueDate`: prazo opcional.

O usuário criador não é informado no corpo da requisição.

Ele é identificado através da claim:

```text
sub
```

do JWT autenticado.

Isso impede que o cliente escolha arbitrariamente outro usuário como criador do projeto.

O backend também consulta novamente o usuário no banco para validar:

```text
usuário pertence ao Tenant
usuário está ativo
perfil atual permite criação
```

O projeto é criado inicialmente com:

```text
Status = Planning
ResponsibleUserId = null
```

O criador é automaticamente adicionado como membro ativo do projeto.

Quando o criador possui perfil `ProjectManager`, sua participação inicial também recebe automaticamente as permissões:

```text
EditProject
ManageProjectMembers
ManageProjectPermissions
```

Essas permissões permitem que o `ProjectManager` recém-criado consiga administrar o projeto sem depender de uma concessão manual inicial.

O `TenantAdmin` não recebe essas permissões automaticamente porque possui bypass administrativo nas operações atualmente protegidas.

A criação utiliza uma transação para garantir consistência:

```text
cria Project
↓
salva e obtém Project.Id
↓
cria ProjectMember para o criador
↓
salva e obtém ProjectMember.Id
↓
se o criador for ProjectManager,
cria EditProject,
ManageProjectMembers e
ManageProjectPermissions
↓
salva permissões
↓
Commit
```

Caso alguma gravação falhe:

```text
Rollback
```

evitando que um projeto fique parcialmente persistido sem seu membro inicial ou, no caso de `ProjectManager`, sem suas permissões administrativas iniciais.

Exemplo de resposta:

```json
{
  "publicId": "00000000-0000-0000-0000-000000000000",
  "tenantPublicId": "00000000-0000-0000-0000-000000000000",
  "createdByUserPublicId": "00000000-0000-0000-0000-000000000000",
  "name": "Novo Projeto",
  "description": "Descrição do projeto",
  "status": 1,
  "dueDate": "2026-12-31T18:00:00Z",
  "createdAt": "2026-09-03T17:25:59Z"
}
```

---

### Consultar projeto por PublicId

```http
GET /api/tenants/{tenantPublicId}/projects/{projectPublicId}
```

A consulta exige:

```text
TenantAccess
```

Regras:

```text
TenantAdmin
→ pode consultar qualquer projeto do próprio Tenant

ProjectManager
→ precisa possuir participação ativa

Member
→ precisa possuir participação ativa
```

Participação ativa significa:

```text
ProjectMember.RemovedAt == null
```

Caso um `ProjectManager` ou `Member` não possua participação ativa:

```text
403 Forbidden
Projects.ViewNotAllowed
```

Projetos arquivados continuam consultáveis segundo as mesmas regras.

A resposta utiliza apenas identificadores públicos para relações externas.

Exemplo:

```json
{
  "publicId": "00000000-0000-0000-0000-000000000000",
  "tenantPublicId": "00000000-0000-0000-0000-000000000000",
  "createdByUserPublicId": "00000000-0000-0000-0000-000000000000",
  "responsibleUserPublicId": null,
  "name": "Projeto de Teste",
  "description": "Descrição do projeto",
  "status": 1,
  "dueDate": "2026-12-31T18:00:00Z",
  "createdAt": "2026-09-08T12:00:00Z",
  "updatedAt": null,
  "archivedAt": null
}
```

---

### Listar projetos

```http
GET /api/tenants/{tenantPublicId}/projects
```

A listagem exige:

```text
TenantAccess
```

e aplica as regras de visibilidade dos projetos no backend.

Regras atuais:

```text
TenantAdmin
→ visualiza todos os projetos do próprio Tenant

ProjectManager
→ visualiza somente projetos nos quais possui participação ativa

Member
→ visualiza somente projetos nos quais possui participação ativa

SystemAdmin
→ não possui acesso operacional aos projetos do Tenant
```

Para `ProjectManager` e `Member`, participação ativa significa:

```text
ProjectMember.RemovedAt == null
```

Projetos nos quais o usuário não possui autorização simplesmente não aparecem na listagem.

A restrição de acesso é aplicada antes da contagem e da paginação.

Parâmetros disponíveis:

```text
pageNumber
pageSize
search
status
responsibleUserPublicId
```

Regras de paginação:

```text
pageNumber >= 1
pageSize entre 1 e 100
```

A busca utiliza:

```text
Name
Description
```

sem diferenciação entre letras maiúsculas e minúsculas.

O filtro `status` utiliza os valores atuais de `ProjectStatus`:

```text
1 = Planning
2 = InProgress
3 = Paused
4 = Completed
5 = Archived
```

Projetos `Archived` ficam fora da listagem padrão.

Para consultar projetos arquivados explicitamente:

```http
GET /api/tenants/{tenantPublicId}/projects?status=5
```

O filtro por responsável utiliza:

```text
responsibleUserPublicId
```

Um identificador válido que não corresponda a nenhum responsável retorna uma listagem vazia, e não um erro de usuário inexistente.

Exemplo:

```http
GET /api/tenants/{tenantPublicId}/projects?pageNumber=1&pageSize=20&search=api&status=2
```

Exemplo de resposta:

```json
{
  "items": [
    {
      "publicId": "00000000-0000-0000-0000-000000000000",
      "name": "Projeto API",
      "description": "Descrição do projeto",
      "status": 2,
      "responsibleUserPublicId": "00000000-0000-0000-0000-000000000000",
      "responsibleUserName": "Usuário Responsável",
      "dueDate": "2026-12-31T18:00:00Z",
      "createdAt": "2026-09-08T12:00:00Z",
      "updatedAt": null,
      "archivedAt": null
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1
}
```

Projetos sem responsável retornam:

```json
{
  "responsibleUserPublicId": null,
  "responsibleUserName": null
}
```

Quando nenhum projeto estiver visível:

```text
200 OK
items = []
totalCount = 0
totalPages = 0
```

A ordenação padrão da persistência utiliza:

```text
CreatedAt DESC
Id DESC
```

mantendo os projetos mais recentes primeiro e uma ordenação determinística para paginação.

---

### Atualizar projeto

```http
PUT /api/tenants/{tenantPublicId}/projects/{projectPublicId}
```

A atualização exige:

```text
TenantAccess
```

e aplica as regras de autorização específicas do projeto no backend.

As regras atuais são:

```text
TenantAdmin
→ pode atualizar qualquer projeto do próprio Tenant

ProjectManager
→ precisa possuir participação ativa e EditProject

Member
→ precisa possuir participação ativa e EditProject

SystemAdmin
→ não possui acesso operacional aos projetos do Tenant
```

Para `ProjectManager` e `Member`, a autorização exige:

```text
ProjectMember.RemovedAt == null
+
EditProject ativa na participação atual
```

Campos disponíveis para atualização:

```text
name
description
dueDate
```

Exemplo de requisição:

```json
{
  "name": "Projeto Atualizado",
  "description": "Descrição atualizada",
  "dueDate": "2027-01-31T18:00:00Z"
}
```

O campo `dueDate` pode ser removido enviando:

```json
{
  "name": "Projeto Atualizado",
  "description": "Descrição atualizada",
  "dueDate": null
}
```

Este endpoint não altera:

```text
Status
ResponsibleUser
CreatedByUser
```

Alterações de status e responsável serão tratadas por casos de uso específicos.

Projetos nos estados:

```text
Planning
InProgress
Paused
Completed
```

podem ter seus dados básicos atualizados.

Projetos `Archived` não podem ser atualizados.

Nesse caso, a API retorna:

```text
409 Conflict
Projects.Archived
```

Um `ProjectManager` ou `Member` sem participação ativa ou sem `EditProject` recebe:

```text
403 Forbidden
Projects.UpdateNotAllowed
```

Exemplo de resposta:

```json
{
  "publicId": "00000000-0000-0000-0000-000000000000",
  "tenantPublicId": "00000000-0000-0000-0000-000000000000",
  "createdByUserPublicId": "00000000-0000-0000-0000-000000000000",
  "responsibleUserPublicId": null,
  "name": "Projeto Atualizado",
  "description": "Descrição atualizada",
  "status": 1,
  "dueDate": "2027-01-31T18:00:00Z",
  "createdAt": "2026-09-08T12:00:00Z",
  "updatedAt": "2026-09-08T15:00:00Z",
  "archivedAt": null
}
```

---

### Iniciar projeto

```http
PATCH /api/tenants/{tenantPublicId}/projects/{projectPublicId}/start
```

A operação exige `TenantAccess`.

Regras atuais:

```text
TenantAdmin
→ pode iniciar qualquer projeto do próprio Tenant

ProjectManager
→ precisa possuir participação ativa e EditProject

Member
→ precisa possuir participação ativa e EditProject

SystemAdmin
→ não possui acesso operacional aos projetos do Tenant
```

A transição permitida é:

```text
Planning
↓
InProgress
```

Tentar iniciar um projeto em outro estado retorna:

```text
409 Conflict
Projects.InvalidStatusTransition
```

Um `ProjectManager` ou `Member` sem autorização recebe:

```text
403 Forbidden
Projects.StatusChangeNotAllowed
```

---

### Pausar projeto

```http
PATCH /api/tenants/{tenantPublicId}/projects/{projectPublicId}/pause
```

Exemplo:

```json
{
  "reason": "Aguardando retorno do cliente."
}
```

A transição permitida é:

```text
InProgress
↓
Paused
```

O motivo da pausa é obrigatório.

A autorização segue as mesmas regras de início do projeto: `TenantAdmin` possui bypass administrativo, enquanto `ProjectManager` e `Member` precisam possuir participação ativa e `EditProject`.

---

### Retomar projeto

```http
PATCH /api/tenants/{tenantPublicId}/projects/{projectPublicId}/resume
```

A transição permitida é:

```text
Paused
↓
InProgress
```

O prazo existente pode ser mantido:

```json
{
  "newDueDate": null
}
```

ou substituído:

```json
{
  "newDueDate": "2027-04-30T18:00:00Z"
}
```

A autorização segue as mesmas regras dos demais fluxos de status.

As respostas de início, pausa e retomada retornam o estado atualizado do projeto, incluindo:

```text
PublicId
TenantPublicId
Status
DueDate
UpdatedAt
ArchivedAt
```

Transições incompatíveis com o estado atual retornam:

```text
409 Conflict
Projects.InvalidStatusTransition
```

Usuários sem autorização recebem:

```text
403 Forbidden
Projects.StatusChangeNotAllowed
```

---

### Adicionar membro ao projeto

```http
POST /api/tenants/{tenantPublicId}/projects/{projectPublicId}/members
```

A operação exige:

```text
TenantAccess
```

e aplica autorização específica do projeto no backend.

As regras atuais são:

```text
TenantAdmin
→ pode adicionar membros a qualquer projeto do próprio Tenant

ProjectManager
→ precisa possuir participação ativa e ManageProjectMembers

Member
→ precisa possuir participação ativa e ManageProjectMembers

SystemAdmin
→ não possui acesso operacional aos projetos do Tenant
```

Para `ProjectManager` e `Member`, a autorização exige:

```text
ProjectMember.RemovedAt == null
+
ManageProjectMembers ativa na participação atual
```

Exemplo de requisição:

```json
{
  "userPublicId": "00000000-0000-0000-0000-000000000000"
}
```

O usuário informado deve:

```text
existir
pertencer ao mesmo Tenant
estar ativo
```

Projetos `Archived` não permitem inclusão de novos membros.

Não pode existir mais de uma participação ativa do mesmo usuário no mesmo projeto.

Nesse caso, a API retorna:

```text
409 Conflict
ProjectMembers.AlreadyActive
```

Uma participação removida continua armazenada no histórico.

Como a unicidade considera somente participações com:

```text
RemovedAt == null
```

um usuário removido poderá futuramente ser adicionado novamente, criando uma nova participação sem apagar a anterior.

O usuário autenticado responsável pela inclusão é registrado em:

```text
AddedByUserId
```

Um `ProjectManager` ou `Member` sem participação ativa ou sem `ManageProjectMembers` recebe:

```text
403 Forbidden
ProjectMembers.AddNotAllowed
```

Exemplo de resposta:

```json
{
  "projectPublicId": "00000000-0000-0000-0000-000000000000",
  "userPublicId": "00000000-0000-0000-0000-000000000000",
  "addedByUserPublicId": "00000000-0000-0000-0000-000000000000",
  "addedAt": "2026-09-09T12:00:00Z"
}
```

A inclusão de um usuário como membro ativo passa a conceder imediatamente o acesso baseado em participação às operações que utilizam essa regra.

---

### Listar membros do projeto

```http
GET /api/tenants/{tenantPublicId}/projects/{projectPublicId}/members
```

A consulta exige:

```text
TenantAccess
```

e aplica as regras de visibilidade do projeto no backend.

As regras atuais são:

```text
TenantAdmin
→ pode listar membros de qualquer projeto do próprio Tenant

ProjectManager
→ pode listar somente quando possui participação ativa no projeto

Member
→ pode listar somente quando possui participação ativa no projeto

SystemAdmin
→ não possui acesso operacional aos projetos do Tenant
```

Para `ProjectManager` e `Member`, participação ativa significa:

```text
ProjectMember.RemovedAt == null
```

Caso não exista participação ativa:

```text
403 Forbidden
Projects.ViewNotAllowed
```

A listagem retorna somente participações ativas.

Participações removidas permanecem armazenadas para histórico, porém não aparecem na listagem operacional.

Projetos arquivados continuam permitindo consulta dos membros, respeitando as mesmas regras de autorização.

Parâmetros disponíveis:

```text
pageNumber
pageSize
search
```

Regras de paginação:

```text
pageNumber >= 1
pageSize entre 1 e 100
```

A busca utiliza:

```text
User.Name
User.Email
```

sem diferenciação entre letras maiúsculas e minúsculas.

Exemplo:

```http
GET /api/tenants/{tenantPublicId}/projects/{projectPublicId}/members?pageNumber=1&pageSize=20&search=lucas
```

Exemplo de resposta:

```json
{
  "items": [
    {
      "userPublicId": "00000000-0000-0000-0000-000000000000",
      "name": "Usuário do Projeto",
      "email": "usuario@empresa.com",
      "role": 4,
      "addedAt": "2026-09-09T12:00:00Z",
      "addedByUserPublicId": "00000000-0000-0000-0000-000000000000"
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1
}
```

A resposta não expõe identificadores internos como:

```text
ProjectMember.Id
ProjectId
UserId
AddedByUserId
```

A ordenação utilizada é:

```text
AddedAt ASC
Id ASC
```

garantindo uma paginação determinística e mantendo os membros na ordem em que foram adicionados ao projeto.

Quando nenhum membro corresponder à consulta:

```text
200 OK
items = []
totalCount = 0
totalPages = 0
```

---

### Remover membro do projeto

```http
DELETE /api/tenants/{tenantPublicId}/projects/{projectPublicId}/members/{userPublicId}
```

A operação exige:

```text
TenantAccess
```

e aplica autorização específica do projeto no backend.

As regras atuais são:

```text
TenantAdmin
→ pode remover membros de qualquer projeto do próprio Tenant

ProjectManager
→ precisa possuir participação ativa e ManageProjectMembers

Member
→ precisa possuir participação ativa e ManageProjectMembers

SystemAdmin
→ não possui acesso operacional aos projetos do Tenant
```

Para `ProjectManager` e `Member`, a autorização exige:

```text
ProjectMember.RemovedAt == null
+
ManageProjectMembers ativa na participação atual
```

Um `ProjectManager` ou `Member` sem participação ativa ou sem `ManageProjectMembers` recebe:

```text
403 Forbidden
ProjectMembers.RemoveNotAllowed
```

Projetos arquivados não permitem remoção de membros.

Nesse caso:

```text
409 Conflict
Projects.Archived
```

O usuário alvo precisa existir dentro do mesmo Tenant, porém não precisa estar ativo.

Isso permite remover de projetos um usuário que tenha sido desativado administrativamente no Tenant.

A remoção é lógica:

```text
ProjectMember.RemovedAt = UTC
```

Nenhum registro histórico é excluído.

Quando o usuário não possui participação ativa no projeto, inclusive quando já foi removido anteriormente:

```text
404 Not Found
ProjectMembers.NotActive
```

Exemplo de resposta:

```json
{
  "projectPublicId": "00000000-0000-0000-0000-000000000000",
  "userPublicId": "00000000-0000-0000-0000-000000000000",
  "removedAt": "2026-09-09T17:00:00Z"
}
```

A remoção faz com que o usuário deixe imediatamente de possuir os acessos concedidos pela participação ativa.

Como o registro anterior permanece preservado, o mesmo usuário pode ser adicionado novamente no futuro, criando uma nova participação.

O criador do projeto também pode deixar de ser membro.

O vínculo histórico através de:

```text
CreatedByUserId
```

permanece preservado independentemente da participação atual do usuário.

---

### Conceder permissão a membro do projeto

`POST /api/tenants/{tenantPublicId}/projects/{projectPublicId}/members/{userPublicId}/permissions`

A operação exige `TenantAccess`.

Regras atuais:

- `TenantAdmin` pode conceder permissões em qualquer projeto do próprio Tenant;
- `ProjectManager` e `Member` precisam possuir participação ativa no projeto;
- `ProjectManager` e `Member` precisam possuir `ManageProjectPermissions`;
- `SystemAdmin` não possui acesso operacional aos projetos de Tenant.

Exemplo de requisição:

    {
      "permission": 7
    }

A permissão é vinculada à participação ativa atual do usuário em `ProjectMember`.

O usuário alvo precisa existir no mesmo Tenant, estar ativo e possuir participação ativa no projeto.

Projetos `Archived` não permitem novas concessões.

Não pode existir mais de uma concessão ativa da mesma permissão para a mesma participação.

Nesse caso:

`409 Conflict`

`ProjectMemberPermissions.AlreadyActive`

Uma permissão anteriormente revogada pode ser concedida novamente. A nova concessão cria um novo registro e preserva o histórico anterior.

---

### Listar permissões de membro do projeto

`GET /api/tenants/{tenantPublicId}/projects/{projectPublicId}/members/{userPublicId}/permissions`

A consulta retorna somente permissões ativas da participação atual.

Regras atuais:

- `TenantAdmin` pode consultar permissões em qualquer projeto do próprio Tenant;
- `ProjectManager` e `Member` precisam possuir participação ativa;
- `ProjectManager` e `Member` precisam possuir `ManageProjectPermissions`.

Projetos arquivados continuam permitindo a consulta.

O usuário alvo pode estar inativo no Tenant, desde que ainda possua participação ativa no projeto.

Permissões revogadas permanecem preservadas na persistência, mas não aparecem nessa consulta operacional.

---

### Revogar permissão de membro do projeto

`DELETE /api/tenants/{tenantPublicId}/projects/{projectPublicId}/members/{userPublicId}/permissions/{permission}`

Regras atuais:

- `TenantAdmin` pode revogar permissões em qualquer projeto do próprio Tenant;
- `ProjectManager` e `Member` precisam possuir participação ativa;
- `ProjectManager` e `Member` precisam possuir `ManageProjectPermissions`.

Projetos `Archived` não permitem revogação.

O usuário alvo pode estar inativo no Tenant.

A revogação é lógica e registra:

- `RevokedAt`;
- `RevokedByUserId`.

A concessão original permanece preservada.

Tentar revogar uma permissão que não está ativa retorna:

`404 Not Found`

`ProjectMemberPermissions.NotActive`

A revogação produz efeito imediatamente nas operações que utilizam a permissão.

Isso já ocorre com `ManageProjectPermissions`: após sua revogação, `ProjectManager` ou `Member` deixa imediatamente de poder conceder, listar ou revogar permissões do projeto.

---


## Autenticação

### Realizar login

```http
POST /api/authentication/login
```

O endpoint de login atende dois contextos diferentes:

```text
Usuário vinculado a Tenant
→ informa TenantPublicId

SystemAdmin
→ não informa TenantPublicId
```

---

### Login de usuário vinculado a Tenant

Usuários com os perfis:

```text
TenantAdmin
ProjectManager
Member
```

pertencem obrigatoriamente a um Tenant.

Exemplo de requisição:

```json
{
  "tenantPublicId": "00000000-0000-0000-0000-000000000000",
  "email": "usuario@empresa.com",
  "password": "uma senha longa e segura"
}
```

Nesse cenário, o login utiliza:

```text
TenantPublicId
+
e-mail
+
senha
```

O `TenantPublicId` é necessário porque o mesmo endereço de e-mail pode existir em empresas diferentes.

A busca pelo e-mail não diferencia letras maiúsculas e minúsculas.

Por exemplo:

```text
usuario@empresa.com
USUARIO@EMPRESA.COM
Usuario@Empresa.com
```

são tratados como o mesmo e-mail dentro daquele Tenant.

O usuário somente pode autenticar através da empresa à qual pertence.

Conhecer o e-mail e a senha de um usuário não permite autenticá-lo utilizando o `TenantPublicId` de outra empresa.

Exemplo de resposta válida:

```json
{
  "accessToken": "eyJ...",
  "expiresAt": "2026-08-31T18:00:00Z",
  "userPublicId": "00000000-0000-0000-0000-000000000000",
  "tenantPublicId": "00000000-0000-0000-0000-000000000000",
  "name": "Usuário Teste",
  "email": "usuario@empresa.com",
  "role": 4
}
```

O JWT de um usuário vinculado a Tenant contém as claims:

```text
sub
email
name
role
tenant_public_id
```

---

### Login de SystemAdmin

O `SystemAdmin` representa um administrador global da plataforma WorkFlow.

Ele não pertence a nenhum Tenant.

Para esse perfil, o login é realizado com:

```text
TenantPublicId = null
+
e-mail
+
senha
```

Exemplo de requisição:

```json
{
  "tenantPublicId": null,
  "email": "admin@workflow.com",
  "password": "uma senha longa e segura"
}
```

Nesse cenário, o backend procura exclusivamente uma conta que possua:

```text
TenantId = null
Role = SystemAdmin
```

Usuários vinculados a um Tenant não podem ser encontrados por esse fluxo de autenticação.

Exemplo de resposta válida:

```json
{
  "accessToken": "eyJ...",
  "expiresAt": "2026-08-31T18:00:00Z",
  "userPublicId": "00000000-0000-0000-0000-000000000000",
  "tenantPublicId": null,
  "name": "Administrador do Sistema",
  "email": "admin@workflow.com",
  "role": 1
}
```

O JWT do `SystemAdmin` contém as claims:

```text
sub
email
name
role
```

O token de `SystemAdmin` não contém:

```text
tenant_public_id
```

A geração do JWT valida a consistência entre o perfil do usuário e o Tenant.

As regras são:

```text
SystemAdmin
→ não pode possuir Tenant no token

TenantAdmin
ProjectManager
Member
→ precisam possuir Tenant no token
```

---

### Erros de autenticação

Quando o e-mail não existe ou a senha está incorreta, a API retorna a mesma resposta:

```text
401 Unauthorized
Authentication.InvalidCredentials
```

Isso evita revelar se determinada conta existe.

Um usuário inativo com senha correta retorna:

```text
403 Forbidden
Authentication.UserInactive
```

Porém, se a senha estiver incorreta, mesmo que a conta esteja inativa, a resposta continua sendo:

```text
401 Unauthorized
Authentication.InvalidCredentials
```

Para usuários vinculados a Tenant, uma empresa inexistente retorna:

```text
404 Not Found
Tenants.NotFound
```

Uma empresa inativa retorna:

```text
409 Conflict
Tenants.Inactive
```

---

### Access token

O access token possui duração configurável.

No ambiente de desenvolvimento, a configuração utilizada atualmente é:

```text
60 minutos
```

O token é assinado utilizando:

```text
HMAC SHA-256
```

A API valida:

```text
Issuer
Audience
assinatura
expiração
```

O tempo adicional de tolerância para expiração está configurado como:

```text
ClockSkew = Zero
```

Portanto, tokens expirados não são aceitos além do horário configurado.

A chave utilizada para assinatura do JWT não é armazenada no repositório.

Durante o desenvolvimento local ela é configurada através de:

```text
User Secrets
```

---

### Consultar usuário autenticado

```http
GET /api/authentication/me
```

Este endpoint exige autenticação.

A requisição deve enviar:

```http
Authorization: Bearer <accessToken>
```

O token é validado antes da execução do endpoint.

Uma requisição sem token válido retorna:

```text
401 Unauthorized
```

Para um usuário vinculado a Tenant, a resposta possui:

```json
{
  "userPublicId": "00000000-0000-0000-0000-000000000000",
  "tenantPublicId": "00000000-0000-0000-0000-000000000000",
  "name": "Usuário Teste",
  "email": "usuario@empresa.com",
  "role": 4
}
```

Para um `SystemAdmin`, a resposta possui:

```json
{
  "userPublicId": "00000000-0000-0000-0000-000000000000",
  "tenantPublicId": null,
  "name": "Administrador do Sistema",
  "email": "admin@workflow.com",
  "role": 1
}
```

O endpoint também valida a consistência entre o perfil e a claim de Tenant.

Portanto:

```text
SystemAdmin sem tenant_public_id
→ válido

SystemAdmin com tenant_public_id
→ inválido

TenantAdmin sem tenant_public_id
→ inválido

ProjectManager sem tenant_public_id
→ inválido

Member sem tenant_public_id
→ inválido
```

Atualmente esse endpoint utiliza as informações já validadas presentes nas claims do JWT.

---

### Provisionamento inicial de SystemAdmin

O primeiro `SystemAdmin` pode ser criado através de um bootstrap controlado na inicialização da API.

As configurações do bootstrap são fornecidas fora do código-fonte, utilizando:

```text
User Secrets
ou
variáveis de ambiente
```

As credenciais reais não devem ser versionadas no repositório.

Quando o bootstrap está habilitado:

```text
Aplicação inicia
↓
valida as configurações
↓
aplica a política de senha
↓
procura o SystemAdmin pelo e-mail normalizado
```

Se o administrador já existir:

```text
nenhum novo usuário é criado
nenhuma senha é alterada
```

Se ainda não existir:

```text
senha
↓
PasswordHasher
↓
PasswordHash
↓
User com TenantId = null
↓
Role = SystemAdmin
↓
PostgreSQL
```

O bootstrap é idempotente.

Após o provisionamento inicial, ele pode ser desabilitado e a senha removida da configuração.

O bootstrap não é um endpoint HTTP e não permite cadastro público de `SystemAdmin`.

---

### Autorização de SystemAdmin

A API possui a policy:

```text
SystemAdmin
```

Ela exige:

```text
usuário autenticado
+
Role = SystemAdmin
```

Os endpoints administrativos globais de Tenant são protegidos por essa policy.

Atualmente isso inclui:

```http
POST /api/tenants

GET /api/tenants

GET /api/tenants/{publicId}

PUT /api/tenants/{publicId}

PATCH /api/tenants/{publicId}/status
```

O comportamento esperado é:

```text
Sem token
→ 401 Unauthorized

Usuário autenticado sem perfil SystemAdmin
→ 403 Forbidden

SystemAdmin autenticado
→ operação permitida
```

O futuro fluxo público de cadastro de empresas do WorkFlow SaaS será tratado separadamente e não utilizará esses endpoints administrativos como cadastro público.

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

Authentication.InvalidCredentials
Authentication.UserInactive

Projects.NotFound
Projects.CreationNotAllowed
Projects.ViewNotAllowed
Projects.UpdateNotAllowed
Projects.StatusChangeNotAllowed
Projects.InvalidStatusTransition
Projects.Archived

ProjectMembers.AddNotAllowed
ProjectMembers.AlreadyActive
ProjectMembers.RemoveNotAllowed
ProjectMembers.NotActive

ProjectMemberPermissions.ManageNotAllowed
ProjectMemberPermissions.AlreadyActive
ProjectMemberPermissions.NotActive

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

As requisições HTTP utilizadas nos testes manuais estão organizadas por módulo:

```text
src/WorkFlow.API/Http
│
├── 01-Authentication.http
├── 02-Tenants.http
├── 03-Users.http
├── 04-Projects.http
├── http-client.env.json
└── http-client.env.json.user
```

Os arquivos possuem responsabilidades separadas:

```text
01-Authentication.http
→ login, autenticação e consulta de /me

02-Tenants.http
→ cadastro, consulta, atualização, status e listagem de Tenants

03-Users.http
→ cadastro, consulta, listagem, atualização, status e perfil de usuários

04-Projects.http
→ criação, consulta individual, listagem, atualização, início, pausa e retomada de projetos; inclusão, listagem e remoção de membros; concessão, listagem e revogação de permissões; filtros e autorização
```

As variáveis compartilhadas e identificadores públicos utilizados nos testes ficam em:

```text
http-client.env.json
```

Valores locais ou sensíveis, como access tokens, ficam em:

```text
http-client.env.json.user
```

O arquivo `http-client.env.json.user` não é versionado pelo Git.

Tokens JWT reais, senhas e outros secrets não devem ser adicionados aos arquivos `.http` versionados.

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

Alguns princípios e recursos adotados no projeto:

- senhas nunca são armazenadas em texto puro;
- secrets não são versionados;
- a chave de assinatura JWT é mantida fora do código-fonte;
- autenticação utiliza JWT Bearer;
- tokens possuem assinatura criptográfica;
- `Issuer`, `Audience`, assinatura e expiração são validados;
- `ClockSkew` está configurado como zero;
- tokens expirados não são aceitos;
- isolamento de Tenant ocorre no backend;
- identificadores públicos não são utilizados como autorização;
- conhecer um `PublicId` não concede acesso ao recurso;
- a claim de perfil identifica o papel global do usuário;
- usuários vinculados a Tenant possuem a claim `tenant_public_id`;
- tokens de `SystemAdmin` não possuem a claim `tenant_public_id`;
- a geração do JWT valida a consistência entre perfil e Tenant;
- a policy `TenantAccess` valida o Tenant autenticado contra o Tenant da rota;
- a policy `TenantAdmin` protege operações administrativas dentro de uma empresa;
- a policy `SystemAdmin` protege operações administrativas globais da plataforma;
- a policy `ProjectCreation` permite a criação de projetos para `TenantAdmin` e `ProjectManager`;
- a criação de projetos combina `TenantAccess` e `ProjectCreation`, garantindo que o usuário crie projetos somente dentro do próprio Tenant;
- a consulta individual de projetos exige `TenantAccess`;
- `TenantAdmin` pode consultar qualquer projeto do próprio Tenant;
- `ProjectManager` e `Member` somente podem consultar projetos nos quais possuam participação ativa;
- o backend valida a participação através de `ProjectMember`;
- estar no mesmo Tenant não concede automaticamente acesso a um projeto;
- projetos arquivados continuam sujeitos às mesmas regras de autorização de consulta;
- a listagem de projetos também exige `TenantAccess`;
- `TenantAdmin` pode listar todos os projetos do próprio Tenant;
- `ProjectManager` e `Member` somente recebem na listagem projetos nos quais possuam participação ativa;
- projetos sem autorização não são incluídos na resposta nem na contagem total;
- a filtragem por participação ocorre no backend antes da paginação;
- projetos arquivados não aparecem na listagem padrão e precisam ser solicitados explicitamente através do filtro de status;
- a atualização dos dados básicos de projetos exige `TenantAccess`;
- `TenantAdmin` pode atualizar qualquer projeto do próprio Tenant sem depender de permissão específica;
- `ProjectManager` e `Member` precisam possuir participação ativa e `EditProject` para atualizar projetos;
- `EditProject` também protege as operações de início, pausa e retomada do projeto;
- a ausência de `EditProject` bloqueia tanto a atualização dos dados básicos quanto essas transições de status;
- projetos concluídos podem ter seus dados básicos atualizados;
- projetos arquivados não podem ser atualizados;
- status e responsável não são alterados pelo endpoint de atualização de dados básicos;
- a inclusão de membros também exige `TenantAccess`;
- `TenantAdmin` pode adicionar membros a qualquer projeto do próprio Tenant sem depender de permissão específica;
- `ProjectManager` e `Member` precisam possuir participação ativa e `ManageProjectMembers` para adicionar membros;
- somente usuários ativos do mesmo Tenant podem ser adicionados;
- projetos arquivados não aceitam novos membros;
- o banco impede mais de uma participação ativa para o mesmo projeto e usuário;
- participações removidas são preservadas para histórico e permitem nova inclusão futura;
- a listagem de membros também exige `TenantAccess`;
- `TenantAdmin` pode listar membros de qualquer projeto do próprio Tenant;
- `ProjectManager` e `Member` somente podem listar membros quando possuem participação ativa no projeto;
- somente participações ativas são retornadas pela listagem operacional;
- projetos arquivados continuam permitindo consulta de membros conforme as regras de autorização;
- dados internos de `ProjectMember` não são expostos pela API;
- a remoção de membros também exige `TenantAccess`;
- `TenantAdmin` pode remover membros de qualquer projeto do próprio Tenant sem depender de permissão específica;
- `ProjectManager` e `Member` precisam possuir participação ativa e `ManageProjectMembers` para remover membros;
- projetos arquivados não permitem remoção de membros;
- a remoção de membros é lógica através de `RemovedAt`;
- usuários inativos ainda podem ser removidos dos projetos;
- participações removidas continuam preservadas para histórico;
- usuários removidos do projeto perdem imediatamente o acesso concedido pela participação;
- permissões específicas de projeto são vinculadas ao `ProjectMember`;
- permissões revogadas permanecem preservadas para histórico;
- somente uma concessão ativa da mesma permissão pode existir por participação;
- `TenantAdmin` pode gerenciar permissões nos projetos do próprio Tenant;
- `ProjectManager` e `Member` somente podem gerenciar permissões quando possuem participação ativa e `ManageProjectPermissions`;
- `ManageProjectPermissions` já é utilizada efetivamente na autorização de concessão, listagem e revogação de permissões;
- a revogação de `ManageProjectPermissions` produz efeito imediato;
- projetos arquivados não permitem concessão ou revogação de permissões, mas permitem consulta;
- permissões associadas a uma participação removida não são transferidas para uma futura nova participação do mesmo usuário;
- usuários desativados no Tenant não podem consultar projetos;
- `SystemAdmin` não possui acesso operacional aos projetos de um Tenant;
- endpoints administrativos de Tenant são restritos a `SystemAdmin`;
- endpoints administrativos de usuários exigem `TenantAccess` e `TenantAdmin`;
- consultas de usuários exigem `TenantAccess`;
- requisições sem autenticação em endpoints protegidos retornam `401 Unauthorized`;
- usuários autenticados sem autorização retornam `403 Forbidden`;
- respostas de autenticação evitam revelar desnecessariamente a existência de usuários;
- respostas da API evitam exposição de informações internas;
- logs não deverão armazenar senhas, tokens completos ou secrets;
- operações críticas deverão possuir testes automatizados;
- histórico importante deverá ser preservado.

O login suporta atualmente dois contextos:

```text
Usuário vinculado a Tenant
→ TenantPublicId obrigatório
→ JWT com tenant_public_id

SystemAdmin
→ TenantPublicId ausente
→ JWT sem tenant_public_id
```

A API impede a geração de tokens inconsistentes:

```text
SystemAdmin + Tenant
→ inválido

TenantAdmin sem Tenant
→ inválido

ProjectManager sem Tenant
→ inválido

Member sem Tenant
→ inválido
```

A autorização atualmente utiliza:

```text
Roles
Policies
TenantAccess
TenantAdmin
SystemAdmin
ProjectCreation
Participação ativa em ProjectMember
```

O `SystemAdmin` pode ser provisionado inicialmente através de um bootstrap controlado por configuração segura.

As credenciais do bootstrap:

```text
não são armazenadas no código-fonte
não são versionadas
```

Durante o desenvolvimento local, podem ser fornecidas através de:

```text
User Secrets
```

O bootstrap é idempotente e pode ser desabilitado após a criação inicial da conta administrativa.

A autorização por projeto já possui regras baseadas em participação ativa para consulta individual, listagem de projetos, atualização e gerenciamento de membros.

O gerenciamento de `ProjectMemberPermission` também está implementado, incluindo concessão, listagem e revogação.

As permissões `ManageProjectPermissions`, `EditProject` e `ManageProjectMembers` já participam efetivamente da autorização para `ProjectManager` e `Member`.

`EditProject` também é utilizada nos fluxos de início, pausa e retomada de projetos.

A autorização continuará sendo evoluída integrando `CompleteProject`, `ReopenProject`, `ArchiveProject` e as permissões específicas de tarefas aos respectivos casos de uso.

```text
Permissões específicas por recurso
Responsabilidade pelo recurso
Estado do domínio
```

Funcionalidades futuras de segurança incluem:

```text
Refresh tokens
Rate limiting
MFA
Recuperação segura de conta
Revogação e gestão de sessões
```

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
- consulta individual;
- listagem;
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

Status atuais incluem:

```text
Planning
InProgress
Paused
Completed
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

Além do perfil global, um usuário pode possuir permissões específicas em cada projeto no qual participa.

As permissões são associadas a `ProjectMemberPermission`, que pertence a um `ProjectMember`.

Isso significa que a permissão pertence à participação do usuário naquele projeto, e não globalmente ao usuário.

As permissões atuais são:

- `1 = EditProject`
- `2 = ManageProjectMembers`
- `3 = ManageProjectPermissions`
- `4 = CompleteProject`
- `5 = ReopenProject`
- `6 = ArchiveProject`
- `7 = CreateTask`
- `8 = EditTask`
- `9 = ClaimTask`
- `10 = AssignTask`
- `11 = ManageTaskCollaborators`
- `12 = CancelTask`
- `13 = ReopenTask`
- `14 = ValidateTask`
- `15 = SelfValidateTask`

O gerenciamento inicial dessas permissões já está implementado através de:

- `GrantProjectMemberPermission`;
- `ListProjectMemberPermissions`;
- `RevokeProjectMemberPermission`.

A persistência mantém histórico de concessões e revogações.

Uma concessão registra:

- `Permission`;
- `GrantedAt`;
- `GrantedByUserId`.

Uma revogação registra:

- `RevokedAt`;
- `RevokedByUserId`.

A revogação não exclui o registro.

Somente uma mesma permissão ativa pode existir para uma determinada participação.

Depois da revogação, a concessão anterior permanece no histórico e uma nova concessão da mesma permissão pode criar um novo registro ativo.

Se um usuário for removido e posteriormente adicionado novamente ao projeto, o `ProjectMember` antigo permanece no histórico com suas permissões anteriores, enquanto a nova participação não herda automaticamente essas permissões.

Regras atuais para gerenciamento:

- `TenantAdmin` pode conceder, listar e revogar permissões no próprio Tenant;
- `ProjectManager` e `Member` precisam possuir participação ativa;
- `ProjectManager` e `Member` precisam possuir `ManageProjectPermissions`.

Atualmente já são permissões operacionais efetivas:

- `EditProject`: permite atualização dos dados básicos e início, pausa e retomada do projeto;
- `ManageProjectMembers`: permite inclusão e remoção de membros;
- `ManageProjectPermissions`: permite concessão, consulta e revogação de permissões.

Quando um `ProjectManager` cria um novo projeto, sua participação inicial recebe automaticamente:

- `EditProject`;
- `ManageProjectMembers`;
- `ManageProjectPermissions`.

O `TenantAdmin` não depende dessas permissões específicas nas operações atualmente cobertas por bypass administrativo.

As demais permissões serão integradas aos respectivos casos de uso conforme os fluxos de conclusão, reabertura, arquivamento e tarefas forem implementados.

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
- [x] Atualização de usuário
- [x] Ativação/desativação de usuário
- [x] Alteração de perfil
- [ ] Recuperação de senha

## Segurança

- [x] Autenticação
- [x] Login
- [x] Login global de `SystemAdmin`
- [x] Tokens
- [x] Autorização inicial
- [x] Policies
- [x] Isolamento de Tenant por JWT
- [x] Policy `TenantAccess`
- [x] Policy `TenantAdmin`
- [x] Policy `SystemAdmin`
- [x] Policy `ProjectCreation`
- [x] Proteção dos endpoints administrativos de Tenant
- [x] Bootstrap inicial de `SystemAdmin`
- [x] Autorização inicial de consulta de projetos por participação ativa
- [x] Autorização da listagem de projetos por participação ativa
- [x] Gerenciamento inicial de permissões por projeto
- [x] Autorização de gerenciamento através de `ManageProjectPermissions`
- [x] Integração de `EditProject` à atualização de projetos
- [x] Integração de `ManageProjectMembers` à inclusão e remoção de membros
- [x] Permissões administrativas iniciais automáticas para `ProjectManager` criador
- [x] Integração de `EditProject` aos fluxos de início, pausa e retomada de projetos
- [ ] Integração de `CompleteProject`, `ReopenProject` e `ArchiveProject` aos respectivos fluxos
- [ ] Integração das permissões específicas aos fluxos de tarefas
- [ ] Rate limiting
- [ ] MFA
- [ ] Recuperação de conta

## Projetos

- [x] Entidade e regras centrais de domínio
- [x] Membros de projeto no domínio
- [x] Permissões de projeto no domínio
- [x] Criação de projeto
- [x] Inclusão automática do criador como membro
- [x] Autorização de criação por `TenantAdmin` e `ProjectManager`
- [x] Persistência da criação de projeto e membro inicial
- [x] Consulta individual por `PublicId`
- [x] Autorização de consulta por participação ativa no projeto
- [x] Isolamento da consulta pelo Tenant
- [x] Consulta de projeto arquivado respeitando as permissões atuais
- [x] Listagem
- [x] Paginação da listagem
- [x] Busca por nome ou descrição
- [x] Filtro por status
- [x] Filtro por responsável
- [x] Autorização da listagem por participação ativa
- [x] Exclusão de projetos arquivados da listagem padrão
- [x] Atualização
- [x] Gerenciamento de membros — inclusão, listagem e remoção
- [x] Gerenciamento de permissões — concessão, listagem e revogação
- [x] Autorização de atualização através de `EditProject`
- [x] Autorização de inclusão e remoção de membros através de `ManageProjectMembers`
- [x] Concessão automática das permissões administrativas iniciais ao `ProjectManager` criador
- [x] Início de projeto — `Planning → InProgress`
- [x] Pausa de projeto — `InProgress → Paused`
- [x] Retomada de projeto — `Paused → InProgress`
- [ ] Conclusão de projeto
- [ ] Reabertura de projeto
- [ ] Arquivamento e restauração de projeto
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
Autenticação JWT Bearer
Login de usuários por Tenant
Login global de SystemAdmin
Autorização baseada em policies
Isolamento multi-tenant por JWT
Policy TenantAccess
Policy TenantAdmin
Policy SystemAdmin
Policy ProjectCreation
Endpoints administrativos de Tenant protegidos
Bootstrap inicial de SystemAdmin
Criação de projetos por TenantAdmin e ProjectManager
Criador adicionado automaticamente como membro do projeto
ProjectManager criador recebe automaticamente EditProject, ManageProjectMembers e ManageProjectPermissions
Criação de projeto protegida por transação
Consulta individual de projetos por PublicId
Consulta de projetos isolada pelo Tenant
TenantAdmin pode consultar qualquer projeto do próprio Tenant
ProjectManager e Member dependem de participação ativa no projeto
Projetos arquivados permanecem consultáveis conforme as regras de autorização
Listagem paginada de projetos
TenantAdmin lista todos os projetos do próprio Tenant
ProjectManager e Member listam somente projetos com participação ativa
Projetos arquivados ficam ocultos da listagem padrão
Projetos arquivados podem ser listados explicitamente por status
Busca de projetos por nome ou descrição
Filtro de projetos por status
Filtro de projetos por responsável
Paginação da listagem de projetos
Atualização dos dados básicos de projetos
TenantAdmin pode atualizar qualquer projeto do próprio Tenant
ProjectManager e Member podem atualizar projetos com participação ativa e EditProject
Usuários sem EditProject não podem atualizar projetos
Projetos Completed permitem atualização dos dados básicos
Projetos Archived bloqueiam atualização
Atualização de nome, descrição e prazo
Remoção de prazo através de DueDate null
Inclusão de membros em projetos
TenantAdmin pode adicionar membros a qualquer projeto do próprio Tenant
ProjectManager e Member podem adicionar membros com participação ativa e ManageProjectMembers
Participações ativas duplicadas são bloqueadas
Participações removidas são preservadas e permitem futura reinclusão
Inclusão de membro registra o usuário responsável pela operação
Listagem paginada de membros ativos dos projetos
TenantAdmin pode listar membros de qualquer projeto do próprio Tenant
ProjectManager e Member podem listar membros quando possuem participação ativa
Participações removidas não aparecem na listagem operacional
Busca de membros por nome ou e-mail
Projetos arquivados permanecem com membros consultáveis conforme autorização
Listagem expõe somente identificadores públicos das relações
Remoção lógica de membros através de RemovedAt
TenantAdmin pode remover membros de qualquer projeto do próprio Tenant
ProjectManager e Member podem remover membros com participação ativa e ManageProjectMembers
Projetos Archived bloqueiam remoção de membros
Usuários inativos podem ser removidos dos projetos
Membro removido perde imediatamente o acesso baseado em participação
Participações removidas permanecem preservadas para histórico
Usuários removidos podem ser adicionados novamente futuramente
Concessão de permissões específicas a membros de projeto
Listagem das permissões ativas de membros
Revogação lógica de permissões
Histórico de concessão e revogação preservado
Permissões vinculadas à participação em ProjectMember
Permissões revogadas podem ser concedidas novamente
TenantAdmin pode gerenciar permissões em projetos do próprio Tenant
ProjectManager e Member dependem de participação ativa e ManageProjectPermissions para gerenciar permissões
ManageProjectPermissions possui efeito efetivo na autorização
EditProject possui efeito efetivo na autorização de atualização
ManageProjectMembers possui efeito efetivo na autorização de inclusão e remoção de membros
EditProject possui efeito efetivo nos fluxos de início, pausa e retomada de projetos
Projetos podem ser iniciados através da transição Planning → InProgress
Projetos InProgress podem ser pausados
Projetos Paused podem ser retomados para InProgress
Retomada pode manter o prazo atual ou definir um novo DueDate
TenantAdmin possui bypass administrativo nas operações atuais de status
ProjectManager e Member dependem de participação ativa e EditProject para início, pausa e retomada
Transições incompatíveis com o estado atual são bloqueadas
Persistência das alterações de status validada com PostgreSQL real
Endpoints PATCH de start, pause e resume validados manualmente
Projetos Archived permitem consulta, mas bloqueiam concessão e revogação de permissões
Usuários alvo inativos podem ter permissões consultadas e revogadas
SystemAdmin não possui acesso operacional aos projetos de Tenant
Requisições HTTP manuais organizadas por módulo
969 testes automatizados aprovados
0 falhas
```

---

# Licença

A licença definitiva do projeto ainda será definida.