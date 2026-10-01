import { useEffect, useState } from "react";

const API = "http://localhost:5000/api/pacientes";

export default function Paciente() {
  const [pacientes, setPacientes] = useState([]);
  const [editando, setEditando] = useState(null);
  const [form, setForm] = useState({
    nome: "", dataNascimento: "", sexo: "", tipoSanguineo: "", alergias: "", observacoes: "",
  });

  function carregar() {
    fetch(API).then((r) => r.json()).then(setPacientes);
  }

  useEffect(() => {
    carregar();
  }, []);

  function abrirNovo() {
    setEditando(null);
    setForm({ nome: "", dataNascimento: "", sexo: "", tipoSanguineo: "", alergias: "", observacoes: "" });
  }

  function abrirEdicao(p) {
    setEditando(p.id);
    setForm({
      nome: p.nome,
      dataNascimento: p.dataNascimento,
      sexo: p.sexo,
      tipoSanguineo: p.tipoSanguineo,
      alergias: p.alergias,
      observacoes: p.observacoes,
    });
  }

  async function salvar(e) {
    e.preventDefault();

    if (editando) {
      await fetch(`${API}/${editando}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(form),
      });
    } else {
      await fetch(API, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(form),
      });
    }

    abrirNovo();
    carregar();
  }

  async function excluir(id) {
    if (!confirm("Excluir este paciente?")) return;
    await fetch(`${API}/${id}`, { method: "DELETE" });
    carregar();
  }

  return (
    <div>
      <h1 className="text-2xl font-bold text-slate-900">Paciente</h1>
      <p className="text-slate-500 mt-1">Cadastro de pacientes.</p>

      <form onSubmit={salvar} className="mt-6 bg-white border border-slate-200 rounded-xl p-5 grid grid-cols-2 gap-3 max-w-2xl">
        <input
          placeholder="Nome"
          value={form.nome}
          onChange={(e) => setForm({ ...form, nome: e.target.value })}
          className="border border-slate-300 rounded-lg px-3 py-2 col-span-2"
          required
        />
        <input
          type="date"
          value={form.dataNascimento}
          onChange={(e) => setForm({ ...form, dataNascimento: e.target.value })}
          className="border border-slate-300 rounded-lg px-3 py-2"
        />
        <select
          value={form.sexo}
          onChange={(e) => setForm({ ...form, sexo: e.target.value })}
          className="border border-slate-300 rounded-lg px-3 py-2"
        >
          <option value="">Sexo</option>
          <option value="Feminino">Feminino</option>
          <option value="Masculino">Masculino</option>
          <option value="Outro">Outro</option>
        </select>
        <select
          value={form.tipoSanguineo}
          onChange={(e) => setForm({ ...form, tipoSanguineo: e.target.value })}
          className="border border-slate-300 rounded-lg px-3 py-2"
        >
          <option value="">Tipo sanguíneo</option>
          {["O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-"].map((t) => (
            <option key={t} value={t}>{t}</option>
          ))}
        </select>
        <input
          placeholder="Alergias"
          value={form.alergias}
          onChange={(e) => setForm({ ...form, alergias: e.target.value })}
          className="border border-slate-300 rounded-lg px-3 py-2"
        />
        <textarea
          placeholder="Observações"
          value={form.observacoes}
          onChange={(e) => setForm({ ...form, observacoes: e.target.value })}
          className="border border-slate-300 rounded-lg px-3 py-2 col-span-2"
        />

        <div className="col-span-2 flex gap-2">
          <button type="submit" className="bg-blue-700 hover:bg-blue-800 text-white px-4 py-2 rounded-lg">
            {editando ? "Salvar alterações" : "Cadastrar"}
          </button>
          {editando && (
            <button type="button" onClick={abrirNovo} className="border border-slate-300 px-4 py-2 rounded-lg">
              Cancelar
            </button>
          )}
        </div>
      </form>

      <div className="mt-6 grid gap-2 max-w-2xl">
        {pacientes.map((p) => (
          <div key={p.id} className="bg-white border border-slate-200 rounded-lg p-3 flex items-center justify-between">
            <div>
              <p className="font-medium">{p.nome}</p>
              <p className="text-xs text-slate-400">{p.sexo} · {p.tipoSanguineo} · {p.dataNascimento}</p>
            </div>
            <div className="flex gap-2">
              <button onClick={() => abrirEdicao(p)} className="text-blue-600 text-sm hover:underline">
                Editar
              </button>
              <button onClick={() => excluir(p.id)} className="text-red-600 text-sm hover:underline">
                Excluir
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}