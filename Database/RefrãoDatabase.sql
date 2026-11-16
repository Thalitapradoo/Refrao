/* =====================================================================
   REFRÃO! — Plataforma de Catálogo de Músicas Pop
   Script de criação do banco RefrãoDB
   ---------------------------------------------------------------------
   Executar no SQL Server Management Studio (SSMS): abrir este arquivo,
   conectar em (localdb)\MSSQLLocalDB e apertar F5 (Execute).
   A API também executa este script sozinha na primeira vez, se o banco
   ainda não existir (Banco:CriarAutomaticamente no appsettings.json).
   ===================================================================== */

IF DB_ID(N'RefrãoDB') IS NULL
    CREATE DATABASE [RefrãoDB] COLLATE Latin1_General_CI_AI;
GO

USE [RefrãoDB];
GO

/* ---------------------------------------------------------------------
   USUARIOS
   SenhaHash: hash BCrypt (a senha nunca é guardada em texto puro)
   Perfil: 'Administrador' ou 'Usuario'
   --------------------------------------------------------------------- */
CREATE TABLE Usuarios (
    Id              INT IDENTITY(1,1)   NOT NULL,
    Nome            VARCHAR(100)        NOT NULL,
    Email           VARCHAR(150)        NOT NULL,
    SenhaHash       VARCHAR(255)        NOT NULL,
    Perfil          VARCHAR(20)         NOT NULL CONSTRAINT DF_Usuarios_Perfil DEFAULT 'Usuario',
    DataCadastro    DATETIME2(0)        NOT NULL CONSTRAINT DF_Usuarios_DataCadastro DEFAULT SYSDATETIME(),
    Ativo           BIT                 NOT NULL CONSTRAINT DF_Usuarios_Ativo DEFAULT 1,

    CONSTRAINT PK_Usuarios          PRIMARY KEY (Id),
    CONSTRAINT UQ_Usuarios_Email    UNIQUE (Email),
    CONSTRAINT CK_Usuarios_Perfil   CHECK (Perfil IN ('Administrador', 'Usuario')),
    CONSTRAINT CK_Usuarios_Nome     CHECK (LEN(LTRIM(RTRIM(Nome))) > 0),
    CONSTRAINT CK_Usuarios_Email    CHECK (Email LIKE '%_@_%._%')
);
GO

/* ---------------------------------------------------------------------
   ARTISTAS
   --------------------------------------------------------------------- */
CREATE TABLE Artistas (
    Id              INT IDENTITY(1,1)   NOT NULL,
    Nome            VARCHAR(100)        NOT NULL,
    Biografia       VARCHAR(2000)       NULL,
    Nacionalidade   VARCHAR(60)         NOT NULL,
    ImagemUrl       VARCHAR(500)        NULL,

    CONSTRAINT PK_Artistas          PRIMARY KEY (Id),
    CONSTRAINT UQ_Artistas_Nome     UNIQUE (Nome),
    CONSTRAINT CK_Artistas_Nome     CHECK (LEN(LTRIM(RTRIM(Nome))) > 0)
);
GO

/* ---------------------------------------------------------------------
   GENEROS
   --------------------------------------------------------------------- */
CREATE TABLE Generos (
    Id              INT IDENTITY(1,1)   NOT NULL,
    Nome            VARCHAR(50)         NOT NULL,
    Descricao       VARCHAR(500)        NULL,

    CONSTRAINT PK_Generos           PRIMARY KEY (Id),
    CONSTRAINT UQ_Generos_Nome      UNIQUE (Nome),
    CONSTRAINT CK_Generos_Nome      CHECK (LEN(LTRIM(RTRIM(Nome))) > 0)
);
GO

/* ---------------------------------------------------------------------
   ALBUNS
   Um artista possui vários álbuns.
   UQ_Albuns_Id_Artista existe para a FK composta da tabela Musicas
   (garante que a música e o álbum sejam do MESMO artista).
   --------------------------------------------------------------------- */
CREATE TABLE Albuns (
    Id              INT IDENTITY(1,1)   NOT NULL,
    Nome            VARCHAR(150)        NOT NULL,
    ArtistaId       INT                 NOT NULL,
    DataLancamento  DATE                NOT NULL,
    CapaUrl         VARCHAR(500)        NULL,
    Descricao       VARCHAR(1000)       NULL,

    CONSTRAINT PK_Albuns                PRIMARY KEY (Id),
    CONSTRAINT FK_Albuns_Artistas       FOREIGN KEY (ArtistaId) REFERENCES Artistas(Id),
    CONSTRAINT UQ_Albuns_Artista_Nome   UNIQUE (ArtistaId, Nome),
    CONSTRAINT UQ_Albuns_Id_Artista     UNIQUE (Id, ArtistaId),
    CONSTRAINT CK_Albuns_Nome           CHECK (LEN(LTRIM(RTRIM(Nome))) > 0),
    CONSTRAINT CK_Albuns_Data           CHECK (DataLancamento >= '19000101')
);
GO

/* ---------------------------------------------------------------------
   MUSICAS
   Duracao em segundos.
   AlbumId é opcional (singles avulsos). Quando informado, o álbum tem
   que ser do mesmo artista da música (FK composta FK_Musicas_Albuns).
   --------------------------------------------------------------------- */
CREATE TABLE Musicas (
    Id              INT IDENTITY(1,1)   NOT NULL,
    Titulo          VARCHAR(150)        NOT NULL,
    ArtistaId       INT                 NOT NULL,
    AlbumId         INT                 NULL,
    GeneroId        INT                 NOT NULL,
    Duracao         INT                 NOT NULL,
    DataLancamento  DATE                NOT NULL,
    CapaUrl         VARCHAR(500)        NULL,
    Descricao       VARCHAR(1000)       NULL,
    InicioRefrao    INT                 NULL,   -- segundo em que o refrão começa
    LinkLetra       VARCHAR(500)        NULL,   -- link para a letra em um site licenciado

    CONSTRAINT PK_Musicas                   PRIMARY KEY (Id),
    CONSTRAINT FK_Musicas_Artistas          FOREIGN KEY (ArtistaId) REFERENCES Artistas(Id),
    CONSTRAINT FK_Musicas_Generos           FOREIGN KEY (GeneroId)  REFERENCES Generos(Id),
    CONSTRAINT FK_Musicas_Albuns            FOREIGN KEY (AlbumId, ArtistaId) REFERENCES Albuns(Id, ArtistaId),
    CONSTRAINT UQ_Musicas_Artista_Titulo    UNIQUE (ArtistaId, Titulo),
    CONSTRAINT CK_Musicas_Titulo            CHECK (LEN(LTRIM(RTRIM(Titulo))) > 0),
    CONSTRAINT CK_Musicas_Duracao           CHECK (Duracao BETWEEN 1 AND 3600),
    CONSTRAINT CK_Musicas_Data              CHECK (DataLancamento >= '19000101'),
    CONSTRAINT CK_Musicas_InicioRefrao      CHECK (InicioRefrao IS NULL OR (InicioRefrao >= 0 AND InicioRefrao < Duracao)),
    CONSTRAINT CK_Musicas_LinkLetra         CHECK (LinkLetra IS NULL OR LinkLetra LIKE 'http://%' OR LinkLetra LIKE 'https://%')
);
GO

/* ---------------------------------------------------------------------
   COMENTARIOS_REFRAO — "por que esse refrão gruda"
   Um comentário por usuário em cada música; sai junto com a música.
   --------------------------------------------------------------------- */
CREATE TABLE ComentariosRefrao (
    Id              INT IDENTITY(1,1)   NOT NULL,
    MusicaId        INT                 NOT NULL,
    UsuarioId       INT                 NOT NULL,
    Texto           VARCHAR(300)        NOT NULL,
    DataCriacao     DATETIME2(0)        NOT NULL CONSTRAINT DF_ComentariosRefrao_Data DEFAULT SYSDATETIME(),

    CONSTRAINT PK_ComentariosRefrao             PRIMARY KEY (Id),
    CONSTRAINT FK_ComentariosRefrao_Musicas     FOREIGN KEY (MusicaId)  REFERENCES Musicas(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ComentariosRefrao_Usuarios    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id),
    CONSTRAINT UQ_ComentariosRefrao_Musica_Usuario UNIQUE (MusicaId, UsuarioId),
    CONSTRAINT CK_ComentariosRefrao_Texto       CHECK (LEN(LTRIM(RTRIM(Texto))) >= 3)
);
GO

/* ---------------------------------------------------------------------
   SUGESTOES — usuários sugerem músicas; o administrador aprova
   (a música entra no catálogo) ou recusa.
   --------------------------------------------------------------------- */
CREATE TABLE Sugestoes (
    Id              INT IDENTITY(1,1)   NOT NULL,
    UsuarioId       INT                 NOT NULL,
    Titulo          VARCHAR(150)        NOT NULL,
    ArtistaNome     VARCHAR(100)        NOT NULL,
    AlbumNome       VARCHAR(150)        NULL,
    Observacao      VARCHAR(500)        NULL,
    Status          VARCHAR(20)         NOT NULL CONSTRAINT DF_Sugestoes_Status DEFAULT 'Pendente',
    RespostaAdmin   VARCHAR(300)        NULL,
    MusicaId        INT                 NULL,
    DataCriacao     DATETIME2(0)        NOT NULL CONSTRAINT DF_Sugestoes_Data DEFAULT SYSDATETIME(),
    DataResposta    DATETIME2(0)        NULL,

    CONSTRAINT PK_Sugestoes             PRIMARY KEY (Id),
    CONSTRAINT FK_Sugestoes_Usuarios    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id),
    CONSTRAINT FK_Sugestoes_Musicas     FOREIGN KEY (MusicaId)  REFERENCES Musicas(Id) ON DELETE SET NULL,
    CONSTRAINT CK_Sugestoes_Status      CHECK (Status IN ('Pendente', 'Aprovada', 'Recusada')),
    CONSTRAINT CK_Sugestoes_Titulo      CHECK (LEN(LTRIM(RTRIM(Titulo))) > 0),
    CONSTRAINT CK_Sugestoes_Artista     CHECK (LEN(LTRIM(RTRIM(ArtistaNome))) > 0)
);
GO

/* ---------------------------------------------------------------------
   TRIGGER: excluir usuário = desativar (soft delete)
   --------------------------------------------------------------------- */
CREATE TRIGGER TR_Usuarios_SoftDelete
ON Usuarios
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Usuarios
    SET Ativo = 0
    WHERE Id IN (SELECT Id FROM deleted);
END;
GO

/* =====================================================================
   DADOS INICIAIS
   ===================================================================== */

-- Administrador inicial
-- E-mail: admin@refrao.com  |  Senha: Refrao@2026  (troque após o primeiro acesso)
-- SenhaHash = hash BCrypt dessa senha (compatível com BCrypt.Net-Next).
INSERT INTO Usuarios (Nome, Email, SenhaHash, Perfil)
VALUES ('Administrador Refrão', 'admin@refrao.com',
        '$2a$10$KqBNdOUSPXvAEznMbX2GSOvpZPXvNR6ieJVIxAoInmBmSLLAYG3yW', 'Administrador');
GO

-- Gêneros (classificação editorial do Refrão!)
INSERT INTO Generos (Nome, Descricao) VALUES
('Pop',        'Música popular de melodias marcantes e refrões fáceis de cantar.'),
('Dance Pop',  'Pop feito para a pista: batida dançante, ritmo acelerado e produção eletrônica.'),
('Synth-pop',  'Pop construído sobre sintetizadores, com influência da música eletrônica dos anos 80.'),
('Pop Rock',   'Pop com guitarras, bateria e banda, mais próximo do rock.'),
('R&B',        'Rhythm and blues contemporâneo: vocais suaves, groove e influência do soul.');
GO

-- Artistas
INSERT INTO Artistas (Nome, Biografia, Nacionalidade, ImagemUrl) VALUES
('Justin Bieber',
 'Cantor e compositor canadense, nascido em 1994 em London, Ontário. Ficou conhecido ainda adolescente por vídeos publicados no YouTube.',
 'Canadense', 'img/artistas/justin-bieber.svg'),
('Shawn Mendes',
 'Cantor e compositor canadense, nascido em 1998 em Toronto. Ganhou visibilidade publicando covers na internet antes de lançar seus álbuns.',
 'Canadense', 'img/artistas/shawn-mendes.svg'),
('Dua Lipa',
 'Cantora e compositora britânica, nascida em 1995 em Londres, filha de pais kosovares-albaneses.',
 'Britânica', 'img/artistas/dua-lipa.svg'),
('Taylor Swift',
 'Cantora e compositora norte-americana, nascida em 1989 em West Reading, Pensilvânia. Começou na música country antes de migrar para o pop.',
 'Norte-americana', 'img/artistas/taylor-swift.svg');
GO

/* ---------------------------------------------------------------------
   Álbuns — datas conferidas nas páginas da Wikipédia de cada álbum
   (consulta em 16/11/2026). Capas: ilustrações próprias do projeto em
   wwwroot/img/capas (as capas oficiais são protegidas por direitos).
   --------------------------------------------------------------------- */
DECLARE @Bieber INT = (SELECT Id FROM Artistas WHERE Nome = 'Justin Bieber');
DECLARE @Shawn  INT = (SELECT Id FROM Artistas WHERE Nome = 'Shawn Mendes');
DECLARE @Dua    INT = (SELECT Id FROM Artistas WHERE Nome = 'Dua Lipa');
DECLARE @Taylor INT = (SELECT Id FROM Artistas WHERE Nome = 'Taylor Swift');

INSERT INTO Albuns (Nome, ArtistaId, DataLancamento, CapaUrl, Descricao) VALUES
('Purpose',          @Bieber, '20151113', 'img/capas/purpose.svg',          'Quarto álbum de estúdio de Justin Bieber.'),
('Justice',          @Bieber, '20210319', 'img/capas/justice.svg',          'Sexto álbum de estúdio de Justin Bieber.'),
('Illuminate',       @Shawn,  '20160923', 'img/capas/illuminate.svg',       'Segundo álbum de estúdio de Shawn Mendes.'),
('Shawn Mendes',     @Shawn,  '20180525', 'img/capas/shawn-mendes.svg',     'Terceiro álbum de estúdio, homônimo, de Shawn Mendes.'),
('Dua Lipa',         @Dua,    '20170602', 'img/capas/dua-lipa.svg',         'Álbum de estreia, homônimo, de Dua Lipa.'),
('Future Nostalgia', @Dua,    '20200327', 'img/capas/future-nostalgia.svg', 'Segundo álbum de estúdio de Dua Lipa.'),
('1989',             @Taylor, '20141027', 'img/capas/1989.svg',             'Quinto álbum de estúdio de Taylor Swift.'),
('Midnights',        @Taylor, '20221021', 'img/capas/midnights.svg',        'Décimo álbum de estúdio de Taylor Swift.');
GO

/* ---------------------------------------------------------------------
   Músicas
   Duracao em segundos (ex.: 3:20 = 200), conforme as listas de faixas
   da Wikipédia. DataLancamento = data do single quando ele saiu ANTES
   do álbum; nos demais casos, a data do álbum.
   Gênero: classificação editorial do Refrão! (editável no painel).
   InicioRefrao e LinkLetra ficam vazios de propósito: são preenchidos
   pelo administrador (não inventamos minutagens nem links).
   --------------------------------------------------------------------- */
DECLARE @Bieber INT = (SELECT Id FROM Artistas WHERE Nome = 'Justin Bieber');
DECLARE @Shawn  INT = (SELECT Id FROM Artistas WHERE Nome = 'Shawn Mendes');
DECLARE @Dua    INT = (SELECT Id FROM Artistas WHERE Nome = 'Dua Lipa');
DECLARE @Taylor INT = (SELECT Id FROM Artistas WHERE Nome = 'Taylor Swift');

DECLARE @Purpose   INT = (SELECT Id FROM Albuns WHERE Nome = 'Purpose');
DECLARE @Justice   INT = (SELECT Id FROM Albuns WHERE Nome = 'Justice');
DECLARE @Illum     INT = (SELECT Id FROM Albuns WHERE Nome = 'Illuminate');
DECLARE @ShawnAlb  INT = (SELECT Id FROM Albuns WHERE Nome = 'Shawn Mendes');
DECLARE @DuaAlb    INT = (SELECT Id FROM Albuns WHERE Nome = 'Dua Lipa');
DECLARE @Future    INT = (SELECT Id FROM Albuns WHERE Nome = 'Future Nostalgia');
DECLARE @A1989     INT = (SELECT Id FROM Albuns WHERE Nome = '1989');
DECLARE @Midnights INT = (SELECT Id FROM Albuns WHERE Nome = 'Midnights');

DECLARE @Pop     INT = (SELECT Id FROM Generos WHERE Nome = 'Pop');
DECLARE @Dance   INT = (SELECT Id FROM Generos WHERE Nome = 'Dance Pop');
DECLARE @Synth   INT = (SELECT Id FROM Generos WHERE Nome = 'Synth-pop');
DECLARE @PopRock INT = (SELECT Id FROM Generos WHERE Nome = 'Pop Rock');
DECLARE @RnB     INT = (SELECT Id FROM Generos WHERE Nome = 'R&B');

INSERT INTO Musicas (Titulo, ArtistaId, AlbumId, GeneroId, Duracao, DataLancamento, Descricao) VALUES
-- Justin Bieber — Purpose (2015)
('What Do You Mean?', @Bieber, @Purpose, @Dance, 205, '20151113', 'Faixa 3 de Purpose.'),
('Sorry',             @Bieber, @Purpose, @Dance, 200, '20151113', 'Faixa 4 de Purpose.'),
('Love Yourself',     @Bieber, @Purpose, @Pop,   233, '20151113', 'Faixa 5 de Purpose.'),
-- Justin Bieber — Justice (2021)
('Holy',              @Bieber, @Justice, @Pop,   212, '20200918', 'Participação de Chance the Rapper. Lançada como single em 18/09/2020.'),
('Anyone',            @Bieber, @Justice, @Pop,   190, '20210101', 'Lançada como single em 01/01/2021.'),
('Ghost',             @Bieber, @Justice, @Pop,   153, '20210319', 'Faixa 11 de Justice.'),
('Peaches',           @Bieber, @Justice, @RnB,   198, '20210319', 'Participação de Daniel Caesar e Giveon. Lançada como single junto com o álbum.'),
-- Shawn Mendes — Illuminate (2016)
('Mercy',             @Shawn, @Illum,    @PopRock, 208, '20160923', 'Faixa 2 de Illuminate.'),
('Treat You Better',  @Shawn, @Illum,    @PopRock, 187, '20160603', 'Primeiro single de Illuminate, lançado em 03/06/2016.'),
-- Shawn Mendes — Shawn Mendes (2018)
('In My Blood',       @Shawn, @ShawnAlb, @PopRock, 211, '20180322', 'Single lançado em 22/03/2018.'),
('Lost in Japan',     @Shawn, @ShawnAlb, @Pop,     201, '20180323', 'Single lançado em 23/03/2018.'),
('Youth',             @Shawn, @ShawnAlb, @Pop,     190, '20180503', 'Participação de Khalid. Single lançado em 03/05/2018.'),
-- Dua Lipa — Dua Lipa (2017)
('Be the One',        @Dua, @DuaAlb, @Synth, 202, '20151030', 'Single lançado em 30/10/2015, antes do álbum.'),
('IDGAF',             @Dua, @DuaAlb, @Pop,   217, '20170602', 'Faixa 5 do álbum Dua Lipa.'),
('New Rules',         @Dua, @DuaAlb, @Dance, 209, '20170602', 'Faixa 10 do álbum Dua Lipa.'),
-- Dua Lipa — Future Nostalgia (2020)
('Don''t Start Now',  @Dua, @Future, @Dance, 183, '20191031', 'Primeiro single de Future Nostalgia, lançado em 31/10/2019.'),
('Physical',          @Dua, @Future, @Synth, 193, '20200327', 'Faixa 4 de Future Nostalgia.'),
('Levitating',        @Dua, @Future, @Dance, 203, '20200327', 'Faixa 5 de Future Nostalgia.'),
('Break My Heart',    @Dua, @Future, @Dance, 221, '20200325', 'Single lançado em 25/03/2020.'),
-- Taylor Swift — 1989 (2014)
('Style',             @Taylor, @A1989, @Synth, 231, '20141027', 'Faixa 3 de 1989.'),
('Shake It Off',      @Taylor, @A1989, @Dance, 219, '20140818', 'Primeiro single de 1989, lançado em 18/08/2014.'),
('Blank Space',       @Taylor, @A1989, @Synth, 232, '20141027', 'Single de 1989.'),
-- Taylor Swift — Midnights (2022)
('Lavender Haze',     @Taylor, @Midnights, @Synth, 202, '20221021', 'Faixa 1 de Midnights.'),
('Anti-Hero',         @Taylor, @Midnights, @Synth, 200, '20221021', 'Primeiro single de Midnights, lançado junto com o álbum.'),
('Karma',             @Taylor, @Midnights, @Pop,   204, '20221021', 'Faixa 11 de Midnights.');
GO

/* ---------------------------------------------------------------------
   Conferência
   --------------------------------------------------------------------- */
SELECT ar.Nome AS Artista, al.Nome AS Album, m.Titulo, g.Nome AS Genero,
       CONCAT(m.Duracao / 60, ':', RIGHT('0' + CAST(m.Duracao % 60 AS VARCHAR(2)), 2)) AS Duracao,
       m.DataLancamento
FROM Musicas m
    JOIN Artistas ar ON ar.Id = m.ArtistaId
    LEFT JOIN Albuns al ON al.Id = m.AlbumId
    JOIN Generos g ON g.Id = m.GeneroId
ORDER BY ar.Nome, al.DataLancamento, m.Titulo;
