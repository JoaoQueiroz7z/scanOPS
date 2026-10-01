import { BrowserRouter, Routes, Route } from "react-router-dom";
import Layout from "./componentes/Layout";
import Login from "./Paginas/login";
import VisaoGeral from "./Paginas/VisaoGeral";
import Paciente from "./Paginas/Paciente";
import NovoProcedimento from "./Paginas/NovoProcedimento";
import Instrumentos from "./Paginas/Instrumentos";
import ContagemInicial from "./Paginas/ContagemInicial";
import ProcedimentoAndamento from "./Paginas/ProcedimentoAndamento";
import ContagemFinal from "./Paginas/ContagemFinal";
import RelatorioFinal from "./Paginas/RelatorioFinal";

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Login />} />
        <Route element={<Layout />}>
          <Route path="/visao-geral" element={<VisaoGeral />} />
          <Route path="/paciente" element={<Paciente />} />
          <Route path="/novo-procedimento" element={<NovoProcedimento />} />
          <Route path="/instrumentos" element={<Instrumentos />} />
          <Route path="/contagem-inicial" element={<ContagemInicial />} />
          <Route path="/procedimento-andamento" element={<ProcedimentoAndamento />} />
          <Route path="/contagem-final" element={<ContagemFinal />} />
          <Route path="/relatorio-final" element={<RelatorioFinal />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}