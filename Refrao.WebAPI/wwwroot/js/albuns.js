// Lista de álbuns com capa, artista, data e músicas.
function blocoAlbum(a) {
    return `
        <article class="album-bloco painel" id="album-${a.id}">
            <img src="${esc(a.capaUrl || CAPA_PADRAO)}" alt="Capa de ${esc(a.nome)}" loading="lazy"
                 onerror="this.onerror=null;this.src='${CAPA_PADRAO}'">
            <div>
                <h2 style="margin-bottom:.1rem">${esc(a.nome)}</h2>
                <a href="artista.html?id=${a.artistaId}" style="color:var(--rosa);font-weight:600">${esc(a.artistaNome)}</a>
                <div class="meta" style="margin-top:.6rem">
                    <span class="selo">${formatarDataLonga(a.dataLancamento)}</span>
                    <span class="selo cinza">${a.totalMusicas} ${a.totalMusicas === 1 ? "música" : "músicas"} no catálogo</span>
                </div>
                <p>${esc(a.descricao || "")}</p>
                ${a.musicas.length ? `<ol class="faixas">${a.musicas.map(m => `
                    <li><a href="musica.html?id=${m.id}">
                        <span class="num">♪</span><span>${esc(m.titulo)}</span><span class="dur">${formatarDuracao(m.duracao)}</span>
                    </a></li>`).join("")}</ol>` : `<p><em>Nenhuma música cadastrada neste álbum.</em></p>`}
            </div>
        </article>`;
}

document.addEventListener("DOMContentLoaded", async () => {
    const lista = document.getElementById("listaAlbuns");
    if (!lista) return;
    try {
        const albuns = await api("/Albuns");
        lista.innerHTML = albuns.length ? albuns.map(blocoAlbum).join("") : `<div class="vazio">Nenhum álbum cadastrado.</div>`;
        if (location.hash) document.querySelector(location.hash)?.scrollIntoView();
    } catch (e) {
        lista.innerHTML = "";
        document.getElementById("erroAlbuns").textContent = e.message;
    }
});
