import { useEffect, useState } from "react";

const API = "http://localhost:5000/api/instrumentos";

export default function Instrumentos() {
  const [instrumentos, setInstrumentos] = useState([]);
  const [editando, setEditando] = useState(null);
  const [form, setForm] = useState({ nome: "", codigo: "", tipo: "Pinca", material: "", tamanho: "", pesoReferencia: 0 });

  function carregar() {
    fetch(API).then((r) => r.json()).then(setInstrumentos);
  }

  useEffect(() => {
    carregar();
  }, []);

  function abrirNovo() {
    setEditando(null);
    setForm({ nome: "", codigo: "", tipo: "Pinca", material: "", tamanho: "", pesoReferencia: 0 });
  }

  function abrirEdicao(item) {
    setEditando(item.id);
    setForm({
      nome: item.nome,
      codigo: item.codigo,
      tipo: item.tipo,
      material: item.material,
      tamanho: item.tamanho,
      pesoReferencia: item.pesoReferencia,
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
    if (!confirm("Excluir este instrumento?")) return;
    await fetch(`${API}/${id}`, { method: "DELETE" });
    carregar();
  }

  return (
    <div>
      <h1 className="text-2xl font-bold text-slate-900">Instrumentos</h1>
      <p className="text-slate-500 mt-1">Cadastro de instrumentos cirúrgicos.</p>

      <form onSubmit={salvar} className="mt-6 bg-white border border-slate-200 rounded-xl p-5 grid grid-cols-2 gap-3 max-w-2xl">
        <input
          placeholder="Nome"
          value={form.nome}
          onChange={(e) => setForm({ ...form, nome: e.target.value })}
          className="border border-slate-300 rounded-lg px-3 py-2"
          required
        />
        <input
          placeholder="Código"
          value={form.codigo}
          onChange={(e) => setForm({ ...form, codigo: e.target.value })}
          className="border border-slate-300 rounded-lg px-3 py-2"
          required
        />
        <select
          value={form.tipo}
          onChange={(e) => setForm({ ...form, tipo: e.target.value })}
          className="border border-slate-300 rounded-lg px-3 py-2"
        >
          <option value="Cortante">Cortante</option>
          <option value="Pinca">Pinça</option>
          <option value="Tesoura">Tesoura</option>
          <option value="Afastador">Afastador</option>
          <option value="Clamp">Clamp</option>
          <option value="Canula">Cânula</option>
          <option value="Agulha">Agulha</option>
          <option value="Outros">Outros</option>
        </select>
        <input
          placeholder="Material"
          value={form.material}
          onChange={(e) => setForm({ ...form, material: e.target.value })}
          className="border border-slate-300 rounded-lg px-3 py-2"
        />
        <input
          placeholder="Tamanho"
          value={form.tamanho}
          onChange={(e) => setForm({ ...form, tamanho: e.target.value })}
          className="border border-slate-300 rounded-lg px-3 py-2"
        />
        <input
          type="number"
          step="0.01"
          placeholder="Peso de referência (g)"
          value={form.pesoReferencia}
          onChange={(e) => setForm({ ...form, pesoReferencia: parseFloat(e.target.value) })}
          className="border border-slate-300 rounded-lg px-3 py-2"
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
        {instrumentos.map((i) => (
          <div key={i.id} className="bg-white border border-slate-200 rounded-lg p-3 flex items-center justify-between">
            <div>
              <p className="font-medium">{i.nome}</p>
              <p className="text-xs text-slate-400">{i.codigo} · {i.tipo} · {i.pesoReferencia}g</p>
            </div>
            <div className="flex gap-2">
              <button onClick={() => abrirEdicao(i)} className="text-blue-600 text-sm hover:underline">
                Editar
              </button>
              <button onClick={() => excluir(i.id)} className="text-red-600 text-sm hover:underline">
                Excluir
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}