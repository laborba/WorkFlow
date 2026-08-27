# WorkFlow — Estratégia de Idiomas

## 1. Decisão

O idioma inicial padrão do WorkFlow será o **inglês**.

O sistema não selecionará nem alterará o idioma automaticamente com base em:

- localização geográfica;
- endereço IP;
- país presumido;
- localização do dispositivo.

A escolha do idioma será realizada explicitamente pelo usuário.

Idiomas inicialmente planejados:

```text
English (en)
Português do Brasil (pt-BR)
Español (es)
```

---

## 2. Primeiro acesso

No primeiro acesso, quando ainda não existir uma preferência salva, o site será apresentado em inglês.

O usuário poderá selecionar outro idioma por meio do seletor disponível na interface.

---

## 3. Persistência no navegador

A preferência escolhida será salva no navegador do usuário.

Conceitualmente:

```text
Chave: workflow.language
Valores permitidos: en | pt-BR | es
```

Essa preferência poderá ser armazenada utilizando `localStorage`, pois não representa informação sensível.

Nos acessos seguintes realizados pelo mesmo navegador, o WorkFlow deverá utilizar automaticamente o idioma anteriormente selecionado.

---

## 4. Usuário autenticado

Futuramente, usuários autenticados também poderão possuir uma preferência de idioma associada à própria conta.

A ordem de prioridade planejada será:

```text
1. preferência salva na conta do usuário
2. preferência salva no navegador
3. inglês como idioma padrão
```

Quando um usuário autenticado alterar o idioma, a preferência poderá ser atualizada tanto na conta quanto no navegador. Dessa forma, o idioma poderá acompanhá-lo em outros dispositivos.

---

## 5. Site público e URLs

O site público deverá considerar URLs específicas por idioma para favorecer indexação, compartilhamento e previsibilidade.

Exemplos conceituais:

```text
/en
/pt-BR
/es

/en/pricing
/pt-BR/pricing
/es/pricing
```

Uma URL que indique explicitamente um idioma deverá ser respeitada, mesmo quando existir outra preferência armazenada no navegador.

A estrutura definitiva das rotas será decidida durante a implementação do frontend e do site público.

---

## 6. Textos da interface

Os textos do frontend não deverão ficar espalhados ou fixados diretamente nos componentes.

Deverão ser utilizados recursos ou chaves de tradução para permitir a manutenção dos idiomas suportados.

Conteúdos criados pelos próprios usuários, como títulos, descrições, mensagens e comentários, não serão traduzidos automaticamente.

---

## 7. Erros da API

Erros deverão possuir códigos técnicos estáveis e independentes do idioma.

Exemplo:

```text
Tenants.NotFound
Validation.InvalidArgument
```

O código do erro não será traduzido. A mensagem apresentada ao usuário poderá variar conforme o idioma selecionado.

As mensagens atualmente existentes em português poderão continuar sendo utilizadas durante o desenvolvimento e migradas gradualmente. O idioma inglês poderá funcionar futuramente como mensagem padrão ou fallback da API.

A localização da interface não deverá exigir que o domínio conheça o idioma do usuário.

---

## 8. Princípio geral

```text
Idioma inicial
→ inglês

Mudança de idioma
→ escolha explícita do usuário

Persistência inicial
→ navegador

Persistência futura
→ conta do usuário e navegador

Seleção por localização geográfica
→ não utilizar
```

Esta decisão deverá orientar futuramente o site público, o frontend da aplicação, as mensagens apresentadas ao usuário e a evolução dos contratos da API.
