---
layout: post
title: 'Utilizando Sqlite com EF no Blazor Web Assembly'
author: emerson.trindade
image: https://user-images.githubusercontent.com/16648655/149034087-6f3d88b5-be58-4b15-aff6-319772d95db9.png
tags: [csharp,dotnet,ef,sqlite,blazor,webassembly]
draft: true
hidden: true
---
Hello everyone, neste artigo vamos construir uma aplicação Blazor Web Assembly sem interação com o Servidor e rodando SQLite com EntityFrameworkCore

<!--more-->

A aplicação é simples mais vai demostrar bem o uso do SQLite com EntityFrameworkCore no Blazor Wasm, é valido ressaltar que, em aplicações reais você deve ter bastante cuidado com os dados que vão ser armazenados no SQLite, pois ele pode ser facilmente obtido já que está armazenado no cache do navegador, tenha bastante cuidado!

Vamos criar três projetos básicos para construção desta aplicação, poderia ser mais simples, mas estou tentando chegar o mais próximo de uma arquitetura final sem tornar um exemplo cansativo e complexo.

> Informações de versões
>
> 1. [.NET SDK:](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) 6.0.300
> 2. [Microsoft.EntityFrameworkCore.Sqlite.Core:](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Sqlite.Core/6.0.5) 6.0.5
> 3. [SQLitePCLRaw.bundle_e_sqlite3](https://www.nuget.org/packages/SQLitePCLRaw.bundle_e_sqlite3/2.1.0-pre20220427180151) (versão preview)
> 4. [MudBlazor](https://mudblazor.com/docs/overview) (apenas para nos poupar tempo com o layout)

Primeiro precisamos criar a solução conforme exemplo a baixo:

```
dotnet new sln -n TodoList
```

Aqui está os projetos que vamos precisamos criar, que, basicamente vão ser três, sendo  eles, de Domínio, acesso a Dados e UI! Abaixo temos os comandos de exemplo para criar os projetos, você pode alterá-los conforme seu gosto, ou usar algum recurso visual de sua IDE, mas você deve respeitar os tipos informados!

| Nome do projeto              | Tipo          | Comando                                                 |
|------------------------------|---------------|---------------------------------------------------------|
| TodoList.Domain              | classlib      | `dotnet new classlib -o Todo.Domain -f net6.0`          |
| TodoList.Infrastructure.Data | razorclasslib | `dotnet new razorclasslib -o Todo.Infra.Data -f net6.0` |
| TodoList.Presentation        | blazorwasm    | `dotnet new blazorwasm -o Todo -f net6.0`               |

**Domain:** Esse projeto é simples e conterá nossas _entidades_ e nossas _interfaces_ de repositório, em projetos reais ele poderia ser bem mais complexo, mas não necessitamos disso, nessa demostração!

**Data:** Aqui teremos basicamente tudo que será necessário para podermos usar de fato o _EntityFrameworkCore_ com o _SQLite_ no navegador com o _Blazor Web Assembly_.

**Presentation:** Esse projeto será nada de mais, apenas algumas paginas para testarmos e utilizarmos os serviços de acesso e gerência do banco de dados por meio do _EntityFrameworkCore_.

> **NOTA:**
> #### Vamos usar basicamente o CLI do dotNET para tornar este artigo replicável nas plataformas suportadas pelo dotNET, como, por exemplo, nos ecos sistemas macOS, Linux ou Windows, com seu editor de código ou IDE preferida sem problemas de compatibilidade.

## 1. Entendendo o projeto

Basicamente estamos criando um  aplicativo que fara a gerenciamento de tarefas, podendo adicionar, atualizar excluir e editar as tarefas conforme a necessidade do usuário, e de extra ele pode fazer o download do banco de dados atual!

## 2. Implementando o domínio

Primeiro vamos criar o projeto de domínio, que será responsável por armazenar as entidades e interfaces de repositórios que serão utilizadas no nosso aplicativo.

Basicamente teremos uma única entidade chamada `Todo`, que será responsável por representar nossa tarefa, e também, uma interface de repositório, que será responsável por armazenar as informações no banco de dados.

``` csharp
namespace TodoList.Domain.Entities;

public class Todo
{
    public Todo(string title, string description)
    {
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        Completed = false;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public bool Completed { get; private set; }
    
    public void MarkAsCompleted()
    {
        Completed = true;
    }
}
```