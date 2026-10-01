import { BrowserRouter, Routes, Route } from "react-router-dom";
import Login from "./Paginas/login";
import VisaoGeral from "./Paginas/VisaoGeral";

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Login />} />
        <Route path="/visao-geral" element={<VisaoGeral />} />
      </Routes>
    </BrowserRouter>
  );
}