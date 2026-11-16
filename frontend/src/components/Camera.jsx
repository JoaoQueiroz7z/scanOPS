import { useEffect, useRef, useState } from 'react'

// A câmera real é acessada pelo back-end. Se VITE_CAMERA_STREAM existir, mostramos esse stream;
// senão usamos a webcam do navegador apenas para teste visual.
const STREAM = import.meta.env.VITE_CAMERA_STREAM

export default function Camera() {
  const video = useRef(null)
  const [erro, setErro] = useState('')
  useEffect(() => {
    if (STREAM) return
    let stream
    navigator.mediaDevices?.getUserMedia({ video: { facingMode: 'environment' } })
      .then(s => { stream = s; if (video.current) video.current.srcObject = s })
      .catch(() => setErro('Câmera indisponível. Permita o acesso no navegador ou defina VITE_CAMERA_STREAM.'))
    return () => stream?.getTracks().forEach(t => t.stop())
  }, [])
  return (
    <div className="camera">
      {STREAM ? <img src={STREAM} alt="Imagem da câmera" /> : <video ref={video} autoPlay playsInline muted />}
      {erro && <p className="camera-erro">{erro}</p>}
    </div>
  )
}
