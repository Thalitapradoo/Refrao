/* =====================================================================
   REFRÃO! — Atualização v2 do banco RefrãoDB
   - Musicas: InicioRefrao (segundo em que o refrão começa) e LinkLetra
   - ComentariosRefrao: "por que esse refrão gruda" (1 por usuário/música)
   - Sugestoes: usuários sugerem músicas, o administrador aprova ou recusa
   ---------------------------------------------------------------------
   Pode ser executado várias vezes sem erro (cada mudança verifica se já
   existe). A API roda este script sozinha a cada inicialização.
   Para quem criou o banco antes da v2: execute no SSMS depois do
   RefrãoDatabase.sql.
   ===================================================================== */

USE [RefrãoDB];
GO

/* ---------- Musicas: InicioRefrao e LinkLetra ---------- */
IF COL_LENGTH('Musicas', 'InicioRefrao') IS NULL
    ALTER TABLE Musicas ADD InicioRefrao INT NULL;
GO
IF COL_LENGTH('Musicas', 'LinkLetra') IS NULL
    ALTER TABLE Musicas ADD LinkLetra VARCHAR(500) NULL;
GO
IF OBJECT_ID('CK_Musicas_InicioRefrao', 'C') IS NULL
    ALTER TABLE Musicas ADD CONSTRAINT CK_Musicas_InicioRefrao
        CHECK (InicioRefrao IS NULL OR (InicioRefrao >= 0 AND InicioRefrao < Duracao));
GO
IF OBJECT_ID('CK_Musicas_LinkLetra', 'C') IS NULL
    ALTER TABLE Musicas ADD CONSTRAINT CK_Musicas_LinkLetra
        CHECK (LinkLetra IS NULL OR LinkLetra LIKE 'http://%' OR LinkLetra LIKE 'https://%');
GO

/* ---------- ComentariosRefrao ----------
   Cada usuário escreve no máximo um comentário por música.
   Se a música for excluída, os comentários dela vão junto (CASCADE). */
IF OBJECT_ID('ComentariosRefrao', 'U') IS NULL
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

/* ---------- Sugestoes ----------
   Status: Pendente -> Aprovada (vira uma música do catálogo) ou Recusada.
   Quem cadastra a música de fato é sempre o administrador. */
IF OBJECT_ID('Sugestoes', 'U') IS NULL
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
