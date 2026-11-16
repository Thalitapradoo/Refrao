// =========================================================
// Refrão! — cabeçalho, rodapé e proteção de páginas
// =========================================================

const LOGO_SVG = `
<svg viewBox="0 0 40 40" aria-hidden="true">
  <defs><linearGradient id="lg" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#8b5cf6"/><stop offset=".55" stop-color="#d946ef"/><stop offset="1" stop-color="#f472b6"/></linearGradient></defs>
  <circle cx="20" cy="20" r="18" fill="url(#lg)"/>
  <circle cx="20" cy="20" r="12" fill="none" stroke="#0f0a1a" stroke-opacity=".35" stroke-width="1.2"/>
  <path d="M17 12.5v11.2a3.2 3.2 0 1 0 2 2.96V16.2l6-1.6v6.9a3.2 3.2 0 1 0 2 2.96V10.5z" fill="#fff"/>
</svg>`;

function montarLayout() {
    const pagina = location.pathname.split("/").pop() || "index.html";
    const usuario = Sessao.usuario;

    const link = (href, texto) => `<a href="${href}" class="${pagina === href ? "ativo" : ""}">${texto}</a>`;

    const areaConta = usuario
        ? `${link("sugerir.html", "Sugerir")}
           ${Sessao.admin ? link("admin.html", "Painel") : ""}
           ${link("perfil.html", "Meu perfil")}
           <button type="button" class="link" id="botaoSair">Sair</button>`
        : `${link("login.html", "Entrar")}
           <a href="cadastro.html" class="botao pequeno">Criar conta</a>`;

    const topo = document.getElementById("topo");
    if (topo) {
        topo.className = "topo";
        topo.innerHTML = `
            <div class="container">
                <a href="index.html" class="logo">${LOGO_SVG}<span>Refrão<em>!</em></span></a>
                <button class="botao-menu" type="button" aria-label="Abrir menu" aria-expanded="false">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M4 7h16M4 12h16M4 17h10"/></svg>
                </button>
                <nav class="menu" aria-label="Navegação principal">
                    ${link("index.html", "Início")}
                    ${link("catalogo.html", "Catálogo")}
                    ${link("artistas.html", "Artistas")}
                    ${link("albuns.html", "Álbuns")}
                    <span class="separador"></span>
                    ${areaConta}
                </nav>
            </div>`;

        const botaoMenu = topo.querySelector(".botao-menu");
        const menu = topo.querySelector(".menu");
        botaoMenu.addEventListener("click", () => {
            const aberto = menu.classList.toggle("aberto");
            botaoMenu.setAttribute("aria-expanded", String(aberto));
        });

        document.getElementById("botaoSair")?.addEventListener("click", () => {
            Sessao.sair();
            location.href = "index.html";
        });
    }

    const rodape = document.getElementById("rodape");
    if (rodape) {
        rodape.className = "rodape";
        rodape.innerHTML = `
            <div class="container">
                <span><strong style="color:#fff">Refrão!</strong> · Plataforma de Catálogo de Músicas Pop · projeto acadêmico</span>
                <span class="equalizador" aria-hidden="true"><span></span><span></span><span></span><span></span></span>
            </div>`;
    }
}

function exigirLogin() {
    if (!Sessao.logado) {
        location.href = `login.html?voltar=${encodeURIComponent(location.pathname.split("/").pop() + location.search)}`;
        return false;
    }
    return true;
}

// A interface esconde, mas quem bloqueia de verdade é a API (403).
function exigirAdmin() {
    if (!exigirLogin()) return false;
    if (!Sessao.admin) {
        location.href = "index.html";
        return false;
    }
    return true;
}

document.addEventListener("DOMContentLoaded", montarLayout);
