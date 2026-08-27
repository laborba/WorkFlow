# WorkFlow — Project Context
## Checkpoint — Final da Aula 2

### 1. Objetivo do projeto

O **WorkFlow** será uma plataforma genérica e multiempresa para gerenciamento de projetos, tarefas, equipes, comunicação interna e acompanhamento de atividades.

O projeto possui dois objetivos simultâneos:

1. Ser utilizado de forma real por empresas para organização de seus projetos.
2. Servir como projeto profissional de portfólio demonstrando conhecimentos exigidos em vagas .NET/React.

O sistema não possui relação com Horus-W ou outros sistemas existentes.

---

# 2. Metodologia de desenvolvimento

O desenvolvimento está sendo conduzido como uma aula.

Fluxo:

1. Explicação do conceito.
2. Explicação do motivo da decisão.
3. Definição da arquitetura/regra.
4. Implementação pelo aluno.
5. Revisão.
6. Testes.
7. Git.
8. Documentação.

O objetivo não é simplesmente fornecer código pronto, mas ensinar o motivo das decisões.

---

# 3. Stack definida

## Backend

- C#
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- Npgsql
- PostgreSQL

**Dapper não será utilizado.**

O objetivo é aprender EF Core e o acesso a banco de dados dentro do ecossistema .NET.

Posteriormente também será estudado ADO.NET para entender as abstrações utilizadas pelo EF Core.

## Frontend

- React
- TypeScript
- Vite

Posteriormente serão utilizadas bibliotecas apropriadas conforme necessidade.

## Tempo real

- SignalR

## Testes

- xUnit
- testes unitários
- testes de integração

## Infraestrutura futura

- Docker
- Docker Compose
- GitHub Actions
- CI/CD
- deploy em cloud

## Tecnologias futuras, somente se justificadas

- Redis
- MongoDB

Não adicionar tecnologias somente para enriquecer currículo.

---

# 4. Arquitetura planejada

```text
WorkFlow/
│
├── src/
│   ├── WorkFlow.API/
│   ├── WorkFlow.Application/
│   ├── WorkFlow.Domain/
│   └── WorkFlow.Infrastructure/
│
├── tests/
│   ├── WorkFlow.UnitTests/
│   └── WorkFlow.IntegrationTests/
│
├── frontend/
├── docs/
└── WorkFlow.sln
```

Responsabilidades:

```text
Domain
→ entidades e regras centrais do negócio

Application
→ casos de uso e coordenação da aplicação

Infrastructure
→ EF Core, PostgreSQL e serviços externos

API
→ HTTP, autenticação, autorização e entrada da aplicação
```

A camada Domain não deve conhecer EF Core, PostgreSQL, HTTP ou React.

---

# 5. Multi-tenancy

O WorkFlow será **multiempresa**.

Cada empresa será um `Tenant`.

```text
WorkFlow
├── Tenant A
│   ├── Users
│   ├── Projects
│   └── Chat
│
└── Tenant B
    ├── Users
    ├── Projects
    └── Chat
```

Dados de um Tenant jamais poderão ser acessados por usuários de outro Tenant.

O isolamento deverá obrigatoriamente ser validado no backend.

Esconder informações no React não é considerado mecanismo de segurança.

---

# 6. Identificadores

Foi discutida a utilização de tokens aleatórios em vez de IDs para dificultar enumeração.

Decisão:

- relacionamentos internos continuarão utilizando Foreign Keys;
- identificadores não devem ser utilizados como mecanismo de autorização;
- poderá existir um `PublicId` não sequencial para exposição externa;
- descobrir um `PublicId` nunca concede acesso ao recurso.

Conceitualmente:

```text
Id
→ identificação/relacionamento interno

PublicId
→ identificação externa
```

A escolha entre `int`, `long`, `Guid`, UUID ou ULID ainda não foi realizada.

---

# 7. Segurança

## Senhas de usuários

Nunca armazenar senha original ou senha criptografada reversível.

Persistir somente:

```text
PasswordHash
```

utilizando mecanismo seguro de hashing de senha.

## Credenciais de infraestrutura

Não poderão ficar:

- hardcoded;
- versionadas no Git;
- dentro do código-fonte com valores reais.

Serão utilizados posteriormente:

- User Secrets;
- environment variables;
- mecanismos de secrets em produção.

## Autorização

Deverá considerar, conforme a operação:

```text
Usuário autenticado
+
Tenant
+
Role global
+
Project Membership
+
Project Permissions
+
Responsabilidade pelo recurso
+
Estado do domínio
```

---

# 8. Roles globais

Foram definidos quatro papéis:

```text
SYSTEM_ADMIN
TENANT_ADMIN
PROJECT_MANAGER
MEMBER
```

## SYSTEM_ADMIN

Administrador da plataforma WorkFlow.

Não representa normalmente um funcionário de um Tenant.

## TENANT_ADMIN

Administrador de uma empresa específica.

Pode administrar usuários e projetos de seu próprio Tenant.

## PROJECT_MANAGER

Gerencia projetos nos quais possui atribuição administrativa.

Ser PROJECT_MANAGER não concede administração automática de todos os projetos do Tenant.

## MEMBER

Usuário comum.

Executa tarefas, comenta e participa dos projetos aos quais foi adicionado.

Pode receber permissões adicionais por projeto.

---

# 9. Autorização por projeto

Além do Role global existirão permissões ligadas ao `ProjectMember`.

Permissões iniciais candidatas:

```text
EDIT_PROJECT
MANAGE_PROJECT_MEMBERS
MANAGE_PROJECT_PERMISSIONS
COMPLETE_PROJECT
REOPEN_PROJECT
ARCHIVE_PROJECT

CREATE_TASK
EDIT_TASK
CLAIM_TASK
ASSIGN_TASK
MANAGE_TASK_COLLABORATORS
CANCEL_TASK
REOPEN_TASK

VALIDATE_TASK
SELF_VALIDATE_TASK
```

Não utilizar dezenas de propriedades booleanas diretamente dentro de `User`.

Entidade conceitual:

```text
ProjectMemberPermission
- Id
- ProjectMemberId
- Permission
- GrantedAt
- GrantedByUserId
- RevokedAt
- RevokedByUserId
```

Permissão revogada pode permanecer armazenada para histórico.

---

# 10. Auto-validação

Foi descartada a regra de que o responsável nunca pode validar sua própria tarefa.

O sistema precisa funcionar também em empresas pequenas.

Foram definidos dois conceitos:

```text
VALIDATE_TASK
→ pode validar tarefas

SELF_VALIDATE_TASK
→ pode validar uma tarefa da qual é o próprio responsável
```

No MVP:

- TENANT_ADMIN poderá auto-validar;
- PROJECT_MANAGER responsável pela gestão poderá auto-validar;
- MEMBER poderá receber essa permissão explicitamente.

Mesmo em auto-validação, o fluxo deverá continuar:

```text
IN_PROGRESS
→ VALIDATION
→ DONE
```

A pessoa não pula diretamente de `IN_PROGRESS` para `DONE`.

---

# 11. Entidades principais identificadas

```text
Tenant
User
Project
ProjectMember
ProjectMemberPermission
Task
TaskCollaborator
TaskComment
ChatMessage
Notification
TaskHistory
ProjectHistory
```

Ainda poderão surgir outras entidades durante o desenvolvimento.

---

# 12. Tenant

Estrutura conceitual atual:

```text
Tenant
- Id
- PublicId
- Name
- RegistrationNumber
- Email
- Phone
- IsActive
- CreatedAt
- UpdatedAt
```

`RegistrationNumber` foi preferido conceitualmente em vez de amarrar o domínio exclusivamente a CNPJ/CPF.

---

# 13. User

Estrutura conceitual:

```text
User
- Id
- PublicId
- TenantId
- Name
- Email
- PasswordHash
- Role
- IsActive
- CreatedAt
- UpdatedAt
```

SYSTEM_ADMIN poderá ser uma exceção sem Tenant.

Usuários comuns deverão pertencer a um Tenant.

Usuários desativados não serão fisicamente removidos, preservando histórico e autoria.

---

# 14. Project

Estrutura conceitual atual:

```text
Project
- Id
- PublicId
- TenantId
- Name
- Description
- ResponsibleUserId
- CreatedByUserId
- Status
- DueDate
- CreatedAt
- UpdatedAt
- ArchivedAt
```

`ResponsibleUserId` é opcional.

O criador do projeto não é necessariamente seu responsável nem proprietário permanente.

---

# 15. ProjectMember

Representa participação em um projeto.

```text
ProjectMember
- Id
- ProjectId
- UserId
- AddedAt
- AddedByUserId
- RemovedAt
```

Um mesmo usuário não poderá possuir duas participações ativas no mesmo projeto.

Remoção não precisa apagar fisicamente o histórico da participação.

---

# 16. Status de Project

```text
PLANNING
IN_PROGRESS
PAUSED
COMPLETED
ARCHIVED
```

Fluxo principal:

```text
PLANNING → IN_PROGRESS
IN_PROGRESS → PAUSED
PAUSED → IN_PROGRESS
IN_PROGRESS → COMPLETED
COMPLETED → IN_PROGRESS
COMPLETED → ARCHIVED
PLANNING → ARCHIVED
```

`COMPLETED → IN_PROGRESS` representa reabertura.

Reabertura exige justificativa.

Projeto arquivado deverá ser restaurado antes de voltar à operação.

`RestoreProject()` e `ReopenProject()` são conceitos diferentes.

---

# 17. Projeto PAUSED

Pausar projeto:

- exige motivo;
- não altera automaticamente os estados das Tasks;
- bloqueia operações de execução enquanto o projeto permanecer pausado;
- não altera automaticamente o prazo.

Ao retomar:

```text
○ manter prazo atual
○ alterar prazo
```

Se alterar, o usuário deverá selecionar explicitamente a nova data.

---

# 18. Conclusão de Project

Um projeto só poderá ser `COMPLETED` se todas as suas Tasks estiverem:

```text
DONE
ou
CANCELLED
```

Não permitir projeto `COMPLETED` com tarefas abertas.

Projeto concluído poderá ser reaberto mediante justificativa.

---

# 19. Arquivamento de Project

Projeto não será normalmente excluído.

Arquivamento mantém:

- dados;
- histórico;
- tarefas;
- informações para relatórios.

Projeto `ARCHIVED` deverá ficar essencialmente read-only até ser restaurado.

---

# 20. Task

Estrutura conceitual atual:

```text
Task
- Id
- PublicId
- ProjectId
- Title
- Description
- Status
- Priority
- ResponsibleUserId
- ValidatorUserId
- DueDate
- CreatedByUserId
- CreatedAt
- UpdatedAt
- ArchivedAt
```

Uma Task poderá existir sem responsável.

Uma Task pode ter somente um responsável principal.

Pode possuir vários colaboradores.

---

# 21. TaskPriority

```text
LOW
MEDIUM
HIGH
CRITICAL
```

---

# 22. TaskStatus

```text
BACKLOG
TODO
IN_PROGRESS
PAUSED
VALIDATION
DONE
CANCELLED
```

---

# 23. Máquina de estados da Task

Fluxo principal:

```text
BACKLOG → TODO
TODO → IN_PROGRESS
IN_PROGRESS → VALIDATION
VALIDATION → DONE
```

Outras transições:

```text
BACKLOG → CANCELLED

TODO → PAUSED
TODO → CANCELLED

IN_PROGRESS → PAUSED
IN_PROGRESS → CANCELLED

VALIDATION → IN_PROGRESS
(rejeição ou retirada da validação)

PAUSED → estado operacional anterior

DONE → IN_PROGRESS
(reabertura)

CANCELLED → TODO
(reabertura)
```

Não permitir alterações arbitrárias de `Status`.

O objetivo futuro é utilizar comportamentos como:

```text
Start()
Pause()
Resume()
SendToValidation()
Approve()
Reject()
Cancel()
Reopen()
```

em vez de simplesmente:

```text
task.Status = ...
```

---

# 24. Responsável da Task

`ResponsibleUserId` poderá ser `null`.

Uma tarefa sem responsável poderá ser assumida por membros autorizados com `CLAIM_TASK`.

Uma tarefa `IN_PROGRESS` ou `VALIDATION` deverá possuir responsável.

Se o responsável for removido/desativado enquanto a tarefa estiver `IN_PROGRESS`:

```text
ResponsibleUserId = null
IN_PROGRESS → TODO
```

A alteração será registrada no histórico e pessoas responsáveis pela gestão serão notificadas.

---

# 25. TaskCollaborator

```text
TaskCollaborator
- Id
- TaskId
- UserId
- AddedAt
- AddedByUserId
- RemovedAt
```

Não armazenar `ProjectId`, pois o projeto pode ser determinado através de:

```text
Task → Project
```

Colaborador deve ser membro do projeto.

Responsável principal não deverá simultaneamente aparecer como colaborador.

---

# 26. Validação de Task

Ao terminar o trabalho, o responsável envia:

```text
IN_PROGRESS → VALIDATION
```

Membro comum não conclui diretamente como `DONE`.

Tarefa em `VALIDATION` poderá possuir:

```text
ValidatorUserId
```

Se:

```text
ValidatorUserId = null
```

a validação está aguardando alguém assumir.

Usuários autorizados poderão realizar:

```text
ClaimValidation()
```

Quando alguém assume:

```text
ValidatorUserId = usuário
```

Apenas um validador poderá existir por vez.

---

# 27. Operações de validação

Foram definidos conceitualmente:

```text
ClaimValidation
ReleaseValidation
ReassignValidation
WithdrawFromValidation
ApproveValidation
RejectValidation
```

O validador poderá liberar a validação.

Usuário administrativo autorizado poderá transferi-la.

Se a Task deixar `VALIDATION`, `ValidatorUserId` deverá ser limpo.

Todas essas operações deverão aparecer no histórico.

---

# 28. Retirada pelo próprio responsável

O responsável que enviou a Task para `VALIDATION` poderá perceber um erro e retirá-la antes da aprovação.

Fluxo:

```text
VALIDATION → IN_PROGRESS
```

Exige motivo.

Isso é diferente de rejeição pelo validador.

Ações distintas:

```text
VALIDATION_WITHDRAWN
VALIDATION_REJECTED
```

---

# 29. Prazo da Task

Pausar tarefa não altera automaticamente `DueDate`.

Ao retomar:

```text
○ manter prazo atual
○ alterar prazo
```

Se selecionar alteração, o usuário escolhe a nova data explicitamente.

A mudança de prazo deverá ser registrada no histórico.

---

# 30. PAUSED da Task

Pausa exige motivo.

Ao retomar, a tarefa deve retornar ao estado operacional anterior válido.

A forma exata de persistir o estado anterior ainda será definida durante implementação.

---

# 31. DONE e CANCELLED

Tasks `DONE` e `CANCELLED` poderão continuar recebendo comentários.

Uma Task `DONE` poderá ser reaberta por usuário autorizado:

```text
DONE → IN_PROGRESS
```

com justificativa.

Task `CANCELLED` poderá ser reaberta:

```text
CANCELLED → TODO
```

com justificativa.

Uma Task não poderá ser reaberta se seu projeto estiver `COMPLETED` ou `ARCHIVED`.

Primeiro deverá ser reaberto/restaurado o Project.

---

# 32. TaskComment

Estrutura conceitual:

```text
TaskComment
- Id
- TaskId
- AuthorUserId
- Content
- CreatedAt
- UpdatedAt
- DeletedAt
- DeletedByUserId
```

Comentários e histórico são conceitos diferentes.

Comentários representam comunicação humana.

Histórico representa acontecimentos do sistema.

Ainda não foi definido o tempo exato para edição/exclusão de comentários de Task.

---

# 33. ChatMessage

Cada Tenant terá inicialmente um chat geral.

```text
ChatMessage
- Id
- TenantId
- AuthorUserId
- Content
- CreatedAt
- UpdatedAt
- ExpiresAt
- DeletedAt
- DeletedByUserId
```

Mensagens permanecem disponíveis por 7 dias.

Autor poderá editar/excluir sua própria mensagem durante **5 minutos após o envio**.

Depois disso fica bloqueada para alteração normal.

Mensagens antigas poderão ser removidas posteriormente por processamento em background.

---

# 34. Chat futuro

Fora do MVP inicial:

- chat privado;
- chat por projeto;
- grupos privados;
- anexos.

---

# 35. Notification

Estrutura conceitual:

```text
Notification
- Id
- RecipientUserId
- ActorUserId
- Type
- Title
- Message
- ResourceType
- ResourceId
- CreatedAt
- ReadAt
- DismissedAt
```

`ActorUserId` poderá ser nulo em notificações automáticas do sistema.

Preferir `ReadAt` a `IsRead`.

Preferir `DismissedAt` a `IsDismissed`.

---

# 36. Persistência das notificações

SignalR não é a notificação.

Fluxo obrigatório:

```text
Evento
↓
Notification persistida no banco
↓
tentativa de envio em tempo real
```

Usuário offline deve receber a notificação quando entrar posteriormente.

SignalR é somente mecanismo de entrega em tempo real.

---

# 37. Retenção de Notification

Notificações poderão permanecer armazenadas por até 90 dias.

Usuário poderá:

- remover notificações individualmente;
- limpar sua lista quando quiser.

Isso afeta somente o histórico daquele destinatário.

Remoção visual poderá utilizar `DismissedAt`.

---

# 38. Política de notificações

Usuário que executou a própria ação normalmente não recebe notificação sobre ela.

Eventos relacionados ao **Project inteiro**:

> notificam todos os demais membros ativos do Project.

Exemplos:

- projeto pausado;
- projeto retomado;
- projeto concluído;
- projeto reaberto;
- prazo do projeto alterado;
- responsável do projeto alterado;
- arquivamento.

Eventos relacionados a uma **Task**:

> normalmente notificam somente os participantes da Task.

Participantes:

```text
ResponsibleUser
+
TaskCollaborators
```

Exemplo:

Prazo de Task alterado:

```text
responsável + colaboradores
```

Prazo de Project alterado:

```text
todos os membros do Project
```

---

# 39. Exceções de notificação da Task

Eventos que precisam de ação de outros usuários podem incluir terceiros relevantes.

Exemplo:

Task enviada para `VALIDATION`:

```text
participantes
+
usuários autorizados a validar
```

Quando alguém assume uma validação, participantes poderão ser avisados e as telas dos demais validadores serão atualizadas em tempo real.

---

# 40. Activity Feed x Notification x History

Três conceitos diferentes:

## History

Registro auditável do que aconteceu com determinado recurso.

## Activity Feed

Visão geral de acontecimentos recentes do Project.

Pode mostrar atividades mesmo para membros que não receberam notificação.

## Notification

Aviso dirigido a alguém que precisa saber daquele evento.

---

# 41. TaskHistory

Estrutura conceitual inicial:

```text
TaskHistory
- Id
- TaskId
- ActorUserId
- Action
- Reason
- OldValue
- NewValue
- CreatedAt
```

Exemplos de ações:

```text
CREATED
ASSIGNED
STARTED
PAUSED
RESUMED
SENT_TO_VALIDATION
VALIDATION_CLAIMED
VALIDATION_RELEASED
VALIDATION_REASSIGNED
VALIDATION_WITHDRAWN
VALIDATION_APPROVED
VALIDATION_REJECTED
DUE_DATE_CHANGED
PRIORITY_CHANGED
CANCELLED
REOPENED
ARCHIVED
```

Histórico deve ser tratado conceitualmente como **append-only**.

Usuários comuns não podem editar histórico.

Administradores também não devem reescrever silenciosamente o passado.

---

# 42. ProjectHistory

Conceitualmente:

```text
ProjectHistory
- Id
- ProjectId
- ActorUserId
- Action
- Reason
- OldValue
- NewValue
- CreatedAt
```

Ações possíveis:

```text
CREATED
STARTED
PAUSED
RESUMED
COMPLETED
REOPENED
ARCHIVED
RESTORED
DUE_DATE_CHANGED
RESPONSIBLE_CHANGED
MEMBER_ADDED
MEMBER_REMOVED
PERMISSION_GRANTED
PERMISSION_REVOKED
```

---

# 43. Eventos

Eventos importantes do domínio/aplicação poderão produzir consequências.

Exemplo:

```text
TaskDueDateChanged
```

poderá gerar:

```text
TaskHistory
Notification
SignalR
```

Uso de eventos não significa microservices.

O projeto começará como **monólito modular**.

Não utilizar RabbitMQ/Kafka/microservices sem necessidade concreta.

---

# 44. Persistência e transações

Operações críticas deverão preservar consistência.

Exemplo:

```text
alterar Task
+
registrar History
+
criar Notification
```

devem ser persistidos de forma consistente.

Se algo crítico falhar:

```text
ROLLBACK
```

SignalR deverá ocorrer **após a persistência bem-sucedida**.

Princípio:

> Primeiro persistimos a verdade; depois avisamos o mundo externo.

---

# 45. Relacionamentos principais

```text
Tenant 1:N User

Tenant 1:N Project

Tenant 1:N ChatMessage

Project 1:N Task

Project N:N User
através de ProjectMember

Task N:N User
através de TaskCollaborator

Task 1:N TaskComment

Task 1:N TaskHistory

Project 1:N ProjectHistory

User 1:N Notification
como destinatário
```

Vários relacionamentos também apontam para `User` com significados diferentes:

```text
CreatedByUser
ResponsibleUser
ValidatorUser
ActorUser
AuthorUser
DeletedByUser
AddedByUser
```

Esses relacionamentos deverão ser explicitamente configurados no EF Core posteriormente.

---

# 46. Exclusão de dados

Política conservadora.

Dados importantes não devem depender de cascade delete destrutivo.

Preferência:

```text
Tenant → desativar
User → desativar
Project → arquivar
Task → arquivar
```

Dados temporários:

```text
ChatMessage → retenção 7 dias
Notification → retenção 90 dias
```

podem futuramente ser removidos fisicamente.

---

# 47. Dashboard

O dashboard deverá ser a principal visão diária do usuário.

Deverá responder rapidamente:

> O que preciso fazer hoje?

> O que aconteceu?

> O que está atrasado?

> Quais projetos precisam da minha atenção?

Exibir futuramente:

- minhas tarefas;
- tarefas atrasadas;
- tarefas próximas do prazo;
- projetos;
- atividades recentes;
- notificações.

---

# 48. Uso contínuo

O WorkFlow foi pensado para permanecer aberto durante o expediente.

No futuro poderá iniciar automaticamente com o computador.

Primeira versão:

```text
Web + React + SignalR
```

Notificações nativas do Windows e comportamento desktop ficam para etapa final do projeto.

---

# 49. Funcionalidades futuras já identificadas

Não implementar inicialmente:

- subtarefas;
- dependências estruturadas entre tarefas;
- chat por projeto;
- chat privado;
- anexos;
- notificações nativas do Windows;
- cliente desktop;
- aplicativo mobile;
- Redis;
- MongoDB;
- integrações externas.

---

# 50. Próxima etapa

As Aulas 1 e 2 estão concluídas.

A próxima aula será:

# Aula 3 — Criação da Solution e arquitetura .NET

Começar do zero no Visual Studio.

Criar:

```text
WorkFlow
│
├── src/
│   ├── WorkFlow.Domain
│   ├── WorkFlow.Application
│   ├── WorkFlow.Infrastructure
│   └── WorkFlow.API
│
├── tests/
│   ├── WorkFlow.UnitTests
│   └── WorkFlow.IntegrationTests
│
└── docs/
```

Explicar antes de implementar:

- tipos de projeto .NET;
- Class Library;
- Web API;
- referências entre projetos;
- direção das dependências;
- por que Domain não depende de Infrastructure;
- Dependency Injection;
- onde EF Core entra;
- onde PostgreSQL entra;
- onde React ficará.

Depois da estrutura, criar a primeira entidade real em C#, começando pelo núcleo do domínio antes de configurar EF Core.

---

# Complemento Estratégico — Evolução para SaaS Comercial

As seções anteriores registram a modelagem realizada ao final das Aulas 1 e 2. A visão do produto foi posteriormente ampliada: o WorkFlow deverá poder evoluir de aplicação multiempresa para um **SaaS comercial de autoatendimento**.

Essa evolução não substitui o domínio operacional já definido. Ela acrescenta uma camada comercial responsável pelo ciclo de vida do cliente.

---

# 51. Limites dos Domínios

O produto possuirá, conceitualmente, pelo menos dois conjuntos de responsabilidades:

```text
Domínio operacional
→ projetos, membros, permissões, tarefas, comentários, chat e notificações

Domínio comercial
→ cadastro da empresa, propriedade da conta, planos, licenças, assinatura e cobrança
```

O domínio operacional não deverá depender diretamente de tipos ou regras específicas de um provedor de pagamento.

O `Tenant` continuará representando a empresa cliente e será o principal ponto de ligação entre esses contextos.

---

# 52. Proprietário do Tenant

O usuário que concluir o cadastro comercial inicial será considerado o proprietário da conta do Tenant.

Propriedade comercial e administração operacional são responsabilidades diferentes:

```text
Tenant Owner
→ assinatura, cobrança, encerramento e transferência de propriedade

TENANT_ADMIN
→ usuários, configurações e administração operacional
```

Inicialmente, o proprietário também receberá as capacidades de `TENANT_ADMIN`.

Não foi decidido ainda se a propriedade será representada por relacionamento no `Tenant`, participação específica, role ou entidade própria. Essa decisão deverá ocorrer antes da implementação do onboarding comercial, evitando alterar prematuramente o modelo operacional atual.

---

# 53. Entidades Comerciais Candidatas

As seguintes entidades foram identificadas conceitualmente para a evolução comercial:

```text
Plan
Subscription
Payment
Invoice
BillingEvent
```

Possíveis responsabilidades:

```text
Plan
- características comerciais do plano
- limites e benefícios configuráveis

Subscription
- Tenant contratado
- plano vigente
- quantidade de licenças
- ciclo de cobrança
- situação e período vigente

Payment
- referência da cobrança
- valor e moeda
- situação
- identificador externo do provedor
- datas relevantes

Invoice
- documento ou registro financeiro associado à cobrança

BillingEvent
- recebimento e processamento rastreável de eventos externos
```

Essas estruturas são candidatas, não autorização para implementação imediata. Seus atributos e relacionamentos deverão ser modelados quando a camada comercial entrar no planejamento de desenvolvimento.

---

# 54. Assinatura e Licenças

A assinatura pertencerá a um `Tenant` e definirá inicialmente um limite de profissionais com acesso ativo.

Princípios definidos:

- quantidade contratada não cria usuários automaticamente;
- ativação de usuários deverá respeitar o limite da assinatura;
- usuários desativados continuarão preservados para histórico;
- preços e limites não deverão ser espalhados como constantes pelo domínio operacional;
- mudança de plano ou licenças deverá manter rastreabilidade;
- cancelamento ou inadimplência não deverão excluir projetos, tarefas ou históricos.

O comportamento exato de convites pendentes, redução de licenças, tolerância de pagamento e acesso somente para consulta será definido na modelagem da assinatura.

---

# 55. Ciclo de Vida Comercial do Tenant

O ciclo comercial deverá contemplar situações além do atual `IsActive` operacional.

Conceitualmente:

```text
Cadastro iniciado
→ conta verificada
→ contratação pendente
→ pagamento confirmado
→ assinatura ativa
→ renovação, atraso, cancelamento ou reativação
```

O estado comercial da assinatura não deverá ser confundido automaticamente com exclusão ou desativação administrativa do `Tenant`.

Um Tenant com assinatura encerrada deverá preservar seus dados conforme políticas de retenção, privacidade e obrigações legais que ainda serão definidas.

---

# 56. Confirmação de Pagamentos

O sistema deverá considerar o backend como autoridade para alterações comerciais.

Fluxo conceitual:

```text
Provedor confirma evento
↓
evento é autenticado e identificado
↓
processamento idempotente
↓
Payment e Subscription são atualizados de forma consistente
↓
acesso correspondente é liberado ou ajustado
```

Redirecionamentos do navegador e estados apresentados pelo frontend não constituem confirmação de pagamento.

Eventos repetidos não poderão aplicar a mesma consequência mais de uma vez.

---

# 57. Internacionalização no Domínio e na API

O domínio deverá permanecer independente do idioma de apresentação.

Princípios:

- enums e identificadores técnicos utilizarão valores estáveis;
- regras de negócio não dependerão de textos traduzidos;
- falhas deverão poder ser representadas por códigos estáveis;
- mensagens apresentadas ao usuário poderão ser localizadas fora do núcleo do domínio;
- datas persistidas continuarão normalizadas, preferencialmente em UTC;
- preferências de idioma, país e fuso horário poderão pertencer ao usuário e/ou Tenant;
- dados informados pelos usuários não serão traduzidos automaticamente.

Idiomas inicialmente planejados:

```text
pt-BR
en
es
```

---

# 58. Identidade e Participação em Múltiplos Tenants

O modelo atual associa cada `User` diretamente a um único `Tenant`, com exceção do `SYSTEM_ADMIN`.

Ainda deverá ser decidido se o produto permitirá que uma única identidade participe de várias empresas. Essa necessidade poderá surgir para consultores, grupos empresariais ou proprietários de múltiplas organizações.

Se essa capacidade for aprovada, será necessário avaliar a separação entre:

```text
Identidade de acesso
→ credenciais e dados pessoais da conta

Participação no Tenant
→ empresa, role, situação e permissões administrativas
```

Até essa decisão, não presumir automaticamente que uma conta global multi-Tenant faz parte do MVP.

---

# 59. Sequência de Evolução

A evolução comercial não interromperá o desenvolvimento atual do núcleo.

Ordem planejada:

```text
1. núcleo operacional funcional
2. preparação do frontend para localização e design system
3. publicação controlada e cliente piloto
4. onboarding comercial
5. assinatura, licenças e cobrança
6. automação e expansão internacional
```

Decisões desta visão deverão ser consideradas antecipadamente quando afetarem autenticação, contrato da API, cadastro de usuários, frontend ou implantação.
