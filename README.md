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

Agora precisaremos criar nossa interface de repositório, que será responsável por armazenar as informações no banco de dados, aqui veremos algo comum nada de novo.

```csharp
using TodoList.Domain.Entities;

namespace TodoList.Domain.Repositories;

public interface ITodoRepository
{
    ValueTask<IEnumerable<Todo>> GetAllAsync();
    ValueTask<Todo> GetByIdAsync(Guid id);
    
    ValueTask RegisterAsync(Todo todo);
    
    void Update(Todo todo);
    
    void Remove(Todo todo);
}
```

Aqui para nos basta essa interface `ITodoRepository`, e com isso finalizamos nosso Dominio!

### 3. Implementando a camada de acesso a dados

É aqui que, de fato vamos destrinchar a mágica do uso do SQLite com EntityFrameworkCore no navegador por meio do Blazor Wasm.

> Neste artigo usamos alguns pré-lançamentos e esses recursos e APIs podem (e certamente mudarão) no futuro até o lançamento.

Em estruturas SPAs populares como Angular ou React, o IndexedDB é frequentemente usado  para armazenar dados do lado do cliente e ele é mais ou menos um banco de dados dos navegadores atuais, e como a maioria das estruturas SPAs são em JavaScript elas conseguem se comunicar diretamente com IndexedDB, mas o Blazor Web Assembly difere, e para se comunicar com o IndexedDB temos que utilizar um invólucro para o JavaScript (JSInterop) para se comunicar com o banco, e assim persistir os dados!

Mas isso é realmente necessário? Como estamos no mundo dotNET, podemos escolher usar o EntityFrameworkCore como a abordagem de acesso ao banco de dados & tecnologia e isso parece ótimo. Com esse cenário, temos o poder do EntityFrameworkCore para executar consultas SQL rápidas e complexas em um banco de dados sem ter que construir a ponte para o IndexedDB com o JSInterop.

Neste artigo sentiremos um gostinho do uso do EntityFrameworkCore e SQLite no navegador, mas o suficiente para lhe dar um caminho para trilhar conforme sua vontade e expertise, tudo isso quase sem usar JavaScript.

Vamos precisar instalar os seguintes pacotes e referenciar o projeto de domínio. Um ponto importante ajuste os caminhos dos projetos e para os nomes caso os tenha alterado!

- [Microsoft.EntityFrameworkCore.Sqlite.Core](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Sqlite.Core/6.0.5) 6.0.5
- [SQLitePCLRaw.bundle_e_sqlite3](https://www.nuget.org/packages/SQLitePCLRaw.bundle_e_sqlite3/2.1.0-pre20220427180151) (versão preview)
- `dotnet add reference ..\Todo.Domain\Todo.Domain.csproj`

O `SQLitePCLRaw.bundle_e_sqlite3` faz magica por trás dos panos, ele é responsável por fornecer e/ou criar a biblioteca SQLite nativa, correta e específica para cada plataforma alvo. Isso é essencialmente o mesmo que se você enviar manualmente um binário específico para cada plataforma como, por exemplo, `sqlite3.dll` para o Windows e `sqlite3.so` para Linux, e como estamos mirando o WebAssembly, a implementação C do SQLite precisa ser compilada para essa plataforma.

> TODO: Pegar um print do sqlite3.a/sqlite3.so etc...

Este é um mecanismo completo para o banco de dado SQLite, pronto para ser carregado no navegador e para ser executado no tempo de execução do Wasm. Com isso, nosso aplicativo Blazor Web Assembly pode usar o EntityFrameworkCore para falar diretamente com um banco de dados SQLite real e incorporado no navegador.

Também precisamos editar o arquivo `.csproject` do projeto de acesso a dados para adicionarmo uma configuração!

```xml
<WasmNativeBuild>true</WasmNativeBuild>
```

Com isso o `.csproject` do projeto de acesso a dado ficara algo como isso

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <WasmNativeBuild>true</WasmNativeBuild>
  </PropertyGroup>


  <ItemGroup>
    <SupportedPlatform Include="browser" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="6.0.5" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite.Core" Version="6.0.5" />		
		<PackageReference Include="SQLitePCLRaw.bundle_e_sqlite3" Version="2.1.0-pre20220427180151" />	
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Todo.Domain\Todo.Domain.csproj" />
  </ItemGroup>

</Project>
```

Agora vamos começar adicionando o nosso DbContext com algo parecido com isso e já será o suficiente para nossa demo, provavelmente se você já trabalhou com EntityFrameworkCore verá algo familiar.

```csharp
using Microsoft.EntityFrameworkCore;
using TodoList.Domain.Entities;

namespace TodoList.Infra.Data;

public class TodoListDbContext : DbContext
{
    public TodoListDbContext(DbContextOptions<TodoListDbContext> options) : base(options)
    { }

    public DbSet<Todo> Todos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Todo>().HasKey(p => p.Id);
        modelBuilder.Entity<Todo>().Property(p => p.Title).IsRequired().HasMaxLength(100);
    }
}
```
Sim, é um DbContext bem simples, mais já será mais que o suficiente para nossa demostração.

Agora vamos criar três interfaces base para nos ajudar a cumprir nossa missão de usar o SQLite com o EntityFrameworkCore no Blazor Web Assembly.

Nossa primeira interface vai nos ajudar a armazenar e sincronizar o arquivo do banco de dados, o salvando, seja no navegador em cache, etc. ou em algum serviço em nuvem para armazenar o banco e aqui vai conforme sua necessidade e imaginação!

