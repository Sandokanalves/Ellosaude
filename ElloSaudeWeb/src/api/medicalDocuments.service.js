import api from './axios';

export const medicalDocumentsService = {
  async getByPatient(patientId) {
    const { data } = await api.get(`/medicaldocuments/patient/${patientId}`);
    return data;
  },

  async getById(id) {
    const { data } = await api.get(`/medicaldocuments/${id}`);
    return data;
  },

  async create(documentData) {
    const { data } = await api.post('/medicaldocuments', documentData);
    return data;
  }
};
