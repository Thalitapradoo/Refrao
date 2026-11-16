// =========================================================
// Refrão! — comunicação com a API REST
// =========================================================

// Quando o site é aberto pela própria API (dotnet run), usa o mesmo endereço.
// Se for aberto pelo Live Server (porta 5500) ou direto do arquivo, aponta para a API.
const API_URL = (location.protocol === "file:" || location.port === "5500" || location.port === "3000")
    ? "http://localhost:5190/api"
    : "/api";

const CHAVE_SESSAO = "refrao.sessao";

const Sessao = {
    obter() {
        try {
            const dados = JSON.parse(localStorage.getItem(CHAVE_SESSAO));
            if (!dados || !dados.token) return null;
            if (dados.expiracaoUtc && new Date(dados.expiracaoUtc) <= new Date()) {
                localStorage.removeItem(CHAVE_SESSAO);
                return null;
            }
            return dados;
        } catch {
            return null;
        }
    },
    salvar(dados) {
        localStorage.setItem(CHAVE_SESSAO, JSON.stringify(dados));
    },
    atualizarUsuario(usuario) {
        const dados = this.obter();
        if (dados) {
            dados.usuario = usuario;
            this.salvar(dados);
        }
    },
    sair() {
        localStorage.removeItem(CHAVE_SESSAO);
    },
    get token() { return this.obter()?.token ?? null; },
    get usuario() { return this.obter()?.usuario ?? null; },
    get logado() { return this.obter() !== null; },
    get admin() { return this.obter()?.usuario?.perfil === "Administrador"; }
};

class ErroApi extends Error {
    constructor(mensagem, status, erros) {
        super(mensagem);
        this.status = status;
        this.erros = erros || {};
    }
}

// Lê a resposta de erro da API: texto simples ou ProblemDetails de validação.
async function lerErro(resposta) {
    const tipo = resposta.headers.get("content-type") || "";
    let mensagem = "";
    let erros = {};

    try {
        if (tipo.includes("json")) {
            const corpo = await resposta.json();
            if (typeof corpo === "string") {
                mensagem = corpo;
            } else if (corpo.errors) {
                erros = corpo.errors || {};
                const lista = Object.values(erros).flat();
                mensagem = lista.length ? lista.join(" ") : (corpo.detail || corpo.title || "");
            } else {
                mensagem = corpo.detail || corpo.title || "";
            }
        } else {
            mensagem = (await resposta.text()).replace(/^"|"$/g, "");
        }
    } catch {
        // corpo vazio
    }

    if (!mensagem) {
        const padrao = {
            400: "Dados inválidos.",
            401: "Você precisa entrar na sua conta.",
            403: "Você não tem permissão para esta ação.",
            404: "Registro não encontrado.",
            409: "Conflito com um registro existente.",
            500: "Erro interno no servidor.",
            503: "Banco de dados indisponível no momento."
        };
        mensagem = padrao[resposta.status] || `Erro ${resposta.status}.`;
    }

    return new ErroApi(mensagem, resposta.status, erros);
}

async function api(caminho, { metodo = "GET", corpo } = {}) {
    const cabecalhos = { "Accept": "application/json" };
    if (corpo !== undefined) cabecalhos["Content-Type"] = "application/json";
    const token = Sessao.token;
    if (token) cabecalhos["Authorization"] = `Bearer ${token}`;

    let resposta;
    try {
        resposta = await fetch(`${API_URL}${caminho}`, {
            method: metodo,
            headers: cabecalhos,
            body: corpo !== undefined ? JSON.stringify(corpo) : undefined
        });
    } catch {
        throw new ErroApi("Não foi possível conectar à API. Verifique se ela está rodando.", 0);
    }

    if (resposta.status === 401 && token) {
        // Token expirado, conta desativada ou perfil alterado: encerra a sessão local.
        Sessao.sair();
    }

    if (!resposta.ok) throw await lerErro(resposta);
    if (resposta.status === 204) return null;

    const tipo = resposta.headers.get("content-type") || "";
    return tipo.includes("json") ? resposta.json() : resposta.text();
}

// Busca todas as páginas de músicas (usado no painel e na página inicial).
async function buscarTodasMusicas(filtros = {}) {
    const todas = [];
    let pagina = 1;
    while (true) {
        const params = new URLSearchParams({ ...filtros, pagina, tamanhoPagina: 100 });
        const dados = await api(`/Musicas?${params}`);
        todas.push(...dados.items);
        if (todas.length >= dados.total || dados.items.length === 0) break;
        pagina++;
    }
    return todas;
}

// ---------- utilitários de exibição ----------
function esc(texto) {
    return String(texto ?? "").replace(/[&<>"']/g, c => ({
        "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
    }[c]));
}

function formatarDuracao(segundos) {
    const s = Number(segundos) || 0;
    return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, "0")}`;
}

function duracaoParaSegundos(texto) {
    const t = String(texto).trim();
    const partes = t.split(":");
    if (partes.length === 2 && /^\d+$/.test(partes[0]) && /^\d{1,2}$/.test(partes[1])) {
        const seg = Number(partes[1]);
        return seg < 60 ? Number(partes[0]) * 60 + seg : NaN;
    }
    return /^\d+$/.test(t) ? Number(t) : NaN;
}

function formatarData(iso) {
    if (!iso) return "—";
    const [ano, mes, dia] = String(iso).substring(0, 10).split("-");
    return `${dia}/${mes}/${ano}`;
}

function formatarDataLonga(iso) {
    if (!iso) return "—";
    const [ano, mes, dia] = String(iso).substring(0, 10).split("-").map(Number);
    const meses = ["janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho",
        "agosto", "setembro", "outubro", "novembro", "dezembro"];
    return `${dia} de ${meses[mes - 1]} de ${ano}`;
}

const CAPA_PADRAO = "img/capas/padrao.svg";
const ARTISTA_PADRAO = "img/artistas/padrao.svg";

function capaDaMusica(m) {
    return m.capaUrl || m.albumCapaUrl || CAPA_PADRAO;
}

function parametro(nome) {
    return new URLSearchParams(location.search).get(nome);
}

function toast(mensagem, tipo = "") {
    let el = document.querySelector(".toast");
    if (!el) {
        el = document.createElement("div");
        el.className = "toast";
        el.setAttribute("role", "status");
        document.body.appendChild(el);
    }
    el.textContent = mensagem;
    el.className = `toast visivel ${tipo}`;
    clearTimeout(el._timer);
    el._timer = setTimeout(() => el.classList.remove("visivel"), 3200);
}

const ICONE_PLAY = `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M8 5v14l11-7z"/></svg>`;

function cardMusica(m, indice = 0) {
    return `
        <a class="card-musica" href="musica.html?id=${m.id}" style="animation-delay:${Math.min(indice, 12) * 40}ms">
            <div class="capa">
                <img src="${esc(capaDaMusica(m))}" alt="Capa de ${esc(m.albumNome || m.titulo)}" loading="lazy"
                     onerror="this.onerror=null;this.src='${CAPA_PADRAO}'">
                <span class="play">${ICONE_PLAY}</span>
            </div>
            <div class="corpo">
                <h3>${esc(m.titulo)}</h3>
                <span class="artista">${esc(m.artistaNome)}</span>
                <span class="album">${esc(m.albumNome || "Single")}</span>
                <div class="rodape-card">
                    <span class="selo">${esc(m.generoNome)}</span>
                    <span>${formatarDuracao(m.duracao)}</span>
                </div>
            </div>
        </a>`;
}

function cardArtista(a, indice = 0) {
    return `
        <a class="card-artista painel" href="artista.html?id=${a.id}" style="animation-delay:${Math.min(indice, 12) * 50}ms">
            <img src="${esc(a.imagemUrl || ARTISTA_PADRAO)}" alt="${esc(a.nome)}" loading="lazy"
                 onerror="this.onerror=null;this.src='${ARTISTA_PADRAO}'">
            <h3>${esc(a.nome)}</h3>
            <span class="selo rosa">${esc(a.nacionalidade)}</span>
            <p>${a.totalAlbuns} ${a.totalAlbuns === 1 ? "álbum" : "álbuns"} · ${a.totalMusicas} ${a.totalMusicas === 1 ? "música" : "músicas"}</p>
        </a>`;
}
