// Login com JWT.
document.addEventListener("DOMContentLoaded", () => {
    if (Sessao.logado) { location.href = Sessao.admin ? "admin.html" : "catalogo.html"; return; }

    const form = document.getElementById("formLogin");
    const mensagem = document.getElementById("mensagem");
    if (parametro("cadastro") === "ok") {
        mensagem.className = "aviso sucesso";
        mensagem.textContent = "Conta criada com sucesso! Agora é só entrar.";
    }

    form.addEventListener("submit", async e => {
        e.preventDefault();
        limparErros(form);
        const email = document.getElementById("email");
        const senha = document.getElementById("senha");
        if (![validarEmail(email), validarObrigatorio(senha, "a senha")].every(Boolean)) return;

        const botao = form.querySelector("button[type=submit]");
        botao.disabled = true;
        mensagem.className = "aviso"; mensagem.textContent = "Entrando...";
        try {
            const dados = await api("/Auth/login", { metodo: "POST", corpo: { email: email.value.trim(), senha: senha.value } });
            Sessao.salvar(dados);
            const voltar = parametro("voltar");
            location.href = voltar && /^[\w-]+\.html/.test(voltar) ? voltar
                : (dados.usuario.perfil === "Administrador" ? "admin.html" : "catalogo.html");
        } catch (erro) {
            mensagem.className = "aviso erro";
            mensagem.textContent = erro.message;
        } finally {
            botao.disabled = false;
        }
    });
});
