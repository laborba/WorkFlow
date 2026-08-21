# WorkFlow

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![C%23](https://img.shields.io/badge/C%23-13-512BD4?logo=csharp)
![Status](https://img.shields.io/badge/status-em%20desenvolvimento-yellow)
![License](https://img.shields.io/badge/licença-todos%20os%20direitos%20reservados-red)

O **WorkFlow** é uma plataforma multiempresa para gestão de projetos, tarefas, equipes e processos de validação.

O projeto está sendo desenvolvido como uma aplicação real e também como demonstração técnica de conhecimentos em **C#**, **ASP.NET Core**, modelagem de domínio, arquitetura em camadas, testes automatizados e boas práticas de desenvolvimento.

> O projeto encontra-se em desenvolvimento. A estrutura inicial, as principais entidades de domínio e parte das regras de negócio já foram implementadas. A API ainda não possui endpoints funcionais.

## Objetivos

O WorkFlow tem como objetivo permitir que empresas organizem seus projetos e equipes em um único ambiente, oferecendo recursos como:

* gerenciamento de múltiplas empresas;
* criação e acompanhamento de projetos;
* organização de tarefas por status e prioridade;
* atribuição de responsáveis e colaboradores;
* possibilidade de um membro assumir uma tarefa disponível;
* processo de validação de tarefas;
* permissões específicas por projeto;
* histórico das principais alterações;
* notificações e comunicação em tempo real;
* controle de prazos e tarefas em atraso.

## Tecnologias

### Utilizadas atualmente

* .NET 10
* C#
* ASP.NET Core Web API
* xUnit
* Git e GitHub

### Planejadas

* ADO.NET
* SQL Server
* ASP.NET Core Identity ou autenticação personalizada baseada em Claims
* SignalR
* HTML
* CSS
* JavaScript
* Bootstrap

## Arquitetura

A solução está organizada em camadas com responsabilidades separadas:

```text
WorkFlow
├── docs
│   ├── requirements.md
│   ├── project-context.md
│   └── domain-model.md
│
├── src
│   ├── WorkFlow.API
│   ├── WorkFlow.Application
│   ├── WorkFlow.Domain
│   └── WorkFlow.Infrastructure
│
├── tests
│   └── WorkFlow.UnitTests
│
└── WorkFlow.sln
```

### WorkFlow.Domain

Contém as entidades, enums e regras essenciais do negócio. Essa camada não possui dependência de banco de dados, interface ou infraestrutura.

### WorkFlow.Application

Será responsável pelos casos de uso, coordenação das operações, validações de aplicação e contratos de repositórios.

### WorkFlow.Infrastructure

Será responsável pela persistência de dados, implementação dos repositórios, conexões com banco de dados e serviços externos.

### WorkFlow.API

Será a porta de entrada da aplicação, expondo os recursos por meio de uma API HTTP.

### WorkFlow.UnitTests

Contém os testes automatizados das regras de domínio, garantindo que alterações futuras não quebrem comportamentos já implementados.

## Domínio implementado

Atualmente, o projeto possui as seguintes entidades principais:

* `Tenant`: representa uma empresa dentro do sistema;
* `User`: representa um usuário pertencente a uma empresa;
* `Project`: representa um projeto e seu ciclo de vida;
* `ProjectTask`: representa uma tarefa pertencente a um projeto;
* `ProjectMember`: representa a participação de um usuário em um projeto.

Também estão sendo modeladas permissões específicas por projeto, permitindo que os membros recebam responsabilidades além de seu perfil geral no sistema.

## Regras de negócio

Algumas das regras já modeladas incluem:

* isolamento dos dados por empresa;
* validação dos dados obrigatórios das entidades;
* controle das transições de status de projetos e tarefas;
* conclusão de projeto somente quando todas as tarefas estiverem concluídas ou canceladas;
* projeto sem tarefas não pode ser concluído;
* tarefas concluídas ou canceladas podem ser arquivadas;
* projetos em planejamento ou concluídos podem ser arquivados;
* elementos arquivados permanecem somente para leitura;
* restauração preserva o estado anterior;
* remoção lógica de membros do projeto;
* validação de identificadores e estados inválidos;
* registro de datas utilizando UTC.

As regras são protegidas por testes unitários.

## Status das tarefas

O fluxo de uma tarefa pode envolver os seguintes estados:

```text
Backlog
→ A fazer
→ Em andamento
→ Validação
→ Concluída
```

Dependendo da situação, uma tarefa também pode ser pausada, cancelada, reaberta ou arquivada.

No processo de validação, usuários autorizados poderão assumir a responsabilidade pela análise da tarefa antes de aprová-la ou devolvê-la para ajustes.

## Executando o projeto

### Pré-requisitos

* .NET 10 SDK
* Visual Studio 2026 ou outra IDE compatível

### Clonar o repositório

```bash
git clone https://github.com/laborba/WorkFlow.git
cd WorkFlow
```

### Restaurar as dependências

```bash
dotnet restore
```

### Compilar a solução

```bash
dotnet build
```

### Executar os testes

```bash
dotnet test
```

### Executar a API

```bash
dotnet run --project src/WorkFlow.API/WorkFlow.API.csproj
```

A API ainda está em fase inicial e não possui endpoints funcionais do WorkFlow.

## Documentação

A documentação detalhada está disponível na pasta [`docs`](docs):

* [Requisitos do sistema](docs/requirements.md)
* [Contexto do projeto](docs/project-context.md)
* [Modelo de domínio](docs/domain-model.md)

## Progresso

* [x] Levantamento inicial dos requisitos
* [x] Definição da estrutura da solução
* [x] Separação inicial das camadas
* [x] Criação das principais entidades de domínio
* [x] Criação dos enums iniciais
* [x] Implementação dos testes unitários iniciais
* [ ] Finalização das permissões por projeto
* [ ] Casos de uso da camada Application
* [ ] Repositórios com ADO.NET
* [ ] Persistência em banco de dados
* [ ] Autenticação e autorização
* [ ] Endpoints da API
* [ ] Interface web
* [ ] Notificações com SignalR
* [ ] Testes de integração
* [ ] Publicação da aplicação

## Segurança

Nenhuma credencial, senha, chave de API ou string de conexão de produção deve ser armazenada no repositório.

As configurações sensíveis de desenvolvimento serão mantidas por meio do Gerenciador de Segredos do Usuário do ASP.NET Core. Em produção, os segredos serão fornecidos por mecanismos externos à aplicação.

Todos os dados utilizados durante o desenvolvimento e nas demonstrações públicas deverão ser fictícios.

## Autor

**Lucas Aaron de Borba**

Desenvolvedor Web com experiência em C#, ASP.NET Core, HTML, CSS, JavaScript, jQuery, Bootstrap, MySQL, SQL Server e SignalR.

* [LinkedIn](https://www.linkedin.com/in/lucas-aaron-de-borba/)
* [GitHub](https://github.com/laborba)

## Licença e direitos autorais

Copyright © 2026 Lucas Aaron de Borba. Todos os direitos reservados.

Este projeto não possui uma licença de código aberto. O código-fonte está disponível publicamente para fins de portfólio, estudo e avaliação técnica.

A disponibilidade pública do código não concede permissão para copiar, distribuir, modificar, sublicenciar ou utilizar comercialmente o projeto ou partes dele sem autorização expressa do autor.
