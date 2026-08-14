# Prompt para continuar o WorkFlow

Quero continuar o projeto **WorkFlow** que estávamos desenvolvendo em outra conversa.

Já concluímos:

- **Aula 1 — Levantamento de requisitos**
- **Aula 2 — Modelagem do domínio**

O WorkFlow é um sistema genérico multiempresa para gerenciamento de projetos, tarefas, equipes, chat temporário, notificações, histórico e atividades.

Quero continuar no mesmo formato de professor/aluno:

1. você explica o que vamos fazer;
2. explica por que faremos daquela forma;
3. me diz o que eu devo implementar;
4. eu implemento e mostro para você;
5. você revisa;
6. só então avançamos.

Não quero simplesmente receber o projeto inteiro pronto.

Agora quero iniciar:

**Aula 3 — Criação da Solution e arquitetura .NET**

Antes de avançar, recupere o contexto anterior do projeto WorkFlow, especialmente o checkpoint final das Aulas 1 e 2, para manter todas as decisões que já tomamos.

Regras importantes já definidas:

- C#/.NET 10;
- ASP.NET Core Web API;
- Entity Framework Core;
- PostgreSQL + Npgsql;
- não utilizar Dapper;
- React + TypeScript no frontend;
- arquitetura separada em Domain, Application, Infrastructure e API;
- sistema multi-tenant;
- senhas armazenadas somente como hash;
- credenciais e secrets nunca hardcoded/versionados;
- autorização global + por projeto + contextual;
- histórico/auditoria e notificações persistentes;
- SignalR para tempo real;
- testes unitários e de integração;
- Docker/CI/CD posteriormente.

Comece a Aula 3 do zero, pelo Visual Studio e criação da Solution, explicando cada escolha antes de eu executá-la.