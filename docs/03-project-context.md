# WorkFlow — Contexto Atual do Projeto

## 1. Visão

O **WorkFlow** é uma plataforma web multiempresa para gerenciamento de projetos, tarefas, equipes, comunicação, notificações e acompanhamento de atividades.

O projeto possui três objetivos complementares:

1. servir como projeto profissional de aprendizagem e portfólio;
2. ser utilizado por empresas reais;
3. evoluir para um SaaS comercial de autoatendimento.

No futuro, uma empresa deverá poder conhecer o produto pelo site público, cadastrar sua organização, contratar um plano, pagar pela internet e administrar seus próprios usuários.

---

## 2. Forma de Desenvolvimento

O desenvolvimento continua no formato professor/aluno:

1. o conceito é explicado;
2. o motivo da decisão é apresentado;
3. o aluno implementa;
4. o código é revisado;
5. testes são executados;
6. somente então o projeto avança.

Não fornecer o projeto inteiro pronto. Sempre indicar claramente onde cada código deverá ser criado ou alterado.

As decisões de produto e evolução futura poderão ser discutidas separadamente antes de gerar implementação.

---

## 3. Stack e Arquitetura

- C# e .NET 10;
- ASP.NET Core Web API;
- Entity Framework Core;
- PostgreSQL com Npgsql;
- React, TypeScript e Vite;
- SignalR;
- xUnit;
- camadas Domain, Application, Infrastructure e API;
- testes unitários e de integração;
- Docker, CI/CD e cloud em etapas futuras;
- Dapper não será utilizado neste projeto.

O sistema é multi-tenant. O backend deverá garantir que usuários de uma empresa nunca acessem dados de outra.

---

## 4. Núcleo Operacional

O núcleo contempla:

- Tenants;
- usuários e perfis;
- projetos e membros;
- permissões por projeto;
- tarefas, responsáveis e colaboradores;
- fluxo de pausa, validação, conclusão e reabertura;
- comentários;
- histórico e auditoria;
- notificações persistentes;
- chat temporário;
- dashboard e Kanban;
- comunicação em tempo real.

O detalhamento das regras está em `requirements.md` e `domain-model.md`.

---

## 5. Evolução Comercial Confirmada

O WorkFlow deverá evoluir para um SaaS com:

- página pública profissional para apresentar e vender o produto;
- cadastro de empresas pela internet;
- criação de uma conta proprietária do Tenant;
- planos baseados inicialmente na quantidade de usuários;
- ciclos mensal, semestral e anual;
- pagamento por cartão e necessidade de Pix no Brasil;
- ativação e renovação automatizadas;
- administração de assinatura e licenças;
- banco de dados e aplicação em nuvem;
- operação com o mínimo possível de intervenção manual.

O primeiro cliente poderá ser acompanhado e cadastrado manualmente antes de toda a automação comercial estar concluída.

---

## 6. Internacionalização

O frontend e os contratos da aplicação deverão ser preparados para:

- português do Brasil (`pt-BR`);
- inglês (`en`);
- espanhol (`es`).

Textos de interface deverão utilizar recursos de tradução. O domínio não deverá depender de mensagens em um idioma específico.

Também deverão ser considerados idioma do usuário e da empresa, país, fuso horário, datas, números, moedas, e-mails e notificações.

---

## 7. Direção Visual

O produto deverá possuir identidade visual moderna, profissional e marcante.

O site público poderá ser mais expressivo e impactante. A aplicação utilizada durante o expediente deverá equilibrar personalidade visual, conforto, legibilidade, acessibilidade e produtividade.

O frontend deverá ser tratado como parte essencial do produto, e não apenas como uma interface funcional sobre a API.

---

## 8. Segurança e Operação

- senhas somente como hash seguro;
- secrets nunca hardcoded ou versionados;
- autorização baseada em identidade, Tenant, perfil, participação e contexto;
- pagamentos confirmados pelo backend;
- dados de cartão não armazenados diretamente;
- histórico e dados do cliente preservados conforme regras de retenção;
- logs, backups, monitoramento e health checks antes da operação comercial;
- SignalR utilizado para entrega em tempo real, não como persistência.

---

## 9. Decisões Ainda Pendentes

Devem ser debatidos antes das respectivas implementações:

- proprietário da conta versus administradores;
- uma conta em um ou vários Tenants;
- preços, limites e benefícios dos planos;
- teste gratuito;
- provedor de pagamento;
- regras de inadimplência;
- tratamento dos convites no limite de licenças;
- domínio e nome comercial definitivos;
- LGPD, privacidade e documentos fiscais;
- países e moedas do lançamento.

Não transformar itens pendentes em regras de domínio sem discussão prévia.

---

## 10. Princípio de Continuidade

O núcleo operacional continuará sendo desenvolvido normalmente. A visão SaaS deverá influenciar antecipadamente apenas as decisões que possam causar retrabalho relevante, especialmente:

- autenticação;
- estrutura de usuários e Tenants;
- contratos da API;
- internacionalização;
- frontend;
- implantação;
- cadastro público e cobrança.
