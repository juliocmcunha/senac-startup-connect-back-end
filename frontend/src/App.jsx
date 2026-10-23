import { useState } from 'react';

export default function App() {
  const [startups, setStartups] = useState([
    { id: '1', name: 'Tech Inova', description: 'Soluções em IA', stage: 'MVP', industry: 'Tecnologia', websiteUrl: 'https://techinova.com', location: 'Recife' }
  ]);
  const [editingId, setEditingId] = useState(null);
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    stage: '',
    industry: '',
    websiteUrl: '',
    location: ''
  });

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    if (editingId) {
      setStartups(startups.map(item => item.id === editingId ? { ...formData, id: editingId } : item));
    } else {
      setStartups([...startups, { ...formData, id: Date.now().toString() }]);
    }
    resetForm();
  };

  const handleEdit = (item) => {
    setEditingId(item.id);
    setFormData({
      name: item.name || '',
      description: item.description || '',
      stage: item.stage || '',
      industry: item.industry || '',
      websiteUrl: item.websiteUrl || '',
      location: item.location || ''
    });
  };

  const handleDelete = (id) => {
    if (confirm('Deseja realmente excluir esta startup?')) {
      setStartups(startups.filter(item => item.id !== id));
    }
  };

  const resetForm = () => {
    setEditingId(null);
    setFormData({
      name: '',
      description: '',
      stage: '',
      industry: '',
      websiteUrl: '',
      location: ''
    });
  };

  return (
    <div className="container">
      <h1>Gerenciamento de Startups</h1>

      <div className="form-card">
        <h3>{editingId ? 'Editar Startup' : 'Nova Startup'}</h3>
        <form onSubmit={handleSubmit} className="form-group">
          <input name="name" placeholder="Nome da Startup" value={formData.name} onChange={handleChange} required />
          <input name="description" placeholder="Descrição" value={formData.description} onChange={handleChange} />
          <input name="stage" placeholder="Estágio (ex: MVP, Início)" value={formData.stage} onChange={handleChange} />
          <input name="industry" placeholder="Setor/Indústria" value={formData.industry} onChange={handleChange} />
          <input name="websiteUrl" placeholder="URL do Website" value={formData.websiteUrl} onChange={handleChange} />
          <input name="location" placeholder="Localização" value={formData.location} onChange={handleChange} />
          
          <div className="button-group">
            <button type="submit" className="btn-primary">
              {editingId ? 'Atualizar' : 'Cadastrar'}
            </button>
            {editingId && (
              <button type="button" onClick={resetForm} className="btn-secondary">
                Cancelar
              </button>
            )}
          </div>
        </form>
      </div>

      <h2>Lista de Startups</h2>
      {startups.length === 0 ? (
        <p className="empty-msg">Nenhuma startup cadastrada ainda.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Nome</th>
              <th>Setor</th>
              <th>Estágio</th>
              <th style={{ width: '150px' }}>Ações</th>
            </tr>
          </thead>
          <tbody>
            {startups.map((item) => (
              <tr key={item.id}>
                <td>{item.name}</td>
                <td>{item.industry || '-'}</td>
                <td>{item.stage || '-'}</td>
                <td>
                  <div style={{ display: 'flex', gap: '6px' }}>
                    <button className="btn-edit" onClick={() => handleEdit(item)}>Editar</button>
                    <button className="btn-delete" onClick={() => handleDelete(item.id)}>Excluir</button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}