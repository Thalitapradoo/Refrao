// Detalhes do artista: imagem, nome, biografia, nacionalidade, álbuns e músicas.
document.addEventListener("DOMContentLoaded", async () => {
    const alvo = document.getElementById("detalheArtista");
    const erro = document.getElementById("erroArtista");
    const id = Number(parametro("id"));
    if (!Number.isInteger(id) || id < 1) { alvo.innerHTML = ""; erro.textContent = "Artista não informado."; return; }

    try {
        const a = await api(`/Artistas/${id}`);
        document.title = `${a.nome} · Refrão!`;
        alvo.innerHTML = `
            <section class="perfil-artista painel">
                <img src="${esc(a.imagemUrl || ARTISTA_PADRAO)}" alt="${esc(a.nome)}" onerror="this.onerror=null;this.src='${ARTISTA_PADRAO}'">
                <div>
                    <span class="sobretitulo">artista</span>
                    <h1>${esc(a.nome)}</h1>
                    <span class="selo rosa">${esc(a.nacionalidade)}</span>
                    <p>${esc(a.biografia || "Sem biografia cadastrada.")}</p>
                    <span class="selo cinza">${a.albuns.length} álbuns · ${a.musicas.length} músicas</span>
                </div>
            </section>

            <div class="secao-topo"><h2>Músicas cadastradas</h2></div>
            <div class="grade-musicas" style="margin-bottom:2.5rem">
                ${a.musicas.length ? a.musicas.map(cardMusica).join("") : `<div class="vazio">Nenhuma música deste artista ainda.</div>`}
            </div>

            <div class="secao-topo"><h2>Álbuns</h2></div>
            <div class="lista-albuns">
                ${a.albuns.length ? a.albuns.map(blocoAlbum).join("") : `<div class="vazio">Nenhum álbum deste artista ainda.</div>`}
            </div>`;
    } catch (e) {
        alvo.innerHTML = e.status === 404 ? `<div class="vazio">Esse artista não existe no catálogo.</div>` : "";
        if (e.status !== 404) erro.textContent = e.message;
    }
});
