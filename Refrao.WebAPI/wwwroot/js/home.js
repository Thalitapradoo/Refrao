// Página inicial: destaques musicais e artistas, carregados da API.
document.addEventListener("DOMContentLoaded", async () => {
    if (Sessao.logado) document.querySelectorAll("[data-visitante]").forEach(el => el.remove());

    try {
        const [pagina, artistas, albuns] = await Promise.all([
            api("/Musicas?tamanhoPagina=100"), api("/Artistas"), api("/Albuns")
        ]);

        document.getElementById("numeros").innerHTML = `
            <div><strong class="texto-gradiente">${pagina.total}</strong><span>músicas</span></div>
            <div><strong class="texto-gradiente">${artistas.length}</strong><span>artistas</span></div>
            <div><strong class="texto-gradiente">${albuns.length}</strong><span>álbuns</span></div>`;

        document.getElementById("capasHero").innerHTML = albuns
            .filter(a => a.capaUrl).sort(() => Math.random() - .5).slice(0, 4)
            .map(a => `<img src="${esc(a.capaUrl)}" alt="">`).join("");

        const sorteadas = [...pagina.items].sort(() => Math.random() - .5).slice(0, 8);
        document.getElementById("destaques").innerHTML = sorteadas.length
            ? sorteadas.map(cardMusica).join("")
            : `<div class="vazio">Nenhuma música cadastrada ainda.</div>`;

        document.getElementById("artistasDestaque").innerHTML = artistas.length
            ? artistas.slice(0, 4).map(cardArtista).join("")
            : `<div class="vazio">Nenhum artista cadastrado ainda.</div>`;
    } catch (e) {
        document.getElementById("erroHome").textContent = e.message;
        document.getElementById("destaques").innerHTML = "";
        document.getElementById("artistasDestaque").innerHTML = "";
    }
});
