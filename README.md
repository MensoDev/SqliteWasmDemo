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

Primeiro precisamos criar a solução conforme exemplo a baixo:

```
dotnet new sln -n TodoList
```

Aqui está os projetos que vamos precisamos criar, que, basicamente vão ser três, sendo  eles, de Domínio, Acesso a Dados e UI! Abaixo temos os comandos de exemplo para criar os projetos, você pode alterá-los conforme seu gosto, ou usar algum recurso visual de sua IDE, mas você deve respeitar os tipos informados!

| Nome do projeto              | Tipo          | Comando                                                 |
|------------------------------|---------------|---------------------------------------------------------|
| TodoList.Domain              | classlib      | `dotnet new classlib -o Todo.Domain -f net6.0`          |
| TodoList.Infrastructure.Data | razorclasslib | `dotnet new razorclasslib -o Todo.Infra.Data -f net6.0` |
| TodoList.Presentation        | blazorwasm    | `dotnet new blazorwasm -o Todo -f net6.0`               |

**Domain:** Esse projeto é simples e conterá nossas _entidades_ e nossas _interfaces_ de repositório, em projetos reais ele poderia ser bem mais complexo, mas não necessitamos disso, nessa demostração!

**Data:** Aqui teremos basicamente tudo que será necessário para podermos usar de fato o _EntityFrameworkCore_ com o _SQLite_ no navegador com o _Blazor Web Assembly_. Por isso o tipo de projeto sera razorclasslib.

**Presentation:** Esse projeto será nada de mais, apenas algumas paginas para testarmos e utilizarmos os serviços de acesso e gerência do banco de dados por meio do _EntityFrameworkCore_.

> **NOTA:**
> #### Vamos usar basicamente o CLI do dotNET para tornar este artigo replicável nas plataformas suportadas pelo dotNET, como, por exemplo, nos ecos sistemas macOS, Linux ou Windows, com seu editor de código ou IDE preferido sem problemas de compatibilidade. 

## 1. Entendendo o projeto

Basicamente estamos criando um  aplicativo que fara a gerenciamento de tarefas, podendo adicionar, atualizar excluir e editar as tarefas conforme a necessidade do usuário, e de extra ele pode fazer o download do banco de dados atual, não vamos nos deter a parte visual.

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
- `dotnet add reference ..\TodoList.Domain\TodoList.Domain.csproj`

O `SQLitePCLRaw.bundle_e_sqlite3` faz magica por trás dos panos, ele é responsável por fornecer e/ou criar a biblioteca SQLite nativa, correta e específica para cada plataforma alvo. Isso é essencialmente o mesmo que se você enviar manualmente um binário específico para cada plataforma como, por exemplo, `sqlite3.dll` para o Windows e `sqlite3.so` para Linux, e como estamos mirando o WebAssembly, a implementação C do SQLite precisa ser compilada para essa plataforma.

![s_sqlite3 a_gerado](https://user-images.githubusercontent.com/36675712/177378987-d94459d4-f88d-4838-bb43-da6e56756959.png)

Este é um mecanismo completo para o banco de dado SQLite, pronto para ser carregado no navegador e para ser executado no tempo de execução do Wasm. Com isso, nosso aplicativo Blazor Web Assembly pode usar o EntityFrameworkCore para falar diretamente com um banco de dados SQLite real e incorporado no navegador.

Também precisamos editar o arquivo `.csproject` do projeto de acesso a dados para adicionarmo uma configuração!

```xml
<WasmNativeBuild>true</WasmNativeBuild>
```

```xml
<PropertyGroup Condition="'$(Configuration)'=='Release'">
	<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
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

```csharp
namespace TodoList.Infra.Data.Services;

public interface IDatabaseStorageService
{
    Task<int> SyncDatabaseAsync(string filename);
    Task<string> GenerateDownloadLinkAsync(string filename);
}
```

Poderíamos talvez, armazenar o banco de dados ou pelo menos um backup dele em alguma nuvem privada do usuário como, por exemplo, Google Drive ou OneDrive, etc., mas para deixar esse artigo o mais simples possível vamos salvar o banco no cache do navegador.

Neste ponto teremos que usar um pouco de JavaScript para poder acessar o cache do navegador, pois o Blazor Web Assembly ainda não consegui fazer isso diretamente!

Este código JavaScript nos ajudara a sincronizar o banco de dados com o cache, e como extra, ira nos dar um link para download do banco de dados!

```javascript
export async function  syncDatabaseWithBrowserCache(filename) {   
    
    window.blazorWasmDatabase = window.blazorWasmDatabase || {
        init: false,
        cache: await caches.open('wasmDatabase')
    };

    const db = window.blazorWasmDatabase;

    const backupPath = `/${filename}_backup`;
    const cachePath = `/database/cache/${filename}`;

    if (!db.init) {
        db.init = true;
        const resp = await db.cache.match(cachePath);

        if (resp && resp.ok) {
            const res = await resp.arrayBuffer();
            if (res) {
                console.log(`Database Restoring  ${res.byteLength} bytes`);
                FS.writeFile(backupPath, new Uint8Array(res));
                return 0;
            }
        }
    }

    if (FS.analyzePath(backupPath).exists) {

        const waitFlush = new Promise((done, _) => {
            setTimeout(done, 10);
        });

        await waitFlush;

        const data = FS.readFile(backupPath);

        const blob = new Blob([data], {
            type: 'application/octet-stream',
            ok: true,
            status: 200
        });

        const headers = new Headers({
            'content-length': blob.size
        });

        const response = new Response(blob, {
            headers
        });

        await db.cache.put(cachePath, response);

        FS.unlink(backupPath);

        return 1;
    }
    
    return -1;
}

export async function generateDownloadLinkAsync(filename) {

    const cachePath = `/database/cache/${filename}`;
    const db = window.blazorWasmDatabase;
    const resp = await db.cache.match(cachePath);

    if (resp && resp.ok) {
        const res = await resp.blob();
        if (res) { return URL.createObjectURL(res); }
    }

    return '';
}
```

Agora vamos implementar a interface `IDatabaseStorageService`, a implementação será bem simples, aqui tenho um código de exemplo, basicamente ele vai fazer chamadas ao código JavaScript acima, por meio do `JSInterop`

Esta classe fornece um exemplo de como a funcionalidade JavaScript pode ser encapsulada em uma classe dotNET para facilitar o consumo. O módulo JavaScript associado é carregado sob demanda quando necessário. Esta classe pode ser registrada como serviço de DI com escopo e então injetada no Blazor componentes para uso.

```csharp
using Microsoft.JSInterop;
using TodoList.Infra.Data.Services;

namespace TodoList.Infra.Data;

public class BrowserCacheDatabaseStorageService : IDatabaseStorageService, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
    
    public BrowserCacheDatabaseStorageService(IJSRuntime jsRuntime)
    {
        _moduleTask = new Lazy<Task<IJSObjectReference>>(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", $"./_content/TodoList.Infra.Data/browserCacheDatabaseStorageService.js" ).AsTask()
        );
    }
    
    public async Task<int> SyncDatabaseAsync(string filename)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<int>("syncDatabaseWithStorageAsync", filename);
    }

    public async Task<string> GenerateDownloadLinkAsync(string filename)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<string>("generateDownloadLinkAsync", filename);
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}
```
Todo esse código é bastante simples, ele vai sincronizar o banco de dados com o cache do navegador, e de extra vai gerar um link para download do banco de dados!

Também vamos precisar de um serviço para fazer Swap do banco de dados vazio pelo do cache, ou seja, basicamente ele vai trocar o banco de dados ativo pelo backup!

```csharp
namespace TodoList.Infra.Data.Services;

public interface IDatabaseSwapService
{
    void DoSwap(string sourceFilename, string targetFilename);
}
```
Aqui temos um código de exemplo implementando esse interface `IDatabaseSwapService`

```csharp
using Microsoft.Data.Sqlite;
using TodoList.Infra.Data.Services;

namespace TodoList.Infra.Data;

public class DatabaseSwapService : IDatabaseSwapService
{
    public void DoSwap(string sourceFilename, string destFilename)
    {
        using var sourceDatabase = new SqliteConnection($"Data Source={sourceFilename}");
        using var targetDatabase = new SqliteConnection($"Data Source={destFilename}");

        sourceDatabase.Open();
        targetDatabase.Open();

        sourceDatabase.BackupDatabase(targetDatabase);

        targetDatabase.Close();
        sourceDatabase.Close();
    }
}
```

Feito isso, vamos precisar criar um BlazorWasmDbContextFactory (`IBlazorWasmDbContextFactory`) que basicamente ele vai orquestrar os serviços de Storage e Swap. Ele espera até que o banco de dados seja restaurado para retorna contexto do EntityFrameworkCore criado, e faz o backup do banco de dados quanto ocorre salvamentos bem-sucedidos, a baixo tenho um exemplo de código para isso, vale ressaltar ser um exemplo e pode ser que o código não esteja em uma boa forma!

```csharp
using Microsoft.EntityFrameworkCore;

namespace TodoList.Infra.Data.Services;

public interface IBlazorWasmDbContextFactory<TContext>
    where TContext : DbContext
{
    Task<TContext> CreateDbContextAsync();
}
```

A implementação parece ser um pouco complexa, mais é simples, apenas fazemos a gerência dos nomes dos arquivos e do banco e dos serviços que criamos anteriormente!

```csharp
using Microsoft.EntityFrameworkCore;
using TodoList.Infra.Data.Services;

namespace TodoList.Infra.Data;

public class BlazorWasmDbContextFactory<TContext> : IBlazorWasmDbContextFactory<TContext>
    where TContext : DbContext
{
    private static readonly IDictionary<Type, string> FileNames = new Dictionary<Type, string>();
    
    private readonly IDbContextFactory<TContext> _dbContextFactory;
    private readonly IDatabaseStorageService _dbStorageService;
    private readonly IDatabaseSwapService _dbSwapService;
    private Task<int>? _startupTask;
    private int _lastStatus = -2;
    private bool _init;

    public BlazorWasmDbContextFactory(
        IDbContextFactory<TContext> dbContextFactory, 
        IDatabaseStorageService dbStorageService, 
        IDatabaseSwapService dbSwapService)
    {
        _dbContextFactory = dbContextFactory;
        _dbStorageService = dbStorageService;
        _dbSwapService = dbSwapService;
        _startupTask = RestoreAsync();
    }
    
    private static string Filename => FileNames[typeof(TContext)];
    private static string BackupFile => $"{Filename}_backup";

    public async Task<TContext> CreateDbContextAsync()
    {
        // Quanto for executado pela primeira vez deve esperar a restauração acontecer.
        await CheckForStartupTaskAsync();

        // Aqui pegamos o contexto do banco de dados.
        var dbContext = await _dbContextFactory.CreateDbContextAsync();

        if (!_init)
        {
            // quando executado pela primeira vez, devemos criar o banco de dados.
            await dbContext.Database.EnsureCreatedAsync();
            _init = true;
        }

        // Aqui vamos monitorar sempre que o saved changes for chamado sincronizar e fechar a conexão com o banco de dados.
        dbContext.SavedChanges += (_, e) => DbContextSavedChanges(dbContext, e);

        return dbContext;
    }
    
    public static string? GetFilenameForType() =>
        FileNames.ContainsKey(typeof(TContext)) ? FileNames[typeof(TContext)] : null;

    private void DoSwap(string source, string target) =>
        _dbSwapService.DoSwap(source, target);

    private string GetFilename()
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        var filename = "fileNotFound.db";
        var type = dbContext.GetType();
        if (FileNames.ContainsKey(type))
        {
            return FileNames[type];
        }

        var connectionString = dbContext.Database.GetConnectionString();

        var file = connectionString
            ?.Split(';')
            .Select(s => s.Split('='))
            .Select(split => new
            {
                key = split[0].ToLowerInvariant(),
                value = split[1],
            })
            .Where(kv => kv.key.Contains("data source") ||
                         kv.key.Contains("datasource") ||
                         kv.key.Contains("filename")
            )
            .Select(kv => kv.value)
            .FirstOrDefault();
        
        if (file is not null)
        {
            filename = file;
        }

        FileNames.Add(type, filename);
        return filename;
    }

    private async Task CheckForStartupTaskAsync()
    {
        if (_startupTask is not null)
        {
            _lastStatus = await _startupTask;
            _startupTask.Dispose();
            _startupTask = null;
        }
    }

    private async void DbContextSavedChanges(TContext ctx, SavedChangesEventArgs e)
    {
        await ctx.Database.CloseConnectionAsync();
        await CheckForStartupTaskAsync();
        if (e.EntitiesSavedCount <= 0) return;
        
        // exclusivo para evitar conflitos. É excluído após o cache.
        var backupName = $"{BackupFile}-{Guid.NewGuid().ToString().Split('-')[0]}";
        DoSwap(Filename, backupName);
        _lastStatus = await _dbStorageService.SyncDatabaseAsync(backupName);
    }

    private async Task<int> RestoreAsync()
    {
        var filename = $"{GetFilename()}_backup";
        _lastStatus = await _dbStorageService.SyncDatabaseAsync(filename);
        if (_lastStatus is 0)
        {
            DoSwap(filename, FileNames[typeof(TContext)]);
        }

        return _lastStatus;
    }
}
```
Agora vamos realizar a implementação da interface ITodoRepository, note que aqui vamos ver algo um pouco diferente de implementações de repositories comuns que usam o EntityFrameworkCore no background.

```csharp
using Microsoft.EntityFrameworkCore;
using TodoList.Domain.Entities;
using TodoList.Domain.Repositories;
using TodoList.Infra.Data.Services;

namespace TodoList.Infra.Data.Repositories;

public class TodoRepository : ITodoRepository
{
    private readonly IBlazorWasmDbContextFactory<TodoListDbContext> _contextFactory;

    public TodoRepository(IBlazorWasmDbContextFactory<TodoListDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }
    
    public async ValueTask<IEnumerable<Todo>> GetAllAsync()
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.Todos.ToListAsync();
    }

    public async ValueTask<Todo?> GetByIdAsync(Guid id)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.Todos.FirstOrDefaultAsync(todo => todo.Id == id);
    }

    public async ValueTask RegisterAsync(Todo todo)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        dbContext.Todos.Add(todo);
        await dbContext.SaveChangesAsync();
    }

    public async ValueTask UpdateAsync(Todo todo)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        dbContext.Todos.Update(todo);
        await dbContext.SaveChangesAsync();
    }

    public async ValueTask RemoveAsync(Todo todo)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        dbContext.Todos.Remove(todo);
        await dbContext.SaveChangesAsync();
    }
}
```

Ok, para facilitar e ser mais eficiente vamos criar um extensions method para registrar esses serviços no contêiner de injeção de dependência de serviços (DI).

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TodoList.Infra.Data.Services;

namespace TodoList.Infra.Data.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlazorWasmDatabaseContextFactory<TContext>( 
        this IServiceCollection serviceCollection, 
        Action<DbContextOptionsBuilder>? optionsAction = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton) where TContext : DbContext
        => AddBlazorWasmDatabaseContextFactory<TContext>(
            serviceCollection,
            optionsAction == null ? null : (_, oa) => optionsAction(oa),
            lifetime);

    public static IServiceCollection AddBlazorWasmDatabaseContextFactory<TContext>(
        this IServiceCollection serviceCollection,
        Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TContext : DbContext
    {
        serviceCollection.TryAdd(
            new ServiceDescriptor(
                typeof(IDatabaseStorageService),
                typeof(BrowserCacheDatabaseStorageService),
                ServiceLifetime.Singleton));

        serviceCollection.TryAdd(
            new ServiceDescriptor(
                typeof(IDatabaseSwapService),
                typeof(DatabaseSwapService),
                ServiceLifetime.Singleton));

        serviceCollection.TryAdd(
            new ServiceDescriptor(
                typeof(IBlazorWasmDbContextFactory<TContext>),
                typeof(BlazorWasmDbContextFactory<TContext>),
                ServiceLifetime.Singleton));

        serviceCollection.AddDbContextFactory<TContext>(
            optionsAction ?? ((_, _) => { }), lifetime);

        return serviceCollection;
    }
}
```

### 4. Implementação da Apresentação

Vamos precisar de algumas referencias ao projeto de acesso a dados e o projeto de domínio.

- `dotnet add reference ..\TodoList.Infra.Data\TodoList.Infra.Data.csproj`
- `dotnet add reference ..\TodoList.Domain\TodoList.Domain.csproj`

Quanto a implementação do Visual não vou me deter a isso aqui neste artigo, implemente conforme seu gosto e necessidade.

Na classe Program vamos precisar de alguns ajustes simples que sera basicamente injetar os serviços que iremos utilizar.

```csharp
builder.Services.AddBlazorWasmDatabaseContextFactory<TodoListDbContext>(options =>
    options.UseSqlite("Data Source=todolist.sqlite3"));
```

A classe Program ficara basicamente assim apos o ajuste

```csharp

builder.Services.AddBlazorWasmDatabaseContextFactory<TodoListDbContext>(options =>
    options.UseSqlite("Data Source=todolist.sqlite3"));

```

Vamos criar uma página para listar nossos todos! Ela ficara assim!

![todo-items](https://user-images.githubusercontent.com/36675712/177382267-a285f94d-2ac2-435e-9df8-888e8b1eeb35.png)

Basicamente só precisamos injetar no componente que quisermos o repositório e usar o banco de dados e tudo estará funcionando bem.

```csharp
@inject ITodoRepository TodoRepository
```

Com isso basicamente já temos tudo implementado, basta seguir com usa imaginação e construir aplicações, é valido reforçar que o banco de dados no navegador pode ser facilmente acessado, não tem muita segurança quanto a isso, siga com cautela com o que vai ser armazenado nele.

Uma demostração dessa implementação pode ser encontrada no meu [GitHub](https://github.com/MensoDev/SqliteWasmDemo).
