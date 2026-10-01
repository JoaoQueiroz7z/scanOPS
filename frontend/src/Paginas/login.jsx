import { useState } from "react";

export default function Login() {
  const [usuario, setUsuario] = useState("");
  const [senha, setSenha] = useState("");
  const [erro, setErro] = useState("");

  async function handleEntrar(e) {
    e.preventDefault();
    setErro("");

    try {
      const resposta = await fetch("http://localhost:5000/api/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ Email: usuario, Senha: senha }),
      });

      if (!resposta.ok) {
        setErro("Usuário ou senha inválidos.");
        return;
      }

      const dados = await resposta.json();
      console.log("Login ok:", dados);
      // próximo passo: salvar o usuário logado e trocar de tela
    } catch {
      setErro("Não foi possível conectar ao servidor.");
    }
  }

  return (
    <div className="min-h-screen grid grid-cols-1 lg:grid-cols-2">
      {/* Lado esquerdo: imagem e texto */}
      <div className="relative hidden lg:flex flex-col justify-between p-12 text-white bg-slate-900">
        <div
          className="absolute inset-0 bg-cover bg-center opacity-40"
          style={{ backgroundImage: "url('/centro-cirurgico.jpg')" }}
        />
        <div className="relative z-10">
          <div className="flex items-center gap-2 text-xl font-bold">
            Device Tech
          </div>
          <p className="text-sm text-slate-300">Tecnologia que cuida</p>
        </div>

        <div className="relative z-10 max-w-md">
          <h1 className="text-4xl font-bold leading-tight">
            Controle <span className="text-cyan-400">inteligente</span> de
            instrumentos cirúrgicos
          </h1>
          <p className="mt-4 text-slate-300">
            Segurança, rastreabilidade e eficiência para o centro cirúrgico.
          </p>
        </div>

        <p className="relative z-10 text-xs text-slate-400">
          Tecnologia a serviço da vida
        </p>
      </div>

      {/* Lado direito: formulário */}
      <div className="flex items-center justify-center p-8 bg-slate-50">
        <form
          onSubmit={handleEntrar}
          className="w-full max-w-sm bg-white rounded-2xl shadow-lg p-8"
        >
          <h2 className="text-2xl font-bold text-slate-900 text-center">
            Acesso ao Sistema
          </h2>
          <p className="text-sm text-slate-500 text-center mt-1">
            Entre com suas credenciais para iniciar o procedimento.
          </p>

          {erro && (
            <p className="mt-3 text-sm text-red-600 text-center">{erro}</p>
          )}

          <label className="block mt-6 text-sm font-medium text-slate-700">
            Usuário ou e-mail
          </label>
          <input
            type="text"
            value={usuario}
            onChange={(e) => setUsuario(e.target.value)}
            placeholder="Digite seu usuário ou e-mail"
            className="mt-1 w-full rounded-lg border border-slate-300 px-4 py-2 outline-none focus:ring-2 focus:ring-blue-500"
          />

          <label className="block mt-4 text-sm font-medium text-slate-700">
            Senha
          </label>
          <input
            type="password"
            value={senha}
            onChange={(e) => setSenha(e.target.value)}
            placeholder="Digite sua senha"
            className="mt-1 w-full rounded-lg border border-slate-300 px-4 py-2 outline-none focus:ring-2 focus:ring-blue-500"
          />

          <div className="flex items-center justify-between mt-3 text-sm">
            <label className="flex items-center gap-2 text-slate-600">
              <input type="checkbox" />
              Lembrar-me
            </label>
            <a href="#" className="text-blue-600 hover:underline">
              Esqueci minha senha
            </a>
          </div>

          <button
            type="submit"
            className="mt-6 w-full bg-blue-700 hover:bg-blue-800 text-white font-medium py-2.5 rounded-lg transition"
          >
            Entrar
          </button>

          <button
            type="button"
            className="mt-2 w-full border border-blue-700 text-blue-700 hover:bg-blue-50 font-medium py-2.5 rounded-lg transition"
          >
            Acessar modo demonstração
          </button>

          <p className="mt-4 text-xs text-slate-400 text-center">
            Acesso restrito a profissionais autorizados.
          </p>
        </form>
      </div>
    </div>
  );
}