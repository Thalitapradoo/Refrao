// Cadastro público: sempre cria um usuário comum (não existe opção de administrador).
document.addEventListener("DOMContentLoaded", () => {
    if (Sessao.logado) { location.href = "perfil.html"; return; }

    const form = document.getElementById("formCadastro");
    const mensagem = document.getElementById("mensagem");

    form.addEventListener("submit", async e => {
        e.preventDefault();
        limparErros(form);
        const nome = document.getElementById("nome");
        const email = document.getElementById("email");
        const senha = document.getElementById("senha");
        const confirmacao = document.getElementById("confirmacao");

        let ok = [validarObrigatorio(nome, "o nome"), validarEmail(email), validarSenha(senha)].every(Boolean);
        if (!confirmacao.value) ok = marcarErro(confirmacao, "Confirme a senha.") && false;
        else if (senha.value !== confirmacao.value) ok = marcarErro(confirmacao, "As senhas não conferem.") && false;
        if (!ok) return;

        const botao = form.querySelector("button[type=submit]");
        botao.disabled = true;
        mensagem.className = "aviso"; mensagem.textContent = "Criando sua conta...";
        try {
            await api("/Auth/registrar", {
                metodo: "POST",
                corpo: { nome: nome.value.trim(), email: email.value.trim(), senha: senha.value, confirmacaoSenha: confirmacao.value }
            });
            location.href = "login.html?cadastro=ok";
        } catch (erro) {
            mensagem.className = "aviso erro";
            mensagem.textContent = erro.message;
        } finally {
            botao.disabled = false;
        }
    });
});
