// ATENÇÃO: não deu para ver os DTOs de resposta do back (Api/Dtos), então os nomes dos campos abaixo
// são suposições. Se algo aparecer vazio na tela, abra "Ver JSON bruto" na tela de operação e ajuste aqui.
const pick = (o, ...caminhos) => {
  for (const c of caminhos) {
    const v = c.split('.').reduce((a, k) => a?.[k], o)
    if (v !== undefined && v !== null) return v
  }
}

export const normalizarItem = i => ({
  id: pick(i, 'id', 'itemId'),
  nome: pick(i, 'instrumento.nome', 'instrumentoNome', 'nome') ?? 'Instrumento',
  previsto: pick(i, 'quantidadePrevista', 'previsto') ?? 0,
  entrada: pick(i, 'quantidadeEntrada', 'quantidadeEmCampo', 'entradas') ?? 0,
  saida: pick(i, 'quantidadeSaida', 'quantidadeDevolvida', 'saidas') ?? 0,
  pesoMedido: pick(i, 'pesoMedido'),
})

export const normalizarProcedimento = r => ({
  raw: r,
  id: pick(r, 'id'),
  status: String(pick(r, 'status') ?? ''),
  tipo: pick(r, 'tipo') ?? '',
  sala: pick(r, 'sala') ?? '',
  descricao: pick(r, 'descricao') ?? '',
  paciente: pick(r, 'paciente.nome', 'pacienteNome') ?? '—',
  cirurgiao: pick(r, 'cirurgiao.nome', 'cirurgiaoNome') ?? '—',
  itens: (r.itens ?? []).map(normalizarItem),
})

export const statusDe = s =>
  /cancel/i.test(s) ? 'cancelado' : /final|conclu/i.test(s) ? 'finalizado'
  : /andamento|iniciad/i.test(s) ? 'andamento' : /planej/i.test(s) ? 'planejado' : 'outro'
