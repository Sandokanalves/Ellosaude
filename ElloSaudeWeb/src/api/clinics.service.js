import api from './axios';

export const clinicsService = {
  async getSettings() {
    const { data } = await api.get('/clinics/settings');
    return data;
  },

  async updateSettings(settings) {
    await api.put('/clinics/settings', settings);
  },

  async getProcedures(specialty = null, activeOnly = false) {
    const params = {};
    if (specialty) params.specialty = specialty;
    if (activeOnly) params.activeOnly = activeOnly;
    const { data } = await api.get('/clinics/procedures', { params });
    return data;
  },

  async createProcedure(procedure) {
    const { data } = await api.post('/clinics/procedures', procedure);
    return data;
  },

  async updateProcedure(id, procedure) {
    await api.put(`/clinics/procedures/${id}`, procedure);
  },

  async toggleProcedureStatus(id) {
    await api.patch(`/clinics/procedures/${id}/toggle-status`);
  }
};
