# Refrão! 🎵

**Refrão!** é uma plataforma de catálogo de músicas pop, com um toque a mais: cada música mostra **onde o refrão começa**, link para a letra e os comentários da comunidade sobre **por que aquele refrão gruda**. Os usuários também podem **sugerir músicas**, que o administrador aprova ou recusa. O administrador gerencia músicas, artistas, álbuns, gêneros e usuários, e o usuário comum navega pelo catálogo, pesquisa, filtra e vê os detalhes de cada faixa.

**Objetivo:** centralizar o gerenciamento e a consulta das músicas da Refrão! (Justin Bieber, Shawn Mendes, Dua Lipa, Taylor Swift e outros) em uma plataforma web moderna e responsiva, com API REST, banco SQL Server e controle de acesso por perfil.

---

## Tecnologias

| Camada | Tecnologia |
|---|---|
| Back-end | C#, ASP.NET Core Web API, Entity Framework Core (SQL Server) |
| Segurança | JWT (Bearer), senhas com hash **BCrypt**, autorização por perfil (`Administrador` / `Usuario`) |
| Banco | SQL Server (LocalDB por padrão), script `RefrãoDatabase.sql` |
| Front-end | HTML, CSS e JavaScript puro, servidos pela própria API (`wwwroot`) |
| Documentação/testes | Swagger (OpenAPI) + coleção do Insomnia |

**Não precisa instalar nenhuma versão nova do .NET.** O projeto usa automaticamente a versão que já estiver instalada (**.NET 8, 9 ou 10**). Para conferir, rode `dotnet --list-sdks`.

---

## Estrutura das pastas

```
Refrao/
├── Refrao.sln
├── Database/RefrãoDatabase.sql      ← criação do banco + dados iniciais (já com a v2)
├── Database/RefrãoAtualizacao_v2.sql← atualização para quem criou o banco antes da v2
├── Insomnia/Refrao_Insomnia.json    ← 114 requisições de teste
├── .vscode/                         ← F5 no VS Code
└── Refrao.WebAPI/
    ├── Configuracoes/   JwtSettings, Perfis, filtro do Swagger, erros do banco
    ├── Context/         RefraoDbContext (EF Core)
    ├── Models/          Usuario, Artista, Genero, Album, Musica
    ├── DTO/             entradas (com validações) e respostas da API
    ├── Interfaces/      contratos dos serviços
    ├── Services/        regras e acesso a dados, TokenService (JWT), SenhaService (BCrypt), InicializadorBanco
    ├── Middleware/      ErroGlobalMiddleware (erros de banco → 503, inesperados → 500)
    ├── Controllers/     Auth, Musicas, Artistas, Albuns, Generos, Usuarios, Comentarios, Sugestoes, Dashboard
    ├── wwwroot/         FRONT-END (páginas, CSS, JS, imagens)
    ├── Program.cs
    └── appsettings.json
```

---

## Como executar (terminal do VS Code)

1. Abra a pasta **`Refrao`** no VS Code (*File → Open Folder*). Escolha a pasta que contém `Refrao.WebAPI`, e não uma pasta de dentro dela.
2. Abra o terminal (*Terminal → New Terminal*) e rode:
   ```
   cd Refrao.WebAPI
   dotnet run
   ```
3. Espere aparecer `Now listening on: http://localhost:5190`. Na primeira vez, a API cria e popula o banco **RefrãoDB** sozinha.
4. No navegador:
   - **Site:** http://localhost:5190
   - **Swagger:** http://localhost:5190/swagger

Deixe o terminal aberto enquanto usa o site. Para parar a API, use **Ctrl + C**.

> Se o Diário Roxo! estiver rodando ao mesmo tempo, não tem problema: ele usa a porta 5180 e o Refrão!, a 5190.

---

## Banco de dados

### Opção A: automática (padrão)
O `dotnet run` executa `Database/RefrãoDatabase.sql` se o banco ainda não existir (`"Banco": { "CriarAutomaticamente": true }`).

Também executa, toda vez, `Database/RefrãoAtualizacao_v2.sql`, que só adiciona o que ainda faltar. Por isso, **quem já tinha o banco da primeira versão recebe as colunas e tabelas novas sozinho, sem perder dados.** No terminal aparece `Banco RefrãoDB pronto (versão 2...)`.

### Opção B: pelo SSMS
1. Abra o **SQL Server Management Studio**. Conecte em **`(localdb)\MSSQLLocalDB`** com *Windows Authentication* e marque *Trust server certificate*.
2. *File → Open → File...* → `Database\RefrãoDatabase.sql` → **Execute (F5)**.
3. No final aparece a lista das 25 músicas.
4. Se o seu banco foi criado **antes** da v2, execute também `Database\RefrãoAtualizacao_v2.sql`. Ele pode ser executado mais de uma vez sem dar erro.

Para recriar do zero, pare a API e rode:
```sql
USE master;
ALTER DATABASE [RefrãoDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE [RefrãoDB];
```

### Conexão com a aplicação
A conexão fica em `Refrao.WebAPI/appsettings.json`:
```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=RefrãoDB;Trusted_Connection=True;TrustServerCertificate=True;"
```
Para usar outro SQL Server, troque pela linha comentada (com usuário e senha).

### Modelagem

```
Artistas (Id PK, Nome UNIQUE, Biografia, Nacionalidade, ImagemUrl)
   │ 1
   ├────────────< N  Albuns (Id PK, Nome, ArtistaId FK, DataLancamento, CapaUrl, Descricao)
   │                    │ 1         UNIQUE (ArtistaId, Nome)
   │ 1                  │
   └────────────< N  Musicas (Id PK, Titulo, ArtistaId FK, AlbumId FK (opcional), GeneroId FK,
                       N >──┘     Duracao [s], DataLancamento, CapaUrl, Descricao)
                                  UNIQUE (ArtistaId, Titulo)
Generos (Id PK, Nome UNIQUE, Descricao) 1 ───< N Musicas

Musicas (+ InicioRefrao [s] CHECK < Duracao, LinkLetra CHECK http/https)
ComentariosRefrao (Id PK, MusicaId FK ON DELETE CASCADE, UsuarioId FK, Texto 3–300, DataCriacao)
                  UNIQUE (MusicaId, UsuarioId)  → um comentário por usuário em cada música
Sugestoes (Id PK, UsuarioId FK, Titulo, ArtistaNome, AlbumNome, Observacao,
           Status CHECK ('Pendente','Aprovada','Recusada'), RespostaAdmin,
           MusicaId FK ON DELETE SET NULL, DataCriacao, DataResposta)

Usuarios (Id PK, Nome, Email UNIQUE, SenhaHash, Perfil CHECK ('Administrador','Usuario'),
          DataCadastro, Ativo)  + TRIGGER TR_Usuarios_SoftDelete (DELETE vira Ativo = 0)
```

Restrições importantes:
- Música **sem título** ou **sem artista** não é aceita (`NOT NULL` + `CHECK`).
- **Duração** entre 1 e 3600 segundos.
- **FK composta** `(AlbumId, ArtistaId) → Albuns(Id, ArtistaId)`: o álbum da música precisa ser do **mesmo artista**. `AlbumId` pode ser nulo (single avulso).
- **E-mail único** e com formato válido; perfil só `Administrador` ou `Usuario`.
- Não é possível excluir artista com álbuns ou músicas, álbum com músicas, nem gênero em uso (a API retorna 409, e as FKs também bloqueiam no banco).

### Dados iniciais
- **4 artistas:** Justin Bieber, Shawn Mendes, Dua Lipa e Taylor Swift.
- **8 álbuns:** *Purpose* (13/11/2015), *Justice* (19/03/2021), *Illuminate* (23/09/2016), *Shawn Mendes* (25/05/2018), *Dua Lipa* (02/06/2017), *Future Nostalgia* (27/03/2020), *1989* (27/10/2014) e *Midnights* (21/10/2022).
- **25 músicas** e **5 gêneros** (Pop, Dance Pop, Synth-pop, Pop Rock, R&B).

**O minuto do refrão e o link da letra começam vazios de propósito:** não existe fonte confiável para a minutagem, e links inventados quebram. O administrador preenche esses campos no painel, ao editar a música. Enquanto o link da letra estiver vazio, o site mostra um botão **Procurar a letra**, que faz a busca no Google. **A letra em si nunca é guardada no sistema**, porque letras de música são protegidas por direitos autorais.

Datas, álbuns e durações foram conferidos nas páginas da Wikipédia de cada álbum e música. A data da música é a do single quando ele saiu antes do álbum; nos outros casos, é a do álbum. O gênero de cada faixa é uma classificação editorial e pode ser mudado no painel.

As capas e as imagens dos artistas são **ilustrações próprias** (`wwwroot/img`). Capas oficiais e fotos de pessoas reais são protegidas, então não foram usadas. O administrador pode trocar por URLs.

---

## Login e perfis

| Perfil | Acesso inicial | Pode |
|---|---|---|
| **Administrador** | `admin@refrao.com` / `Refrao@2026` | Tudo: CRUD de músicas, artistas, álbuns, gêneros e usuários; desativar usuários; painel com totais; editar o próprio perfil |
| **Usuário comum** | Crie em **Criar conta** (`cadastro.html`) | Ver catálogo, pesquisar por título ou artista, filtrar por gênero, ver artistas, álbuns e detalhes; ver e editar o próprio perfil (trocar a senha exige a senha atual); **comentar por que um refrão gruda** (editar ou apagar o próprio comentário); **sugerir músicas** e acompanhar o status |

- **Sugestões respeitam a regra do enunciado:** o usuário comum **não cadastra música**. Ele só envia uma sugestão, e quem cadastra de fato é o administrador, ao aprovar (a música entra no catálogo e a sugestão fica *Aprovada*, com link para a música) ou ao recusar com um motivo.
- O administrador pode apagar comentários de qualquer pessoa (moderação), mas não editar o texto dos outros.
- O cadastro público **não tem opção de administrador**: a API sempre grava o perfil `Usuario`.
- As permissões são verificadas **na API** (`[Authorize(Roles = "Administrador")]`), e não só escondidas na tela. Um usuário comum que chamar uma rota administrativa recebe **403**.
- A cada requisição, o token é conferido no banco. Se a conta for desativada ou o perfil mudar, o token antigo deixa de valer.

**Para testar um usuário comum:** crie uma conta em *Criar conta*, entre com ela e confira que o menu *Painel* não aparece. Abrir `admin.html` manda você de volta ao início. No Swagger ou no Insomnia, as rotas de administrador devolvem 403 para esse usuário.

---

## Endpoints

| Método | Rota | Acesso |
|---|---|---|
| POST | `/api/Auth/login` | público |
| POST | `/api/Auth/registrar` | público (sempre perfil Usuario) |
| GET | `/api/Auth/perfil` | logado |
| GET | `/api/Musicas` (`busca`, `titulo`, `artista`, `artistaId`, `albumId`, `generoId`, `pagina`, `tamanhoPagina`) | público |
| GET | `/api/Musicas/{id}` | público |
| POST / PUT / DELETE | `/api/Musicas[/{id}]` | Administrador |
| GET | `/api/Artistas`, `/api/Artistas/{id}` (com álbuns e músicas) | público |
| POST / PUT / DELETE | `/api/Artistas[/{id}]` | Administrador |
| GET | `/api/Albuns` (`artistaId`, `busca`), `/api/Albuns/{id}` | público |
| POST / PUT / DELETE | `/api/Albuns[/{id}]` | Administrador |
| GET | `/api/Generos`, `/api/Generos/{id}` | público |
| POST / PUT / DELETE | `/api/Generos[/{id}]` | Administrador |
| GET | `/api/Usuarios` | Administrador |
| GET | `/api/Usuarios/{id}` | o próprio usuário ou Administrador |
| POST | `/api/Usuarios` | Administrador (escolhe o perfil) |
| PUT | `/api/Usuarios/{id}` | o próprio usuário (nome, e-mail, senha) ou Administrador (também perfil e situação) |
| DELETE | `/api/Usuarios/{id}` | Administrador (**desativa** a conta) |
| GET | `/api/Comentarios?musicaId=` | público |
| POST | `/api/Comentarios` | logado (um por música) |
| PUT | `/api/Comentarios/{id}` | autor do comentário |
| DELETE | `/api/Comentarios/{id}` | autor ou Administrador |
| GET | `/api/Sugestoes` (`status`) | Administrador |
| GET | `/api/Sugestoes/minhas`, `/api/Sugestoes/{id}` | logado (as próprias) / Administrador |
| POST | `/api/Sugestoes` | logado |
| POST | `/api/Sugestoes/{id}/aprovar` (dados da música + resposta) | Administrador: cria a música e aprova, na mesma transação |
| POST | `/api/Sugestoes/{id}/recusar` (motivo) | Administrador |
| DELETE | `/api/Sugestoes/{id}` | autor (só pendente) ou Administrador |
| GET | `/api/Dashboard` (inclui sugestões pendentes) | Administrador |

Nas músicas, `POST` e `PUT` também aceitam `inicioRefrao` (em segundos, menor que a duração) e `linkLetra` (http/https). As respostas trazem `inicioRefrao`, `linkLetra` e `totalComentarios`.

Status usados: **200, 201, 204, 400** (validação), **401** (sem login), **403** (sem permissão), **404**, **409** (duplicado ou vínculo), **503** (banco indisponível).

### Swagger
Abra http://localhost:5190/swagger e faça `POST /api/Auth/login` com o administrador. Copie o `token`, clique em **Authorize** e cole o token. Depois disso, as rotas protegidas podem ser testadas.

### Insomnia
Importe `Insomnia/Refrao_Insomnia.json` (**Import → From File**). São 114 requisições em 11 pastas, e o nome de cada uma traz o status esperado.

Ordem recomendada:
1. Na pasta **1. Autenticação**, rode **Login ADMINISTRADOR**, depois **Cadastro público** e por fim **Login USUÁRIO COMUM**. Os tokens são salvos sozinhos.
2. Depois siga as pastas na ordem (2 → 8).

Os IDs de teste valem para um banco recém-criado (música 26, artista 5, álbum 9, gênero 6, usuário comum 2, comentário 1, sugestões 1, 2 e 3). Se o cadastro devolver outro ID, ajuste em *Manage Environments*.

---

## Testes realizados

| Teste | Situação |
|---|---|
| Telas do site (início, catálogo, música, artistas, artista, álbuns, login, cadastro, perfil, painel, celular), carregadas num navegador automatizado contra um servidor de teste com o mesmo formato JSON da API e os dados do script SQL | ✅ sem erros de JavaScript; busca "dua" → 7 músicas; "dua" + Dance Pop → 4; validação de confirmação de senha funcionando |
| v2: página da música com a linha do tempo do refrão e os comentários; página **Sugerir**; aba **Sugestões** no painel e formulário de aprovação já preenchido com a sugestão | ✅ testadas no mesmo servidor de teste, sem erros de JavaScript |
| Sintaxe de todos os arquivos JavaScript (`node --check`) | ✅ |
| Hash BCrypt do administrador (gerado e conferido com o vetor de teste oficial do bcrypt) | ✅ |
| **Compilação do C# e execução real contra o SQL Server** | ⚠️ não foi possível no ambiente onde o projeto foi gerado, que não tem .NET nem SQL Server. O código segue o mesmo padrão do projeto Diário Roxo!, que já compilou e rodou no seu computador. **Rode `dotnet run` e a coleção do Insomnia para validar.** Se aparecer algum erro, envie o print. |
