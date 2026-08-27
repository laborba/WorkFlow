Quero continuar o projeto **WorkFlow** no mesmo formato professor/aluno utilizado anteriormente.

Estou anexando os arquivos:

- `requirements.md`
- `domain-model.md`
- `project-context.md`

Eles contêm os requisitos, modelagem e checkpoint das Aulas 1 e 2.

## Metodologia

Continue obrigatoriamente neste formato:

1. explique o conceito;
2. explique por que estamos fazendo daquela forma;
3. diga exatamente o que eu devo implementar;
4. eu implemento;
5. eu mostro ou confirmo o resultado;
6. você revisa;
7. somente depois avançamos.

Não quero receber o projeto inteiro pronto.

Quero aprender arquitetura .NET, domínio, EF Core, testes, segurança e demais conceitos durante a construção.

---

# Situação atual

As **Aulas 1 e 2 estão concluídas**.

Estamos atualmente na **Aula 3 — Criação da Solution e arquitetura .NET**.

A maior parte da estrutura da Solution já foi concluída.

---

# Ambiente de desenvolvimento

O computador atual é do trabalho e utiliza:

```text
Windows 10
Visual Studio Community 2022
Versão 17.14.36
```

Por causa disso, o Visual Studio suporta oficialmente somente até `.NET 9`.

Portanto tomamos a decisão temporária:

```text
DESENVOLVIMENTO ATUAL:
.NET 9

TARGET FUTURO:
.NET 10
```

Mais adiante, quando o projeto estiver avançado, utilizarei meu computador de casa, que possui **Windows 11**, para instalar o ambiente adequado e migrar o projeto para **.NET 10**.

Essa migração deverá ser tratada posteriormente como uma etapa/aula do próprio projeto.

**Não alterar para .NET 10 agora.**

Também é importante que meu **Visual Studio está em português**.

Portanto, ao me orientar pelo Visual Studio, utilize os nomes dos menus, templates e opções em português sempre que possível.

Exemplos:

```text
Class Library = Biblioteca de Classes
Test Explorer = Gerenciador de Testes
Add Project Reference = Adicionar Referência de Projeto
```

---

# Estrutura atual da Solution

Fisicamente:

```text
WorkFlow/
│
├── src/
│   ├── WorkFlow.Domain/
│   ├── WorkFlow.Application/
│   ├── WorkFlow.Infrastructure/
│   └── WorkFlow.API/
│
├── tests/
│   ├── WorkFlow.UnitTests/
│   └── WorkFlow.IntegrationTests/
│
├── frontend/
├── docs/
│
└── WorkFlow.sln
```

Todos os projetos atuais utilizam:

```xml
<TargetFramework>net9.0</TargetFramework>
```

---

# Tipos dos projetos

```text
WorkFlow.Domain
→ Biblioteca de Classes

WorkFlow.Application
→ Biblioteca de Classes

WorkFlow.Infrastructure
→ Biblioteca de Classes

WorkFlow.API
→ API Web do ASP.NET Core

WorkFlow.UnitTests
→ Projeto de Teste xUnit

WorkFlow.IntegrationTests
→ Projeto de Teste xUnit
```

---

# Referências entre projetos

As referências estão configuradas exatamente assim:

```text
WorkFlow.API
├──→ WorkFlow.Application
└──→ WorkFlow.Infrastructure

WorkFlow.Infrastructure
├──→ WorkFlow.Application
└──→ WorkFlow.Domain

WorkFlow.Application
└──→ WorkFlow.Domain

WorkFlow.Domain
└──→ nenhuma camada do WorkFlow
```

Testes:

```text
WorkFlow.UnitTests
├──→ WorkFlow.Application
└──→ WorkFlow.Domain

WorkFlow.IntegrationTests
└──→ WorkFlow.API
```

O `Domain` deve continuar sem depender de:

```text
Application
Infrastructure
API
EF Core
PostgreSQL
ASP.NET Core
HTTP
React
SignalR
```

---

# WorkFlow.API

A API já foi criada.

Configuração escolhida no template:

```text
.NET 9.0

Tipo de autenticação:
Nenhum

Configurar para HTTPS:
marcado

Habilitar o suporte a contêineres:
desmarcado

Habilitar o suporte a OpenAPI:
marcado

Usar controladores:
marcado

Não use instruções de nível superior:
desmarcado

.NET Aspire:
desmarcado
```

O projeto `WorkFlow.API` está definido como **Projeto de Inicialização**.

A API foi executada com sucesso.

Portas observadas:

```text
https://localhost:7040
http://localhost:5068
```

O `Program.cs` atual é:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
```

Já estudamos:

```text
WebApplication.CreateBuilder
builder.Services
Dependency Injection
Build()
pipeline HTTP
UseHttpsRedirection
UseAuthorization
MapControllers
Run()
OpenAPI
Controllers
rotas
verbos HTTP
serialização JSON
```

O exemplo `WeatherForecast` foi executado e testado com sucesso.

Depois removemos:

```text
WeatherForecast.cs
WeatherForecastController.cs
```

O arquivo:

```text
WorkFlow.API.http
```

foi mantido.

---

# Testes

O xUnit já está funcionando.

Já aprendemos:

```text
[Fact]
[Theory]
[InlineData]
Assert.Equal
Assert.True
Assert.False
Assert.Null
Assert.NotNull
Assert.NotEqual
Assert.Throws
Arrange / Act / Assert
```

Também foi explicado que:

```text
Compilar o projeto
≠
Executar os testes
```

Os testes permanecem no projeto e futuramente serão executados automaticamente através de CI/CD/GitHub Actions.

---

# Domain atual

Foi criada:

```text
WorkFlow.Domain/
├── Entities/
│   ├── Tenant.cs
│   └── User.cs
│
└── Enums/
    └── UserRole.cs
```

---

# UserRole

O enum atual é:

```csharp
namespace WorkFlow.Domain.Enums;

public enum UserRole
{
    SystemAdmin = 1,
    TenantAdmin = 2,
    ProjectManager = 3,
    Member = 4
}
```

Foi decidido deixar `0` sem representar nenhum Role válido.

Ainda não foi decidido se PostgreSQL armazenará enum como número ou string; isso será uma decisão futura da camada `Infrastructure`.

---

# Tenant

A entidade `Tenant` já foi criada.

Estrutura:

```csharp
public long Id { get; private set; }

public Guid PublicId { get; private set; }

public string Name { get; private set; }

public string RegistrationNumber { get; private set; }

public string Email { get; private set; }

public string? Phone { get; private set; }

public bool IsActive { get; private set; }

public DateTime CreatedAt { get; private set; }

public DateTime? UpdatedAt { get; private set; }
```

O construtor recebe:

```text
name
registrationNumber
email
phone opcional
```

E realiza:

```text
validação de Name
validação de RegistrationNumber
validação de Email

Guid.NewGuid() para PublicId
Trim() dos textos apropriados
IsActive = true
CreatedAt = DateTime.UtcNow
```

`Id` não é informado no construtor porque futuramente será atribuído pela persistência.

`UpdatedAt` começa como `null`.

`PasswordHash` ou outras informações técnicas não existem em Tenant.

---

# Comportamentos atuais de Tenant

Já implementamos:

```text
Rename()
UpdateContact()
Deactivate()
Activate()
```

As propriedades utilizam `private set`.

A intenção é evitar coisas como:

```csharp
tenant.Name = "";
tenant.IsActive = false;
```

fora da entidade.

O domínio deve controlar suas próprias mudanças.

Também foi decidido **não criar ainda**:

```text
ChangeRegistrationNumber()
```

porque alterar a identificação legal da empresa pode exigir uma regra específica posteriormente.

---

# Datas

Atualmente usamos:

```csharp
DateTime.UtcNow
```

diretamente no Domain.

Discutimos abstração de relógio/`TimeProvider`, mas decidimos **não adicionar ainda**.

Quando surgirem regras realmente dependentes de tempo, como:

```text
chat editável por 5 minutos
mensagens expirando em 7 dias
notificações
prazos
```

o assunto deverá ser revisitado.

---

# TenantTests

Foi criado:

```text
WorkFlow.UnitTests/
└── Domain/
    └── Entities/
        └── TenantTests.cs
```

Os testes atuais cobrem, entre outros:

```text
criação válida
Tenant nasce ativo
PublicId é criado
CreatedAt é definido
UpdatedAt começa null
Phone pode ser null

Name null/vazio/espaços → exceção
RegistrationNumber null/vazio/espaços → exceção
Email null/vazio/espaços → exceção

Trim de Name
Trim de RegistrationNumber
Trim de Email
Trim de Phone
Phone vazio/espaços → null

Rename válido
Rename inválido
Deactivate
Activate
UpdateContact
```

Todos os testes estão passando.

---

# User

A entidade `User` já foi criada seguindo a modelagem:

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

Propriedades:

```csharp
public long Id { get; private set; }

public Guid PublicId { get; private set; }

public long? TenantId { get; private set; }

public string Name { get; private set; }

public string Email { get; private set; }

public string PasswordHash { get; private set; }

public UserRole Role { get; private set; }

public bool IsActive { get; private set; }

public DateTime CreatedAt { get; private set; }

public DateTime? UpdatedAt { get; private set; }
```

O construtor recebe:

```text
long? tenantId
string name
string email
string passwordHash
UserRole role
```

Já são validados:

```text
Name obrigatório
Email obrigatório
PasswordHash obrigatório
Role precisa ser válido
```

Também existe a regra:

```text
SystemAdmin
→ pode possuir TenantId null

TenantAdmin
ProjectManager
Member
→ precisam possuir TenantId
```

O construtor também:

```text
gera PublicId
faz Trim de Name
faz Trim de Email
NÃO altera/trimma PasswordHash
IsActive = true
CreatedAt = DateTime.UtcNow
```

---

# Senhas

Foi decidido novamente que:

```text
senha original nunca será persistida
senha reversivelmente criptografada nunca será persistida
somente PasswordHash será armazenado
```

O `Domain.User` **não conhece algoritmo ou biblioteca de hashing**.

Conceitualmente, futuramente teremos algo nessa direção:

```text
Application
↓
IPasswordHasher

Infrastructure
↓
implementação de IPasswordHasher
↓
algoritmo/biblioteca escolhida
```

Mas **IPasswordHasher ainda NÃO foi criado**.

Também ainda NÃO escolhemos BCrypt, PBKDF2, ASP.NET PasswordHasher ou outra implementação.

Isso será feito posteriormente quando chegarmos à autenticação.

---

# UserTests

Foi criado:

```text
WorkFlow.UnitTests/
└── Domain/
    └── Entities/
        └── UserTests.cs
```

Já implementamos e executamos com sucesso:

```text
Constructor_ShouldCreateActiveUser_WhenDataIsValid

Constructor_ShouldAllowSystemAdminWithoutTenant

Constructor_ShouldThrow_WhenNonSystemAdminHasNoTenant
```

O último usa `[Theory]` para testar:

```text
TenantAdmin
ProjectManager
Member
```

sem Tenant.

Todos os testes do projeto estão atualmente verdes.

---

# Decisões de arquitetura importantes

Continuar respeitando:

```text
Domain
→ regras e comportamento de negócio

Application
→ casos de uso/orquestração

Infrastructure
→ EF Core, PostgreSQL, hashing e serviços externos

API
→ HTTP, autenticação/autorização, entrada e Composition Root
```

Não instalar/adicionar tecnologia antes de existir justificativa.

Ainda NÃO configuramos:

```text
EF Core
Npgsql
PostgreSQL
DbContext
Migrations
autenticação
JWT
SignalR
React
Docker
```

Não antecipar essas etapas.

---

# Ponto EXATO onde paramos

Acabamos de criar `User` e executar os três primeiros testes com sucesso.

O professor havia indicado que o próximo passo seria:

1. testar valores inválidos das strings obrigatórias do `User`;
2. testar valores inválidos de `UserRole`;
3. discutir a seguinte questão de domínio:

> **Um `TenantId = 0` deve ser considerado válido pelo Domain?**

Essa discussão deve explicar a diferença entre:

```text
identificador não informado
identificador ainda não persistido
Foreign Key válida
```

**Continue exatamente deste ponto, ainda dentro da Aula 3.**

Não pule diretamente para EF Core, banco ou próxima entidade antes de concluir essa discussão e revisar os testes do `User`.